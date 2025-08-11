# Security Enhancement Plan: JWKS Validation for All Services

## Current Security Assessment

### ❌ **Security Gap Identified**
The ping and pong services currently use **shared secret JWT validation** while only the MCP server uses **JWKS validation**. This creates an inconsistent security model with potential vulnerabilities.

## Current Implementation Analysis

### **Ping Service (Port 8000)**
```python
# Current: Uses shared secret validation in shared/auth.py
payload = jwt.decode(
    token,
    settings.jwt_secret_key,    # ❌ Shared secret
    algorithms=[settings.jwt_algorithm]  # HS256
)
```

### **Pong Service (Port 8001)**
```python
# Current: Same shared secret validation
user_info = auth_middleware.auth_handler.verify_jwt_token(jwt_token)
# Falls back to shared secret validation
```

### **MCP Server (Port 8002)**
```python
# Current: Proper JWKS validation ✅
payload = jwt.decode(
    token,
    public_key,  # ✅ Microsoft's public key from JWKS
    algorithms=["RS256"],  # ✅ Asymmetric encryption
    issuer=f"https://login.microsoftonline.com/{tenant_id}/v2.0",
    audience=client_id
)
```

## Security Risks of Current Approach

1. **Shared Secret Vulnerability**: Single point of failure if JWT_SECRET_KEY is compromised
2. **Key Rotation Difficulty**: Manual key rotation required vs automatic JWKS rotation
3. **Inconsistent Security**: Different validation methods across services
4. **Token Forgery Risk**: Easier to forge tokens with known shared secret

## Recommended Enhancement: Uniform JWKS Validation

### **Phase 1: Enable JWKS in Ping Service**

```python
# Enhanced ping/main.py
from shared.auth import EntraIDAuth

@app.middleware("http")
async def auth_middleware(request: Request, call_next):
    if request.url.path in ["/health", "/", "/.well-known/agent.json"]:
        return await call_next(request)
    
    # Use JWKS validation for all JWT tokens
    auth_header = request.headers.get("authorization")
    if auth_header and auth_header.startswith("Bearer "):
        token = auth_header.split(" ")[1]
        try:
            # ✅ Use JWKS validation instead of shared secret
            user_info = await auth_handler.verify_azure_ad_token_with_jwks(token)
            request.state.user = user_info
        except Exception as e:
            raise HTTPException(status_code=401, detail="Invalid token")
    
    return await call_next(request)
```

### **Phase 2: Enable JWKS in Pong Service**

```python
# Enhanced pong/main.py
async def require_auth(request: Request):
    auth_header = request.headers.get("authorization")
    if not auth_header or not auth_header.startswith("Bearer "):
        raise HTTPException(status_code=401, detail="Authorization header required")
    
    token = auth_header.split(" ")[1]
    try:
        # ✅ Use JWKS validation for pong service too
        user_info = await auth_middleware.auth_handler.verify_azure_ad_token_with_jwks(token)
        return user_info
    except Exception as e:
        raise HTTPException(status_code=401, detail="Invalid token")
```

### **Phase 3: Enhanced Auth Module**

```python
# Enhanced shared/auth.py
class EntraIDAuth:
    async def verify_azure_ad_token_with_jwks(self, token: str) -> UserInfo:
        """Verify Azure AD token using JWKS - same as MCP server."""
        try:
            # Get Microsoft's public keys
            jwks = await self._get_microsoft_jwks()
            
            # Decode JWT header to get key ID
            unverified_header = jwt.get_unverified_header(token)
            kid = unverified_header.get("kid")
            
            # Find corresponding public key
            public_key = None
            for key in jwks.get("keys", []):
                if key.get("kid") == kid:
                    public_key = jwk.construct(key)
                    break
            
            if not public_key:
                raise JWTError(f"No public key found for kid: {kid}")
            
            # Verify JWT signature and claims
            payload = jwt.decode(
                token,
                public_key,
                algorithms=["RS256"],
                issuer=f"https://login.microsoftonline.com/{self.tenant_id}/v2.0",
                audience=self.client_id
            )
            
            # Extract user information and check admin role
            return await self._extract_user_info_from_payload(payload)
            
        except Exception as e:
            log_token_validation(token, None, str(e))
            raise HTTPException(status_code=401, detail="Token validation failed")
```

## Implementation Steps

### **Step 1: Backup Current Implementation**
```bash
# Create backup branch
git checkout -b backup/shared-secret-auth

# Commit current state
git add .
git commit -m "Backup: Current shared secret authentication"
```

### **Step 2: Add JWKS Support to Shared Auth**
1. Copy JWKS functionality from MCP server to shared auth module
2. Add `verify_azure_ad_token_with_jwks` method
3. Maintain backward compatibility with shared secret validation

### **Step 3: Update Ping Service**
1. Replace shared secret validation with JWKS validation
2. Update middleware to use new validation method
3. Test OAuth flow still works correctly

### **Step 4: Update Pong Service**
1. Replace shared secret validation with JWKS validation
2. Update A2A message handling to use JWKS
3. Test MCP integration still works

### **Step 5: Configuration Updates**
```bash
# Update .env file
# Remove or deprecate
JWT_SECRET_KEY=deprecated-no-longer-used

# Ensure Azure AD configuration is complete
AZURE_TENANT_ID=your-tenant-id
AZURE_CLIENT_ID=your-client-id
AZURE_CLIENT_SECRET=your-client-secret
```

### **Step 6: Testing & Validation**
```bash
# Test full authentication flow
python test_enhanced_security.py

# Verify all services use JWKS
python -c "
from shared.auth import EntraIDAuth
auth = EntraIDAuth()
print('✅ All services now use JWKS validation')
"
```

## Benefits of Enhanced Security

1. **✅ Uniform Security Model**: All services use same validation method
2. **✅ Automatic Key Rotation**: Microsoft handles key rotation automatically
3. **✅ Stronger Cryptography**: RS256 vs HS256 provides better security
4. **✅ Reduced Attack Surface**: No shared secrets to compromise
5. **✅ Enterprise Grade**: Matches industry best practices

## Testing Strategy

### **Security Testing**
```bash
# Test with invalid tokens
curl -H "Authorization: Bearer invalid-token" http://localhost:8000/ping

# Test with expired tokens
curl -H "Authorization: Bearer expired-token" http://localhost:8001/pong/mcp

# Test token signature verification
python test_jwks_validation.py
```

### **Integration Testing**
```bash
# Test full OAuth → A2A → MCP flow
python test_end_to_end_security.py

# Verify debug logging shows JWKS validation
tail -f debug.log | grep "JWKS"
```

## Migration Timeline

- **Week 1**: Implement JWKS support in shared auth module
- **Week 2**: Update ping service to use JWKS validation
- **Week 3**: Update pong service to use JWKS validation  
- **Week 4**: Full testing and deployment

## Rollback Plan

If issues arise, the shared secret validation can be re-enabled by:
1. Reverting to backup branch
2. Re-enabling `JWT_SECRET_KEY` in configuration
3. Switching back to `verify_jwt_token` method

This enhancement will bring the ping and pong services up to the same security standard as the MCP server, creating a consistent and robust authentication architecture.
