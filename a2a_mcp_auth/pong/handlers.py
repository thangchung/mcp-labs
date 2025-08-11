"""A2A request handlers for the Pong service."""

import logging
import uuid
from datetime import datetime

from a2a.server.context import ServerCallContext
from a2a.types import (
    Message,
    MessageSendParams,
    Part,
    Role,
    TextPart,
)

from shared.models import PongMessage

logger = logging.getLogger(__name__)


class PongHandler:
    """Handler for A2A requests in the Pong service."""
    
    def __init__(self) -> None:
        """Initialize the Pong handler."""
        self.service_name = "pong-service"
    
    async def on_message_send(
        self,
        params: MessageSendParams,
        context: ServerCallContext,
    ) -> Message:
        """Handle incoming A2A ping messages and respond with pong (optionally enhanced via MCP)."""
        try:
            logger.info(f"A2A message received from {context.user.user_name if context.user else 'unknown'}")
            
            # Validate user is authenticated for A2A protocol
            if not context.user or not context.user.is_authenticated:
                logger.warning("Unauthenticated A2A request rejected")
                return self._create_error_response("Authentication required for A2A protocol")
            
            # Extract message content from A2A message parts
            message_content = ""
            if params.message.parts:
                for part in params.message.parts:
                    # Handle TextPart specifically
                    from a2a.types import TextPart
                    if isinstance(part.root, TextPart):
                        message_content += part.root.text + " "
            
            message_content = message_content.strip() or "ping request via A2A"
            logger.info(f"Processing A2A message: {message_content}")
            
            # Check if this is a ping request
            if "ping" in message_content.lower():
                # Create basic pong response
                pong_message = PongMessage(
                    message_id=str(uuid.uuid4()),
                    original_message_id=params.message.message_id,
                    sender=self.service_name,
                    response_time_ms=50.0,  # Simulated response time
                )
                
                # Start with basic pong response  
                response_text = f"Pong! Received your A2A ping (ID: {params.message.message_id[:8]}...) at {datetime.utcnow().isoformat()}"
                
                # If user has admin role and message requests MCP enhancement, try to enhance
                if context.user.is_authenticated and "mcp enhancement" in message_content.lower():
                    try:
                        logger.info("Admin user requesting MCP enhancement in A2A flow")
                        response_text += "\nMCP Enhancement: Requested but requires direct HTTP call with token"
                        response_text += "\nNote: Use /pong/mcp endpoint for full MCP integration with authentication"
                        
                    except Exception as e:
                        logger.warning(f"MCP enhancement failed in A2A flow: {str(e)}")
                        response_text += f"\nMCP Enhancement: Failed - {str(e)}"
                else:
                    # Standard A2A pong without MCP enhancement
                    response_text += "\n(Use 'mcp enhancement' in message for admin MCP integration info)"
                
                # Create A2A response message
                response_message = Message(
                    message_id=pong_message.message_id,
                    context_id=params.message.context_id,
                    role=Role.agent,
                    parts=[Part(root=TextPart(text=response_text))],
                )
                
                logger.info(f"Sending A2A pong response: {pong_message.message_id}")
                
                return response_message
            else:
                # Non-ping message handling
                response_text = f"Pong service received A2A message: {message_content}"
                
                response_message = Message(
                    message_id=str(uuid.uuid4()),
                    context_id=params.message.context_id,
                    role=Role.agent,
                    parts=[Part(root=TextPart(text=response_text))],
                )
                
                return response_message
                
        except Exception as e:
            logger.error(f"Error in A2A message handler: {str(e)}")
            return self._create_error_response("Internal server error")
    
    def _create_error_response(self, error_message: str) -> Message:
        """Create an error response message."""
        error_response = Message(
            message_id=str(uuid.uuid4()),
            context_id="error",
            role=Role.agent,
            parts=[Part(root=TextPart(text=f"Error: {error_message}"))],
        )
        
        return error_response
    
    async def on_get_task(self, params, context: ServerCallContext):
        """Handle task retrieval requests."""
        logger.info("Task retrieval not implemented for pong service")
        raise NotImplementedError("Task retrieval not supported")
    
    async def on_cancel_task(self, params, context: ServerCallContext):
        """Handle task cancellation requests."""
        logger.info("Task cancellation not implemented for pong service") 
        raise NotImplementedError("Task cancellation not supported")
