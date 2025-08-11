"""Configuration management for A2A Ping/Pong application."""

from pydantic import Field
from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    """Application settings loaded from environment variables."""
    
    # Microsoft Entra ID Configuration
    azure_tenant_id: str = Field(..., description="Azure AD tenant ID")
    azure_client_id: str = Field(..., description="Azure AD client ID")
    azure_client_secret: str = Field(..., description="Azure AD client secret")
    azure_redirect_uri: str = Field(
        default="http://localhost:8000/auth/callback",
        description="OAuth2 redirect URI"
    )
    
    # Service Configuration
    ping_service_url: str = Field(
        default="http://localhost:8000",
        description="Ping service URL"
    )
    pong_service_url: str = Field(
        default="http://localhost:8001", 
        description="Pong service URL"
    )
    mcp_server_url: str = Field(
        default="http://localhost:8002",
        description="MCP server URL"
    )
    mcp_server_port: int = Field(
        default=8002,
        description="MCP server port"
    )
    
    # Security Configuration
    jwt_secret_key: str = Field(
        default="your-super-secret-jwt-key-change-this-in-production",
        description="JWT secret key"
    )
    jwt_algorithm: str = Field(default="HS256", description="JWT algorithm")
    jwt_expire_minutes: int = Field(default=30, description="JWT expiration time in minutes")
    
    # Role Configuration
    admin_role_name: str = Field(
        default="PingPongAdmin",
        description="Admin role name in Azure AD"
    )
    admin_group_id: str = Field(
        default="",
        description="Admin group ID in Azure AD (optional)"
    )
    required_scopes: str = Field(
        default="api://your-app-id/admin",
        description="Required OAuth2 scopes"
    )
    
    # Development Configuration
    debug: bool = Field(default=False, description="Debug mode")
    log_level: str = Field(default="INFO", description="Logging level")
    debug_auth: bool = Field(default=False, description="Enable authentication debug logging")
    debug_a2a: bool = Field(default=False, description="Enable A2A debug logging")
    debug_mcp: bool = Field(default=False, description="Enable MCP debug logging")
    debug_requests: bool = Field(default=False, description="Enable HTTP request/response debug logging")
    
    class Config:
        env_file = ".env"
        env_file_encoding = "utf-8"


# Global settings instance
settings = Settings()
