# McpAgent.XClient - Blazor WebAssembly SignalR Chat Application

This project is a Blazor WebAssembly application with SignalR support that provides a real-time chat interface. It's designed as an interactive client for the McpAgent ecosystem.

## Project Structure

```
McpAgent.XClient/
├── Pages/
│   ├── Index.razor          # Home page
│   └── Chat.razor           # Real-time chat page with SignalR
├── Shared/
│   ├── MainLayout.razor     # Main layout component
│   ├── MainLayout.razor.css # Layout styles
│   ├── NavMenu.razor        # Navigation menu
│   └── NavMenu.razor.css    # Navigation styles
├── wwwroot/
│   ├── css/
│   │   ├── app.css          # Application styles
│   │   ├── bootstrap/       # Bootstrap CSS
│   │   └── open-iconic/     # Icon fonts
│   ├── index.html           # Main HTML template
│   └── favicon.png          # Application icon
├── _Imports.razor           # Global using statements
├── App.razor                # Root application component
└── Program.cs               # Application entry point
```

## Features

- **Real-time Chat**: Built with SignalR for instant messaging
- **Responsive Design**: Bootstrap-based UI that works on all devices
- **Modern Blazor**: Uses latest Blazor WebAssembly features
- **Clean Architecture**: Well-organized component structure

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code with C# extension

### Running the Application

1. **Start the Server** (McpAgent.XServer):
   ```bash
   cd src/McpAgent.XServer
   dotnet run
   ```
   The server will start on `https://localhost:5001`

2. **The Client** is automatically served by the server at the same URL

### Development

The application consists of two main projects:

- **McpAgent.XClient**: Blazor WebAssembly client application
- **McpAgent.XServer**: ASP.NET Core server hosting SignalR hub

## Chat Functionality

The chat feature (`/chat` page) includes:

- **User Input**: Set your display name
- **Message Input**: Type messages with Enter key support
- **Real-time Updates**: Messages appear instantly for all connected users
- **Connection Status**: Visual indicator of SignalR connection state
- **Responsive Layout**: Works on desktop and mobile devices

## SignalR Hub

The `ChatHub` class provides the following functionality:

- `SendMessage(user, message)`: Broadcasts messages to all connected clients
- Real-time connection management
- Cross-platform compatibility

## Customization

### Styling
- Modify `wwwroot/css/app.css` for custom styles
- Update Bootstrap theme in `wwwroot/css/bootstrap/`
- Customize component styles in `.razor.css` files

### Features
- Add new pages in the `Pages/` folder
- Extend the SignalR hub for additional functionality
- Integrate with other McpAgent services

## Architecture Notes

This project follows the Blazor WebAssembly hosted model:
- Client runs in the browser using WebAssembly
- Server provides SignalR hub and static file hosting
- Real-time communication via SignalR WebSocket connections

## Future Enhancements

- User authentication and authorization
- Message persistence and history
- File sharing capabilities
- Integration with MCP (Model Context Protocol) servers
- Advanced chat features (typing indicators, message reactions, etc.)
