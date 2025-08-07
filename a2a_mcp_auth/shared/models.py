"""Shared data models for A2A Ping/Pong application."""

from datetime import datetime
from typing import Any, Dict, Optional

from pydantic import BaseModel, Field


class PingMessage(BaseModel):
    """Ping message model."""
    
    message_id: str = Field(..., description="Unique message identifier")
    timestamp: datetime = Field(default_factory=datetime.utcnow, description="Message timestamp")
    sender: str = Field(..., description="Sender identifier")
    content: str = Field(default="ping", description="Message content")
    metadata: Optional[Dict[str, Any]] = Field(default=None, description="Additional metadata")


class PongMessage(BaseModel):
    """Pong message model."""
    
    message_id: str = Field(..., description="Unique message identifier")
    original_message_id: str = Field(..., description="Original ping message ID")
    timestamp: datetime = Field(default_factory=datetime.utcnow, description="Message timestamp")
    sender: str = Field(..., description="Sender identifier")
    content: str = Field(default="pong", description="Message content")
    response_time_ms: float = Field(..., description="Response time in milliseconds")
    metadata: Optional[Dict[str, Any]] = Field(default=None, description="Additional metadata")


class UserInfo(BaseModel):
    """User information from Microsoft Entra ID."""
    
    user_id: str = Field(..., description="User ID")
    email: str = Field(..., description="User email")
    name: str = Field(..., description="User display name")
    roles: list[str] = Field(default_factory=list, description="User roles")
    is_admin: bool = Field(default=False, description="Whether user has admin role")


class AuthToken(BaseModel):
    """Authentication token information."""
    
    access_token: str = Field(..., description="Access token")
    token_type: str = Field(default="Bearer", description="Token type")
    expires_in: int = Field(..., description="Token expiration time in seconds")
    scope: str = Field(..., description="Token scope")
    user_info: UserInfo = Field(..., description="User information")
