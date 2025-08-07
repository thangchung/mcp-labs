using System.Text.Json.Serialization;

namespace McpAgent.XClient.Models;

/// <summary>
/// Represents an MCP elicitation request following the official Model Context Protocol specification.
/// This class implements the elicitation/create protocol for interactive parameter collection.
/// </summary>
public class McpElicitationRequest
{
    /// <summary>
    /// Human-readable message explaining what information is being requested from the user.
    /// This message will be displayed in the elicitation modal to provide context.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// JSON Schema defining the structure and validation rules for the expected user response.
    /// Must be a flat object with primitive properties only (string, number, boolean, null).
    /// </summary>
    [JsonPropertyName("requestedSchema")]
    public McpJsonSchema RequestedSchema { get; set; } = new();
}

/// <summary>
/// Represents the user's response to an MCP elicitation request.
/// Follows the three-action model: accept, reject, or cancel.
/// </summary>
public class McpElicitationResponse
{
    /// <summary>
    /// The user's action choice:
    /// - "accept": User provided the requested information
    /// - "reject": User explicitly declined to provide information
    /// - "cancel": User dismissed/cancelled the interaction
    /// </summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// The user's submitted data (only present when Action is "accept").
    /// Contains key-value pairs matching the requestedSchema structure.
    /// </summary>
    [JsonPropertyName("content")]
    public Dictionary<string, object> Content { get; set; } = new();
}

/// <summary>
/// JSON Schema definition for MCP elicitation requests.
/// Restricted to flat objects with primitive properties to simplify client implementation.
/// </summary>
public class McpJsonSchema
{
    /// <summary>
    /// Always "object" for MCP elicitation schemas.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    /// <summary>
    /// Dictionary of property definitions where key is the property name
    /// and value contains the property's schema definition.
    /// </summary>
    [JsonPropertyName("properties")]
    public Dictionary<string, McpJsonSchemaProperty> Properties { get; set; } = new();

    /// <summary>
    /// Array of property names that are required for submission.
    /// Optional properties may be left empty by the user.
    /// </summary>
    [JsonPropertyName("required")]
    public string[]? Required { get; set; }
}

/// <summary>
/// Individual property definition within an MCP JSON Schema.
/// Supports validation rules and UI generation hints.
/// </summary>
public class McpJsonSchemaProperty
{
    /// <summary>
    /// JSON Schema type: "string", "number", "integer", "boolean", or "null"
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Optional display title for the property (used as field label in UI)
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Human-readable description explaining what this property represents
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// JSON Schema format hint (e.g., "email", "date", "uri")
    /// Used to determine appropriate UI input type
    /// </summary>
    [JsonPropertyName("format")]
    public string? Format { get; set; }

    /// <summary>
    /// Minimum value for numeric types
    /// </summary>
    [JsonPropertyName("minimum")]
    public double? Minimum { get; set; }

    /// <summary>
    /// Maximum value for numeric types
    /// </summary>
    [JsonPropertyName("maximum")]
    public double? Maximum { get; set; }

    /// <summary>
    /// Minimum length for string types
    /// </summary>
    [JsonPropertyName("minLength")]
    public int? MinLength { get; set; }

    /// <summary>
    /// Maximum length for string types
    /// </summary>
    [JsonPropertyName("maxLength")]
    public int? MaxLength { get; set; }

    /// <summary>
    /// Regular expression pattern for string validation
    /// </summary>
    [JsonPropertyName("pattern")]
    public string? Pattern { get; set; }

    /// <summary>
    /// Array of allowed values (creates a dropdown/select in UI)
    /// </summary>
    [JsonPropertyName("enum")]
    public string[]? Enum { get; set; }

    /// <summary>
    /// Human-readable names for enum values (parallel to Enum array)
    /// </summary>
    [JsonPropertyName("enumNames")]
    public string[]? EnumNames { get; set; }

    /// <summary>
    /// Default value for the property
    /// </summary>
    [JsonPropertyName("default")]
    public object? Default { get; set; }
}
