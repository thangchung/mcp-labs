# MCP Agent Client with Google Gemini AI

This project has been enhanced with Google Gemini AI integration using direct HTTP calls for simplicity.

## Features

The MCP Agent Client now supports four modes:

1. **Basic Mode** (`--basic`): Original MCP client functionality
2. **Enhanced Mode** (default): Enhanced MCP client with additional features
3. **Gemini AI Mode** (`--gemini`): Simple AI-powered client using Google Gemini
4. **Enhanced Gemini AI Mode** (`--gemini-enhanced`): Full-featured AI client with sampling, notifications, progress tracking, and elicitation

## Usage

### Prerequisites

For Gemini AI mode, you'll need:
- A Google Gemini API key (get one from [Google AI Studio](https://makersuite.google.com/app/apikey))

### Running the Client

```bash
# Basic mode
dotnet run -- --basic --url http://127.0.0.1:8006/mcp

# Enhanced mode (default)
dotnet run -- --url http://127.0.0.1:8006/mcp

# Simple Gemini AI mode
dotnet run -- --gemini --gemini-key YOUR_GEMINI_API_KEY --url http://127.0.0.1:8006/mcp

# Enhanced Gemini AI mode (recommended for full features)
dotnet run -- --gemini-enhanced --gemini-key YOUR_GEMINI_API_KEY --url http://127.0.0.1:8006/mcp
```

### Command Line Options

- `--url`, `-u`: MCP server URL (default: http://127.0.0.1:8006/mcp)
- `--verbose`, `-v`: Enable verbose logging
- `--clear-session`, `-c`: Clear existing session before starting
- `--basic`, `-b`: Use basic client mode
- `--gemini`, `-g`: Use simple Google Gemini AI-powered client
- `--gemini-enhanced`, `-e`: Use enhanced Gemini AI client with full features
- `--gemini-key`, `-k`: Google Gemini API key (required for Gemini modes)

### Enhanced Gemini AI Mode Features

The Enhanced Gemini AI mode (`--gemini-enhanced`) provides advanced features:

#### 🧠 **AI-Powered Intelligence**
- **Natural Language Processing**: Understands complex, conversational requests
- **Context-Aware Responses**: Maintains session context across interactions
- **Intelligent Tool Selection**: AI determines optimal tool usage
- **Smart Parameter Extraction**: Automatically extracts relevant parameters from natural language

#### 🔄 **MCP Sampling**
- **Server-Side AI Assistance**: Leverages MCP server's AI for complex decisions
- **Intelligent Parameter Validation**: AI validates and optimizes tool parameters
- **Context-Aware Sampling**: Server provides contextual guidance for tool execution

#### 📬 **Real-Time Notifications**
- **Progress Tracking**: Live updates during tool execution
- **Status Notifications**: Success, error, and warning messages
- **User Action Alerts**: Notifications for required user input
- **AI Analysis Updates**: Insights and analysis notifications

#### ❓ **Smart Elicitation**
- **Missing Parameter Detection**: AI identifies incomplete requests
- **Interactive Parameter Collection**: Conversational parameter gathering
- **Context-Aware Prompting**: Smart questions based on user intent
- **Multi-Round Elicitation**: Handles complex, multi-step information gathering

#### 📊 **Progress Updates**
- **Visual Progress Bars**: Real-time execution progress
- **Step-by-Step Tracking**: Detailed operation breakdowns
- **Time Estimates**: AI-powered completion time predictions
- **Interactive Feedback**: User can monitor and interact during execution

### Example Interactions (Enhanced Gemini Mode)

#### 🌍 **Travel Planning with Elicitation**
```
💭 You: I want to travel somewhere warm
🧠 AI: I'd be happy to help you plan a warm destination trip! To provide the best recommendations, could you tell me:
     - What time of year are you planning to travel?
     - Do you prefer beaches, cities, or nature destinations?
     - What's your approximate budget range?

💭 You: I'm thinking about March, beaches, around $2000
🔄 [MCP Sampling] Analyzing optimal beach destinations for March within budget...
📊 [Progress] ████████░░ 80% - Gathering destination data...
✅ [Notification] Found 5 perfect matches for your criteria!
🤖 Assistant: Based on your preferences, I found excellent warm beach destinations for March:
     1. Maldives - Perfect weather, luxury resorts within budget
     2. Thailand (Phuket) - Great value, beautiful beaches, vibrant culture
     3. Costa Rica - Adventure + beaches, eco-friendly options
     [Full travel recommendations with booking options...]
```

#### 🔬 **Research with Progress Tracking**
```
💭 You: Research the latest developments in quantum computing
📊 [Progress] ████░░░░░░ 40% - Searching academic databases...
📊 [Progress] ████████░░ 80% - Analyzing recent papers...
📬 [Notification] Found 127 relevant papers from 2024-2025
🔄 [MCP Sampling] Prioritizing most significant breakthroughs...
✅ [Notification] Analysis complete - 3 major breakthroughs identified!
🤖 Assistant: Here are the most significant quantum computing developments from 2024-2025:
     1. IBM's 1000-qubit processor breakthrough
     2. Google's quantum error correction milestone
     3. Microsoft's topological qubit advancement
     [Detailed analysis with sources and implications...]
```

#### ❓ **Interactive Elicitation Example**
```
💭 You: Book me a flight
❓ [Elicitation] I'll help you book a flight! I need some details:
🧠 AI: Where would you like to fly from?

💭 You: Seattle
📝 [Notification] Origin: Seattle ✓
❓ [Elicitation] Great! And what's your destination?

💭 You: New York
📝 [Notification] Destination: New York ✓
❓ [Elicitation] When would you like to travel?

💭 You: Next Friday
🔄 [MCP Sampling] Validating travel date and checking availability...
📊 [Progress] ██████████ 100% - Flight search complete!
✅ [Notification] Found 12 flights for Friday, August 8th, 2025
🤖 Assistant: [Complete flight options with prices and booking links...]
```

### Testing Advanced Features

#### 🧪 **Testing MCP Sampling**
```bash
# Run enhanced mode and try these commands:
💭 "I need help choosing between destinations"
💭 "What's the best research approach for [complex topic]?"
💭 "Help me decide on travel dates"
```

#### 🧪 **Testing Notifications**
```bash
# View notification history:
💭 "notifications"

# Check current status:
💭 "status"

# Clear notifications:
💭 "clear"
```

#### 🧪 **Testing Progress Updates**
```bash
# Try long-running operations:
💭 "Research comprehensive analysis of climate change impacts"
💭 "Plan a complex multi-city European tour"
💭 "Find and compare 20 hotels in Tokyo"
```

#### 🧪 **Testing Elicitation**
```bash
# Try incomplete requests:
💭 "I want to travel" (missing destination)
💭 "Research something" (missing topic)
💭 "Book a hotel" (missing location, dates)
💭 "Find flights" (missing origin/destination)
```

### Built-in Commands (Enhanced Mode)

- `help` - Show AI-enhanced help with feature explanations
- `tools` - List available tools with AI descriptions
- `status` - Display AI session analytics and statistics
- `notifications` - View notification history with filtering
- `clear` - Clear session with AI confirmation
- `exit` - Exit with AI session summary

## Implementation Details

### Simple Gemini Mode (`--gemini`)
The basic Gemini integration uses:
- **Direct HTTP API calls** to Google's Gemini API for simplicity
- **No external AI SDKs** - minimal dependencies
- **Simple keyword-based tool detection** for reliability
- **Basic conversation history management** for context
- **Error handling and fallbacks** for robustness

### Enhanced Gemini Mode (`--gemini-enhanced`)
The advanced integration includes:
- **Sophisticated Natural Language Processing** with context awareness
- **MCP Sampling Integration** for server-side AI assistance
- **Real-time Progress Tracking** with visual indicators
- **Smart Elicitation System** for interactive parameter collection
- **Comprehensive Notification System** with categorized alerts
- **Session Management** with persistent context and analytics
- **Advanced Error Handling** with intelligent recovery strategies
- **Fallback Mechanisms** for offline operation

### Error Handling & Resilience

The enhanced client provides robust error handling:

#### 🔄 **API Resilience**
- **Automatic Retry Logic** with exponential backoff
- **Network Timeout Handling** with user feedback
- **Rate Limiting Detection** with wait recommendations
- **Service Unavailable Fallbacks** with offline capabilities

#### 🛡️ **Fallback Mechanisms**
- **Offline Operation Mode** when AI services are unavailable
- **Pattern-Based Parameter Extraction** without AI assistance
- **Context-Aware Help System** for guidance during outages
- **Graceful Degradation** maintaining core MCP functionality

#### 📋 **Comprehensive Error Messages**
```bash
# API Key Issues
❌ Invalid API key detected. Please check your Gemini API key.
   → Get a new key at: https://makersuite.google.com/app/apikey

# Rate Limiting
⚠️  Rate limit exceeded. Retrying in 30 seconds...
   → Consider upgrading your API plan for higher limits.

# Network Issues
🌐 Network timeout detected. Using offline fallback mode.
   → Check your internet connection and retry.

# Service Unavailable
🔧 Gemini API temporarily unavailable. Operating in fallback mode.
   → Core MCP functionality remains available.
```

## Architecture

### Simple Gemini Mode Flow
```
User Input → Gemini AI → Tool Detection → MCP Tools → Response → User
```

### Enhanced Gemini Mode Flow
```
User Input → Context Analysis → Intent Detection → Parameter Elicitation
     ↓                              ↓                       ↓
Session Context ← AI Processing → MCP Sampling → Progress Tracking
     ↓                              ↓                       ↓
Tool Execution ← Notifications ← AI Enhancement → User Response
```

### Enhanced Client Components

#### 🧠 **AI Processing Pipeline**
1. **Input Analysis**: Natural language understanding with context
2. **Intent Detection**: Identifies user goals and required tools
3. **Parameter Extraction**: Smart parameter identification from conversation
4. **Elicitation Management**: Interactive missing information collection
5. **MCP Sampling**: Server-side AI consultation for complex decisions
6. **Progress Tracking**: Real-time operation monitoring
7. **Response Generation**: Contextual, helpful responses with AI insights

#### 📊 **Session Management**
- **Persistent Context**: Maintains conversation history and user preferences
- **Analytics Tracking**: Usage statistics and performance metrics
- **Notification History**: Categorized message history with filtering
- **Progress State**: Tracks ongoing operations and completion status
- **Error Recovery**: Maintains state during failures and recovery

#### 🔄 **MCP Integration**
- **Tool Discovery**: Dynamic tool identification and capability analysis
- **Sampling Requests**: Server-side AI assistance for complex decisions
- **Progress Monitoring**: Real-time feedback from MCP server operations
- **Error Handling**: Graceful handling of MCP server issues
- **Fallback Operations**: Continued functionality during server unavailability

## Quick Start Guide

### 1. Prerequisites Setup
```bash
# Get your Gemini API key
# Visit: https://makersuite.google.com/app/apikey
export GEMINI_API_KEY="your_api_key_here"
```

### 2. Start MCP Server
```bash
# In a separate terminal, start your MCP server
# Example: python your_mcp_server.py --port 8006
```

### 3. Run Enhanced Client
```bash
# Navigate to the client directory
cd src/McpAgent.Client

# Run enhanced mode with all features
dotnet run -- --gemini-enhanced --gemini-key $GEMINI_API_KEY --url http://127.0.0.1:8006/mcp
```

### 4. Test Advanced Features
```bash
# Try these examples once connected:

# Test elicitation
💭 "I want to travel"

# Test progress tracking
💭 "Research quantum computing in detail"

# Test sampling
💭 "Help me choose the best destination for my budget"

# View notifications
💭 "notifications"

# Check session status
💭 "status"
```

### 5. Troubleshooting
```bash
# Check connection
💭 "tools"  # Should list available MCP tools

# Enable verbose logging
dotnet run -- --gemini-enhanced --gemini-key $GEMINI_API_KEY --url http://127.0.0.1:8006/mcp --verbose

# Clear session if issues persist
dotnet run -- --gemini-enhanced --gemini-key $GEMINI_API_KEY --url http://127.0.0.1:8006/mcp --clear-session
```
