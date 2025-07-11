#!/bin/bash

# Test the integration demo without requiring the weather server to be running
echo "🧪 Running A2A .NET Integration Demo (offline mode)..."

cd "$(dirname "$0")/integration-example"

# Set environment variable to skip server connection test
export SKIP_SERVER_CONNECTION=true

echo "🚀 Running integration example..."
dotnet run