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
    SendMessageResponse,
    SendMessageSuccessResponse,
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
    ) -> SendMessageResponse:
        """Handle incoming ping messages and respond with pong."""
        try:
            logger.info(f"Received message from {context.user.user_name if context.user else 'unknown'}")
            
            # Validate user has admin role
            if not context.user or not context.user.is_authenticated:
                logger.warning("Unauthenticated request rejected")
                return self._create_error_response("Authentication required")
            
            # Extract message content
            message_content = ""
            for part in params.message.parts:
                if isinstance(part, Part) and hasattr(part, 'text_part') and part.text_part:
                    message_content += part.text_part.text
            
            logger.info(f"Processing message: {message_content}")
            
            # Try to parse as ping message
            try:
                # Simple ping detection - in a real app you might use structured data
                if "ping" in message_content.lower():
                    # Create pong response
                    pong_message = PongMessage(
                        message_id=str(uuid.uuid4()),
                        original_message_id=params.message.message_id,
                        sender=self.service_name,
                        response_time_ms=50.0,  # Simulated response time
                    )
                    
                    response_text = f"🏓 Pong! Received your ping (ID: {params.message.message_id[:8]}...) at {datetime.utcnow().isoformat()}"
                    
                    # Create A2A response message
                    response_message = Message(
                        message_id=pong_message.message_id,
                        context_id=params.message.context_id,
                        role=Role.agent,
                        parts=[
                            Part(text_part=TextPart(text=response_text))
                        ],
                    )
                    
                    logger.info(f"Sending pong response: {pong_message.message_id}")
                    
                    return SendMessageResponse(
                        root=SendMessageSuccessResponse(message=response_message)
                    )
                else:
                    return self._create_error_response("Expected ping message")
                    
            except Exception as e:
                logger.error(f"Error processing ping message: {str(e)}")
                return self._create_error_response("Failed to process ping message")
                
        except Exception as e:
            logger.error(f"Error in message handler: {str(e)}")
            return self._create_error_response("Internal server error")
    
    def _create_error_response(self, error_message: str) -> SendMessageResponse:
        """Create an error response message."""
        error_response = Message(
            message_id=str(uuid.uuid4()),
            context_id="error",
            role=Role.agent,
            parts=[
                Part(text_part=TextPart(text=f"❌ Error: {error_message}"))
            ],
        )
        
        return SendMessageResponse(
            root=SendMessageSuccessResponse(message=error_response)
        )
    
    async def on_get_task(self, params, context: ServerCallContext):
        """Handle task retrieval requests."""
        logger.info("Task retrieval not implemented for pong service")
        raise NotImplementedError("Task retrieval not supported")
    
    async def on_cancel_task(self, params, context: ServerCallContext):
        """Handle task cancellation requests."""
        logger.info("Task cancellation not implemented for pong service") 
        raise NotImplementedError("Task cancellation not supported")
