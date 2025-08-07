using System.Text.Json.Serialization;

namespace McpAgent.XServer.Models;

/// <summary>
/// MCP-compliant elicitation request model
/// </summary>
public class McpElicitationRequest
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("requestedSchema")]
    public McpJsonSchema RequestedSchema { get; set; } = new();

    [JsonPropertyName("sessionId")]
    public int SessionId { get; set; }

    [JsonPropertyName("toolName")]
    public string ToolName { get; set; } = string.Empty;
}

/// <summary>
/// MCP-compliant elicitation response model
/// </summary>
public class McpElicitationResponse
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty; // "accept", "reject", "cancel"

    [JsonPropertyName("content")]
    public Dictionary<string, object?> Content { get; set; } = new();

    [JsonPropertyName("sessionId")]
    public int SessionId { get; set; }
}

/// <summary>
/// MCP-compliant JSON Schema model
/// </summary>
public class McpJsonSchema
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    [JsonPropertyName("properties")]
    public Dictionary<string, McpSchemaProperty> Properties { get; set; } = new();

    [JsonPropertyName("required")]
    public List<string> Required { get; set; } = new();
}

/// <summary>
/// MCP JSON Schema property definition
/// </summary>
public class McpSchemaProperty
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("minimum")]
    public double? Minimum { get; set; }

    [JsonPropertyName("maximum")]
    public double? Maximum { get; set; }

    [JsonPropertyName("minLength")]
    public int? MinLength { get; set; }

    [JsonPropertyName("maxLength")]
    public int? MaxLength { get; set; }

    [JsonPropertyName("pattern")]
    public string? Pattern { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("enum")]
    public List<string>? Enum { get; set; }

    [JsonPropertyName("enumNames")]
    public List<string>? EnumNames { get; set; }

    [JsonPropertyName("default")]
    public object? Default { get; set; }
}

/// <summary>
/// Elicitation UI state model
/// </summary>
public class ElicitationUIState
{
    public bool IsVisible { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<ElicitationField> Fields { get; set; } = new();
    public int SessionId { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public bool IsSubmitting { get; set; }
    public Dictionary<string, string> ValidationErrors { get; set; } = new();
}

/// <summary>
/// Individual elicitation field for UI rendering
/// </summary>
public class ElicitationField
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public object? Value { get; set; }
    public string? ValidationMessage { get; set; }
    
    // Type-specific properties
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? Pattern { get; set; }
    public string? Format { get; set; }
    public List<string>? Options { get; set; }
    public List<string>? OptionLabels { get; set; }
    public object? DefaultValue { get; set; }
}
