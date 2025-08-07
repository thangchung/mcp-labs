"""Pong service FastAPI application."""

import logging
import uuid
from datetime import datetime

import uvicorn
from fastapi import FastAPI, HTTPException, Request
from fastapi.responses import JSONResponse, RedirectResponse

from a2a.server.apps import A2AFastAPIApplication
from a2a.server.context import ServerCallContext
from a2a.auth.user import User as A2AUser

from shared.auth import auth_handler, auth_middleware
from shared.config import settings
from shared.models import UserInfo
from .handlers import PongHandler
from .pong_agent import create_pong_agent_card

# Configure logging
logging.basicConfig(
    level=getattr(logging, settings.log_level.upper()),
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s"
)
logger = logging.getLogger(__name__)


class A2AUserProxy(A2AUser):
    """Proxy to adapt UserInfo to A2A User interface."""
    
    def __init__(self, user_info: UserInfo):
        self.user_info = user_info
    
    @property
    def is_authenticated(self) -> bool:
        return True
    
    @property
    def user_name(self) -> str:
        return self.user_info.name


class CustomCallContextBuilder:
    """Custom call context builder that includes authentication."""
    
    def build(self, request: Request) -> ServerCallContext:
        """Build server call context with authentication."""
        user = None
        
        # Try to get user from authorization header
        auth_header = request.headers.get("authorization")
        if auth_header and auth_header.startswith("Bearer "):
            token = auth_header.split(" ")[1]
            try:
                user_info = auth_middleware.auth_handler.verify_jwt_token(token)
                user = A2AUserProxy(user_info)
            except HTTPException:
                pass  # Invalid token, user remains None
        
        return ServerCallContext(
            user=user,
            activated_extensions=set(),
            metadata={}
        )


# Create FastAPI app
app = FastAPI(
    title="Pong Service",
    description="A2A Pong service with Microsoft Entra ID authentication",
    version="1.0.0",
    debug=settings.debug
)

# Create agent card and handler
agent_card = create_pong_agent_card()
pong_handler = PongHandler()
context_builder = CustomCallContextBuilder()

# Create A2A application
a2a_app = A2AFastAPIApplication(
    agent_card=agent_card,
    http_handler=pong_handler,
    context_builder=context_builder
)

# Add A2A routes to the main app
a2a_app.add_routes_to_app(app)


@app.get("/auth/login")
async def login(request: Request):
    """Initiate OAuth2 login flow."""
    # Generate state parameter for security
    state = str(uuid.uuid4())
    
    # Store state in session (in production, use proper session management)
    auth_url = auth_handler.get_auth_url(state=state)
    
    logger.info("Redirecting to OAuth2 authorization URL for pong service")
    return RedirectResponse(url=auth_url)


@app.get("/auth/callback")
async def auth_callback(request: Request, code: str = None, state: str = None, error: str = None):
    """Handle OAuth2 callback."""
    if error:
        logger.error(f"OAuth2 error: {error}")
        raise HTTPException(status_code=400, detail=f"Authentication error: {error}")
    
    if not code:
        raise HTTPException(status_code=400, detail="Authorization code not provided")
    
    try:
        # Exchange code for token
        auth_token = await auth_handler.exchange_code_for_token(code)
        
        # Create JWT token
        jwt_token = auth_handler.create_jwt_token(auth_token.user_info)
        
        logger.info(f"User {auth_token.user_info.email} authenticated successfully")
        
        return JSONResponse({
            "message": "Authentication successful",
            "user": {
                "name": auth_token.user_info.name,
                "email": auth_token.user_info.email,
                "is_admin": auth_token.user_info.is_admin,
                "roles": auth_token.user_info.roles
            },
            "token": jwt_token,
            "token_type": "Bearer"
        })
        
    except HTTPException as e:
        logger.error(f"Authentication failed: {e.detail}")
        raise e
    except Exception as e:
        logger.error(f"Unexpected error during authentication: {str(e)}")
        raise HTTPException(status_code=500, detail="Authentication failed")


@app.post("/pong/test")
async def test_pong(request_data: dict = None):
    """Simple test pong endpoint (no authentication required for development)."""
    try:
        logger.info("Test pong request received")
        
        # Extract the ping message
        ping_message = request_data.get("message", "ping") if request_data else "ping"
        
        return {
            "status": "success",
            "message": "pong",
            "received": ping_message,
            "timestamp": datetime.now().isoformat(),
            "service": "pong"
        }
        
    except Exception as e:
        logger.error(f"Error in test pong: {str(e)}")
        return {
            "status": "error",
            "message": "Test pong failed",
            "error": str(e)
        }


@app.get("/health")
async def health_check():
    """Health check endpoint."""
    return {"status": "healthy", "service": "pong"}


@app.get("/")
async def root():
    """Root endpoint with service information."""
    return {
        "service": "Pong Service",
        "version": "1.0.0",
        "description": "A2A Pong service with Microsoft Entra ID authentication",
        "endpoints": {
            "agent_card": "/.well-known/agent.json",
            "a2a_rpc": "/",
            "login": "/auth/login",
            "callback": "/auth/callback",
            "health": "/health"
        }
    }


def main():
    """Run the Pong service."""
    logger.info("Starting Pong service...")
    logger.info(f"Service URL: {settings.pong_service_url}")
    logger.info(f"Debug mode: {settings.debug}")
    
    # Extract port from URL
    port = 8001
    if ":" in settings.pong_service_url:
        port_str = settings.pong_service_url.split(":")[-1]
        try:
            port = int(port_str)
        except ValueError:
            pass
    
    uvicorn.run(
        "pong.main:app",
        host="0.0.0.0",
        port=port,
        reload=settings.debug,
        log_level=settings.log_level.lower()
    )


if __name__ == "__main__":
    main()
