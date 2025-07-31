using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpAgent.Server.Services;

/// <summary>
/// JSON-RPC 2.0 protocol types for MCP communication
/// </summary>
public static class McpJsonRpcTypes
{
    /// <summary>
    /// JSON-RPC 2.0 notification (no response expected)
    /// </summary>
    public class JsonRpcNotification
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; init; } = "2.0";

        [JsonPropertyName("method")]
        public required string Method { get; init; }

        [JsonPropertyName("params")]
        public object? Params { get; init; }
    }

    /// <summary>
    /// JSON-RPC 2.0 request (response expected)
    /// </summary>
    public class JsonRpcRequest : JsonRpcNotification
    {
        [JsonPropertyName("id")]
        public required object Id { get; init; }
    }

    /// <summary>
    /// JSON-RPC 2.0 response
    /// </summary>
    public class JsonRpcResponse
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; init; } = "2.0";

        [JsonPropertyName("id")]
        public required object Id { get; init; }

        [JsonPropertyName("result")]
        public object? Result { get; init; }

        [JsonPropertyName("error")]
        public JsonRpcError? Error { get; init; }
    }

    /// <summary>
    /// JSON-RPC 2.0 error object
    /// </summary>
    public class JsonRpcError
    {
        [JsonPropertyName("code")]
        public required int Code { get; init; }

        [JsonPropertyName("message")]
        public required string Message { get; init; }

        [JsonPropertyName("data")]
        public object? Data { get; init; }
    }

    /// <summary>
    /// MCP progress notification parameters
    /// </summary>
    public class ProgressParams
    {
        [JsonPropertyName("progressToken")]
        public required string ProgressToken { get; init; }

        [JsonPropertyName("progress")]
        public required int Progress { get; init; }

        [JsonPropertyName("total")]
        public required int Total { get; init; }

        [JsonPropertyName("message")]
        public required string Message { get; init; }

        [JsonPropertyName("relatedRequestId")]
        public string? RelatedRequestId { get; init; }
    }

    /// <summary>
    /// MCP log message notification parameters
    /// </summary>
    public class LogParams
    {
        [JsonPropertyName("level")]
        public required string Level { get; init; }

        [JsonPropertyName("data")]
        public required string Data { get; init; }

        [JsonPropertyName("logger")]
        public required string Logger { get; init; }

        [JsonPropertyName("relatedRequestId")]
        public string? RelatedRequestId { get; init; }
    }

    /// <summary>
    /// MCP elicitation request parameters
    /// </summary>
    public class ElicitationParams
    {
        [JsonPropertyName("message")]
        public required string Message { get; init; }

        [JsonPropertyName("requestedSchema")]
        public object? RequestedSchema { get; init; }

        [JsonPropertyName("relatedRequestId")]
        public string? RelatedRequestId { get; init; }
    }

    /// <summary>
    /// MCP elicitation response
    /// </summary>
    public class ElicitationResponse
    {
        [JsonPropertyName("action")]
        public required string Action { get; init; }

        [JsonPropertyName("content")]
        public required Dictionary<string, object?> Content { get; init; }
    }

    /// <summary>
    /// MCP sampling request parameters
    /// </summary>
    public class SamplingParams
    {
        [JsonPropertyName("messages")]
        public required IEnumerable<SamplingMessageJson> Messages { get; init; }

        [JsonPropertyName("maxTokens")]
        public int? MaxTokens { get; init; }

        [JsonPropertyName("relatedRequestId")]
        public string? RelatedRequestId { get; init; }
    }

    /// <summary>
    /// JSON representation of sampling message
    /// </summary>
    public class SamplingMessageJson
    {
        [JsonPropertyName("role")]
        public required string Role { get; init; }

        [JsonPropertyName("content")]
        public required SamplingContentJson Content { get; init; }
    }

    /// <summary>
    /// JSON representation of sampling content
    /// </summary>
    public class SamplingContentJson
    {
        [JsonPropertyName("type")]
        public required string Type { get; init; }

        [JsonPropertyName("text")]
        public required string Text { get; init; }
    }

    /// <summary>
    /// MCP sampling response
    /// </summary>
    public class SamplingResponse
    {
        [JsonPropertyName("role")]
        public required string Role { get; init; }

        [JsonPropertyName("content")]
        public required SamplingContentJson Content { get; init; }

        [JsonPropertyName("model")]
        public required string Model { get; init; }

        [JsonPropertyName("stopReason")]
        public required string StopReason { get; init; }
    }

    /// <summary>
    /// MCP method names according to specification
    /// </summary>
    public static class Methods
    {
        public const string Progress = "notifications/progress";
        public const string Log = "notifications/log";
        public const string Elicit = "elicitation/request";
        public const string Sampling = "sampling/createSample";
    }
}
