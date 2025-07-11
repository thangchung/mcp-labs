#!/bin/bash

# Test A2A Integration script
echo "🧪 Testing A2A .NET Integration..."

# Check if Weather MCP Server is running
echo "🔍 Checking if Weather MCP Server is running on port 3002..."
if curl -s http://localhost:3002/sse > /dev/null; then
    echo "✅ Weather MCP Server is running"
else
    echo "❌ Weather MCP Server is not running"
    echo "   Please start it first using: ./start-weather-server.sh"
    exit 1
fi

# Run the integration example
echo "🤖 Running A2A Integration Example..."
cd "$(dirname "$0")/integration-example"

# Build the project
echo "🔨 Building .NET project..."
dotnet build

if [ $? -eq 0 ]; then
    echo "✅ Build successful"
    echo "🚀 Running integration example..."
    dotnet run
else
    echo "❌ Build failed"
    exit 1
fi