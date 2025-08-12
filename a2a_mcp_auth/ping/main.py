"""Ping service FastAPI application."""

import logging
import time
import uuid
from datetime import datetime
from typing import Optional

import httpx
import uvicorn
from fastapi import Depends, FastAPI, HTTPException, Request
from fastapi.responses import JSONResponse, RedirectResponse
from fastapi.security import HTTPBearer
from jose import jwt, jwk
from jose.exceptions import JWTError

from a2a.server.apps import A2AFastAPIApplication
from a2a.server.context import ServerCallContext
from a2a.auth.user import User as A2AUser

from shared.auth import auth_handler, auth_middleware
from shared.config import settings
from shared.models import UserInfo
from shared.debug_utils import setup_debug_logging, log_auth_attempt, log_token_validation
from .handlers import PingHandler
from .ping_agent import create_ping_agent_card

# Setup debug logging
setup_debug_logging()

# Configure logging
logging.basicConfig(
    level=getattr(logging, settings.log_level.upper()),
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s"
)
logger = logging.getLogger(__name__)

# Security
security = HTTPBearer(auto_error=False)


class JWKSTokenValidator:
    """JWT token validator using Microsoft Entra ID JWKS for Ping service."""
    
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
                user_id="dev-user-ping",
                name="Development User (Ping)",
                email="dev-ping@example.com",
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
                logger.warning(f"Non-admin user {user_email} attempted ping service access")
                log_auth_attempt(user_email, False, {"type": "ping_service_access", "reason": "not_admin"})
                log_token_validation(token, payload, "User does not have admin role")
                raise HTTPException(status_code=403, detail="Admin role required")
            
            # 7. Return valid UserInfo
            logger.info(f"Admin user {user_email} granted ping service access via JWKS validation")
            log_auth_attempt(user_email, True, {"type": "ping_service_access", "admin": True, "method": "jwks"})
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
            log_auth_attempt("unknown", False, {"type": "ping_service_access", "error": str(e)})
            raise HTTPException(status_code=401, detail="Invalid token")
        except Exception as e:
            logger.error(f"Token verification error: {e}")
            log_token_validation(token, None, f"Token verification error: {e}")
            log_auth_attempt("unknown", False, {"type": "ping_service_access", "error": str(e)})
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


async def verify_admin_token_jwks(request: Request) -> UserInfo:
    """Verify JWT token using JWKS and check admin role for ping service."""
    try:
        # Extract JWT token from Authorization header
        auth_header = request.headers.get("authorization")
        if not auth_header or not auth_header.startswith("Bearer "):
            raise HTTPException(status_code=401, detail="Bearer token required")
        
        jwt_token = auth_header.split(" ")[1]
        
        # Verify the token using JWKS validation and check admin role
        user_info = await jwks_validator.verify_jwt_token(jwt_token)
        logger.info(f"JWKS validation successful for user {user_info.email} in ping service")
        
        return user_info
        
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"JWKS validation error in ping service: {e}")
        raise HTTPException(status_code=401, detail="Token validation failed")


class A2AUserProxy(A2AUser):
    """Proxy to adapt UserInfo to A2A User interface."""
    
    def __init__(self, user_info: UserInfo):
        self.user_info = user_info
    
    @property
    def is_authenticated(self) -> bool:
        return True
    
    @property
    def user_name(self) -> str:
        return self.user_info.name


class CustomCallContextBuilder:
    """Custom call context builder that includes authentication."""
    
    def build(self, request: Request) -> ServerCallContext:
        """Build server call context with authentication."""
        user = None
        metadata = {}
        
        # Try to get user from authorization header
        auth_header = request.headers.get("authorization")
        if auth_header and auth_header.startswith("Bearer "):
            token = auth_header.split(" ")[1]
            metadata["authorization"] = auth_header
            try:
                user_info = auth_middleware.auth_handler.verify_jwt_token(token)
                user = A2AUserProxy(user_info)
            except HTTPException:
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
    title="Ping Service",
    description="A2A Ping service with Microsoft Entra ID authentication",
    version="1.0.0",
    debug=settings.debug
)

# Add debug middleware if enabled
if settings.debug_requests:
    from shared.debug_utils import DebugMiddleware
    app.add_middleware(DebugMiddleware)

# Create agent card and handler
agent_card = create_ping_agent_card()
ping_handler = PingHandler()
context_builder = CustomCallContextBuilder()

# Create A2A application
a2a_app = A2AFastAPIApplication(
    agent_card=agent_card,
    http_handler=ping_handler,
    context_builder=context_builder
)

# Add A2A routes to the main app
a2a_app.add_routes_to_app(app)


@app.get("/auth/login")
async def login(request: Request):
    """Initiate OAuth2 login flow."""
    # Generate state parameter for security
    state = str(uuid.uuid4())
    
    # Store state in session (in production, use proper session management)
    auth_url = auth_handler.get_auth_url(state=state)
    
    logger.info("Redirecting to OAuth2 authorization URL for ping service")
    return RedirectResponse(url=auth_url)


@app.get("/auth/callback")
async def auth_callback(request: Request, code: str = None, state: str = None, error: str = None):
    """Handle OAuth2 callback."""
    if error:
        logger.error(f"OAuth2 error: {error}")
        raise HTTPException(status_code=400, detail=f"Authentication error: {error}")
    
    if not code:
        raise HTTPException(status_code=400, detail="Authorization code not provided")
    
    try:
        # Exchange code for token
        auth_token = await auth_handler.exchange_code_for_token(code)
        
        # Create JWT token
        jwt_token = auth_handler.create_jwt_token(auth_token.user_info)
        
        logger.info(f"User {auth_token.user_info.email} authenticated successfully")
        
        return JSONResponse({
            "message": "Authentication successful",
            "user": {
                "name": auth_token.user_info.name,
                "email": auth_token.user_info.email,
                "is_admin": auth_token.user_info.is_admin,
                "roles": auth_token.user_info.roles
            },
            "token": jwt_token,
            "token_type": "Bearer",
            "instructions": "Use this token in the Authorization header as 'Bearer <token>' for authenticated requests"
        })
        
    except HTTPException as e:
        logger.error(f"Authentication failed: {e.detail}")
        raise e
    except Exception as e:
        logger.error(f"Unexpected error during authentication: {str(e)}")
        raise HTTPException(status_code=500, detail="Authentication failed")


@app.post("/ping")
async def manual_ping(
    request: Request,
    user_info: UserInfo = Depends(verify_admin_token_jwks)
):
    """Manual ping endpoint that sends a ping via A2A protocol to pong service (requires admin role with JWKS validation)."""
    try:
        logger.info(f"Manual ping request from {user_info.name} (JWKS validated)")
        
        # Get auth token from request for forwarding
        auth_header = request.headers.get("authorization")
        
        results = {
            "message": "Ping sent to pong service via A2A protocol",
            "user": user_info.name,
            "user_id": user_info.user_id,
            "is_admin": user_info.is_admin,
            "timestamp": datetime.now().isoformat(),
            "pong_service_a2a": None
        }
        
        # Call pong service using A2A protocol only
        try:
            logger.info("Calling pong service via A2A protocol...")
            
            # Create A2A message for pong service - request includes MCP enhancement
            a2a_message_id = str(uuid.uuid4())
            context_id = str(uuid.uuid4())
            
            a2a_request = {
                "jsonrpc": "2.0",
                "id": str(uuid.uuid4()),
                "method": "message/send",
                "params": {
                    "message": {
                        "messageId": a2a_message_id,
                        "contextId": context_id,
                        "role": "user",
                        "parts": [
                            {
                                "kind": "text",
                                "text": f"ping from {user_info.name} via A2A protocol - please include MCP enhancement if admin user"
                            }
                        ]
                    }
                }
            }
            
            headers = {"Content-Type": "application/json"}
            if auth_header:
                headers["Authorization"] = auth_header
            
            async with httpx.AsyncClient() as client:
                pong_response = await client.post(
                    f"{settings.pong_service_url}/",  # A2A endpoint (root)
                    json=a2a_request,
                    headers=headers,
                    timeout=10.0
                )
                
                if pong_response.status_code == 200:
                    pong_data = pong_response.json()
                    results["pong_service_a2a"] = {
                        "status": "success",
                        "protocol": "A2A",
                        "response": pong_data,
                        "status_code": pong_response.status_code
                    }
                    logger.info("Pong service (A2A) responded successfully")
                else:
                    results["pong_service_a2a"] = {
                        "status": "error",
                        "protocol": "A2A",
                        "error": f"HTTP {pong_response.status_code}",
                        "response": pong_response.text[:200]
                    }
                    logger.warning(f"Pong service (A2A) returned {pong_response.status_code}")
                    
        except Exception as e:
            logger.error(f"Error calling pong service via A2A: {str(e)}")
            results["pong_service_a2a"] = {
                "status": "error",
                "protocol": "A2A",
                "error": str(e)
            }
        
        return results
        
    except HTTPException as e:
        raise e
    except Exception as e:
        logger.error(f"Error in manual ping: {str(e)}")
        raise HTTPException(status_code=500, detail="Failed to process ping")


@app.get("/health")
async def health_check():
    """Health check endpoint."""
    return {"status": "healthy", "service": "ping"}


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
        "service": "Ping Service",
        "version": "1.0.0",
        "description": "A2A Ping service with Microsoft Entra ID authentication",
        "architecture": "Pure A2A Protocol - communicates only via Agent-to-Agent protocol",
        "endpoints": {
            "agent_card": "/.well-known/agent.json",
            "a2a_rpc": "/",
            "login": "/auth/login", 
            "callback": "/auth/callback",
            "ping": "/ping",
            "health": "/health",
            "debug": "/debug"
        },
        "communication": {
            "protocol": "A2A (Agent-to-Agent)",
            "pong_service": settings.pong_service_url,
            "note": "Direct HTTP calls removed - uses A2A protocol exclusively"
        }
    }


def main():
    """Run the Ping service."""
    logger.info("Starting Ping service...")
    logger.info(f"Service URL: {settings.ping_service_url}")
    logger.info(f"Pong service URL: {settings.pong_service_url}")
    logger.info(f"Debug mode: {settings.debug}")
    
    # Extract port from URL
    port = 8000
    if ":" in settings.ping_service_url:
        port_str = settings.ping_service_url.split(":")[-1]
        try:
            port = int(port_str)
        except ValueError:
            pass
    
    uvicorn.run(
        "ping.main:app",
        host="0.0.0.0",
        port=port,
        reload=settings.debug,
        log_level=settings.log_level.lower()
    )


if __name__ == "__main__":
    main()
