namespace McpAgent.XClient.Models;

// Supporting classes for elicitation state management
public class ElicitationState
{
    public bool IsVisible { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<FieldInfo> Fields { get; set; } = new();
    public string RequestId { get; set; } = string.Empty;
}

public class FieldInfo
{
    public string Name { get; set; } = string.Empty;
    public FieldType Type { get; set; }
    public bool Required { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string Pattern { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

public enum FieldType
{
    String,
    Number,
    Boolean,
    Email,
    Date,
    Enum
}
