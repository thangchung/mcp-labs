# Test script for MCP Research Agent workflow
$body = @'
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "id": 2,
  "params": {
    "name": "research_agent",
    "arguments": {
      "topic": "Artificial Intelligence trends in 2025",
      "depth": "comprehensive"
    }
  }
}
'@

Write-Host "Testing MCP Research Agent workflow..."
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
