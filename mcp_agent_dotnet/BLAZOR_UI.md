# MCP Agent Client - Simple Web UI

## 🌟 Overview

The MCP Agent Client now includes a **simple HTML/JavaScript web interface** for easy interaction with MCP agents without needing to use the console. This lightweight solution provides all the functionality you need without the complexity of a full Blazor Server setup.

## 🚀 Quick Start

### 1. Start the MCP Client with Web UI
```bash
dotnet run --project src/McpAgent.Client -- --host --port 8007
```

### 2. Start the MCP Server (in another terminal)
```bash
dotnet run --project src/McpAgent.Server
```

### 3. Open the Web UI
Navigate to: **http://localhost:8007**

## 🎯 Features

### **Quick Actions**
- **✈️ Travel Agent**: One-click travel booking with predefined parameters
- **🔍 Research Agent**: One-click research on AI trends

### **Custom Tool Calls**
- Select any available tool (`travel_agent` or `research_agent`)
- Provide custom JSON arguments
- Execute and see real-time responses

### **Real-time Feedback**
- Loading indicators during processing
- Success responses with formatted output
- Error handling with detailed messages

## 📡 API Endpoints

| Endpoint | Purpose |
|----------|---------|
| **http://localhost:8007** | Simple Web UI |
| **http://localhost:8007/mcp** | JSON-RPC MCP API |
| **http://localhost:8007/health** | Health check |
| **http://localhost:8006/mcp** | MCP Server API |

## 🛠️ Usage Examples

### Travel Agent Example
```json
{
  "destination": "Tokyo",
  "budget": 2000,
  "preferences": "Cultural experiences and traditional food"
}
```

### Research Agent Example
```json
{
  "topic": "Artificial Intelligence trends in 2025",
  "depth": "comprehensive"
}
```

## 🎨 Technical Implementation

### **Simple & Fast**
- **Pure HTML/CSS/JavaScript**: No complex framework overhead
- **Bootstrap 5**: Modern, responsive styling
- **Vanilla JavaScript**: Direct fetch API calls to MCP server
- **Real-time Updates**: Immediate visual feedback

### **Key Features**
- **Loading States**: Visual spinner during processing
- **Error Handling**: User-friendly error messages
- **JSON Validation**: Client-side validation for custom arguments
- **Responsive Design**: Works on desktop, tablet, and mobile

## 🔄 Workflow

1. **Choose Action**: Use quick actions or custom tool calls
2. **Execute**: Click the execute button to run the tool
3. **Monitor**: Watch the real-time progress indicators
4. **Review**: See formatted responses and any errors
5. **Repeat**: Continue with more tool calls as needed

## 🌐 Benefits

- **No Console Required**: Easy web-based interaction
- **Lightning Fast**: Simple HTML loads instantly
- **User-Friendly**: Intuitive interface for complex MCP operations
- **No Dependencies**: Works in any modern browser
- **Lightweight**: Minimal overhead compared to full framework solutions
- **Real-time**: Immediate feedback and responses
- **Cross-Platform**: Works on Windows, Mac, Linux, mobile devices

The simple web UI maintains all the powerful MCP protocol features while providing an accessible interface that loads fast and works everywhere. Perfect for users who want a web interface without the complexity of heavy JavaScript frameworks!
