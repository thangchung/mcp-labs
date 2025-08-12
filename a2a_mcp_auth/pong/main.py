"""Pong service FastAPI application."""

import asyncio
import logging
import time
from datetime import datetime
from typing import Optional

import httpx
import uvicorn
from fastapi import FastAPI, HTTPException, Request
from jose import jwt, jwk
from jose.exceptions import JWTError

from a2a.server.apps import A2AFastAPIApplication
from a2a.server.context import ServerCallContext
from a2a.auth.user import User as A2AUser

from shared.auth import auth_middleware
from shared.config import settings
from shared.models import UserInfo
from shared.debug_utils import setup_debug_logging, log_auth_attempt, log_token_validation
from .handlers import PongHandler
from .pong_agent import create_pong_agent_card
from .mcp_client import mcp_client

# Setup debug logging
setup_debug_logging()

# Configure logging
logging.basicConfig(
    level=getattr(logging, settings.log_level.upper()),
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s"
)
logger = logging.getLogger(__name__)


class JWKSTokenValidator:
    """JWT token validator using Microsoft Entra ID JWKS."""
    
    def __init__(self):
        """Initialize the JWKS token validator."""
        self.tenant_id = settings.azure_tenant_id
        self.client_id = settings.azure_client_id
        self.admin_group_id = settings.admin_group_id
        self.jwks_cache = {}
        self.jwks_cache_expiry = 0
        
        # Skip JWKS fetching if using placeholder values
        if self.tenant_id == "your-tenant-id-here":
            logger.warning("Using placeholder Entra ID configuration. JWKS validation will not work.")
            self.enabled = False
        else:
            self.enabled = True
    
    async def verify_jwt_token(self, token: str) -> UserInfo:
        """
        Verify JWT token using JWKS and return UserInfo if valid.
        
        Args:
            token: The JWT token to verify
            
        Returns:
            UserInfo object if token is valid
            
        Raises:
            HTTPException: If token is invalid or user is not admin
        """
        if not self.enabled:
            # Development mode - create mock admin user
            logger.warning("Development mode: Creating mock admin user")
            log_token_validation(token, {"dev": True})
            log_auth_attempt("dev@example.com", True, {"mode": "development"})
            return UserInfo(
                user_id="dev-user",
                name="Development User",
                email="dev@example.com",
                is_admin=True,
                roles=["admin"]
            )
        
        try:
            log_token_validation(token, None, None)  # Log attempt
            
            # 1. Get Microsoft's public keys for signature verification
            jwks = await self._get_microsoft_jwks()
            
            # 2. Decode JWT header to get key ID
            unverified_header = jwt.get_unverified_header(token)
            kid = unverified_header.get("kid")
            
            if not kid:
                logger.warning("Token missing key ID (kid) in header")
                log_token_validation(token, unverified_header, "Missing key ID (kid) in header")
                raise HTTPException(status_code=401, detail="Invalid token format")
            
            # 3. Find the corresponding public key
            public_key = None
            for key in jwks.get("keys", []):
                if key.get("kid") == kid:
                    public_key = jwk.construct(key)
                    break
            
            if not public_key:
                logger.warning(f"No public key found for kid: {kid}")
                log_token_validation(token, {"kid": kid}, f"No public key found for kid: {kid}")
                raise HTTPException(status_code=401, detail="Token verification failed")
            
            # 4. Verify JWT signature and claims
            payload = jwt.decode(
                token,
                public_key,
                algorithms=["RS256"],
                issuer=f"https://login.microsoftonline.com/{self.tenant_id}/v2.0",
                audience=self.client_id
            )
            
            # 5. Extract user information
            user_id = payload.get("sub", "")
            user_email = payload.get("email") or payload.get("upn", "")
            user_name = payload.get("name", user_email.split("@")[0] if user_email else "Unknown")
            
            # 6. Check admin role/scope
            is_admin = await self._check_admin_role(payload)
            roles = self._extract_roles(payload)
            
            if not is_admin:
                logger.warning(f"Non-admin user {user_email} attempted pong service access")
                log_auth_attempt(user_email, False, {"type": "pong_service_access", "reason": "not_admin"})
                log_token_validation(token, payload, "User does not have admin role")
                raise HTTPException(status_code=403, detail="Admin role required")
            
            # 7. Return valid UserInfo
            logger.info(f"Admin user {user_email} granted pong service access via JWKS validation")
            log_auth_attempt(user_email, True, {"type": "pong_service_access", "admin": True, "method": "jwks"})
            log_token_validation(token, payload)  # Success
            
            return UserInfo(
                user_id=user_id,
                name=user_name,
                email=user_email,
                is_admin=is_admin,
                roles=roles
            )
            
        except HTTPException:
            raise
        except JWTError as e:
            logger.warning(f"JWT validation failed: {e}")
            log_token_validation(token, None, f"JWT validation failed: {e}")
            log_auth_attempt("unknown", False, {"type": "pong_service_access", "error": str(e)})
            raise HTTPException(status_code=401, detail="Invalid token")
        except Exception as e:
            logger.error(f"Token verification error: {e}")
            log_token_validation(token, None, f"Token verification error: {e}")
            log_auth_attempt("unknown", False, {"type": "pong_service_access", "error": str(e)})
            raise HTTPException(status_code=500, detail="Token verification failed")
    
    async def _get_microsoft_jwks(self) -> dict:
        """Get Microsoft's JSON Web Key Set for token signature verification."""
        current_time = time.time()
        
        # Use cached JWKS if still valid (cache for 1 hour)
        if current_time < self.jwks_cache_expiry and self.jwks_cache:
            return self.jwks_cache
        
        try:
            jwks_url = f"https://login.microsoftonline.com/{self.tenant_id}/discovery/v2.0/keys"
            
            async with httpx.AsyncClient() as client:
                response = await client.get(jwks_url, timeout=10.0)
                response.raise_for_status()
                
                jwks = response.json()
                
                # Cache JWKS for 1 hour
                self.jwks_cache = jwks
                self.jwks_cache_expiry = current_time + 3600
                
                logger.debug(f"Successfully fetched and cached Microsoft JWKS from {jwks_url}")
                return jwks
                
        except Exception as e:
            logger.error(f"Failed to fetch Microsoft JWKS: {e}")
            # Return cached JWKS if available, even if expired
            return self.jwks_cache if self.jwks_cache else {"keys": []}
    
    async def _check_admin_role(self, payload: dict) -> bool:
        """
        Check if the user has admin role based on token claims.
        
        Args:
            payload: JWT token payload
            
        Returns:
            True if user has admin role, False otherwise
        """
        # Method 1: Check roles claim (App roles)
        roles = payload.get("roles", [])
        if "Admin" in roles or "admin" in roles or settings.admin_role_name in roles:
            return True
        
        # Method 2: Check groups claim (Azure AD groups)
        groups = payload.get("groups", [])
        if self.admin_group_id and self.admin_group_id in groups:
            return True
        
        # Method 3: Check scope claim (OAuth2 scopes)
        scopes = payload.get("scp", "").split()
        if "admin" in scopes or settings.required_scopes in scopes:
            return True
        
        # Method 4: Check custom extension attributes
        admin_extensions = [
            "extension_admin",
            "extension_is_admin", 
            "extension_role_admin"
        ]
        for ext in admin_extensions:
            if payload.get(ext) in ["true", "1", True]:
                return True
        
        return False
    
    def _extract_roles(self, payload: dict) -> list[str]:
        """Extract roles from JWT payload."""
        roles = []
        
        # Add roles from roles claim
        token_roles = payload.get("roles", [])
        if isinstance(token_roles, list):
            roles.extend(token_roles)
        
        # Add scope-based roles
        scopes = payload.get("scp", "").split()
        if "admin" in scopes:
            roles.append("admin")
        
        return roles


# Global JWKS validator instance
jwks_validator = JWKSTokenValidator()


class A2AUserProxy(A2AUser):
    """Proxy to adapt UserInfo to A2A User interface."""
    
    def __init__(self, user_info: UserInfo, jwt_token: Optional[str] = None):
        self.user_info = user_info
        self.jwt_token = jwt_token  # Store JWT token if available
    
    @property
    def is_authenticated(self) -> bool:
        return True
    
    @property
    def user_name(self) -> str:
        return self.user_info.name


class CustomCallContextBuilder:
    """Custom call context builder that includes JWKS-based authentication."""
    
    def _sync_verify_jwt_token(self, token: str) -> UserInfo:
        """
        Synchronous JWT verification using JWKS for A2A context.
        This is a simplified sync version of the async JWKS validator.
        """
        import httpx
        import time
        from jose import jwt, jwk
        from jose.exceptions import JWTError
        
        try:
            # Use development mode if placeholder tenant
            if not jwks_validator.enabled:
                logger.warning("Development mode: Creating mock admin user for A2A context")
                return UserInfo(
                    user_id="dev-user-a2a",
                    name="Development User (A2A)",
                    email="dev-a2a@example.com",
                    is_admin=True,
                    roles=["admin"]
                )
            
            # Get JWKS synchronously
            jwks_url = f"https://login.microsoftonline.com/{jwks_validator.tenant_id}/discovery/v2.0/keys"
            with httpx.Client(timeout=10.0) as client:
                response = client.get(jwks_url)
                response.raise_for_status()
                jwks = response.json()
            
            # Decode JWT header to get key ID
            unverified_header = jwt.get_unverified_header(token)
            kid = unverified_header.get("kid")
            
            if not kid:
                raise HTTPException(status_code=401, detail="Invalid token format - missing kid")
            
            # Find the corresponding public key
            public_key = None
            for key in jwks.get("keys", []):
                if key.get("kid") == kid:
                    public_key = jwk.construct(key)
                    break
            
            if not public_key:
                raise HTTPException(status_code=401, detail="Token verification failed - invalid kid")
            
            # Verify JWT signature and claims
            payload = jwt.decode(
                token,
                public_key,
                algorithms=["RS256"],
                issuer=f"https://login.microsoftonline.com/{jwks_validator.tenant_id}/v2.0",
                audience=jwks_validator.client_id
            )
            
            # Extract user information
            user_id = payload.get("sub", "")
            user_email = payload.get("email") or payload.get("upn", "")
            user_name = payload.get("name", user_email.split("@")[0] if user_email else "Unknown")
            
            # Check admin role
            is_admin = self._check_admin_role_sync(payload)
            roles = self._extract_roles_sync(payload)
            
            if not is_admin:
                logger.warning(f"Non-admin user {user_email} attempted A2A access")
                raise HTTPException(status_code=403, detail="Admin role required")
            
            logger.info(f"A2A JWKS validation successful for admin user: {user_email}")
            
            return UserInfo(
                user_id=user_id,
                name=user_name,
                email=user_email,
                is_admin=is_admin,
                roles=roles
            )
            
        except HTTPException:
            raise
        except Exception as e:
            logger.error(f"A2A JWKS validation error: {e}")
            raise HTTPException(status_code=401, detail="Token verification failed")
    
    def _check_admin_role_sync(self, payload: dict) -> bool:
        """Check admin role in JWT payload (sync version)."""
        # Method 1: Check roles claim
        roles = payload.get("roles", [])
        if "Admin" in roles or "admin" in roles or settings.admin_role_name in roles:
            return True
        
        # Method 2: Check groups claim
        groups = payload.get("groups", [])
        if settings.admin_group_id and settings.admin_group_id in groups:
            return True
        
        # Method 3: Check scope claim
        scopes = payload.get("scp", "").split()
        if "admin" in scopes or settings.required_scopes in scopes:
            return True
        
        return False
    
    def _extract_roles_sync(self, payload: dict) -> list[str]:
        """Extract roles from JWT payload (sync version)."""
        roles = []
        
        token_roles = payload.get("roles", [])
        if isinstance(token_roles, list):
            roles.extend(token_roles)
        
        scopes = payload.get("scp", "").split()
        if "admin" in scopes:
            roles.append("admin")
        
        return roles
    
    def build(self, request: Request) -> ServerCallContext:
        """Build server call context with enhanced JWT validation."""
        user = None
        jwt_token = None
        
        # Try to get user from authorization header
        auth_header = request.headers.get("authorization")
        if auth_header and auth_header.startswith("Bearer "):
            jwt_token = auth_header.split(" ")[1]
            try:
                # Use sync JWKS validation for A2A context
                user_info = self._sync_verify_jwt_token(jwt_token)
                user = A2AUserProxy(user_info, jwt_token)
                logger.debug(f"A2A context JWKS authentication successful for user: {user_info.email}")
            except HTTPException as e:
                logger.warning(f"A2A context JWKS authentication failed: {e.detail}")
                # Fallback to shared auth for backward compatibility
                try:
                    user_info = auth_middleware.auth_handler.verify_jwt_token(jwt_token)
                    user = A2AUserProxy(user_info, jwt_token)
                    logger.debug(f"A2A context fallback authentication successful for user: {user_info.email}")
                except Exception as fallback_e:
                    logger.warning(f"A2A context fallback authentication also failed: {fallback_e}")
                    pass  # Invalid token, user remains None
            except Exception as e:
                logger.error(f"Unexpected error during A2A JWKS authentication: {e}")
                pass  # Invalid token, user remains None
        
        # If no valid user found, create an anonymous user for A2A compatibility
        if user is None:
            from shared.models import UserInfo
            anonymous_user_info = UserInfo(
                user_id="anonymous",
                name="Anonymous User",
                email="anonymous@example.com",
                is_admin=False
            )
            user = A2AUserProxy(anonymous_user_info)
        
        return ServerCallContext(
            user=user,
            activated_extensions=set()
        )


# Create FastAPI app
app = FastAPI(
    title="Pong Service",
    description="A2A Pong service with Microsoft Entra ID authentication",
    version="1.0.0",
    debug=settings.debug
)

# Add debug middleware if enabled
if settings.debug_requests:
    from shared.debug_utils import DebugMiddleware
    app.add_middleware(DebugMiddleware)

# Create agent card and handler
agent_card = create_pong_agent_card()
pong_handler = PongHandler()
context_builder = CustomCallContextBuilder()

# Create A2A application
a2a_app = A2AFastAPIApplication(
    agent_card=agent_card,
    http_handler=pong_handler,
    context_builder=context_builder
)

# Add A2A routes to the main app
a2a_app.add_routes_to_app(app)


@app.get("/health")
async def health_check():
    """Health check endpoint with MCP server status."""
    try:
        mcp_health = await mcp_client.health_check()
        return {
            "status": "healthy",
            "service": "pong",
            "mcp_server": mcp_health
        }
    except Exception as e:
        return {
            "status": "degraded",
            "service": "pong",
            "mcp_server": {"status": "error", "error": str(e)}
        }


@app.get("/debug")
async def debug_info():
    """Debug information endpoint (only available in debug mode)."""
    if not settings.debug:
        raise HTTPException(status_code=404, detail="Debug endpoint not available")
    
    from shared.debug_utils import create_debug_endpoint
    debug_func = create_debug_endpoint()
    return debug_func()


@app.get("/")
async def root():
    """Root endpoint with service information."""
    return {
        "service": "Pong Service",
        "version": "1.1.0",
        "description": "A2A Pong service with Microsoft Entra ID authentication and MCP integration",
        "architecture": "A2A Server + MCP Client - receives A2A messages and handles MCP communication",
        "endpoints": {
            "agent_card": "/.well-known/agent.json",
            "a2a_rpc": "/",
            "health": "/health",
            "debug": "/debug"
        },
        "authentication": {
            "method": "Microsoft Entra ID JWT",
            "validation": "JWKS (JSON Web Key Set)",
            "a2a_context": "Shared auth middleware for A2A protocol",
            "direct_endpoints": "JWKS validation for enhanced security"
        },
        "mcp_integration": {
            "server_url": settings.mcp_server_url,
            "description": "MCP server access only through A2A protocol for admin users",
            "required_role": "admin",
            "access_method": "A2A protocol with JWT token relay",
            "security_policy": "No direct MCP endpoints - A2A-only access"
        },
        "security_enhancements": {
            "jwks_validation": True,
            "token_verification": "Cryptographic signature validation",
            "admin_role_checking": "Multiple validation methods (roles, groups, scopes)",
            "debug_logging": "Comprehensive authentication audit trails"
        }
    }


def main():
    """Run the Pong service."""
    logger.info("Starting Pong service...")
    logger.info(f"Service URL: {settings.pong_service_url}")
    logger.info(f"Debug mode: {settings.debug}")
    
    # Extract port from URL
    port = 8001
    if ":" in settings.pong_service_url:
        port_str = settings.pong_service_url.split(":")[-1]
        try:
            port = int(port_str)
        except ValueError:
            pass
    
    uvicorn.run(
        "pong.main:app",
        host="0.0.0.0",
        port=port,
        reload=settings.debug,
        log_level=settings.log_level.lower()
    )


if __name__ == "__main__":
    main()
