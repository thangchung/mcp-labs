"""Ping service FastAPI application."""

import logging
import uuid
from datetime import datetime

import httpx
import uvicorn
from fastapi import Depends, FastAPI, HTTPException, Request
from fastapi.responses import JSONResponse, RedirectResponse
from fastapi.security import HTTPBearer

from a2a.server.apps import A2AFastAPIApplication
from a2a.server.context import ServerCallContext
from a2a.auth.user import User as A2AUser

from shared.auth import auth_handler, auth_middleware
from shared.config import settings
from shared.models import UserInfo
from shared.debug_utils import setup_debug_logging
from .handlers import PingHandler
from .ping_agent import create_ping_agent_card

# Setup debug logging
setup_debug_logging()

# Configure logging
logging.basicConfig(
    level=getattr(logging, settings.log_level.upper()),
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s"
)
logger = logging.getLogger(__name__)

# Security
security = HTTPBearer(auto_error=False)


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
        metadata = {}
        
        # Try to get user from authorization header
        auth_header = request.headers.get("authorization")
        if auth_header and auth_header.startswith("Bearer "):
            token = auth_header.split(" ")[1]
            metadata["authorization"] = auth_header
            try:
                user_info = auth_middleware.auth_handler.verify_jwt_token(token)
                user = A2AUserProxy(user_info)
            except HTTPException:
                pass  # Invalid token, user remains None
        
        return ServerCallContext(
            user=user,
            activated_extensions=set()
        )


# Create FastAPI app
app = FastAPI(
    title="Ping Service",
    description="A2A Ping service with Microsoft Entra ID authentication",
    version="1.0.0",
    debug=settings.debug
)

# Add debug middleware if enabled
if settings.debug_requests:
    from shared.debug_utils import DebugMiddleware
    app.add_middleware(DebugMiddleware)

# Create agent card and handler
agent_card = create_ping_agent_card()
ping_handler = PingHandler()
context_builder = CustomCallContextBuilder()

# Create A2A application
a2a_app = A2AFastAPIApplication(
    agent_card=agent_card,
    http_handler=ping_handler,
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
    
    logger.info("Redirecting to OAuth2 authorization URL for ping service")
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
            "token_type": "Bearer",
            "instructions": "Use this token in the Authorization header as 'Bearer <token>' for authenticated requests"
        })
        
    except HTTPException as e:
        logger.error(f"Authentication failed: {e.detail}")
        raise e
    except Exception as e:
        logger.error(f"Unexpected error during authentication: {str(e)}")
        raise HTTPException(status_code=500, detail="Authentication failed")


@app.post("/ping")
async def manual_ping(
    request: Request,
    user_info: UserInfo = Depends(auth_middleware.require_admin)
):
    """Manual ping endpoint that sends a ping via A2A protocol to pong service (requires admin role)."""
    try:
        logger.info(f"Manual ping request from {user_info.name}")
        
        # Get auth token from request for forwarding
        auth_header = request.headers.get("authorization")
        
        results = {
            "message": "Ping sent to pong service via A2A protocol",
            "user": user_info.name,
            "user_id": user_info.user_id,
            "is_admin": user_info.is_admin,
            "timestamp": datetime.now().isoformat(),
            "pong_service_a2a": None
        }
        
        # Call pong service using A2A protocol only
        try:
            logger.info("Calling pong service via A2A protocol...")
            
            # Create A2A message for pong service - request includes MCP enhancement
            a2a_message_id = str(uuid.uuid4())
            context_id = str(uuid.uuid4())
            
            a2a_request = {
                "jsonrpc": "2.0",
                "id": str(uuid.uuid4()),
                "method": "message/send",
                "params": {
                    "message": {
                        "messageId": a2a_message_id,
                        "contextId": context_id,
                        "role": "user",
                        "parts": [
                            {
                                "kind": "text",
                                "text": f"ping from {user_info.name} via A2A protocol - please include MCP enhancement if admin user"
                            }
                        ]
                    }
                }
            }
            
            headers = {"Content-Type": "application/json"}
            if auth_header:
                headers["Authorization"] = auth_header
            
            async with httpx.AsyncClient() as client:
                pong_response = await client.post(
                    f"{settings.pong_service_url}/pong/mcp",  # A2A endpoint root
                    json=a2a_request,
                    headers=headers,
                    timeout=10.0
                )
                
                if pong_response.status_code == 200:
                    pong_data = pong_response.json()
                    results["pong_service_a2a"] = {
                        "status": "success",
                        "protocol": "A2A",
                        "response": pong_data,
                        "status_code": pong_response.status_code
                    }
                    logger.info("Pong service (A2A) responded successfully")
                else:
                    results["pong_service_a2a"] = {
                        "status": "error",
                        "protocol": "A2A",
                        "error": f"HTTP {pong_response.status_code}",
                        "response": pong_response.text[:200]
                    }
                    logger.warning(f"Pong service (A2A) returned {pong_response.status_code}")
                    
        except Exception as e:
            logger.error(f"Error calling pong service via A2A: {str(e)}")
            results["pong_service_a2a"] = {
                "status": "error",
                "protocol": "A2A",
                "error": str(e)
            }
        
        return results
        
    except HTTPException as e:
        raise e
    except Exception as e:
        logger.error(f"Error in manual ping: {str(e)}")
        raise HTTPException(status_code=500, detail="Failed to process ping")


@app.get("/health")
async def health_check():
    """Health check endpoint."""
    return {"status": "healthy", "service": "ping"}


@app.get("/debug")
async def debug_info():
    """Debug information endpoint (only available in debug mode)."""
    if not settings.debug:
        raise HTTPException(status_code=404, detail="Debug endpoint not available")
    
    from shared.debug_utils import create_debug_endpoint
    debug_func = create_debug_endpoint()
    return debug_func()


@app.get("/")
async def root():
    """Root endpoint with service information."""
    return {
        "service": "Ping Service",
        "version": "1.0.0",
        "description": "A2A Ping service with Microsoft Entra ID authentication",
        "architecture": "Pure A2A Protocol - communicates only via Agent-to-Agent protocol",
        "endpoints": {
            "agent_card": "/.well-known/agent.json",
            "a2a_rpc": "/",
            "login": "/auth/login", 
            "callback": "/auth/callback",
            "ping": "/ping",
            "health": "/health",
            "debug": "/debug"
        },
        "communication": {
            "protocol": "A2A (Agent-to-Agent)",
            "pong_service": settings.pong_service_url,
            "note": "Direct HTTP calls removed - uses A2A protocol exclusively"
        }
    }


def main():
    """Run the Ping service."""
    logger.info("Starting Ping service...")
    logger.info(f"Service URL: {settings.ping_service_url}")
    logger.info(f"Pong service URL: {settings.pong_service_url}")
    logger.info(f"Debug mode: {settings.debug}")
    
    # Extract port from URL
    port = 8000
    if ":" in settings.ping_service_url:
        port_str = settings.ping_service_url.split(":")[-1]
        try:
            port = int(port_str)
        except ValueError:
            pass
    
    uvicorn.run(
        "ping.main:app",
        host="0.0.0.0",
        port=port,
        reload=settings.debug,
        log_level=settings.log_level.lower()
    )


if __name__ == "__main__":
    main()
