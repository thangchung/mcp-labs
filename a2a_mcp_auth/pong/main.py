"""Pong service FastAPI application."""

import logging
from datetime import datetime

import uvicorn
from fastapi import FastAPI, HTTPException, Request

from a2a.server.apps import A2AFastAPIApplication
from a2a.server.context import ServerCallContext
from a2a.auth.user import User as A2AUser

from shared.auth import auth_middleware
from shared.config import settings
from shared.models import UserInfo
from shared.debug_utils import setup_debug_logging
from .handlers import PongHandler
from .pong_agent import create_pong_agent_card
from .mcp_client import mcp_client

# Setup debug logging
setup_debug_logging()

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
    title="Pong Service",
    description="A2A Pong service with Microsoft Entra ID authentication",
    version="1.0.0",
    debug=settings.debug
)

# Add debug middleware if enabled
if settings.debug_requests:
    from shared.debug_utils import DebugMiddleware
    app.add_middleware(DebugMiddleware)

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


@app.post("/pong/mcp")
async def pong_mcp(request: Request, request_data: dict = None):
    """Send ping to MCP server and return enhanced response (Admin access required)."""
    try:
        # Extract JWT token from Authorization header
        auth_header = request.headers.get("authorization")
        if not auth_header or not auth_header.startswith("Bearer "):
            raise HTTPException(status_code=401, detail="Bearer token required")
        
        jwt_token = auth_header.split(" ")[1]
        
        # Verify the token and check admin role
        try:
            user_info = auth_middleware.auth_handler.verify_jwt_token(jwt_token)
            if not user_info.is_admin:
                raise HTTPException(
                    status_code=403, 
                    detail="Admin role required to access MCP server"
                )
        except HTTPException:
            raise
        except Exception:
            raise HTTPException(status_code=401, detail="Invalid token")
        
        # Extract the ping message
        ping_message = request_data.get("message", "ping") if request_data else "ping"
        
        logger.info(f"Admin user {user_info.email} requesting MCP ping for: {ping_message}")
        
        # Call MCP server
        mcp_response = await mcp_client.ping_mcp(ping_message, jwt_token)
        
        return {
            "status": "success",
            "message": "pong",
            "received": ping_message,
            "mcp_response": mcp_response,
            "timestamp": datetime.now().isoformat(),
            "service": "pong",
            "user": {
                "name": user_info.name,
                "email": user_info.email,
                "is_admin": user_info.is_admin
            }
        }
        
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Error in MCP pong: {str(e)}")
        raise HTTPException(status_code=500, detail=f"MCP pong failed: {str(e)}")


@app.get("/health")
async def health_check():
    """Health check endpoint with MCP server status."""
    try:
        mcp_health = await mcp_client.health_check()
        return {
            "status": "healthy",
            "service": "pong",
            "mcp_server": mcp_health
        }
    except Exception as e:
        return {
            "status": "degraded",
            "service": "pong",
            "mcp_server": {"status": "error", "error": str(e)}
        }


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
        "service": "Pong Service",
        "version": "1.0.0",
        "description": "A2A Pong service with Microsoft Entra ID authentication and MCP integration",
        "architecture": "A2A Server + MCP Client - receives A2A messages and handles MCP communication",
        "endpoints": {
            "agent_card": "/.well-known/agent.json",
            "a2a_rpc": "/",
            "mcp_pong": "/pong/mcp",
            "health": "/health",
            "debug": "/debug"
        },
        "mcp_integration": {
            "server_url": settings.mcp_server_url,
            "description": "Admin-only access to Ping MCP server",
            "required_role": "admin",
            "a2a_support": "MCP enhancement available via A2A for admin users"
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
