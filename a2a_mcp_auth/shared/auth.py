"""Microsoft Entra ID authentication and authorization module."""

import logging
from datetime import datetime, timedelta
from typing import Optional

import httpx
import msal
from fastapi import HTTPException, Request
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer
from jose import JWTError, jwt

from .config import settings
from .models import AuthToken, UserInfo

logger = logging.getLogger(__name__)


class EntraIDAuth:
    """Microsoft Entra ID authentication handler."""
    
    def __init__(self) -> None:
        """Initialize the Entra ID authentication handler."""
        self.authority = f"https://login.microsoftonline.com/{settings.azure_tenant_id}"
        self.scope = ["openid", "profile", "email", settings.required_scopes]
        
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
            
            # Get user info from token claims
            user_info = await self._extract_user_info(result["access_token"])
            
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
    
    async def _extract_user_info(self, access_token: str) -> UserInfo:
        """Extract user information from access token."""
        try:
            # Call Microsoft Graph API to get user info
            headers = {"Authorization": f"Bearer {access_token}"}
            
            async with httpx.AsyncClient() as client:
                # Get user profile
                response = await client.get(
                    "https://graph.microsoft.com/v1.0/me",
                    headers=headers
                )
                response.raise_for_status()
                user_data = response.json()
                
                # Get user's group memberships to check for admin role
                groups_response = await client.get(
                    "https://graph.microsoft.com/v1.0/me/memberOf",
                    headers=headers
                )
                groups_response.raise_for_status()
                groups_data = groups_response.json()
                
                # Extract roles from groups
                roles = []
                is_admin = False
                
                for group in groups_data.get("value", []):
                    if group.get("@odata.type") == "#microsoft.graph.group":
                        group_name = group.get("displayName", "")
                        roles.append(group_name)
                        if group_name == settings.admin_role_name:
                            is_admin = True
                
                return UserInfo(
                    user_id=user_data.get("id", ""),
                    email=user_data.get("mail") or user_data.get("userPrincipalName", ""),
                    name=user_data.get("displayName", ""),
                    roles=roles,
                    is_admin=is_admin,
                )
                
        except Exception as e:
            logger.error(f"Failed to extract user info: {str(e)}")
            raise HTTPException(
                status_code=500,
                detail="Failed to retrieve user information"
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
        """Verify and decode a JWT token."""
        try:
            payload = jwt.decode(
                token,
                settings.jwt_secret_key,
                algorithms=[settings.jwt_algorithm]
            )
            
            return UserInfo(
                user_id=payload["sub"],
                email=payload["email"],
                name=payload["name"],
                roles=payload.get("roles", []),
                is_admin=payload.get("is_admin", False),
            )
            
        except JWTError as e:
            logger.error(f"JWT verification failed: {str(e)}")
            raise HTTPException(
                status_code=401,
                detail="Invalid authentication token"
            )


class AuthMiddleware:
    """Authentication middleware for FastAPI."""
    
    def __init__(self) -> None:
        """Initialize the authentication middleware."""
        self.auth_handler = EntraIDAuth()
        self.security = HTTPBearer(auto_error=False)
    
    async def get_current_user(
        self, 
        request: Request,
        credentials: Optional[HTTPAuthorizationCredentials] = None
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
        request: Request,
        credentials: Optional[HTTPAuthorizationCredentials] = None
    ) -> UserInfo:
        """Require authentication."""
        if not credentials:
            raise HTTPException(
                status_code=401,
                detail="Authentication required",
                headers={"WWW-Authenticate": "Bearer"},
            )
        
        user_info = self.auth_handler.verify_jwt_token(credentials.credentials)
        return user_info
    
    async def require_admin(
        self,
        request: Request,
        credentials: Optional[HTTPAuthorizationCredentials] = None
    ) -> UserInfo:
        """Require admin authentication."""
        user_info = await self.require_auth(request, credentials)
        
        if not user_info.is_admin:
            raise HTTPException(
                status_code=403,
                detail="Admin privileges required"
            )
        
        return user_info


# Global authentication instances
auth_handler = EntraIDAuth()
auth_middleware = AuthMiddleware()
