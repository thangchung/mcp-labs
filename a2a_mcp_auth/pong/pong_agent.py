"""Pong service A2A agent implementation."""

from a2a.types import (
    AgentCapabilities,
    AgentCard,
    AgentSkill,
    AuthorizationCodeOAuthFlow,
    OAuthFlows,
    OAuth2SecurityScheme,
    SecuritySchemeBase,
)

from shared.config import settings


def create_pong_agent_card() -> AgentCard:
    """Create the agent card for the Pong service."""
    
    # Define OAuth2 security scheme for Microsoft Entra ID
    oauth2_scheme = OAuth2SecurityScheme(
        type="oauth2",
        description="Microsoft Entra ID OAuth2 authentication",
        flows=OAuthFlows(
            authorization_code=AuthorizationCodeOAuthFlow(
                authorizationUrl=f"https://login.microsoftonline.com/{settings.azure_tenant_id}/oauth2/v2.0/authorize",
                tokenUrl=f"https://login.microsoftonline.com/{settings.azure_tenant_id}/oauth2/v2.0/token",
                scopes={
                    "openid": "OpenID Connect authentication",
                    "profile": "Access to user profile",
                    "email": "Access to user email",
                    settings.required_scopes: "Admin access to pong service"
                }
            )
        )
    )
    
    # Define the pong skill
    pong_skill = AgentSkill(
        id="pong",
        name="pong",
        description="Responds to ping messages with pong responses",
        tags=["ping-pong", "communication", "test"],
        examples=[
            "Send me a ping",
            "ping",
            "Can you pong my ping?"
        ]
    )
    
    # Create agent capabilities
    capabilities = AgentCapabilities(
        streaming=False
    )
    
    # Create the agent card
    agent_card = AgentCard(
        name="Pong Service",
        description="A2A Pong service that responds to ping messages with Microsoft Entra ID authentication",
        url=settings.pong_service_url,
        version="1.0.0",
        protocolVersion="0.3.0",
        capabilities=capabilities,
        skills=[pong_skill],
        defaultInputModes=["text"],
        defaultOutputModes=["text"],
        security_schemes={
            "oauth2": oauth2_scheme
        },
        security=[
            {"oauth2": ["openid", "profile", "email", settings.required_scopes]}
        ]
    )
    
    return agent_card
