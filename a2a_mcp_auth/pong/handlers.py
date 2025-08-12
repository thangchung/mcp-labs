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

from shared.config import settings
from shared.models import PongMessage

logger = logging.getLogger(__name__)


class PongHandler:
    """Handler for A2A requests in the Pong service."""
    
    def __init__(self) -> None:
        """Initialize the Pong handler."""
        self.service_name = "pong-service"
        logger.info("[PONG HANDLER] Initialized - Ready to handle incoming A2A messages")
        
        # Import MCP client here to avoid circular imports
        from .mcp_client import mcp_client
        self.mcp_client = mcp_client
    
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
                response_text = f"Pong! Received your A2A ping (ID: {params.message.message_id[:8]}...) at {datetime.now().isoformat()}"
                
                # Check if user is authenticated and has admin privileges for MCP enhancement
                user_is_admin = False
                
                if context.user and context.user.is_authenticated:
                    # Check if user has admin privileges (using our A2AUserProxy structure)
                    # Use getattr to safely access user_info attribute
                    user_info = getattr(context.user, 'user_info', None)
                    if user_info and hasattr(user_info, 'is_admin'):
                        user_is_admin = user_info.is_admin
                
                if user_is_admin:
                    try:
                        logger.info("Admin user detected - attempting MCP enhancement with JWT token")
                        
                        # Extract JWT token from user proxy if available (type: ignore for A2AUserProxy)
                        auth_token = None
                        if hasattr(context.user, 'jwt_token'):
                            auth_token = getattr(context.user, 'jwt_token', None)  # type: ignore
                            if auth_token:
                                logger.debug("JWT token extracted from A2A user context for MCP call")
                                logger.debug("Note: For enhanced security, direct /pong/mcp endpoint uses JWKS validation")
                        
                        # Call MCP with JWT token if available, otherwise fall back to service call
                        mcp_response = await self.mcp_client.ping_mcp_service_call(
                            message_content, 
                            self.service_name,
                            auth_token=auth_token
                        )
                        
                        if mcp_response["status"] == "success":
                            response_text += f"\n🚀 MCP Enhancement: {mcp_response['content']}"
                            if "metadata" in mcp_response:
                                response_text += f"\n📊 MCP Details: Service call from {mcp_response['metadata'].get('caller', 'unknown')}"
                                if auth_token:
                                    response_text += " (JWT token forwarded)"
                        else:
                            response_text += f"\n⚠️ MCP Enhancement: {mcp_response['content']}"
                            
                    except Exception as e:
                        logger.error(f"MCP integration error: {str(e)}")
                        response_text += f"\n❌ MCP Enhancement: Error - {str(e)}"
                        
                elif context.user and context.user.is_authenticated:
                    response_text += "\n🔒 MCP Enhancement: Admin privileges required"
                    response_text += "\n💡 Tip: Contact admin to upgrade your role for MCP access"
                else:
                    response_text += "\n🔐 MCP Enhancement: Authentication required"
                    response_text += "\n💡 Tip: Use valid JWT token for authentication"
                    
                # Always provide guidance for full MCP integration
                response_text += "\n🚀 For enhanced security: POST /pong/mcp with Authorization header (JWKS validation)"
                
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
