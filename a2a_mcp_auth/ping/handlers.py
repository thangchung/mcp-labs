"""A2A request handlers for the Ping service."""

import logging

from shared.config import settings

logger = logging.getLogger(__name__)


class PingHandler:
    """Handler for A2A requests in the Ping service."""
    
    def __init__(self) -> None:
        """Initialize the Ping handler."""
        self.service_name = "ping-service"
        self.pong_service_url = settings.pong_service_url
        logger.info("[PING HANDLER] Initialized - Ready to handle incoming A2A messages")
    
    # Note: on_message_send is not implemented because this ping service
    # acts as a CLIENT that sends messages to other services, not as a SERVER
    # that receives A2A messages. The /ping endpoint handles user requests directly.
