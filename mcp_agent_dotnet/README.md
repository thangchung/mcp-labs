# .NET MCP Agent

A C# implementation of Model Context Protocol (MCP) agents with resumable HTTP server and simple event store capabilities.

Ref:

- https://devblogs.microsoft.com/blog/can-you-build-agent2agent-communication-on-mcp-yes
- https://github.com/victordibia/ai-agents-for-beginners/tree/main/11-mcp/code_samples/mcp-agents

## Features

- **Resumable HTTP MCP Server**: Supports session resumption and message redelivery
- **SimpleEventStore**: Event store implementation for durability
- **Travel Agent**: Simulates travel booking with price confirmation via elicitation
- **Research Agent**: Performs research tasks with AI-assisted summaries via sampling
- **Real-time Progress Updates**: Streaming progress notifications
- **Interactive Confirmations**: Mid-execution user input via elicitation
- **AI Assistance**: Sampling for complex decisions during execution
- **Google Gemini Integration**: AI-powered client using Google Gemini 1.5 Flash model
- **Enhanced AI Client**: Advanced features with session management, notifications, and progress tracking

## Project Structure

```
├── src/
│   ├── McpAgent.Server/           # MCP Server implementation
│   ├── McpAgent.Client/           # MCP Client implementation  
│   ├── McpAgent.Core/             # Shared types and utilities
│   └── McpAgent.EventStore/       # Event store implementation
├── tests/
│   ├── McpAgent.Server.Tests/     # Server unit tests
│   ├── McpAgent.Client.Tests/     # Client unit tests
│   ├── McpAgent.Core.Tests/       # Core unit tests
│   └── McpAgent.Integration.Tests/ # Integration tests
└── samples/
    └── ConsoleApp/                # Sample console application
```

## Requirements

- .NET 8.0 or later
- ModelContextProtocol NuGet packages

## Getting Started

### Running the Server

```bash
cd src/McpAgent.Server
dotnet run --port 8006
```

### Running the Client

#### Basic MCP Client
```bash
cd src/McpAgent.Client
dotnet run --url http://127.0.0.1:8006/mcp
```

#### Enhanced MCP Client  
```bash
cd src/McpAgent.Client
dotnet run --url http://127.0.0.1:8006/mcp
```

#### Google Gemini AI Client (Simple)
```bash
cd src/McpAgent.Client
dotnet run --gemini --gemini-key YOUR_GEMINI_API_KEY --url http://127.0.0.1:8006/mcp
```

#### Google Gemini AI Client (Enhanced)
```bash
cd src/McpAgent.Client
dotnet run --gemini-enhanced --gemini-key YOUR_GEMINI_API_KEY --url http://127.0.0.1:8006/mcp
```

**Note**: To get a Google Gemini API key, visit https://ai.google.dev/gemini-api/docs/api-key

## Testing

Run all tests:

```bash
dotnet test
```

## Architecture

This implementation follows the MCP specification with these key components:

1. **Resumable HTTP Transport**: StreamableHTTP transport with session IDs and event replay
2. **Event Store**: Simple in-memory event store for session resumption  
3. **Agent Tools**: Travel and research agents demonstrating agentic behaviors
4. **Progress Notifications**: Real-time updates during long-running tasks
5. **Elicitation & Sampling**: Interactive capabilities for user input and AI assistance
6. **Google Gemini Integration**: Direct HTTP API integration with Gemini 1.5 Flash model
7. **AI-Powered Execution**: Intelligent tool selection and natural language processing

### Gemini Client Features

The Google Gemini integration provides two modes:

- **Simple Mode** (`--gemini`): Direct AI-powered interaction with natural language understanding
- **Enhanced Mode** (`--gemini-enhanced`): Advanced features including:
  - Session management with persistent conversation history
  - Real-time notifications and progress tracking  
  - Interactive elicitation for user confirmations
  - AI-guided tool execution and decision making
  - Comprehensive logging and error handling

## License

MIT License
