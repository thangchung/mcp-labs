# OpenAI Endpoint Configuration

## Overview
The `OpenAiMcpClientEnhanced` now supports configurable OpenAI API endpoints through the `appsettings.json` configuration file. This allows you to use different OpenAI-compatible services or custom endpoints.

## Configuration

### appsettings.json Setup
Add or update the `OpenAI` section in your `appsettings.json`:

```json
{
  "OpenAI": {
    "ApiKey": "your-api-key-here",
    "Model": "gpt-4o-mini",
    "Endpoint": "https://api.openai.com/v1"
  }
}
```

### Configuration Options

| Property | Description | Default Value | Examples |
|----------|-------------|---------------|----------|
| `ApiKey` | Your OpenAI API key | - | `sk-proj-...` or `gsk_...` (for Gemini) |
| `Model` | The AI model to use | `gpt-4o-mini` | `gpt-4`, `gpt-4-turbo`, `gemini-pro` |
| `Endpoint` | The API endpoint URL | `https://api.openai.com/v1` | See examples below |

### Supported Endpoints

#### 1. OpenAI Official API
```json
"Endpoint": "https://api.openai.com/v1"
```

#### 2. Azure OpenAI Service
```json
"Endpoint": "https://your-resource.openai.azure.com/openai/deployments/your-deployment"
```

#### 3. GitHub Models (Free)
```json
"Endpoint": "https://models.inference.ai.azure.com"
```

#### 4. Google AI Studio / Gemini
```json
"Endpoint": "https://generativelanguage.googleapis.com/v1beta"
```

#### 5. Local LLM Services
```json
"Endpoint": "http://localhost:11434/v1"
```
*For Ollama or other local OpenAI-compatible services*

#### 6. Other OpenAI-Compatible Services
```json
"Endpoint": "https://api.anthropic.com/v1"
"Endpoint": "https://api.groq.com/openai/v1"
"Endpoint": "https://api.together.xyz/v1"
```

## Environment Variables
You can also set the endpoint via environment variable:
```bash
export OPENAI_ENDPOINT="https://api.openai.com/v1"
```

The priority order is:
1. Environment variable `OPENAI_ENDPOINT`
2. Configuration file `OpenAI:Endpoint`
3. Default value `https://api.openai.com/v1`

## How It Works

The endpoint configuration is read during application startup in `Program.cs`:

```csharp
builder.Services.Configure<McpAgentOptions>(options =>
{
    options.OpenAiEndpoint = builder.Configuration["OpenAI:Endpoint"] ?? "https://api.openai.com/v1";
    // ... other configurations
});
```

The `McpAgentService` then passes this endpoint to the `OpenAiMcpClientEnhanced` constructor:

```csharp
var endpoint = _options.OpenAiEndpoint ?? "https://api.openai.com/v1";
_mcpClient = new OpenAiMcpClientEnhanced(serverUrl, _loggerFactory, apiKey, model, endpoint);
```

## Features

- ✅ **Flexible Endpoint Support**: Use any OpenAI-compatible API
- ✅ **Configuration-Driven**: No code changes needed to switch providers
- ✅ **Environment Variable Support**: Easy deployment configuration
- ✅ **Fallback Defaults**: Graceful handling of missing configuration
- ✅ **Real-time Configuration**: Endpoint logging for verification
- ✅ **Multiple Provider Support**: OpenAI, Azure, GitHub, Gemini, Local LLMs

## Example Use Cases

### Development with GitHub Models (Free)
```json
{
  "OpenAI": {
    "ApiKey": "github_pat_...",
    "Model": "gpt-4o-mini",
    "Endpoint": "https://models.inference.ai.azure.com"
  }
}
```

### Production with Azure OpenAI
```json
{
  "OpenAI": {
    "ApiKey": "your-azure-key",
    "Model": "gpt-4",
    "Endpoint": "https://your-resource.openai.azure.com"
  }
}
```

### Local Development with Ollama
```json
{
  "OpenAI": {
    "ApiKey": "not-needed-for-local",
    "Model": "llama2",
    "Endpoint": "http://localhost:11434/v1"
  }
}
```

## Troubleshooting

- **Invalid Endpoint**: Check the URL format and ensure it ends with the correct API version path
- **Authentication Errors**: Verify your API key is correct for the chosen endpoint
- **Model Compatibility**: Ensure the specified model is available on your chosen endpoint
- **Network Issues**: Check firewall settings for custom endpoints

## Logs
The application will log the configured endpoint during startup:
```
✅ OpenAI MCP Client initialized successfully with endpoint: https://api.openai.com/v1
```
