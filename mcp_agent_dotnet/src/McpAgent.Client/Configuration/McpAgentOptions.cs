namespace McpAgent.Client.Configuration;

public class McpAgentOptions
{
    public const string SectionName = "McpAgent";

    public int HostPort { get; set; } = 8007;
    public string? GeminiApiKey { get; set; }
    public OpenAIOptions OpenAI { get; set; } = new();
}

public class OpenAIOptions
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gpt-4o-mini";
    public string Endpoint { get; set; } = "https://api.openai.com/v1";
}
