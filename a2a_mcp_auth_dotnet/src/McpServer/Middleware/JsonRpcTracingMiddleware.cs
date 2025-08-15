using System.Diagnostics;
using System.Text.Json;

namespace McpServer.Middleware;

/// <summary>
/// Middleware to trace all JSON-RPC method calls and responses
/// </summary>
public class JsonRpcTracingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JsonRpcTracingMiddleware> _logger;
    private static readonly ActivitySource ActivitySource = new("JsonRpcTracing", "1.0.0");

    public JsonRpcTracingMiddleware(RequestDelegate next, ILogger<JsonRpcTracingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only process requests that might be JSON-RPC
        if (ShouldTrace(context))
        {
            await TraceJsonRpcRequestAsync(context);
        }
        else
        {
            await _next(context);
        }
    }

    private bool ShouldTrace(HttpContext context)
    {
        // Trace all POST requests (A2A and MCP typically use POST)
        if (context.Request.Method != "POST")
            return false;

        // Check content type for JSON
        var contentType = context.Request.ContentType?.ToLowerInvariant();
        var hasJsonContent = contentType?.Contains("application/json") == true;

        // Check paths that might contain JSON-RPC
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        var hasRelevantPath = path.Contains("/a2a") || 
                             path.Contains("/mcp") || 
                             path.Contains("/pong") || 
                             path.Contains("/ping") ||
                             path.StartsWith("/message") ||
                             path.StartsWith("/call") ||
                             path.StartsWith("/send");

        var shouldTrace = hasJsonContent || hasRelevantPath;
        
        // Add debug logging to understand what requests are coming through
        _logger.LogDebug("JsonRpcTracing: Method={Method}, Path={Path}, ContentType={ContentType}, ShouldTrace={ShouldTrace}",
            context.Request.Method, context.Request.Path, contentType, shouldTrace);
            
        return shouldTrace;
    }

    private async Task TraceJsonRpcRequestAsync(HttpContext context)
    {
        // Read the request body
        context.Request.EnableBuffering();
        var requestBody = await ReadRequestBodyAsync(context.Request);
        context.Request.Body.Position = 0; // Reset stream position

        // Parse JSON-RPC if possible
        var jsonRpcInfo = TryParseJsonRpc(requestBody);
        
        using var activity = ActivitySource.StartActivity("JsonRpc.Call");
        if (activity != null)
        {
            // Add tags for the activity
            activity.SetTag("http.method", context.Request.Method);
            activity.SetTag("http.url", context.Request.Path);
            activity.SetTag("rpc.system", "jsonrpc");
            
            if (jsonRpcInfo != null)
            {
                activity.SetTag("rpc.method", jsonRpcInfo.Method);
                activity.SetTag("rpc.jsonrpc", jsonRpcInfo.Version);
                activity.SetTag("rpc.id", jsonRpcInfo.Id?.ToString());
                activity.SetTag("rpc.request_size", requestBody.Length);
            }

            // Log the JSON-RPC request
            _logger.LogInformation("JSON-RPC Request: Method={Method}, Id={Id}, Path={Path}, Size={Size}",
                jsonRpcInfo?.Method ?? "unknown",
                jsonRpcInfo?.Id?.ToString() ?? "none",
                context.Request.Path,
                requestBody.Length);

            // Capture response
            var originalBodyStream = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            try
            {
                await _next(context);

                // Read response
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
                var responseJsonRpc = TryParseJsonRpcResponse(responseBody);

                // Add response tags
                activity.SetTag("http.status_code", context.Response.StatusCode);
                activity.SetTag("rpc.response_size", responseBody.Length);
                
                if (responseJsonRpc != null)
                {
                    activity.SetTag("rpc.response_id", responseJsonRpc.Id?.ToString());
                    activity.SetTag("rpc.has_error", responseJsonRpc.Error != null);
                    
                    if (responseJsonRpc.Error != null)
                    {
                        activity.SetTag("rpc.error_code", responseJsonRpc.Error.Code);
                        activity.SetTag("rpc.error_message", responseJsonRpc.Error.Message);
                        activity.SetStatus(ActivityStatusCode.Error, responseJsonRpc.Error.Message);
                    }
                }

                // Log the JSON-RPC response
                _logger.LogInformation("JSON-RPC Response: Method={Method}, Id={Id}, Status={Status}, Size={Size}, HasError={HasError}",
                    jsonRpcInfo?.Method ?? "unknown",
                    responseJsonRpc?.Id?.ToString() ?? jsonRpcInfo?.Id?.ToString() ?? "none",
                    context.Response.StatusCode,
                    responseBody.Length,
                    responseJsonRpc?.Error != null);

                // Copy response back to original stream
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex, "Error during JSON-RPC call: Method={Method}, Id={Id}",
                    jsonRpcInfo?.Method ?? "unknown",
                    jsonRpcInfo?.Id?.ToString() ?? "none");
                throw;
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }
        else
        {
            await _next(context);
        }
    }

    private async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        using var reader = new StreamReader(request.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private JsonRpcRequest? TryParseJsonRpc(string body)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(body))
                return null;

            var jsonDoc = JsonDocument.Parse(body);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("method", out var methodProp))
            {
                return new JsonRpcRequest
                {
                    Method = methodProp.GetString(),
                    Version = root.TryGetProperty("jsonrpc", out var versionProp) ? versionProp.GetString() : null,
                    Id = root.TryGetProperty("id", out var idProp) ? idProp.GetRawText() : null
                };
            }
        }
        catch (JsonException)
        {
            // Not valid JSON or not JSON-RPC format
        }

        return null;
    }

    private JsonRpcResponse? TryParseJsonRpcResponse(string body)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(body))
                return null;

            var jsonDoc = JsonDocument.Parse(body);
            var root = jsonDoc.RootElement;

            // Check if it's a JSON-RPC response (has id and either result or error)
            if (root.TryGetProperty("id", out var idProp))
            {
                var response = new JsonRpcResponse
                {
                    Id = idProp.GetRawText(),
                    Version = root.TryGetProperty("jsonrpc", out var versionProp) ? versionProp.GetString() : null
                };

                if (root.TryGetProperty("error", out var errorProp))
                {
                    response.Error = new JsonRpcError
                    {
                        Code = errorProp.TryGetProperty("code", out var codeProp) ? codeProp.GetInt32() : 0,
                        Message = errorProp.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : null
                    };
                }

                return response;
            }
        }
        catch (JsonException)
        {
            // Not valid JSON or not JSON-RPC format
        }

        return null;
    }

    private class JsonRpcRequest
    {
        public string? Method { get; set; }
        public string? Version { get; set; }
        public string? Id { get; set; }
    }

    private class JsonRpcResponse
    {
        public string? Id { get; set; }
        public string? Version { get; set; }
        public JsonRpcError? Error { get; set; }
    }

    private class JsonRpcError
    {
        public int Code { get; set; }
        public string? Message { get; set; }
    }
}
