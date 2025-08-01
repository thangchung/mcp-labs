# Test script for MCP Travel Agent workflow
$body = @'
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "id": 1,
  "params": {
    "name": "travel_agent",
    "arguments": {
      "destination": "Tokyo",
      "budget": 2000,
      "preferences": "Cultural experiences and traditional food"
    }
  }
}
'@

Write-Host "Testing MCP Travel Agent workflow..."
Write-Host "Sending request to: http://localhost:8006/mcp"
Write-Host "Request body: $body"
Write-Host "---"

try {
    $response = Invoke-RestMethod -Uri "http://localhost:8006/mcp" -Method POST -ContentType "application/json" -Body $body
    Write-Host "✅ Response received:"
    $response | ConvertTo-Json -Depth 10
} catch {
    Write-Host "❌ Error occurred:"
    Write-Host $_.Exception.Message
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response body: $responseBody"
    }
}
