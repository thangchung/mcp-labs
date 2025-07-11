#!/bin/bash

# Start Weather MCP Server script
echo "🌤️  Starting Weather MCP Server on port 3002..."

cd "$(dirname "$0")/weather-server"

# Check if requirements are installed
if ! pip show mcp &> /dev/null; then
    echo "📦 Installing Python dependencies..."
    pip install -r requirements.txt
fi

echo "🚀 Starting Weather MCP Server..."
python weather_server.py --server_type sse --host 0.0.0.0 --port 3002