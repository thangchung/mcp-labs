"""A2A request handlers for the Ping service."""

import logging
import uuid
from datetime import datetime

import httpx
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

from shared.config import settings
from shared.models import PingMessage

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
    ) -> SendMessageResponse:
        """Handle incoming messages and potentially send ping to pong service."""
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
            
            # Check if this is a ping request
            if "ping" in message_content.lower():
                # Send ping to pong service
                pong_response = await self._send_ping_to_pong_service(
                    params.message, context
                )
                return pong_response
            else:
                # Regular message handling
                response_text = f"🏓 Ping service received: {message_content}"
                
                response_message = Message(
                    message_id=str(uuid.uuid4()),
                    context_id=params.message.context_id,
                    role=Role.agent,
                    parts=[
                        Part(text_part=TextPart(text=response_text))
                    ],
                )
                
                return SendMessageResponse(
                    root=SendMessageSuccessResponse(message=response_message)
                )
                
        except Exception as e:
            logger.error(f"Error in message handler: {str(e)}")
            return self._create_error_response("Internal server error")
    
    async def _send_ping_to_pong_service(
        self, 
        original_message: Message, 
        context: ServerCallContext
    ) -> SendMessageResponse:
        """Send a ping message to the pong service via A2A protocol."""
        try:
            # Create ping message
            ping_message = PingMessage(
                message_id=str(uuid.uuid4()),
                sender=self.service_name,
                content="ping"
            )
            
            logger.info(f"Sending ping to pong service: {ping_message.message_id}")
            
            # Get authentication token from context (would be passed through in real scenario)
            auth_header = None
            if hasattr(context, 'metadata') and context.metadata:
                auth_header = context.metadata.get('authorization')
            
            # Create A2A message to send to pong service
            ping_a2a_message = Message(
                message_id=ping_message.message_id,
                context_id=original_message.context_id,
                role=Role.user,
                parts=[
                    Part(text_part=TextPart(text=f"🏓 Ping from ping service! (ID: {ping_message.message_id[:8]}...) at {datetime.utcnow().isoformat()}"))
                ],
            )
            
            # Send HTTP request to pong service A2A endpoint
            headers = {"Content-Type": "application/json"}
            if auth_header:
                headers["Authorization"] = auth_header
            
            request_data = {
                "jsonrpc": "2.0",
                "id": str(uuid.uuid4()),
                "method": "message/send",
                "params": {
                    "message": {
                        "message_id": ping_a2a_message.message_id,
                        "context_id": ping_a2a_message.context_id,
                        "role": "user",
                        "parts": [
                            {
                                "kind": "text",
                                "text": ping_a2a_message.parts[0].text_part.text
                            }
                        ]
                    }
                }
            }
            
            async with httpx.AsyncClient() as client:
                response = await client.post(
                    self.pong_service_url,
                    json=request_data,
                    headers=headers,
                    timeout=30.0
                )
                
                if response.status_code == 200:
                    pong_data = response.json()
                    
                    if "result" in pong_data and "message" in pong_data["result"]:
                        pong_message_data = pong_data["result"]["message"]
                        pong_text = ""
                        
                        # Extract text from pong response
                        for part in pong_message_data.get("parts", []):
                            if part.get("kind") == "text":
                                pong_text += part.get("text", "")
                        
                        # Create response message with pong result
                        response_message = Message(
                            message_id=str(uuid.uuid4()),
                            context_id=original_message.context_id,
                            role=Role.agent,
                            parts=[
                                Part(text_part=TextPart(
                                    text=f"🏓 Ping sent and received response!\n\n📤 Sent: Ping to pong service\n📥 Received: {pong_text}"
                                ))
                            ],
                        )
                        
                        logger.info("Successfully received pong response")
                        
                        return SendMessageResponse(
                            root=SendMessageSuccessResponse(message=response_message)
                        )
                    else:
                        logger.error(f"Invalid pong response format: {pong_data}")
                        return self._create_error_response("Invalid response from pong service")
                else:
                    logger.error(f"Pong service returned status {response.status_code}: {response.text}")
                    return self._create_error_response(f"Pong service error: {response.status_code}")
                    
        except httpx.TimeoutException:
            logger.error("Timeout connecting to pong service")
            return self._create_error_response("Timeout connecting to pong service")
        except Exception as e:
            logger.error(f"Error sending ping to pong service: {str(e)}")
            return self._create_error_response("Failed to communicate with pong service")
    
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
        logger.info("Task retrieval not implemented for ping service")
        raise NotImplementedError("Task retrieval not supported")
    
    async def on_cancel_task(self, params, context: ServerCallContext):
        """Handle task cancellation requests."""
        logger.info("Task cancellation not implemented for ping service")
        raise NotImplementedError("Task cancellation not supported")
