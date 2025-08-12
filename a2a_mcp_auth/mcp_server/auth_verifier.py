"""Microsoft Entra ID token verifier for MCP server authentication."""

import logging
import time
from typing import Optional
import httpx
from jose import jwt, jwk
from jose.exceptions import JWTError

from mcp.server.auth.provider import AccessToken, TokenVerifier
from shared.config import settings
from shared.debug_utils import log_auth_attempt, log_token_validation

logger = logging.getLogger(__name__)


class EntraIDTokenVerifier(TokenVerifier):
    """Token verifier that validates Microsoft Entra ID access tokens for MCP server access."""
    
    def __init__(self):
        """Initialize the Entra ID token verifier."""
        self.tenant_id = settings.azure_tenant_id
        self.client_id = settings.azure_client_id
        self.admin_group_id = settings.admin_group_id
        self.jwks_cache = {}
        self.jwks_cache_expiry = 0
        
        # Skip JWKS fetching if using placeholder values
        if self.tenant_id == "your-tenant-id-here":
            logger.warning("Using placeholder Entra ID configuration. Token verification will not work.")
            self.enabled = False
        else:
            self.enabled = True
    
    async def verify_token(self, token: str) -> Optional[AccessToken]:
        """
        Verify a bearer token and return AccessToken if valid and user has admin role.
        
        Args:
            token: The bearer token to verify
            
        Returns:
            AccessToken if token is valid and user is admin, None otherwise
        """
        if not self.enabled:
            # Development mode - create mock admin token
            logger.warning("Development mode: Creating mock admin token")
            log_token_validation(token, {"dev": True})  # Log development mode
            log_auth_attempt("dev@example.com", True, {"mode": "development"})
            return AccessToken(
                token=token,
                client_id="dev-client",
                scopes=["admin"],
                expires_at=int(time.time()) + 3600,
                user_id="dev-user", # type: ignore
                user_email="dev@example.com" # type: ignore
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
                return None
            
            # 3. Find the corresponding public key
            public_key = None
            for key in jwks.get("keys", []):
                if key.get("kid") == kid:
                    public_key = jwk.construct(key)
                    break
            
            if not public_key:
                logger.warning(f"No public key found for kid: {kid}")
                log_token_validation(token, {"kid": kid}, f"No public key found for kid: {kid}")
                return None
            
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
            
            # 6. Check admin role/scope
            is_admin = await self._check_admin_role(payload)
            
            if not is_admin:
                logger.warning(f"Non-admin user {user_email} attempted MCP server access")
                log_auth_attempt(user_email, False, {"type": "mcp_access", "reason": "not_admin"})
                log_token_validation(token, payload, "User does not have admin role")
                return None
            
            # 7. Return valid AccessToken with admin scope
            logger.info(f"Admin user {user_email} granted MCP server access")
            log_auth_attempt(user_email, True, {"type": "mcp_access", "admin": True})
            log_token_validation(token, payload)  # Success
            return AccessToken(
                token=token,
                client_id=payload.get("azp", payload.get("appid", "")),
                scopes=["admin"],
                expires_at=payload.get("exp", 0),
                user_id=user_id, # type: ignore
                user_email=user_email # type: ignore
            )
            
        except JWTError as e:
            logger.warning(f"JWT validation failed: {e}")
            log_token_validation(token, None, f"JWT validation failed: {e}")
            log_auth_attempt("unknown", False, {"type": "mcp_access", "error": str(e)})
            return None
        except Exception as e:
            logger.error(f"Token verification error: {e}")
            log_token_validation(token, None, f"Token verification error: {e}")
            log_auth_attempt("unknown", False, {"type": "mcp_access", "error": str(e)})
            return None
    
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
        if "Admin" in roles or "admin" in roles:
            return True
        
        # Method 2: Check groups claim (Azure AD groups)
        groups = payload.get("groups", [])
        if self.admin_group_id in groups:
            return True
        
        # Method 3: Check scope claim (OAuth2 scopes)
        scopes = payload.get("scp", "").split()
        if "admin" in scopes:
            return True
        
        # Method 4: Check custom extension attributes
        # This could be used for custom admin indicators
        admin_extensions = [
            "extension_admin",
            "extension_is_admin", 
            "extension_role_admin"
        ]
        for ext in admin_extensions:
            if payload.get(ext) in ["true", "1", True]:
                return True
        
        return False


# Add security warning logger
class SecurityLogger:
    """Enhanced logger for security events."""
    
    @staticmethod
    def security_warning(message: str):
        """Log security warnings with enhanced formatting."""
        logger.warning(f"[SECURITY] {message}")
    
    @staticmethod
    def security_audit(message: str):
        """Log security audit events."""
        logger.info(f"[AUDIT] {message}")


# Patch logger with security methods
logger.security_warning = SecurityLogger.security_warning
logger.security_audit = SecurityLogger.security_audit
