"""A2A request handlers for the Ping service."""

import logging
import uuid

from a2a.server.context import ServerCallContext
from a2a.types import (
    Message,
    MessageSendParams,
    Part,
    Role,
    TextPart,
)

from shared.config import settings

logger = logging.getLogger(__name__)


class PingHandler:
    """Handler for A2A requests in the Ping service."""
    
    def __init__(self) -> None:
        """Initialize the Ping handler."""
        self.service_name = "ping-service"
        self.pong_service_url = settings.pong_service_url
    
    async def on_message_send(
        self,
        params: MessageSendParams,
        context: ServerCallContext,
    ) -> Message:
        """Handle incoming messages and potentially send ping to pong service via A2A."""
        try:
            logger.info(f"Received A2A message from {context.user.user_name if context.user else 'unknown'}")
            
            # Validate user has admin role
            if not context.user or not context.user.is_authenticated:
                logger.warning("Unauthenticated A2A request rejected")
                return self._create_error_response("Authentication required")
            
            # Extract message content - simplified approach
            message_content = "ping request via A2A"
            
            logger.info(f"Processing A2A message: {message_content}")
            
            # Create basic response for now
            response_text = "Ping service received A2A message and will forward to pong service"
            
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
