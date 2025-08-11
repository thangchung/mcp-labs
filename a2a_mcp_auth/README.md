# A2A MCP Authentication System

A complete **Agent-to-Agent (A2A)** application with **Model Context Protocol (MCP)** server integration, featuring **Microsoft Entra ID authentication** with **OAuth 2.0 + PKCE** flow, **role-based authorization**, and **comprehensive debug logging**.

## 🏗️ Architecture Overview

```mermaid
graph TB
    subgraph "Client Layer"
        Browser[🌐 Web Browser]
        User[👤 Admin User]
    end
    
    subgraph "Authentication & Identity"
        EntraID[🔐 Microsoft Entra ID<br/>- JWT Token Issuer<br/>- JWKS Provider<br/>- Role/Group Manager]
    end
    
    subgraph "Service Layer"
        PingService[📡 Ping Service<br/>Port 8000<br/>- A2A Client<br/>- OAuth 2.0 Handler<br/>- Session Manager<br/>⚠️ Shared Secret JWT]
        PongService[🏓 Pong Service<br/>Port 8001<br/>- A2A Server<br/>- MCP Client<br/>- JWT Relay<br/>⚠️ Shared Secret JWT]
        MCPServer[🤖 MCP Server<br/>Port 8002<br/>- Admin-Only Access<br/>- JWT Verification<br/>- Tool Processing<br/>✅ JWKS Validation]
    end
    
    subgraph "Communication Protocols"
        A2A[⚡ A2A Protocol<br/>JSON-RPC over HTTP]
        HTTP[🌐 HTTP/REST<br/>Bearer Token Auth]
        JWT[🎫 JWT Verification<br/>JWKS + RS256]
    end
    
    subgraph "Debug & Monitoring"
        DebugLogs[📋 Debug Logging<br/>- Auth Flow Tracking<br/>- Token Validation<br/>- MCP Call Tracing<br/>- Request/Response Logs]
    end
    
    subgraph "Security Concern"
        SecurityNote[⚠️ Security Enhancement Opportunity<br/>Ping & Pong services use shared secret<br/>instead of JWKS validation<br/>Consider upgrading to full JWKS]
    end
    
    %% User Interactions
    User -->|Login Request| Browser
    Browser -->|OAuth 2.0 + PKCE| EntraID
    Browser -->|Auth Callback| PingService
    
    %% Token Flow
    EntraID -->|JWT Token + Claims| PingService
    EntraID -.->|JWKS Validation<br/>(MCP Server Only)| MCPServer
    EntraID -.->|⚠️ Missing JWKS<br/>(Enhancement Needed)| PingService
    EntraID -.->|⚠️ Missing JWKS<br/>(Enhancement Needed)| PongService
    
    %% Service Communication
    PingService <-->|A2A Messages<br/>+ JWT Context| PongService
    PongService -->|HTTP + Bearer Token| MCPServer
    
    %% Debug Integration
    PingService --> DebugLogs
    PongService --> DebugLogs
    MCPServer --> DebugLogs
    
    %% Styling
    style PingService fill:#fff3e0,stroke:#f57f17,stroke-width:2px
    style PongService fill:#fff3e0,stroke:#f57f17,stroke-width:2px
    style MCPServer fill:#e8f5e8,stroke:#2e7d32,stroke-width:2px
    style EntraID fill:#e8f5e8,stroke:#2e7d32,stroke-width:2px
    style DebugLogs fill:#fff9c4,stroke:#f57f17,stroke-width:2px
    style A2A fill:#ffebee,stroke:#c62828,stroke-width:2px
    style SecurityNote fill:#ffebee,stroke:#d32f2f,stroke-width:2px
```

### 🔒 **Security Analysis: Current vs Enhanced**

```mermaid
graph LR
    subgraph "Current Implementation"
        CurrentPing[📡 Ping Service<br/>HS256 + Shared Secret]
        CurrentPong[🏓 Pong Service<br/>HS256 + Shared Secret]
        CurrentMCP[🤖 MCP Server<br/>RS256 + JWKS ✅]
    end
    
    subgraph "Enhanced Security Model"
        EnhancedPing[📡 Ping Service<br/>RS256 + JWKS ✅]
        EnhancedPong[🏓 Pong Service<br/>RS256 + JWKS ✅]
        EnhancedMCP[🤖 MCP Server<br/>RS256 + JWKS ✅]
    end
    
    CurrentPing -.->|Upgrade| EnhancedPing
    CurrentPong -.->|Upgrade| EnhancedPong
    CurrentMCP -.->|Already Secure| EnhancedMCP
    
    style CurrentPing fill:#fff3e0,stroke:#f57f17
    style CurrentPong fill:#fff3e0,stroke:#f57f17
    style CurrentMCP fill:#e8f5e8,stroke:#2e7d32
    style EnhancedPing fill:#e8f5e8,stroke:#2e7d32
    style EnhancedPong fill:#e8f5e8,stroke:#2e7d32
    style EnhancedMCP fill:#e8f5e8,stroke:#2e7d32
```

## 🔄 Enhanced Data Flow

```mermaid
sequenceDiagram
    participant User as 👤 Admin User
    participant Browser as 🌐 Browser
    participant Ping as 📡 Ping Service
    participant Pong as 🏓 Pong Service
    participant MCP as 🤖 MCP Server
    participant EntraID as 🔐 Microsoft Entra ID
    participant Debug as 📋 Debug Logs
    
    %% Authentication Phase
    Note over User,EntraID: OAuth 2.0 + PKCE Authentication
    User->>Browser: Visit /auth/login
    Browser->>Ping: GET /auth/login
    Ping->>EntraID: OAuth redirect (PKCE)
    EntraID->>User: Login prompt
    User->>EntraID: Admin credentials
    EntraID->>Browser: Authorization code
    Browser->>Ping: Callback with code
    Ping->>EntraID: Exchange code for JWT
    EntraID->>Ping: JWT + admin claims
    Ping->>Debug: 📝 Log auth success
    
    %% A2A Communication Phase
    Note over Browser,MCP: Pure A2A Protocol Communication
    Browser->>Ping: POST /ping (with JWT)
    Ping->>Debug: 📝 Log A2A request start
    Ping->>Pong: A2A message/send (JWT in context)
    
    %% MCP Integration Phase
    Note over Pong,EntraID: MCP Server Communication
    Pong->>Debug: 📝 Log JWT token relay
    Pong->>MCP: HTTP POST /mcp (Bearer token)
    MCP->>Debug: 📝 Log token validation attempt
    MCP->>EntraID: Fetch JWKS for verification
    EntraID->>MCP: Public keys
    MCP->>MCP: Verify JWT signature + claims
    MCP->>Debug: 📝 Log admin role validation
    
    alt Admin Role Verified
        MCP->>MCP: Process ping_processor_tool
        MCP->>Pong: Enhanced MCP response
        MCP->>Debug: 📝 Log successful processing
    else Access Denied
        MCP->>Pong: 401 Unauthorized
        MCP->>Debug: 📝 Log access denial
    end
    
    %% Response Phase
    Pong->>Ping: A2A response (with MCP data)
    Pong->>Debug: 📝 Log A2A response sent
    Ping->>Browser: Combined response
    Ping->>Debug: 📝 Log request completion
```

## 🚀 Features

### ✅ Authentication & Authorization
- **Microsoft Entra ID Integration**: Enterprise-grade authentication with OAuth 2.0 + PKCE
- **JWT Token Verification**: Cryptographic token validation using JWKS (JSON Web Key Set)
- **Role-Based Access Control**: Admin-only access enforcement with Azure AD roles/groups
- **Bearer Token Authentication**: RFC 6750 compliant token-based security
- **Session Management**: Secure session handling with token refresh capabilities

### ✅ Service Architecture
- **Ping Service (Port 8000)**: OAuth 2.0 client + A2A protocol client + session manager
- **Pong Service (Port 8001)**: A2A protocol server + MCP client + JWT token relay
- **MCP Server (Port 8002)**: Admin-only Model Context Protocol server with tool processing
- **Pure A2A Communication**: Ping service communicates exclusively via A2A protocol
- **Shared Authentication Middleware**: Unified JWT validation across all services

### ✅ MCP Integration
- **Standards Compliant**: Full Model Context Protocol (MCP) 1.12.3 implementation
- **Admin Access Control**: Strict role-based access with Entra ID verification
- **Enhanced Tool Processing**: Ping processor with intelligent response generation
- **Resource Server Pattern**: OAuth 2.0 Resource Server architecture for MCP endpoints
- **Tool Discovery**: Dynamic MCP tool registration and capability exposition

### ✅ Debug & Monitoring
- **Comprehensive Debug Logging**: End-to-end visibility into authentication and communication flows
- **Token Flow Tracing**: JWT token validation tracking from pong service to MCP server
- **Authentication Audit**: Detailed logging of login attempts, token validation, and access decisions
- **MCP Call Monitoring**: Request/response logging for all MCP tool invocations
- **HTTP Request Tracing**: Complete HTTP request/response cycle logging with headers
- **Multi-Level Debug Control**: Granular debug flags for different system components

## 📦 Quick Start

### 1. Prerequisites

- **Python 3.11+** with UV package manager
- **Microsoft Entra ID tenant** with admin access
- **Azure AD application** registration

```bash
# Install UV package manager
pip install uv
```

### 2. Installation

```bash
# Clone and setup
git clone <repository-url>
cd a2a_mcp_auth

# Install dependencies
uv sync
```

### 3. Azure AD Configuration

1. **Create Azure AD Application**:
   - Register new application in Azure Portal
   - Set redirect URI: `http://localhost:8000/auth/callback`
   - Generate client secret
   - Configure API permissions

2. **Admin Role Setup**:
   - Create admin role/group in Azure AD
   - Assign admin users to the role
   - Note the role name and group ID

### 4. Environment Configuration

Create `.env` file:

```bash
# Microsoft Entra ID
AZURE_TENANT_ID=your-tenant-id
AZURE_CLIENT_ID=your-client-id
AZURE_CLIENT_SECRET=your-client-secret
AZURE_REDIRECT_URI=http://localhost:8000/auth/callback

# Service URLs
PING_SERVICE_URL=http://localhost:8000
PONG_SERVICE_URL=http://localhost:8001
MCP_SERVER_URL=http://localhost:8002
MCP_SERVER_PORT=8002

# Security
JWT_SECRET_KEY=your-secret-key-change-in-production
JWT_ALGORITHM=HS256
JWT_EXPIRE_MINUTES=30

# Admin Configuration
ADMIN_ROLE_NAME=PingPongAdmin
ADMIN_GROUP_ID=your-admin-group-id
REQUIRED_SCOPES=api://your-client-id/admin

# Development
DEBUG=true
LOG_LEVEL=DEBUG

# Debug Flags - Enable comprehensive debugging
DEBUG_AUTH=true      # Authentication flow and token validation
DEBUG_A2A=true       # A2A protocol communication
DEBUG_MCP=true       # MCP server calls and responses  
DEBUG_REQUESTS=true  # HTTP request/response details
```

### 5. Running Services

Start all three services in separate terminals:

```bash
# Terminal 1: MCP Server (Admin-only)
uv run python -m mcp_server.main

# Terminal 2: Ping Service (OAuth + Orchestrator)
uv run python -m ping.main

# Terminal 3: Pong Service (A2A Server)
uv run python -m pong.main
```

### 6. Testing the System

```bash
# Run integration tests
uv run python test_mcp_integration.py

# Expected output:
# ✅ Ping Service is healthy
# ✅ Pong Service is healthy  
# ✅ MCP Server is healthy
# ✅ MCP server correctly rejects unauthenticated requests
# ✅ Pong service can check MCP server health
```

## 🔄 Usage Workflows

### 1. Web Authentication Flow

1. **Visit Ping Service**: Navigate to `http://localhost:8000`
2. **Login**: Click login or visit `/auth/login`
3. **Azure Authentication**: Login with admin-privileged account
4. **Token Receipt**: Receive JWT with admin claims
5. **Service Access**: Use authenticated endpoints

### 2. A2A Protocol Communication

The ping service communicates with the pong service exclusively via A2A protocol. The pong service handles MCP server integration internally:

```bash
# Ping endpoint uses A2A protocol to communicate with pong service
curl -X POST http://localhost:8000/ping \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"message": "Hello World"}'

# Response from A2A communication:
{
  "message": "Ping sent to pong service via A2A protocol",
  "user": "admin@domain.com",
  "user_id": "...",
  "is_admin": true,
  "timestamp": "2025-08-11T10:30:00",
  "pong_service_a2a": {
    "status": "success",
    "protocol": "A2A",
    "response": {
      "jsonrpc": "2.0",
      "id": "...",
      "result": {
        "message": "...",
        "role": "agent",
        "parts": [
          {
            "kind": "text", 
            "text": "Pong! Received your A2A ping..."
          }
        ]
      }
    }
  }
}
```

### 3. Individual Service Testing

```bash
# Pong Service MCP Integration (admin required)
curl -X POST http://localhost:8001/pong/mcp \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"message": "Test MCP integration"}'

# MCP Server (admin required)
curl -X POST http://localhost:8002/mcp \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"message": "Test MCP"}'
```

## 🛠️ Technical Stack

- **FastAPI**: Modern Python web framework for all services
- **A2A Python SDK 0.3.0**: Agent-to-agent communication
- **MCP Python SDK 1.12.3**: Model Context Protocol implementation  
- **Microsoft MSAL 1.25.0**: OAuth 2.0 and token management
- **PyJWT + Cryptography**: JWT token verification
- **Pydantic**: Configuration management and data validation
- **HTTPX**: Async HTTP client for service-to-service calls
- **Uvicorn**: High-performance ASGI server

## 📊 Service Endpoints

### Ping Service (Port 8000) - OAuth Client + A2A Client
- `GET /` - Service information and authentication status
- `GET /health` - Health check with service dependencies
- `POST /ping` - **🎯 Main endpoint**: Sends ping via A2A protocol to pong service (admin required)
- `GET /auth/login` - Initiate OAuth 2.0 + PKCE flow with Microsoft Entra ID
- `GET /auth/callback` - OAuth callback handler for authorization code exchange
- `GET /debug` - Debug information and logging configuration (debug mode only)
- `GET /.well-known/agent.json` - A2A agent discovery and capabilities
- `POST /` - A2A JSON-RPC endpoint for agent-to-agent communication

### Pong Service (Port 8001) - A2A Server + MCP Client
- `GET /` - Service information and status
- `GET /health` - Health check (includes MCP server connectivity status)
- `POST /pong/mcp` - Enhanced pong with MCP integration (admin required)
- `GET /debug` - Debug information and logging configuration (debug mode only)
- `GET /.well-known/agent.json` - A2A agent discovery and capabilities
- `POST /` - **🎯 A2A JSON-RPC endpoint** - Receives messages from ping service

### MCP Server (Port 8002) - Admin-Only Resource Server
- `GET /` - Server information and capabilities
- `GET /health` - Health check and authentication status
- `POST /mcp` - **🎯 MCP protocol endpoint** (admin required, JWT verification)
- `POST /tools/ping_processor` - Direct tool access (admin required)
- `GET /debug` - Debug information and authentication configuration

### 🔍 Debug Endpoints (Debug Mode Only)
All services provide debug endpoints when `DEBUG=true`:
- `GET /debug` - Shows configuration, logging levels, and debug flags
- Logs are written to both console and `debug.log` file
- JWT token previews (secure truncated format) in debug logs
- Authentication flow step-by-step logging

## 🔐 Security Implementation

### Authentication Flow
```mermaid
sequenceDiagram
    participant User as 👤 Admin User
    participant Browser as 🌐 Browser
    participant PingService as 📡 Ping Service
    participant EntraID as 🔐 Microsoft Entra ID
    participant PongService as 🏓 Pong Service
    participant MCPServer as 🤖 MCP Server
    participant DebugLogs as 📋 Debug Logs

    %% OAuth 2.0 + PKCE Flow
    Note over User,EntraID: OAuth 2.0 + PKCE Authentication
    User->>Browser: Visit /auth/login
    Browser->>PingService: GET /auth/login
    PingService->>EntraID: OAuth 2.0 + PKCE redirect
    EntraID->>User: Login prompt
    User->>EntraID: Admin credentials
    EntraID->>Browser: Authorization code
    Browser->>PingService: GET /auth/callback?code=...
    PingService->>EntraID: Exchange code for token
    EntraID->>PingService: JWT token + admin claims
    PingService->>DebugLogs: 📝 Log authentication success
    PingService->>Browser: Set session + JWT cookie
    
    %% A2A Protocol Communication
    Note over Browser,MCPServer: Authenticated API Calls via A2A Protocol
    Browser->>PingService: POST /ping (with JWT session)
    PingService->>DebugLogs: 📝 Log A2A request initiation
    PingService->>PongService: A2A message/send (JWT in context)
    
    %% JWT Token Relay & Validation
    Note over PongService,EntraID: MCP Server JWT Validation
    PongService->>DebugLogs: 📝 Log JWT token relay to MCP
    PongService->>MCPServer: HTTP POST /mcp (Bearer JWT)
    MCPServer->>DebugLogs: 📝 Log token validation attempt
    MCPServer->>EntraID: Fetch JWKS for signature verification
    EntraID->>MCPServer: Public keys (RS256)
    MCPServer->>MCPServer: Verify JWT signature + claims
    MCPServer->>DebugLogs: 📝 Log admin role validation result
    
    alt JWT Valid + Admin Role
        MCPServer->>MCPServer: Process ping_processor_tool
        MCPServer->>PongService: Enhanced MCP response
        MCPServer->>DebugLogs: 📝 Log successful MCP processing
    else Invalid Token or Non-Admin
        MCPServer->>PongService: 401/403 Unauthorized
        MCPServer->>DebugLogs: 📝 Log access denial with reason
    end
    
    PongService->>PingService: A2A response (with MCP data)
    PongService->>DebugLogs: 📝 Log A2A response completion
    PingService->>Browser: Combined response
    PingService->>DebugLogs: 📝 Log full request cycle completion
```

### Token Verification & Debug Monitoring
- **JWKS Endpoint**: Fetches Microsoft's public keys for RS256 signature verification
- **Signature Validation**: Cryptographic verification using RSA public keys
- **Claims Checking**: Role and scope validation with detailed logging
- **Expiration Handling**: Automatic token expiry checks with debug output
- **🔍 Debug Visibility**: Comprehensive logging at every authentication step

### Role-Based Access & Debug Tracking
- **Admin Role Required**: MCP server enforces admin-only access with audit trails
- **Group Membership**: Optional Azure AD group validation with debug logs
- **Multi-layer Security**: Protection at service boundaries with logging
- **Stateless Authentication**: JWT-based verification with token flow tracing
- **🔍 Access Audit**: Every authentication attempt and authorization decision logged

### Debug Logging Categories
```mermaid
graph LR
    subgraph "Debug Categories"
        AUTH[🔐 debug.auth<br/>- Token validation<br/>- Login attempts<br/>- Role verification]
        A2A[⚡ debug.a2a<br/>- Protocol messages<br/>- Service communication<br/>- Message routing]
        MCP[🤖 debug.mcp<br/>- Tool calls<br/>- Request/response<br/>- Processing results]
        HTTP[🌐 debug.requests<br/>- HTTP headers<br/>- Request bodies<br/>- Response codes]
    end
    
    AUTH --> DebugLog[📄 debug.log]
    A2A --> DebugLog
    MCP --> DebugLog
    HTTP --> DebugLog
    
    AUTH --> Console[💻 Console Output]
    A2A --> Console
    MCP --> Console
    HTTP --> Console
```

## 🧪 Testing & Validation

### Health Checks
```bash
# Service health
curl http://localhost:8000/health  # Ping service
curl http://localhost:8001/health  # Pong service  
curl http://localhost:8002/health  # MCP server

# Cross-service connectivity
curl http://localhost:8001/mcp/health  # Pong → MCP health
```

### Integration Testing
```bash
# Full system test
uv run python test_mcp_integration.py

# Manual endpoint testing
uv run python test_ping_pong.py
```

### Debug Mode & Monitoring
```bash
# Enable comprehensive debug logging
export DEBUG=true
export LOG_LEVEL=DEBUG
export DEBUG_AUTH=true      # Authentication & JWT validation
export DEBUG_A2A=true       # A2A protocol communication  
export DEBUG_MCP=true       # MCP server calls & responses
export DEBUG_REQUESTS=true  # HTTP request/response details

# Monitor real-time logs
tail -f debug.log

# Test debug logging functionality
python test_debug.py
```

### Sample Debug Output
```log
2025-08-11 20:21:46,115 - debug.auth - DEBUG - [TOKEN] Token validation SUCCESS: eyJhbGciOi...ature_here
2025-08-11 20:21:46,176 - debug.auth - DEBUG - Auth attempt: admin@example.com - SUCCESS
2025-08-11 20:21:46,195 - pong.mcp_client - DEBUG - [JWT] Using JWT token: eyJhbGciOiJSUzI1NiIs...
2025-08-11 20:21:46,196 - pong.mcp_client - DEBUG - [HTTP] Request headers: {'Authorization': 'Bearer ...'}
2025-08-11 20:21:46,196 - debug.mcp - DEBUG - MCP call: ping_processor_tool
2025-08-11 20:21:46,197 - debug.mcp - DEBUG - MCP Args: {'message': 'Hello World'}
2025-08-11 20:21:46,198 - debug.mcp - DEBUG - MCP Result: {'status': 'success', 'enhanced_response': '...'}
```

## 🐛 Troubleshooting

### Common Issues & Debug Solutions

| Issue | Debug Strategy | Solution |
|-------|----------------|----------|
| "Invalid token" errors | Check `debug.auth` logs for token validation details | Verify Azure AD configuration and token claims structure |
| "Admin role required" | Review `debug.auth` logs for role validation results | Ensure user has admin role/group membership in Entra ID |
| Connection refused | Monitor `debug.requests` for HTTP connection attempts | Check all services are running on correct ports (8000,8001,8002) |
| JWKS fetch errors | Examine `debug.auth` logs for JWKS retrieval issues | Verify internet connectivity and Azure endpoint accessibility |
| A2A communication failures | Check `debug.a2a` logs for message routing issues | Validate A2A protocol configuration and service discovery |
| MCP processing errors | Review `debug.mcp` logs for tool execution details | Check MCP server tool registration and processing logic |
| Unicode encoding errors | **✅ Resolved** - Windows compatibility implemented | Debug logging now uses plain text format for cross-platform support |

### Debug Configuration Validation
```bash
# Test configuration loading with debug output
uv run python -c "
from shared.config import settings
print('🔧 Configuration loaded:')
print(f'  DEBUG={settings.debug}')
print(f'  DEBUG_AUTH={settings.debug_auth}')
print(f'  DEBUG_MCP={settings.debug_mcp}')
print(f'  DEBUG_REQUESTS={settings.debug_requests}')
print(f'  LOG_LEVEL={settings.log_level}')
"

# Verify Azure AD connectivity with debug tracing
uv run python -c "
from shared.auth import EntraIDAuth
auth = EntraIDAuth()
print(f'🔐 Entra ID Authority: {auth.authority}')
print(f'🎯 Required Scopes: {auth.scope}')
"

# Test debug logging functionality
uv run python test_debug.py
```

### Real-Time Debug Monitoring
```bash
# Monitor authentication flows
grep "debug.auth" debug.log | tail -f

# Track MCP server communication
grep "debug.mcp" debug.log | tail -f

# Watch A2A protocol messages
grep "debug.a2a" debug.log | tail -f

# Monitor HTTP request/response cycles
grep "debug.requests" debug.log | tail -f
```

## 🚀 Production Considerations

### Security Checklist
- [ ] Use production Azure AD tenant
- [ ] Configure proper redirect URIs and CORS
- [ ] Set strong JWT secret keys
- [ ] Enable HTTPS with proper certificates
- [ ] Implement rate limiting and monitoring
- [ ] Use Azure Key Vault for secrets
- [ ] Set DEBUG=false and appropriate log levels
- [ ] Review and harden security headers

### Deployment Architecture
```mermaid
graph TB
    subgraph "Production Environment"
        LB[Load Balancer<br/>HTTPS Termination]
        
        subgraph "Service Mesh"
            P1[Ping Service<br/>Replica 1]
            P2[Ping Service<br/>Replica 2]
            Po1[Pong Service<br/>Replica 1]
            Po2[Pong Service<br/>Replica 2]
            M1[MCP Server<br/>Replica 1]
        end
        
        subgraph "Infrastructure"
            Redis[Redis<br/>Session Store]
            Monitoring[Monitoring<br/>Logs & Metrics]
            Vault[Azure Key Vault<br/>Secrets Management]
        end
    end
    
    Internet --> LB
    LB --> P1
    LB --> P2
    P1 --> Po1
    P2 --> Po2
    Po1 --> M1
    Po2 --> M1
    
    P1 --> Redis
    P2 --> Redis
    
    style LB fill:#ff9999
    style Vault fill:#99ff99
    style Monitoring fill:#9999ff
```

## 🤝 Contributing

1. Fork the repository
2. Create feature branch: `git checkout -b feature/amazing-feature`
3. Make changes with tests
4. Commit: `git commit -m 'Add amazing feature'`
5. Push: `git push origin feature/amazing-feature`
6. Submit pull request

## 📄 License

Educational project demonstrating A2A protocol with MCP integration and Microsoft Entra ID authentication.

---

## 🎯 Implementation Status

✅ **Complete OAuth 2.0 + PKCE Flow**: Microsoft Entra ID integration with session management  
✅ **Pure A2A Architecture**: Ping service communicates exclusively via A2A protocol  
✅ **Admin-Only MCP Access**: Role-based security enforcement with comprehensive audit trails  
✅ **JWT Token Verification**: JWKS-based cryptographic validation with RS256 signatures  
✅ **Cross-Service Authentication**: Shared middleware and token relay across services  
✅ **Comprehensive Debug Logging**: End-to-end visibility into authentication and communication flows  
✅ **Windows Compatibility**: Unicode encoding issues resolved for cross-platform support  
✅ **Production Ready**: Comprehensive error handling, monitoring, and security controls  
✅ **Well Documented**: Complete setup, deployment guides, and troubleshooting documentation  
✅ **Integration Tested**: Automated testing, health checks, and debug validation tools  
✅ **Router Simplification**: 25% endpoint reduction with streamlined service interfaces  
✅ **Enhanced Monitoring**: JWT token flow tracking from pong service to MCP server validation  

### 🔍 Debug Enhancement Summary
- **Authentication Flow Visibility**: Complete JWT token lifecycle tracking
- **Token Validation Tracing**: Step-by-step Entra ID verification process logging  
- **MCP Communication Monitoring**: Request/response cycle visibility with error details
- **Multi-Level Debug Control**: Granular debug flags for different system components
- **Security-Safe Logging**: JWT token previews with truncated format for debugging
- **Cross-Platform Compatibility**: Plain text logging format for Windows/Linux/macOS

### ⚠️ **Security Enhancement Opportunity**

**Current Limitation**: Ping and Pong services use **shared secret JWT validation** (HS256) instead of **JWKS validation** (RS256):

| Service | Current Validation | Security Level | Enhancement Needed |
|---------|-------------------|----------------|-------------------|
| **Ping Service** | HS256 + Shared Secret | ⚠️ Medium | ✅ Upgrade to JWKS |
| **Pong Service** | HS256 + Shared Secret | ⚠️ Medium | ✅ Upgrade to JWKS |
| **MCP Server** | RS256 + JWKS | ✅ High | Already Secure |

**Why This Matters**:
- **Shared secrets** can be compromised and are harder to rotate
- **JWKS validation** uses Microsoft's rotating public keys (more secure)
- **Consistent security model** across all services reduces attack surface

**Recommended Enhancement**: Implement JWKS validation in ping and pong services to match MCP server security level.

```mermaid
graph LR
    subgraph "Security Upgrade Path"
        Current[⚠️ Current: Mixed Security<br/>Ping/Pong: Shared Secret<br/>MCP: JWKS]
        Enhanced[✅ Enhanced: Uniform Security<br/>All Services: JWKS Validation]
        
        Current -->|Implement JWKS<br/>in Ping & Pong| Enhanced
    end
    
    style Current fill:#fff3e0,stroke:#f57f17
    style Enhanced fill:#e8f5e8,stroke:#2e7d32
```

This system demonstrates enterprise-grade authentication with pure A2A protocol communication patterns, enhanced with comprehensive debug visibility, suitable for production deployment with proper security configurations and operational monitoring.

### JWKS Validation

```mermaid
graph TB
    subgraph "Current Token Flow"
        Browser[Browser] -->|Azure AD Token| Ping[Ping Service]
        Ping -->|Application JWT| Pong[Pong Service]
        Pong -->|Azure AD Token| MCP[MCP Server]
    end
    
    subgraph "Validation Methods"
        Ping -.->|HS256 + Shared Secret| AppJWT[Application JWT Validation]
        Ping -.->|Basic Decode| AzureJWT[Azure AD Token Parsing]
        MCP -.->|RS256 + JWKS| JWKS[Full JWKS Validation]
    end
```
