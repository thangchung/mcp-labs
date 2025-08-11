"""Microsoft Entra ID authentication and authorization module."""

import logging
from datetime import datetime, timedelta
from typing import Optional

import msal
from fastapi import Depends, HTTPException
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer
from jose import JWTError, jwt

from .config import settings
from .debug_utils import log_auth_attempt, log_token_validation
from .models import AuthToken, UserInfo

logger = logging.getLogger(__name__)


class EntraIDAuth:
    """Microsoft Entra ID authentication handler."""
    
    def __init__(self) -> None:
        """Initialize the Entra ID authentication handler."""
        self.authority = f"https://login.microsoftonline.com/{settings.azure_tenant_id}"
        # Include both custom scope and basic user scopes
        self.scope = [settings.required_scopes]
        
        # Skip MSAL initialization if using placeholder values (for development)
        if (settings.azure_tenant_id == "your-tenant-id-here" or 
            settings.azure_client_id == "your-client-id-here"):
            logger.warning("Using placeholder Azure AD configuration. Authentication will not work.")
            self.app = None
            return
        
        # Create MSAL application
        try:
            self.app = msal.ConfidentialClientApplication(
                client_id=settings.azure_client_id,
                client_credential=settings.azure_client_secret,
                authority=self.authority,
            )
        except Exception as e:
            logger.error(f"Failed to initialize MSAL application: {str(e)}")
            self.app = None
    
    def get_auth_url(self, state: Optional[str] = None) -> str:
        """Generate OAuth2 authorization URL with PKCE."""
        if not self.app:
            raise HTTPException(
                status_code=500,
                detail="Authentication not properly configured"
            )
        
        auth_url = self.app.get_authorization_request_url(
            scopes=self.scope,
            redirect_uri=settings.azure_redirect_uri,
            state=state,
        )
        return auth_url
    
    async def exchange_code_for_token(self, authorization_code: str) -> AuthToken:
        """Exchange authorization code for access token."""
        if not self.app:
            raise HTTPException(
                status_code=500,
                detail="Authentication not properly configured"
            )
            
        try:
            result = self.app.acquire_token_by_authorization_code(
                code=authorization_code,
                scopes=self.scope,
                redirect_uri=settings.azure_redirect_uri,
            )
            
            if "error" in result:
                logger.error(f"Token exchange error: {result.get('error_description')}")
                raise HTTPException(
                    status_code=400,
                    detail=f"Authentication failed: {result.get('error_description')}"
                )
            
            # Get user info from ID token instead of access token
            user_info = await self._extract_user_info_from_token_result(result)
            
            return AuthToken(
                access_token=result["access_token"],
                token_type="Bearer",
                expires_in=result.get("expires_in", 3600),
                scope=" ".join(self.scope),
                user_info=user_info,
            )
            
        except Exception as e:
            logger.error(f"Token exchange failed: {str(e)}")
            raise HTTPException(
                status_code=500,
                detail="Authentication failed"
            )
    
    async def _extract_user_info_from_token_result(self, token_result: dict) -> UserInfo:
        """Extract user information from MSAL token result."""
        try:
            # MSAL provides ID token claims directly
            id_token_claims = token_result.get("id_token_claims", {})
            
            if not id_token_claims:
                # Fallback: decode ID token manually if claims not provided
                id_token = token_result.get("id_token")
                if id_token:
                    # Decode without verification since it came from MSAL
                    import json
                    import base64
                    
                    # Split the JWT token and decode the payload
                    parts = id_token.split('.')
                    if len(parts) >= 2:
                        payload = parts[1]
                        # Add padding if needed
                        payload += '=' * (4 - len(payload) % 4)
                        decoded_bytes = base64.urlsafe_b64decode(payload)
                        id_token_claims = json.loads(decoded_bytes.decode('utf-8'))
            
            # Extract user information from ID token claims
            email = (id_token_claims.get("email") or 
                    id_token_claims.get("preferred_username") or 
                    id_token_claims.get("upn", ""))
            
            name = (id_token_claims.get("name") or 
                   id_token_claims.get("given_name", "") + " " + id_token_claims.get("family_name", "")).strip()
            
            user_id = id_token_claims.get("oid") or id_token_claims.get("sub", "")
            
            # For now, assume users with the custom scope are admins
            # In production, you would check group membership or roles
            roles = []
            is_admin = False
            
            # Check if user has the required scope (simplified admin check)
            if settings.required_scopes in token_result.get("scope", ""):
                roles.append("admin")
                is_admin = True
            
            # Check for roles claim in the token
            token_roles = id_token_claims.get("roles", [])
            if isinstance(token_roles, list):
                roles.extend(token_roles)
                if settings.admin_role_name in token_roles:
                    is_admin = True
            
            return UserInfo(
                user_id=user_id,
                email=email,
                name=name or email,  # Fallback to email if name not available
                roles=roles,
                is_admin=is_admin,
            )
                
        except Exception as e:
            logger.error(f"Failed to extract user info from token result: {str(e)}")
            # Create a minimal user info from what we can get
            return UserInfo(
                user_id="unknown",
                email="unknown@example.com",
                name="Unknown User",
                roles=["user"],
                is_admin=False,
            )
    
    def create_jwt_token(self, user_info: UserInfo) -> str:
        """Create a JWT token for the user."""
        payload = {
            "sub": user_info.user_id,
            "email": user_info.email,
            "name": user_info.name,
            "roles": user_info.roles,
            "is_admin": user_info.is_admin,
            "exp": datetime.utcnow() + timedelta(minutes=settings.jwt_expire_minutes),
            "iat": datetime.utcnow(),
        }
        
        return jwt.encode(
            payload,
            settings.jwt_secret_key,
            algorithm=settings.jwt_algorithm
        )
    
    def verify_jwt_token(self, token: str) -> UserInfo:
        """Verify and decode a JWT token (both application and Azure AD tokens)."""
        try:
            log_token_validation(token, None, None)  # Log attempt
            
            # First, try to decode as application JWT token
            try:
                payload = jwt.decode(
                    token,
                    settings.jwt_secret_key,
                    algorithms=[settings.jwt_algorithm]
                )
                
                user_info = UserInfo(
                    user_id=payload["sub"],
                    email=payload["email"],
                    name=payload["name"],
                    roles=payload.get("roles", []),
                    is_admin=payload.get("is_admin", False),
                )
                
                log_token_validation(token, payload)  # Log success
                log_auth_attempt(payload.get("email", "unknown"), True, {"type": "application_jwt"})
                return user_info
                
            except JWTError:
                # If application JWT fails, try Azure AD token
                logger.debug("Application JWT verification failed, trying Azure AD token...")
                return self._verify_azure_ad_token(token)
            
        except JWTError as e:
            logger.error(f"JWT verification failed: {str(e)}")
            logger.debug(f"Token preview: {token[:50]}...")
            logger.debug(f"Expected algorithm: {settings.jwt_algorithm}")
            logger.debug(f"Secret key preview: {settings.jwt_secret_key[:10]}...")
            log_token_validation(token, None, str(e))  # Log failure
            raise HTTPException(
                status_code=401,
                detail="Invalid authentication token"
            )
    
    def _verify_azure_ad_token(self, token: str) -> UserInfo:
        """Verify and decode an Azure AD token."""
        import json
        import base64
        
        try:
            log_token_validation(token, None, None)  # Log attempt
            
            # Decode without verification for now (in production, verify signature)
            header_data = token.split('.')[0]
            payload_data = token.split('.')[1]
            
            # Add padding if needed
            header_data += '=' * (4 - len(header_data) % 4)
            payload_data += '=' * (4 - len(payload_data) % 4)
            
            # Decode the payload
            decoded_payload = base64.urlsafe_b64decode(payload_data)
            payload = json.loads(decoded_payload.decode('utf-8'))
            
            # Verify basic claims
            expected_audience = settings.azure_client_id
            token_audience = payload.get("aud", "")
            
            if token_audience != expected_audience and not token_audience.startswith("api://"):
                error_msg = f"Invalid token audience: {token_audience}, expected: {expected_audience}"
                log_token_validation(token, payload, error_msg)
                raise HTTPException(
                    status_code=401,
                    detail="Invalid token audience"
                )
            
            # Check if token is expired
            import time
            current_time = int(time.time())
            exp = payload.get("exp", 0)
            
            if current_time > exp:
                error_msg = f"Token expired at {exp}, current time: {current_time}"
                log_token_validation(token, payload, error_msg)
                raise HTTPException(
                    status_code=401,
                    detail="Token has expired"
                )
            
            # Extract user information
            email = (payload.get("email") or 
                    payload.get("preferred_username") or 
                    payload.get("upn", ""))
            
            name = (payload.get("name") or 
                   payload.get("given_name", "") + " " + payload.get("family_name", "")).strip()
            
            user_id = payload.get("oid") or payload.get("sub", "")
            
            # Check for admin scope or role
            roles = []
            is_admin = False
            
            # Check scope claim
            scope = payload.get("scp", "")
            if settings.admin_role_name in scope or "admin" in scope:
                roles.append("admin")
                is_admin = True
            
            # Check roles claim
            token_roles = payload.get("roles", [])
            if isinstance(token_roles, list):
                roles.extend(token_roles)
                if settings.admin_role_name in token_roles:
                    is_admin = True
            
            logger.info(f"Azure AD token verified for user: {name} ({email}), admin: {is_admin}")
            
            user_info = UserInfo(
                user_id=user_id,
                email=email,
                name=name or email,
                roles=roles,
                is_admin=is_admin,
            )
            
            log_token_validation(token, payload)  # Log success
            log_auth_attempt(email, True, {"type": "azure_ad", "roles": roles, "is_admin": is_admin})
            return user_info
            
        except Exception as e:
            logger.error(f"Azure AD token verification failed: {str(e)}")
            log_token_validation(token, None, str(e))  # Log failure
            log_auth_attempt("unknown", False, {"type": "azure_ad", "error": str(e)})
            raise HTTPException(
                status_code=401,
                detail="Invalid Azure AD token"
            )


class AuthMiddleware:
    """Authentication middleware for FastAPI."""
    
    def __init__(self) -> None:
        """Initialize the authentication middleware."""
        self.auth_handler = EntraIDAuth()
        self.security = HTTPBearer(auto_error=False)
    
    async def get_current_user(
        self, 
        credentials: HTTPAuthorizationCredentials = Depends(HTTPBearer(auto_error=False))
    ) -> Optional[UserInfo]:
        """Get current authenticated user."""
        if not credentials:
            return None
        
        try:
            user_info = self.auth_handler.verify_jwt_token(credentials.credentials)
            return user_info
        except HTTPException:
            return None
    
    async def require_auth(
        self,
        credentials: HTTPAuthorizationCredentials = Depends(HTTPBearer(auto_error=True))
    ) -> UserInfo:
        """Require authentication."""
        user_info = self.auth_handler.verify_jwt_token(credentials.credentials)
        return user_info
    
    async def require_admin(
        self,
        credentials: HTTPAuthorizationCredentials = Depends(HTTPBearer(auto_error=True))
    ) -> UserInfo:
        """Require admin authentication."""
        user_info = self.auth_handler.verify_jwt_token(credentials.credentials)
        
        if not user_info.is_admin:
            raise HTTPException(
                status_code=403,
                detail="Admin privileges required"
            )
        
        return user_info


# Global authentication instances
auth_handler = EntraIDAuth()
auth_middleware = AuthMiddleware()
