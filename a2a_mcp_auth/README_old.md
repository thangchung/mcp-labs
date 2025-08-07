# A2A Ping/Pong Application with Microsoft Entra ID Authentication

A distributed ping/pong application built using the Agent-to-Agent (A2A) protocol with Microsoft Entra ID OAuth 2.0 authentication and role-based authorization.

## Features

- **A2A Protocol**: Full implementation using the A2A Python SDK
- **Microsoft Entra ID**: OAuth 2.0 Authorization Code Flow + PKCE
- **Role-Based Access**: Only admin users can access ping/pong functionality
- **Distributed Services**: Separate ping and pong services communicating via A2A protocol
- **FastAPI**: Modern web framework with automatic API documentation

## Architecture

- **Ping Service**: Initiates ping requests to the pong service
- **Pong Service**: Responds to ping requests from the ping service
- **Shared Authentication**: Common authentication and authorization logic

## Quick Start

### Prerequisites

- Python 3.11+
- UV package manager
- Microsoft Entra ID tenant with app registrations

### Setup

1. Clone and navigate to the project:
```bash
cd a2a_mcp_auth
```

2. Install dependencies:
```bash
uv sync
```

3. Copy environment configuration:
```bash
cp .env.example .env
```

4. Configure your Microsoft Entra ID settings in `.env`

5. Start the services:

**Terminal 1 - Pong Service:**
```bash
uv run python -m pong.main
```

**Terminal 2 - Ping Service:**
```bash
uv run python -m ping.main
```

### Usage

1. Navigate to `http://localhost:8000/docs` (Ping service) for API documentation
2. Use the `/auth/login` endpoint to authenticate with Microsoft Entra ID
3. Use the `/ping` endpoint to send a ping message to the pong service

## Configuration

See `.env.example` for required environment variables.

## Services

- **Ping Service**: http://localhost:8000
- **Pong Service**: http://localhost:8001

## API Endpoints

### Both Services
- `GET /.well-known/agent.json` - A2A Agent card
- `POST /` - A2A JSON-RPC endpoint
- `GET /auth/login` - Initiate OAuth flow
- `GET /auth/callback` - OAuth callback handler

### Ping Service Only
- `POST /ping` - Manual ping trigger (requires admin role)
