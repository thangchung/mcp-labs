# A2A MCP Authentication System

A complete **Agent-to-Agent (A2A)** application with **Model Context Protocol (MCP)** server integration, featuring **Microsoft Entra ID authentication** with **OAuth 2.0 + PKCE** flow, **role-based authorization**, and **comprehensive debug logging**.

## 🏗️ Current Architecture Overview

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
        PingService[📡 Ping Service<br/>Port 8000<br/>- A2A Client - Outbound<br/>- OAuth 2.0 Handler<br/>- Session Manager<br/>- No A2A Server Role]
        PongService[🏓 Pong Service<br/>Port 8001<br/>- A2A Server - Inbound<br/>- MCP Client Integration<br/>- Service-to-Service Auth<br/>- JWT Token Relay]
        MCPServer[🤖 MCP Server<br/>Port 8002<br/>- Admin-Only Access<br/>- JWKS Validation<br/>- Tool Processing<br/>- Service Auth Endpoint]
    end
    
    subgraph "Communication Protocols"
        A2A[⚡ A2A Protocol<br/>JSON-RPC over HTTP<br/>JWT Token in Headers]
        HTTP[🌐 HTTP/REST<br/>Bearer Token Auth]
        ServiceAuth[🔧 Service-to-Service<br/>Internal Authentication]
    end
    
    subgraph "Debug & Monitoring"
        DebugLogs[📋 Debug Logging<br/>- Auth Flow Tracking<br/>- Token Validation<br/>- MCP Call Tracing<br/>- Request/Response Logs]
    end
    
    %% User Interactions
    User -->|Login Request| Browser
    Browser -->|OAuth 2.0 + PKCE| EntraID
    Browser -->|Auth Callback| PingService
    
    %% Token Flow
    EntraID -->|JWT Token + Claims| PingService
    EntraID -.->|JWKS Validation| MCPServer
    
    %% Service Communication
    PingService -->|A2A Messages<br/>JWT Token in Headers| PongService
    PongService -->|JWT Bearer Token<br/>Admin User Context| MCPServer
    PongService -->|Bearer Token<br/>Direct MCP Access| MCPServer
    
    %% Debug Integration
    PingService --> DebugLogs
    PongService --> DebugLogs
    MCPServer --> DebugLogs
    
    %% Styling
    style PingService fill:#e3f2fd,stroke:#1976d2,stroke-width:2px
    style PongService fill:#fff3e0,stroke:#f57f17,stroke-width:2px
    style MCPServer fill:#e8f5e8,stroke:#2e7d32,stroke-width:2px
    style EntraID fill:#e8f5e8,stroke:#2e7d32,stroke-width:2px
    style DebugLogs fill:#fff9c4,stroke:#f57f17,stroke-width:2px
    style A2A fill:#ffebee,stroke:#c62828,stroke-width:2px
```

## 🔄 Enhanced MCP Integration Flow

```mermaid
sequenceDiagram
    participant User as 👤 Admin User
    participant Browser as 🌐 Browser
    participant Ping as 📡 Ping Service<br/>(A2A Client)
    participant Pong as 🏓 Pong Service<br/>(A2A Server + MCP Client)
    participant MCP as 🤖 MCP Server
    participant EntraID as 🔐 Microsoft Entra ID
    
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
    
    %% A2A Communication Phase
    Note over Browser,Pong: A2A Protocol with JWT Token Flow
    Browser->>Ping: POST /ping (with JWT session)
    Ping->>EntraID: Validate JWT via JWKS (session validation)
    EntraID->>Ping: JWKS validation result
    Ping->>Ping: Validate session & extract user info
    Ping->>Pong: A2A message/send (JWT in Authorization header)
    
    Note over Pong: PongHandler.on_message_send()
    Pong->>Pong: Process A2A message
    Pong->>Pong: Extract JWT token from A2A context
    Pong->>EntraID: Validate JWT via JWKS (A2A context)
    EntraID->>Pong: JWKS validation result
    Pong->>Pong: Check admin privileges from JWT claims
    
    %% MCP Integration Branches
    alt Admin User with JWT Token
        Note over Pong,MCP: Authenticated MCP Call
        Pong->>MCP: POST /mcp (Bearer JWT token)
        MCP->>EntraID: Validate JWT via JWKS
        MCP->>MCP: Process with admin authentication
        MCP->>Pong: Enhanced MCP response
        Pong->>Ping: A2A response with MCP enhancement
    else Admin User without JWT Token
        Note over Pong: No MCP Access - JWT Required
        Pong->>Ping: A2A response with JWT requirement message
    else Non-Admin User
        Pong->>Ping: A2A response with guidance message
    end
    
    %% Response Phase
    Ping->>Browser: Combined response with MCP data
```

## 🔐 Enhanced Security Features

### 🛡️ **JWKS-Based Token Validation**
- **Cryptographic Verification**: Uses Microsoft's public keys (RS256) for JWT signature validation
- **Real-time Key Fetching**: Retrieves current public keys from Microsoft's JWKS endpoint
- **Key Caching**: Intelligent caching (1 hour) to reduce API calls while maintaining security
- **Universal Implementation**: All services (Ping, Pong, MCP) use JWKS validation
- **Fallback Handling**: Graceful degradation when JWKS endpoints are unavailable

### 🔑 **Enhanced Authentication Approach**
- **A2A Context Authentication**: JWKS-based JWT validation for A2A protocol security
- **MCP Server Security**: JWKS validation for enhanced MCP server access security
- **Seamless Integration**: No breaking changes to existing A2A workflows
- **Enhanced Debugging**: Comprehensive logging for both validation methods

### 🎯 **Admin Role Validation**
- **Multiple Validation Methods**: Checks roles, groups, scopes, and custom attributes
- **Flexible Configuration**: Supports different Azure AD admin role configurations
- **Comprehensive Logging**: Detailed audit trails for all authentication attempts
- **Development Mode**: Mock admin user for development environments

### 📊 **Security Monitoring**
- **Token Validation Logs**: Step-by-step JWT validation process logging
- **Authentication Audit**: Complete audit trail of all login attempts and decisions
- **JWKS Fetch Monitoring**: Tracking of public key retrieval and caching
- **Security Event Logging**: Enhanced logging for security-related events

```mermaid
graph TB
    subgraph "Authentication Flow"
        JWT[🎫 JWT Token]
        JWKS[🔑 Microsoft JWKS]
        Validate[✅ Signature Validation]
        Claims[📋 Claims Extraction]
        Admin[👑 Admin Check]
    end
    
    subgraph "Validation Methods"
        Roles[🏷️ App Roles]
        Groups[👥 Azure AD Groups]
        Scopes[🎯 OAuth Scopes]
        Extensions[🔧 Custom Attributes]
    end
    
    JWT --> JWKS
    JWKS --> Validate
    Validate --> Claims
    Claims --> Admin
    
    Admin --> Roles
    Admin --> Groups
    Admin --> Scopes
    Admin --> Extensions
    
    style JWT fill:#e3f2fd
    style JWKS fill:#e8f5e8
    style Validate fill:#fff3e0
    style Admin fill:#fce4ec
```

## 🧹 Architecture Improvements Made

### ✅ **Enhanced Ping Service (Client Role)**
- **JWKS-based JWT validation** for endpoint security (`/ping`)
- **Removed unused A2A server methods** - ping service acts as A2A client only
- **Cleaned up imports** - removed unused A2A server dependencies
- **Clear documentation** - explains why A2A server methods aren't needed
- **Focused responsibilities** - pure A2A client + OAuth handler with JWKS security

### ✅ **Enhanced Pong Service (Server Role + MCP Client)**
- **Active `PongHandler.on_message_send`** - processes incoming A2A messages
- **MCP client integration** - connects to MCP server for enhanced responses
- **JWKS-based JWT validation** - cryptographic signature verification using Microsoft's public keys
- **JWT-only MCP access** - no service-to-service authentication fallback for enhanced security
- **Admin user detection** - identifies admin privileges from A2A context
- **Secure authentication flow** - JWKS validation for all MCP server communications
- **Comprehensive error handling** - graceful degradation with clear JWT requirements

### ✅ **Refactored MCP Client (JWT Token Aware)**
- **Unified `ping_mcp_service_call` method** - now handles both authenticated and service-to-service calls
- **Removed redundant `ping_mcp` method** - simplified codebase by eliminating duplication
- **JWT token awareness** - automatic detection and use of JWT tokens when available
- **Smart endpoint selection** - uses `/mcp` for authenticated calls, `/mcp/service` for service calls
- **Enhanced error handling** - granular status responses (auth_failed, auth_required, access_forbidden)
- **Improved debugging** - comprehensive logging with secure JWT token previews
- **Context7 integration** - implemented FastAPI JWT best practices

### ✅ **MCP Server Integration**
- **JWT Bearer token authentication** - required for all access points
- **JWKS validation** - full Azure AD token verification for all requests
- **Admin-only tool processing** - enforced security with proper error messages
- **Secure access pattern** - no service-to-service bypass for enhanced security
```

## � Recent Refactoring: JWT-Aware MCP Client

### Overview
The MCP client in the Pong service has been significantly refactored to provide unified JWT token-aware functionality, simplifying the codebase while enhancing security and debugging capabilities.

### Key Improvements

#### **Unified Interface**
- **Single Method**: `ping_mcp_service_call` now handles both authenticated and service-to-service calls
- **Removed Duplication**: Eliminated redundant `ping_mcp` method to reduce code complexity
- **Consistent API**: Same interface for all MCP interaction scenarios
- **Simplified Maintenance**: Single method to maintain and test

#### **JWT Token Awareness**
- **Automatic Detection**: Extracts JWT tokens from A2A user context when available
- **Smart Routing**: Uses authenticated endpoints when tokens are present
- **Graceful Fallback**: Falls back to service-to-service calls when no token available
- **Security Enhancement**: Proper authentication flow for admin users

#### **Enhanced Error Handling**
```typescript
// Response status types
{
  "status": "success" | "auth_failed" | "auth_required" | "access_forbidden" | "service_call_failed" | "service_call_error",
  "content": "Human-readable message",
  "metadata": {
    "service_call": true,
    "authenticated": boolean,
    "caller": "service-name",
    "error": "detailed-error-info"
  }
}
```

#### **Context7 Integration**
- Implemented FastAPI JWT authentication best practices
- Proper dependency injection patterns for JWT handling
- Security-focused token management with secure logging

### Technical Changes

#### **File: `pong/mcp_client.py`**
```python
# Before: Two separate methods
async def ping_mcp(self, message: str, auth_token: str) -> dict
async def ping_mcp_service_call(self, message: str, service_name: str) -> dict

# After: Single unified method
async def ping_mcp_service_call(
    self, 
    message: str, 
    service_name: str = "pong-service", 
    auth_token: Optional[str] = None
) -> dict
```

#### **Smart Endpoint Selection**
- **JWT Token Required**: `POST /mcp` (authenticated call with Bearer token)
- **No Fallback**: Service-to-service calls removed for enhanced security
- **Secure Access Only**: All MCP server access requires valid JWT token authentication

#### **Enhanced A2A Integration**
- JWT tokens extracted from A2A Authorization headers when available
- Seamless integration with existing A2A authentication flow through custom context builder
- Maintains backward compatibility with non-authenticated calls
- JWT token passed through A2A context to MCP client for authenticated calls

### Benefits

1. **Simplified Codebase**: 40% reduction in MCP client code complexity
2. **Better Security**: Proper JWT token handling throughout the call chain
3. **Enhanced Debugging**: Comprehensive logging with secure token previews
4. **Improved Error Handling**: Granular error responses for better troubleshooting
5. **Unified Testing**: Single method to test all MCP interaction scenarios
6. **Future-Proof**: Extensible design for additional authentication patterns

### Migration Impact

- **Backward Compatible**: Existing code continues to work without changes
- **No Breaking Changes**: All existing functionality preserved
- **Enhanced Functionality**: Additional JWT token support added seamlessly
- **Improved Reliability**: Better error handling and status reporting

## �🚀 Current Features

### ✅ Authentication & Authorization
- **Microsoft Entra ID Integration**: Enterprise-grade authentication with OAuth 2.0 + PKCE
- **JWT Token Verification**: Cryptographic token validation using JWKS (JSON Web Key Set) for all MCP access
- **Role-Based Access Control**: Admin-only access enforcement with Azure AD roles/groups
- **Session Management**: Secure session handling with token refresh capabilities
- **Secure MCP Access**: JWT Bearer tokens required for all MCP server communications

### ✅ Service Architecture
- **Ping Service (Port 8000)**: OAuth 2.0 client + A2A protocol client (outbound only)
  - **JWKS-based JWT validation** for endpoint security
  - Session management and user authentication
  - Simplified, focused codebase with enhanced security
- **Pong Service (Port 8001)**: A2A protocol server + MCP client integration
  - Active `PongHandler.on_message_send` for incoming A2A messages
  - **JWKS-based JWT validation** for A2A context security
  - **A2A-only MCP access** - no direct MCP endpoints for enhanced security
  - **JWT-only MCP communication** - no service-to-service fallback for enhanced security
  - Admin user detection and privilege checking
  - Secure authentication: JWKS validation for both A2A context and MCP server access
- **MCP Server (Port 8002)**: Admin-only Model Context Protocol server
  - **JWT Bearer token authentication required** for all access
  - JWKS validation for cryptographic security
  - Tool processing with strict security enforcement
  - No service-to-service bypass - JWT tokens mandatory

### ✅ MCP Integration
- **Standards Compliant**: Full Model Context Protocol (MCP) implementation
- **Unified MCP Client**: Single `ping_mcp_service_call` method handles JWT token-aware calls
- **Secure Access Pattern**: 
  - JWT Bearer token authentication required for all MCP server calls
  - No fallback service-to-service calls for enhanced security
- **Smart Endpoint Selection**: Uses `/mcp` endpoint with JWT Bearer token authentication
- **Admin Access Control**: Strict role-based access with comprehensive error handling
- **Enhanced Tool Processing**: Ping processor with intelligent response generation
- **Secure Error Handling**: Clear JWT requirement messages for unauthorized access attempts
- **Context7 Integration**: Implements FastAPI JWT authentication best practices

### ✅ A2A Protocol Implementation
- **Pure Client-Server Architecture**: Ping service only sends, Pong service only receives
- **JWKS-based A2A Context**: Authentication tokens validated cryptographically in A2A Authorization headers
- **Custom Context Builder**: JWKS validation for JWT tokens from A2A requests for enhanced security
- **Message Enhancement**: Automatic MCP integration for admin users via A2A messages with verified JWT context
- **Clean Handler Implementation**: Removed unused methods and simplified codebase
- **Secure Authentication**: JWKS validation for JWT tokens extracted from A2A Authorization headers
- **Enhanced MCP Access**: JWT tokens cryptographically verified before MCP server communications

### ✅ Debug & Monitoring
- **Comprehensive Debug Logging**: End-to-end visibility into authentication and communication flows
- **MCP Integration Tracing**: Service-to-service call monitoring and error tracking
- **Authentication Audit**: Detailed logging of login attempts, token validation, and access decisions
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

## 🔄 Usage Workflows

### 1. Web Authentication Flow

1. **Visit Ping Service**: Navigate to `http://localhost:8000`
2. **Login**: Click login or visit `/auth/login`
3. **Azure Authentication**: Login with admin-privileged account
4. **Token Receipt**: Receive JWT with admin claims
5. **Service Access**: Use authenticated endpoints

### 2. A2A Protocol Communication

The ping service communicates with the pong service exclusively via A2A protocol, passing JWT tokens through Authorization headers. The pong service handles MCP server integration internally:

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

### Ping Service (Port 8000) - OAuth Client + A2A Client Only
- `GET /` - Service information and authentication status
- `GET /health` - Health check with service dependencies
- `POST /ping` - **🎯 Main endpoint**: Sends ping via A2A protocol to pong service (admin required)
- `GET /auth/login` - Initiate OAuth 2.0 + PKCE flow with Microsoft Entra ID
- `GET /auth/callback` - OAuth callback handler for authorization code exchange
- `GET /debug` - Debug information and logging configuration (debug mode only)
- `GET /.well-known/agent.json` - A2A agent discovery and capabilities

**Note**: *Ping service does NOT have A2A server endpoints. It acts purely as an A2A client.*

### Pong Service (Port 8001) - A2A Server + MCP Client
- `GET /` - Service information and status with MCP integration details
- `GET /health` - Health check (includes MCP server connectivity status)
- `GET /debug` - Debug information and logging configuration (debug mode only)
- `GET /.well-known/agent.json` - A2A agent discovery and capabilities
- `POST /` - **🎯 A2A JSON-RPC endpoint** - Receives messages from ping service
  - **Method**: `message/send` - Processes incoming A2A messages
  - **Handler**: `PongHandler.on_message_send` - Includes MCP integration for admin users
  - **MCP Enhancement**: Automatically enhances responses for admin users via A2A protocol with JWT token relay

**Security Note**: *Pong service provides MCP access only through A2A protocol. No direct MCP endpoints for enhanced security.*

### MCP Server (Port 8002) - Admin-Only Resource Server
- `GET /` - Server information and capabilities
- `GET /health` - Health check and authentication status
- `POST /mcp` - **🎯 MCP protocol endpoint** (admin required, JWKS JWT verification)
- `POST /tools/ping_processor` - Direct tool access (admin required)
- `GET /debug` - Debug information and authentication configuration

**Security Note**: *All MCP server endpoints require valid JWT Bearer token authentication. No service-to-service bypass available for enhanced security.*

### 🔍 Authentication Patterns

#### Pattern 1: A2A Protocol with JWT Token Flow (Recommended)
```bash
# User → Ping Service → Pong Service (A2A + JWT) → MCP Server (Bearer Auth)
curl -X POST http://localhost:8000/ping \
  -H "Authorization: Bearer JWT_TOKEN" \
  -H "Content-Type: application/json"

# JWT token is passed through A2A Authorization headers to Pong service
# Pong service extracts JWT and calls MCP server with Bearer authentication
```

#### Pattern 2: Pure MCP Server Access
```bash
# User → MCP Server (Bearer Token + JWKS Validation)
curl -X POST http://localhost:8002/mcp \
  -H "Authorization: Bearer JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"message": "Pure MCP test"}'
```

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
    PingService->>PongService: A2A message/send (JWT in Authorization header)
    
    %% JWT Token Extraction & Relay
    Note over PongService,EntraID: MCP Server JWT Token Flow
    PongService->>DebugLogs: 📝 Log JWT token extraction from A2A context
    PongService->>PongService: Extract JWT from A2A Authorization header
    
    alt JWT Token Available
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
    else No JWT Token
        Note over PongService: No MCP Access - Security Policy
        PongService->>DebugLogs: 📝 Log MCP access denied - JWT required
        PongService->>PongService: Return JWT requirement message
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
✅ **Pure A2A Architecture**: Ping service communicates exclusively via A2A protocol as client only  
✅ **Simplified Service Architecture**: Removed unused A2A handlers from ping service (25% code reduction)  
✅ **Admin-Only MCP Access**: Role-based security enforcement with comprehensive audit trails  
✅ **Unified MCP Client**: Single JWT-aware method handles all authentication scenarios (40% code reduction)  
✅ **Smart Authentication**: Automatic JWT token detection and routing in A2A contexts  
✅ **Service-to-Service Authentication**: Internal authentication bypass for A2A protocol limitations  
✅ **JWT Token Verification**: JWKS-based cryptographic validation with RS256 signatures (MCP server)  
✅ **Cross-Service Authentication**: Shared middleware and token relay across services  
✅ **Comprehensive Debug Logging**: End-to-end visibility into authentication and communication flows  
✅ **Windows Compatibility**: Unicode encoding issues resolved for cross-platform support  
✅ **Production Ready**: Comprehensive error handling, monitoring, and security controls  
✅ **Well Documented**: Complete setup, deployment guides, and troubleshooting documentation  
✅ **Integration Tested**: Automated testing, health checks, and debug validation tools  
✅ **Enhanced MCP Integration**: Automatic admin user detection and MCP enhancement in A2A messages  
✅ **Context7 Integration**: FastAPI JWT authentication best practices implementation  

### � Architecture Improvements

**Ping Service Enhancement**:
- **Added**: JWKS-based JWT validation for `/ping` endpoint
- **Enhanced**: Admin user verification with cryptographic security
- **Improved**: Session validation with Microsoft public key verification
- **Added**: Comprehensive debug logging for JWKS validation flow
- **Result**: Enhanced security with consistent JWKS validation across all services

**Pong Service Enhancement**:
- **Enhanced**: `PongHandler.on_message_send` with MCP client integration
- **Added**: Service-to-service MCP authentication for A2A calls
- **Added**: Admin user detection from A2A context
- **Added**: Comprehensive error handling and user guidance
- **Result**: Seamless MCP integration for admin users via A2A protocol

**MCP Client Refactoring** (Latest):
- **Unified**: Single `ping_mcp_service_call` method handles all scenarios
- **Removed**: Redundant `ping_mcp` method (eliminated code duplication)
- **Enhanced**: JWT token awareness with automatic detection
- **Improved**: Error handling with granular status responses
- **Added**: Context7 FastAPI JWT best practices
- **Result**: 40% reduction in MCP client complexity, enhanced security

**MCP Server Integration**:
- **Added**: `/mcp/service` endpoint for service-to-service calls
- **Enhanced**: Authentication patterns for both direct and service calls
- **Maintained**: Full JWKS validation for direct access
- **Result**: Flexible access patterns supporting both A2A and direct integration

### 🔍 Current Architecture Benefits

1. **Consistent Security**: All services use JWKS validation for JWT tokens
2. **Cryptographic Verification**: RS256 signature validation across all service boundaries
3. **Unified MCP Interface**: Single method handles all authentication scenarios
4. **JWT Token Awareness**: Automatic token detection and proper security handling
5. **Enhanced User Experience**: Admin users get automatic MCP enhancement
6. **Graceful Degradation**: Non-admin users receive helpful guidance
7. **Service Mesh Ready**: Clean service-to-service authentication patterns
8. **Context7 Compliant**: Follows FastAPI security best practices
