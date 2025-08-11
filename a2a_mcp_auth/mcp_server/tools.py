"""MCP tools for ping/pong processing."""

import logging
from typing import Any, Dict

from mcp import Tool

logger = logging.getLogger(__name__)


async def ping_processor(arguments: Dict[str, Any]) -> str:
    """
    Process ping strings and return MCP response.
    
    This tool processes incoming ping messages and transforms them
    into MCP-formatted responses. Only admin users can execute this tool.
    
    Args:
        arguments: Tool arguments containing the message to process
        
    Returns:
        Processed message with MCP prefix
    """
    message = arguments.get("message", "ping")
    
    # Log successful execution
    logger.info(f"Processing ping message: {message}")
    
    # Transform the message
    result = f"ping MCP {message}"
    
    logger.info(f"Returning MCP response: {result}")
    return result


# Tool definition for MCP server registration
PING_PROCESSOR_TOOL = Tool(
    name="ping_processor",
    description="Process ping strings and return MCP response (Admin access required)",
    inputSchema={
        "type": "object",
        "properties": {
            "message": {
                "type": "string",
                "description": "Ping message to process"
            }
        },
        "required": ["message"]
    }
)


# Available tools registry
AVAILABLE_TOOLS = {
    "ping_processor": {
        "tool": PING_PROCESSOR_TOOL,
        "handler": ping_processor
    }
}
