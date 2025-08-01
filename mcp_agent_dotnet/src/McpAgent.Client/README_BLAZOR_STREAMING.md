# MCP Agent Client - Blazor Streaming Chat

## Overview

The MCP Agent Client has been successfully transformed from a console application into a modern **Blazor Server** application with **streaming chat capabilities**. This implementation provides a real-time web interface for interacting with MCP (Model Context Protocol) services.

## 🚀 Features

### Streaming Chat Interface
- **Real-time messaging** with smooth streaming responses
- **Modern UI** built with Bootstrap 5.3.0 and Font Awesome icons
- **Responsive design** that works on desktop and mobile devices
- **Auto-scrolling** message list with smooth animations
- **Typing indicators** during response generation
- **Message history** with role-based styling (user vs assistant)

### Component Architecture
- **Modular Blazor components** for maintainability
- **Server-side rendering** with SignalR for real-time updates
- **JavaScript interop** for enhanced user experience
- **CSS-in-component** styling for encapsulation

### Quick Actions
- **Travel Agent** - Plan trips with budget considerations
- **Research Agent** - Conduct research on various topics
- **Custom tool calls** with JSON parameter support

## 🏗️ Architecture

### Project Structure
```
McpAgent.Client/
├── Components/
│   ├── App.razor                 # Root application component
│   ├── Routes.razor             # Routing configuration
│   ├── Layout/
│   │   └── MainLayout.razor     # Main layout component
│   ├── Pages/
│   │   └── Chat/
│   │       ├── Chat.razor       # Main chat page
│   │       ├── ChatHeader.razor # Chat header component
│   │       ├── ChatInput.razor  # Message input component
│   │       ├── ChatMessageList.razor # Message container
│   │       └── ChatMessageItem.razor # Individual message
│   └── Shared/
│       └── LoadingSpinner.razor # Loading indicator
├── Extensions/
│   └── ChatExtensions.cs        # Chat message utilities
├── wwwroot/
│   ├── app.css                  # Global styles
│   ├── js/
│   │   ├── chat-input.js        # Input enhancements
│   │   └── chat-scroll.js       # Auto-scroll functionality
│   └── favicon.ico
├── Properties/
│   └── launchSettings.json      # Launch profiles
└── Program.cs                   # Application entry point
```

### Technology Stack
- **.NET 8** - Target framework
- **Blazor Server** - Web application framework with SignalR
- **Microsoft.Extensions.AI** - AI integration and chat messaging
- **Bootstrap 5.3.0** - UI framework
- **Font Awesome 6.0.0** - Icons
- **Model Context Protocol** - MCP integration

## 🚀 Running the Application

### 1. Blazor Streaming Chat Mode
```powershell
# Run the web interface with streaming chat
dotnet run --project src/McpAgent.Client -- --blazor --port 8007

# Or use the launch profile
dotnet run --project src/McpAgent.Client --launch-profile "McpAgent.Client (Blazor)"
```

**Access the web interface at:** `http://localhost:8007`

### 2. Console Mode (Original)
```powershell
# Run the enhanced console client
dotnet run --project src/McpAgent.Client

# Run with specific modes
dotnet run --project src/McpAgent.Client -- --gemini-enhanced --gemini-key <your-key>
dotnet run --project src/McpAgent.Client -- --openai --openai-key <your-key>
```

### 3. Host Mode (API + UI)
```powershell
# Run with both API and simple web UI
dotnet run --project src/McpAgent.Client -- --host --port 8007
```

## 🎨 User Interface

### Chat Interface Features
- **Gradient backgrounds** with modern CSS styling
- **Message bubbles** with role-based colors:
  - User messages: Blue gradient on the right
  - Assistant messages: Gray on the left with robot icon
- **Auto-resize text input** that grows with content
- **Keyboard shortcuts**: Enter to send, Shift+Enter for new line
- **Loading animations** with smooth transitions
- **Error handling** with user-friendly messages

### Quick Actions
1. **Travel Agent** - Click to plan a trip to Tokyo with $2000 budget
2. **Research Agent** - Click to research AI trends in 2025
3. **Custom Tools** - Select tool and provide JSON parameters

## 🔧 Development

### Building the Project
```powershell
dotnet build src/McpAgent.Client
```

### Project Configuration
- **SDK**: `Microsoft.NET.Sdk.Web` (converted from console)
- **Target Framework**: `net8.0`
- **Blazor Server**: Enabled with interactive components
- **Static Files**: Served from `wwwroot`

### Launch Profiles
- **Console Mode**: Traditional console application
- **Blazor Mode**: Web interface on port 8007
- **Host Mode**: API + simple UI on port 8007
- **IIS Express**: Development server configuration

## 🔌 Integration

### MCP (Model Context Protocol)
The application is designed to integrate with MCP servers for:
- **Tool discovery** and execution
- **Resource access** and management
- **AI model** communication
- **Session management** and persistence

### AI Providers
- **Microsoft.Extensions.AI** integration for streaming
- **OpenAI** support with configurable models
- **Google Gemini** integration (when configured)

## 🎯 Usage Examples

### Quick Start
1. Run the application: `dotnet run --project src/McpAgent.Client -- --blazor`
2. Open browser to `http://localhost:8007`
3. Try the quick actions or type a message
4. Watch the streaming response simulation

### Custom Tool Calls
1. Select "Travel Agent" or "Research Agent" from dropdown
2. Enter JSON parameters like: `{"destination": "Paris", "budget": 3000}`
3. Click "Execute Tool" to see the response

### Message Streaming
- Type any message and press Enter
- Watch the assistant respond with streaming text
- See typing indicators and smooth animations
- Messages are saved in conversation history

## 📱 Responsive Design

The interface adapts to:
- **Desktop browsers** - Full-width layout with sidebar
- **Tablets** - Responsive grid layout
- **Mobile devices** - Stack layout with touch-friendly controls

## 🛠️ Customization

### Styling
- Modify `wwwroot/app.css` for global styles
- Update component CSS for specific styling
- Customize gradient colors in CSS variables

### Components
- Extend chat components for new features
- Add new pages under `Components/Pages/`
- Create shared components in `Components/Shared/`

### JavaScript
- Enhance `js/chat-input.js` for input features
- Modify `js/chat-scroll.js` for scroll behavior
- Add new JS modules as needed

## 🚀 Next Steps

To fully activate the MCP integration:
1. **Connect to MCP server** at runtime
2. **Replace simulation** with real AI provider calls
3. **Add authentication** for secure access
4. **Implement tool discovery** from MCP services
5. **Add message persistence** and session management

## 📋 Command Reference

```powershell
# Blazor mode (recommended)
dotnet run --project src/McpAgent.Client -- --blazor --port 8007

# Console mode with AI
dotnet run --project src/McpAgent.Client -- --openai --openai-key sk-...

# Host mode (API + UI)
dotnet run --project src/McpAgent.Client -- --host --port 8007

# Clear session
dotnet run --project src/McpAgent.Client -- --clear-session

# Verbose logging
dotnet run --project src/McpAgent.Client -- --blazor --verbose
```

---

## ✅ Implementation Status

- ✅ **Blazor Server application** - Complete
- ✅ **Streaming chat interface** - Complete with simulation
- ✅ **Component architecture** - Modular and maintainable
- ✅ **Responsive UI** - Bootstrap-based design
- ✅ **JavaScript interop** - Input enhancement and scrolling
- ✅ **Build and run** - Successfully building and running
- ✅ **Launch profiles** - Multiple run configurations
- 🔄 **MCP integration** - Ready for connection
- 🔄 **Real AI streaming** - Ready for provider integration

The transformation from console to web application is **complete and functional**! 🎉
