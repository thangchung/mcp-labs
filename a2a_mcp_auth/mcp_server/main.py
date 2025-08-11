"""MCP Server with Microsoft Entra ID authentication."""

import logging
import sys
from datetime import datetime

import uvicorn
from fastapi import FastAPI, HTTPException, Depends
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials

from shared.config import settings
from shared.auth import auth_middleware
from shared.debug_utils import setup_debug_logging
from .tools import AVAILABLE_TOOLS, ping_processor

# Setup debug logging
setup_debug_logging()

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.StreamHandler(sys.stdout)
    ],
    encoding='utf-8',  # Explicitly set UTF-8 encoding
    force=True  # Force reconfiguration
)

logger = logging.getLogger(__name__)

# Security
security = HTTPBearer()


async def verify_admin_token(credentials: HTTPAuthorizationCredentials = Depends(security)):
    """Verify token and check admin role using shared auth middleware."""
    try:
        # Use the shared authentication middleware to verify the token
        user_info = auth_middleware.auth_handler.verify_jwt_token(credentials.credentials)
        
        if not user_info.is_admin:
            raise HTTPException(status_code=403, detail="Admin role required")
        
        # Return dict format for backward compatibility with existing code
        return {
            "email": user_info.email,
            "name": user_info.name,
            "is_admin": user_info.is_admin,
            "user_id": user_info.user_id,
            "roles": user_info.roles
        }
        
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Token verification error: {str(e)}")
        raise HTTPException(status_code=401, detail="Token verification failed")


def create_mcp_server() -> FastAPI:
    """Create and configure the MCP server with Entra ID authentication."""
    
    logger.info("Creating MCP server with Entra ID authentication...")
    
    # Create FastAPI application
    app = FastAPI(
        title="Ping MCP Server",
        version="1.0.0",
        description="MCP server for ping/pong processing with admin-only access"
    )
    
    # Add debug middleware if enabled
    if settings.debug_requests:
        from shared.debug_utils import DebugMiddleware
        app.add_middleware(DebugMiddleware)
    
    @app.get("/health")
    async def health_check():
        """Health check endpoint."""
        return {
            "status": "healthy",
            "service": "mcp_server",
            "timestamp": datetime.now().isoformat(),
            "version": "1.0.0"
        }
    
    @app.get("/")
    async def root():
        """Root endpoint with service information."""
        return {
            "service": "Ping MCP Server",
            "version": "1.0.0",
            "description": "MCP server for ping/pong processing with admin-only access",
            "endpoints": {
                "health": "/health",
                "mcp": "/mcp",
                "ping_tool": "/tools/ping_processor"
            },
            "authentication": {
                "type": "Bearer Token",
                "issuer": f"https://login.microsoftonline.com/{settings.azure_tenant_id}/v2.0",
                "required_scopes": ["admin"]
            },
            "tools": list(AVAILABLE_TOOLS.keys())
        }
    
    @app.get("/debug")
    async def debug_info():
        """Debug information endpoint (only available in debug mode)."""
        if not settings.debug:
            raise HTTPException(status_code=404, detail="Debug endpoint not available")
        
        from shared.debug_utils import create_debug_endpoint
        debug_func = create_debug_endpoint()
        return debug_func()
    
    @app.post("/mcp")
    async def mcp_endpoint(
        request_data: dict,
        user_info=Depends(verify_admin_token)
    ):
        """MCP endpoint for tool calls and simple ping requests (Admin access required)."""
        try:
            # Check if this is a simple ping request (from ping service)
            if "message" in request_data and "sender" in request_data:
                # Simple ping request
                message = request_data.get("message", "ping")
                sender = request_data.get("sender", "unknown")
                user = request_data.get("user", "unknown")
                
                logger.info(f"Simple ping request from {sender} (user: {user}): {message}")
                
                # Process with ping_processor tool
                result = await ping_processor({"message": message})
                
                return {
                    "status": "success",
                    "message": f"MCP processed ping from {sender}",
                    "original_message": message,
                    "processed_result": result,
                    "user": user_info.get("email"),
                    "timestamp": datetime.now().isoformat()
                }
            
            # Standard MCP protocol request
            method = request_data.get("method")
            params = request_data.get("params", {})
            
            if not method:
                raise HTTPException(status_code=400, detail="Missing 'method' field for MCP request")
            
            if method == "tools/call":
                tool_name = params.get("name")
                tool_args = params.get("arguments", {})
                
                if tool_name == "ping_processor_tool":
                    message = tool_args.get("message", "ping")
                    result = await ping_processor({"message": message})
                    
                    logger.info(f"Admin {user_info.get('email')} used ping_processor tool")
                    
                    return {
                        "result": result,
                        "user": user_info.get("email"),
                        "timestamp": datetime.now().isoformat()
                    }
                else:
                    raise HTTPException(status_code=404, detail=f"Tool '{tool_name}' not found")
            else:
                raise HTTPException(status_code=400, detail=f"Method '{method}' not supported")
                
        except HTTPException:
            raise
        except Exception as e:
            logger.error(f"MCP endpoint error: {str(e)}")
            raise HTTPException(status_code=500, detail="MCP processing failed")
    
    @app.post("/tools/ping_processor")
    async def ping_processor_tool(
        request_data: dict,
        user_info=Depends(verify_admin_token)
    ):
        """Direct ping processor tool endpoint (Admin access required)."""
        try:
            message = request_data.get("message", "ping")
            result = await ping_processor({"message": message})
            
            logger.info(f"Admin {user_info.get('email')} used direct ping_processor")
            
            return {
                "result": result,
                "user": user_info.get("email"),
                "timestamp": datetime.now().isoformat()
            }
            
        except Exception as e:
            logger.error(f"Ping processor error: {str(e)}")
            raise HTTPException(status_code=500, detail="Ping processing failed")
    
    logger.info("MCP server configured successfully")
    return app


def setup_app():
    """Setup and configure the MCP application."""
    logger.info("Starting Ping MCP Server...")
    logger.info(f"Started at: {datetime.now().isoformat()}")
    logger.info("Configuration:")
    logger.info(f"   - Server URL: {settings.mcp_server_url}")
    logger.info(f"   - Port: {settings.mcp_server_port}")
    logger.info(f"   - Azure Tenant: {settings.azure_tenant_id}")
    logger.info(f"   - Admin Group: {settings.admin_group_id}")
    
    # Create MCP server
    app = create_mcp_server()
    
    logger.info("MCP server ready to accept connections")
    logger.info("Authentication: Bearer tokens with admin scope required")
    logger.info("Available tools: ping_processor")
    
    return app


def main():
    """Main entry point - returns the configured app."""
    return setup_app()


# For direct execution
app = setup_app()


if __name__ == "__main__":
    # Run the server
    uvicorn.run(
        app,
        host="0.0.0.0",
        port=settings.mcp_server_port,
        log_level="info"
    )
