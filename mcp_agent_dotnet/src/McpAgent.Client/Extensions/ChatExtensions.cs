using Microsoft.Extensions.AI;

namespace McpAgent.Client;

public static class ChatExtensions
{
    public static void AddMessages(this IList<ChatMessage> messages, ChatMessage update, Func<AIContent, bool>? filter = null)
    {
        // For simplicity, we'll just add the message
        // In a real implementation, you might want to merge content or handle different content types
        messages.Add(update);
    }

    public static string? GetText(this ChatMessage message)
    {
        // Microsoft.Extensions.AI ChatMessage has Text property
        return message.Text;
    }
}

public static class TextContentExtensions
{
    public static string Text { get; set; } = string.Empty;
}
