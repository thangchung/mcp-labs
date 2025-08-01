# MCP Agent Client Configuration

The MCP Agent Client has been updated to use `appsettings.json` configuration instead of command-line arguments.

## Configuration Files

### appsettings.json
The main configuration file containing default settings:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "McpAgent": {
    "ServerUrl": "http://127.0.0.1:8006/mcp",
    "Verbose": false,
    "ClearSession": false,
    "Mode": "Enhanced",
    "HostPort": 8007,
    "GeminiApiKey": "",
    "OpenAI": {
      "ApiKey": "",
      "Model": "gpt-4o-mini",
      "Endpoint": "https://api.openai.com/v1"
    }
  }
}
```

### appsettings.Development.json
Development-specific overrides:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "System": "Information",
      "Microsoft": "Information"
    }
  },
  "McpAgent": {
    "Verbose": true
  }
}
```

## Client Modes

The application supports the following modes (set via `McpAgent.Mode`):
- `Basic` - Basic MCP client
- `Enhanced` - Enhanced MCP client (default)
- `Gemini` - Google Gemini AI-powered client
- `GeminiEnhanced` - Enhanced Gemini client with session management
- `OpenAI` - OpenAI-powered client
- `Host` - Host mode with web UI and MCP service
- `Blazor` - Blazor web interface with streaming chat

## Command Line Override

You can still override configuration values using command-line arguments:

```bash
# Start in Blazor mode
dotnet run --blazor

# Use a different server URL
dotnet run --url http://localhost:8080/mcp

# Enable verbose logging
dotnet run --verbose

# Use Gemini mode with API key
dotnet run --gemini-enhanced --gemini-key YOUR_API_KEY

# Use OpenAI mode
dotnet run --openai --openai-key YOUR_API_KEY

# Host mode on different port
dotnet run --host --port 9000
```

## Environment Variables

You can also use environment variables:
```bash
export McpAgent__GeminiApiKey="your-api-key"
export McpAgent__OpenAI__ApiKey="your-openai-key"
```

## API Keys

For security, it's recommended to set API keys via:
1. Environment variables
2. appsettings.Development.json (for development)
3. Azure Key Vault or similar (for production)

Never commit API keys to source control.
