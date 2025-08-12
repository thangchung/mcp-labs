"""MCP client for connecting to the Ping MCP server."""

import logging
from datetime import datetime
from typing import Optional

import httpx
from fastapi import HTTPException

from shared.config import settings
from shared.debug_utils import log_mcp_call

logger = logging.getLogger(__name__)


class MCPClient:
    """Client for connecting to the Ping MCP server."""
    
    def __init__(self):
        self.base_url = settings.mcp_server_url.rstrip('/')
        self.timeout = 30.0
    
    async def ping_mcp_service_call(self, message: str, service_name: str = "pong-service", auth_token: Optional[str] = None) -> dict:
        """
        Send ping message to MCP server with JWT token requirement (secure mode).
        This method requires JWT token authentication for all MCP server access.
        
        Args:
            message: The ping message to send
            service_name: Name of the calling service
            auth_token: JWT token for authenticated calls (REQUIRED)
            
        Returns:
            dict: Response from MCP server with service context
            
        Raises:
            HTTPException: If the request fails
        """
        # Enforce JWT token requirement for enhanced security
        if not auth_token:
            logger.warning(f"MCP access denied for {service_name}: JWT token required for all MCP server access")
            return {
                "status": "auth_required",
                "content": "JWT token required for MCP server access. No service-to-service fallback available for enhanced security.",
                "metadata": {
                    "service_call": True,
                    "authenticated": False,
                    "caller": service_name,
                    "security_policy": "jwt_required",
                    "error": "no_jwt_token"
                }
            }
        
        headers = {
            "Content-Type": "application/json",
            "X-Service-Name": service_name,
            "X-Service-Call": "true",
            "Authorization": f"Bearer {auth_token}"
        }
        
        # Log JWT token preview for debugging
        token_preview = auth_token[:20] + "..." + auth_token[-20:] if len(auth_token) > 40 else auth_token
        logger.debug(f"[JWT] Using JWT token in secure MCP call: {token_preview}")
        
        # MCP tool call payload - authenticated service call only
        payload = {
            "method": "tools/call",
            "params": {
                "name": "ping_processor_tool",
                "arguments": {
                    "message": message,
                    "service_context": {
                        "caller": service_name,
                        "type": "authenticated_service_call",
                        "has_jwt": True,
                        "security_mode": "jwt_required",
                        "timestamp": datetime.now().isoformat()
                    }
                }
            }
        }
        
        try:
            logger.info(f"Sending secure authenticated MCP request from {service_name}")
            log_mcp_call("ping_processor_tool", {"message": message, "service": service_name, "authenticated": True, "secure_mode": True})
            
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                # Use only the authenticated MCP endpoint - no fallback
                endpoint = f"{self.base_url}/mcp"
                
                response = await client.post(
                    endpoint,
                    headers=headers,
                    json=payload
                )
                
                # Handle authentication errors more gracefully for service calls
                if response.status_code == 401:
                    if auth_token:
                        logger.error("MCP server rejected JWT token - insufficient privileges")
                        return {
                            "status": "auth_failed",
                            "content": "MCP server rejected JWT token - admin role required",
                            "metadata": {
                                "service_call": True,
                                "authenticated": True,
                                "error": "insufficient_privileges"
                            }
                        }
                    else:
                        logger.warning("MCP server requires authentication for this request")
                        return {
                            "status": "auth_required",
                            "content": "MCP server requires authentication for this request",
                            "metadata": {
                                "service_call": True,
                                "authenticated": False,
                                "error": "authentication_required"
                            }
                        }
                elif response.status_code == 403:
                    logger.error("MCP server access forbidden")
                    return {
                        "status": "access_forbidden",
                        "content": "Access forbidden: Admin privileges required",
                        "metadata": {
                            "service_call": True,
                            "authenticated": bool(auth_token),
                            "error": "access_forbidden"
                        }
                    }
                elif response.status_code != 200:
                    logger.warning(f"MCP service call failed: {response.status_code} - {response.text}")
                    return {
                        "status": "service_call_failed",
                        "content": f"MCP service unavailable (HTTP {response.status_code})",
                        "metadata": {
                            "service_call": True,
                            "authenticated": bool(auth_token),
                            "error": response.text[:100] if response.text else "Unknown error"
                        }
                    }
                
                result = response.json()
                logger.info(f"MCP {'authenticated' if auth_token else 'service'} request successful")
                
                return {
                    "status": "success",
                    "content": result.get("content", result.get("result", "MCP processing completed")),
                    "metadata": {
                        "service_call": True,
                        "authenticated": bool(auth_token),
                        "caller": service_name,
                        "mcp_response": result
                    }
                }
                
        except Exception as e:
            logger.warning(f"MCP service call error: {str(e)}")
            return {
                "status": "service_call_error", 
                "content": f"MCP service temporarily unavailable: {str(e)[:100]}",
                "metadata": {
                    "service_call": True,
                    "authenticated": bool(auth_token),
                    "error": str(e)
                }
            }
    
    async def health_check(self) -> dict:
        """Check if MCP server is healthy."""
        try:
            async with httpx.AsyncClient(timeout=5.0) as client:
                response = await client.get(f"{self.base_url}/health")
                
                if response.status_code == 200:
                    return response.json()
                else:
                    return {"status": "unhealthy", "code": response.status_code}
                    
        except Exception as e:
            logger.error(f"MCP health check failed: {str(e)}")
            return {"status": "error", "error": str(e)}


# Global MCP client instance
mcp_client = MCPClient()
