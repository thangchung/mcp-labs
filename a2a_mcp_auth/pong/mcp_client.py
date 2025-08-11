"""MCP client for connecting to the Ping MCP server."""

import logging

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
    
    async def ping_mcp(self, message: str, auth_token: str) -> dict:
        """
        Send ping message to MCP server.
        
        Args:
            message: The ping message to send
            auth_token: Bearer token for authentication
            
        Returns:
            dict: Response from MCP server
            
        Raises:
            HTTPException: If the request fails or token is invalid
        """
        if not auth_token:
            raise HTTPException(status_code=401, detail="Authorization token required")
        
        headers = {
            "Authorization": f"Bearer {auth_token}",
            "Content-Type": "application/json"
        }
        
        # MCP tool call payload
        payload = {
            "method": "tools/call",
            "params": {
                "name": "ping_processor_tool",
                "arguments": {
                    "message": message
                }
            }
        }
        
        try:
            logger.info(f"Sending MCP request to {self.base_url}/mcp")
            
            # Log debug information with JWT token preview for debugging
            token_preview = auth_token[:20] + "..." + auth_token[-20:] if len(auth_token) > 40 else auth_token
            logger.debug(f"[JWT] Using JWT token: {token_preview}")
            logger.debug(f"[HTTP] Request headers: {headers}")
            
            # Log debug information
            log_mcp_call("ping_processor_tool", {"message": message})
            
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                response = await client.post(
                    f"{self.base_url}/mcp",
                    headers=headers,
                    json=payload
                )
                
                if response.status_code == 401:
                    logger.error("MCP server rejected token - insufficient privileges")
                    raise HTTPException(
                        status_code=401, 
                        detail="Access denied: Admin role required for MCP server"
                    )
                elif response.status_code == 403:
                    logger.error("MCP server access forbidden")
                    raise HTTPException(
                        status_code=403,
                        detail="Access forbidden: Admin privileges required"
                    )
                elif response.status_code != 200:
                    logger.error(f"MCP server error: {response.status_code} - {response.text}")
                    raise HTTPException(
                        status_code=response.status_code,
                        detail=f"MCP server error: {response.text}"
                    )
                
                result = response.json()
                logger.info("MCP request successful")
                
                # Log successful result
                log_mcp_call("ping_processor_tool", {"message": message}, result=result)
                
                return result
                
        except httpx.TimeoutException:
            logger.error("MCP server request timed out")
            raise HTTPException(
                status_code=504,
                detail="MCP server request timed out"
            )
        except httpx.ConnectError:
            logger.error("Failed to connect to MCP server")
            raise HTTPException(
                status_code=503,
                detail="MCP server unavailable"
            )
        except HTTPException:
            raise
        except Exception as e:
            logger.error(f"Unexpected error calling MCP server: {str(e)}")
            
            # Log error
            log_mcp_call("ping_processor_tool", {"message": message}, error=str(e))
            
            raise HTTPException(
                status_code=500,
                detail="Failed to call MCP server"
            )
    
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
