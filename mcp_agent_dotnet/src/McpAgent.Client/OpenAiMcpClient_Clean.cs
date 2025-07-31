//using Microsoft.Extensions.Logging;
//using System.Text.Json;
//using System.Text;

//namespace McpAgent.Client;

//public class OpenAiMcpClient
//{
//    private readonly string _serverUrl;
//    private readonly ILoggerFactory _loggerFactory;
//    private readonly ILogger<OpenAiMcpClient> _logger;
//    private readonly HttpClient _httpClient;
//    private readonly string _model;
//    private readonly List<object> _conversationHistory;

//    public OpenAiMcpClient(string serverUrl, ILoggerFactory loggerFactory, string apiKey, string? model = null, string? endpoint = null)
//    {
//        _serverUrl = serverUrl;
//        _loggerFactory = loggerFactory;
//        _logger = loggerFactory.CreateLogger<OpenAiMcpClient>();
//        _model = model ?? "gpt-4o-mini";
//        _conversationHistory = new List<object>();

//        // Configure HTTP client for OpenAI API
//        _httpClient = new HttpClient();
//        var baseEndpoint = endpoint ?? "https://api.openai.com/v1";
//        _httpClient.BaseAddress = new Uri(baseEndpoint);
//        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

//        _logger.LogInformation("🤖 OpenAI client configured with model: {Model}, endpoint: {Endpoint}", _model, baseEndpoint);
//    }

//    public async Task RunAsync()
//    {
//        _logger.LogInformation("🚀 Starting OpenAI-powered MCP client...");

//        // Test AI connection first
//        try
//        {
//            await TestOpenAIConnection();
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Failed to connect to OpenAI. Please check your API key and endpoint.");
//            return;
//        }

//        try
//        {
//            using var mcpHttpClient = new HttpClient();
//            var client = new DirectHttpMcpClient(mcpHttpClient, _serverUrl, _loggerFactory);
            
//            await client.InitializeAsync();
//            _logger.LogInformation("✅ Connected to MCP server");

//            var toolsResult = await client.ListToolsAsync();
//            var toolsArray = toolsResult.GetProperty("tools").EnumerateArray().ToList();
//            _logger.LogInformation("🔧 Available tools: {Tools}", string.Join(", ", 
//                toolsArray.Select(t => t.GetProperty("name").GetString())));

//            ShowWelcomeMessage();

//            while (true)
//            {
//                Console.Write("\n💭 You: ");
//                var input = Console.ReadLine();

//                if (string.IsNullOrWhiteSpace(input))
//                    continue;

//                if (input.ToLowerInvariant() == "exit" || input.ToLowerInvariant() == "quit")
//                    break;

//                if (input.ToLowerInvariant() == "help")
//                {
//                    ShowHelp();
//                    continue;
//                }

//                if (input.ToLowerInvariant() == "tools")
//                {
//                    await DisplayToolsList(client);
//                    continue;
//                }

//                if (input.ToLowerInvariant() == "clear")
//                {
//                    _conversationHistory.Clear();
//                    Console.WriteLine("🗑️ Conversation history cleared");
//                    continue;
//                }

//                try
//                {
//                    await ProcessUserInput(client, input);
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "💥 Error processing user input");
//                    Console.WriteLine($"❌ An error occurred: {ex.Message}");
//                }
//            }
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "💥 Fatal error in OpenAI MCP client");
//            Console.WriteLine($"❌ Connection failed: {ex.Message}");
//        }

//        Console.WriteLine("👋 Goodbye!");
//    }

//    private async Task TestOpenAIConnection()
//    {
//        var testPayload = new
//        {
//            model = _model,
//            messages = new[]
//            {
//                new { role = "user", content = "Hello, this is a connection test. Please respond with 'Connection successful'." }
//            },
//            max_tokens = 50
//        };

//        var json = JsonSerializer.Serialize(testPayload);
//        var content = new StringContent(json, Encoding.UTF8, "application/json");

//        var response = await _httpClient.PostAsync("/chat/completions", content);
        
//        if (!response.IsSuccessStatusCode)
//        {
//            var errorContent = await response.Content.ReadAsStringAsync();
//            throw new Exception($"OpenAI API test failed: {response.StatusCode} - {errorContent}");
//        }

//        _logger.LogInformation("🔗 OpenAI connection test successful");
//    }

//    private void ShowWelcomeMessage()
//    {
//        Console.WriteLine();
//        Console.WriteLine("🤖 Welcome to the OpenAI-powered MCP Client!");
//        Console.WriteLine("════════════════════════════════════════════════");
//        Console.WriteLine($"🧠 Powered by OpenAI {_model} with natural language understanding");
//        Console.WriteLine("🔧 Connected to MCP server for tool execution");
//        Console.WriteLine("💬 Type naturally - I'll understand and help you!");
//        Console.WriteLine();
//        Console.WriteLine("💡 Examples:");
//        Console.WriteLine("  • 'I want to travel to Tokyo'");
//        Console.WriteLine("  • 'Research the latest AI trends'");
//        Console.WriteLine("  • 'help' - Show available commands");
//        Console.WriteLine("  • 'tools' - List available MCP tools");
//        Console.WriteLine("  • 'clear' - Clear conversation history");
//        Console.WriteLine("  • 'exit' - Exit the client");
//        Console.WriteLine("════════════════════════════════════════════════");
//        Console.WriteLine();
//    }

//    private void ShowHelp()
//    {
//        Console.WriteLine();
//        Console.WriteLine("🤖 OpenAI MCP Client Help");
//        Console.WriteLine("═══════════════════════════");
//        Console.WriteLine($"🧠 AI-Powered Features (using {_model}):");
//        Console.WriteLine("  • Natural language conversation with OpenAI");
//        Console.WriteLine("  • Intelligent tool selection and execution");
//        Console.WriteLine("  • Context-aware responses");
//        Console.WriteLine("  • Conversation history management");
//        Console.WriteLine("  • Support for GitHub AI Models endpoint");
//        Console.WriteLine();
//        Console.WriteLine("💬 Commands:");
//        Console.WriteLine("  help  - Show this help message");
//        Console.WriteLine("  tools - List available MCP tools");
//        Console.WriteLine("  clear - Clear conversation history");
//        Console.WriteLine("  exit  - Exit the client");
//        Console.WriteLine("═══════════════════════════");
//        Console.WriteLine();
//    }

//    private async Task DisplayToolsList(DirectHttpMcpClient client)
//    {
//        Console.WriteLine();
//        Console.WriteLine("🔧 Available MCP Tools");
//        Console.WriteLine("═══════════════════════");

//        try
//        {
//            var toolsResult = await client.ListToolsAsync();
//            var toolsArray = toolsResult.GetProperty("tools").EnumerateArray().ToList();
            
//            foreach (var tool in toolsArray)
//            {
//                var name = tool.GetProperty("name").GetString();
//                var description = tool.TryGetProperty("description", out var desc) ? desc.GetString() : "No description";
                
//                Console.WriteLine($"🛠️  {name}");
//                Console.WriteLine($"   📝 {description}");
                
//                if (tool.TryGetProperty("inputSchema", out var inputSchema) && 
//                    inputSchema.TryGetProperty("properties", out var properties))
//                {
//                    Console.WriteLine($"   📋 Parameters:");
//                    foreach (var param in properties.EnumerateObject())
//                    {
//                        var required = inputSchema.TryGetProperty("required", out var requiredArray) && 
//                                     requiredArray.EnumerateArray().Any(r => r.GetString() == param.Name) ? " (required)" : "";
//                        Console.WriteLine($"      • {param.Name}{required}");
//                    }
//                }
//                Console.WriteLine();
//            }
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error listing tools");
//            Console.WriteLine("❌ Failed to list tools");
//        }

//        Console.WriteLine("═══════════════════════");
//        Console.WriteLine();
//    }

//    private async Task ProcessUserInput(DirectHttpMcpClient client, string userInput)
//    {
//        _logger.LogDebug("Processing user input: {Input}", userInput);

//        // Add user message to conversation history
//        _conversationHistory.Add(new { role = "user", content = userInput });

//        // Determine if we need to use MCP tools
//        var toolAction = await AnalyzeInputForTools(userInput);

//        string aiResponse;
//        if (toolAction != null)
//        {
//            _logger.LogInformation("🔧 Executing tool: {ToolName}", toolAction.ToolName);
            
//            // Execute the MCP tool
//            var toolResult = await ExecuteTool(client, toolAction);
            
//            // Get AI response with tool result context
//            aiResponse = await GetOpenAIResponseWithContext(userInput, toolResult, toolAction.ToolName);
//        }
//        else
//        {
//            // Direct AI conversation without tools
//            aiResponse = await GetOpenAIResponse();
//        }

//        // Add AI response to conversation history
//        _conversationHistory.Add(new { role = "assistant", content = aiResponse });

//        Console.WriteLine($"🤖 Assistant: {aiResponse}");
//    }

//    private async Task<ToolAction?> AnalyzeInputForTools(string input)
//    {
//        var analysisPrompt = $@"Analyze this user input and determine if it requires using MCP tools:

//Input: ""{input}""

//Available tools:
//- travel_agent: For travel booking, destinations, hotels, flights
//- research_agent: For research, information gathering, analysis

//If a tool is needed, respond with JSON in this format:
//{{
//    ""toolName"": ""tool_name"",
//    ""arguments"": {{
//        ""param1"": ""value1"",
//        ""param2"": ""value2""
//    }}
//}}

//If no tool is needed, respond with: {{""toolName"": null}}

//Extract specific parameters from the input when possible. For missing required parameters, use ""EXTRACT_NEEDED"" as the value.";

//        try
//        {
//            var messages = new[]
//            {
//                new { role = "system", content = "You are a tool selection assistant. Analyze user inputs and determine appropriate MCP tool usage." },
//                new { role = "user", content = analysisPrompt }
//            };

//            var response = await GetOpenAIResponse(messages);

//            _logger.LogDebug("Tool analysis response: {Response}", response);

//            // Try to parse JSON response
//            if (response.Contains("\"toolName\""))
//            {
//                try
//                {
//                    var jsonStart = response.IndexOf('{');
//                    var jsonEnd = response.LastIndexOf('}') + 1;
//                    if (jsonStart >= 0 && jsonEnd > jsonStart)
//                    {
//                        var jsonText = response.Substring(jsonStart, jsonEnd - jsonStart);
//                        var toolAnalysis = JsonSerializer.Deserialize<ToolAnalysis>(jsonText);
                        
//                        if (!string.IsNullOrWhiteSpace(toolAnalysis?.ToolName))
//                        {
//                            return new ToolAction
//                            {
//                                ToolName = toolAnalysis.ToolName,
//                                Arguments = toolAnalysis.Arguments ?? new Dictionary<string, object?>()
//                            };
//                        }
//                    }
//                }
//                catch (JsonException ex)
//                {
//                    _logger.LogWarning(ex, "Failed to parse tool analysis JSON, falling back to keyword detection");
//                }
//            }

//            // Fallback to simple keyword detection
//            return DetectToolsFromKeywords(input);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogWarning(ex, "AI tool analysis failed, using keyword detection");
//            return DetectToolsFromKeywords(input);
//        }
//    }

//    private ToolAction? DetectToolsFromKeywords(string input)
//    {
//        var lowerInput = input.ToLowerInvariant();

//        // Travel-related keywords
//        if (lowerInput.Contains("travel") || lowerInput.Contains("trip") || lowerInput.Contains("flight") || 
//            lowerInput.Contains("hotel") || lowerInput.Contains("destination") || lowerInput.Contains("vacation") ||
//            lowerInput.Contains("visit") || lowerInput.Contains("go to") || lowerInput.Contains("book"))
//        {
//            var destination = ExtractDestination(input);
//            return new ToolAction
//            {
//                ToolName = "travel_agent",
//                Arguments = new Dictionary<string, object?>
//                {
//                    ["destination"] = !string.IsNullOrWhiteSpace(destination) ? destination : "EXTRACT_NEEDED"
//                }
//            };
//        }

//        // Research-related keywords
//        if (lowerInput.Contains("research") || lowerInput.Contains("study") || lowerInput.Contains("analyze") ||
//            lowerInput.Contains("information") || lowerInput.Contains("learn about") || lowerInput.Contains("find out") ||
//            lowerInput.Contains("investigate") || lowerInput.Contains("explore"))
//        {
//            var topic = ExtractTopic(input);
//            return new ToolAction
//            {
//                ToolName = "research_agent",
//                Arguments = new Dictionary<string, object?>
//                {
//                    ["topic"] = !string.IsNullOrWhiteSpace(topic) ? topic : "EXTRACT_NEEDED"
//                }
//            };
//        }

//        return null;
//    }

//    private string ExtractDestination(string input)
//    {
//        // Simple extraction logic - could be enhanced with NLP
//        var patterns = new[]
//        {
//            @"(?:travel to|go to|visit|trip to)\s+([A-Za-z\s,]+?)(?:\s|$|[.!?])",
//            @"(?:in|to)\s+([A-Z][a-z]+(?:\s+[A-Z][a-z]+)*)",
//        };

//        foreach (var pattern in patterns)
//        {
//            var match = System.Text.RegularExpressions.Regex.Match(input, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
//            if (match.Success && match.Groups.Count > 1)
//            {
//                return match.Groups[1].Value.Trim();
//            }
//        }

//        return "";
//    }

//    private string ExtractTopic(string input)
//    {
//        // Simple extraction logic - could be enhanced with NLP
//        var patterns = new[]
//        {
//            @"(?:research|study|analyze|learn about|find out about|investigate|explore)\s+([A-Za-z\s,]+?)(?:\s|$|[.!?])",
//            @"(?:about|on)\s+([A-Za-z\s,]+?)(?:\s|$|[.!?])",
//        };

//        foreach (var pattern in patterns)
//        {
//            var match = System.Text.RegularExpressions.Regex.Match(input, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
//            if (match.Success && match.Groups.Count > 1)
//            {
//                return match.Groups[1].Value.Trim();
//            }
//        }

//        return "";
//    }

//    private async Task<string> ExecuteTool(DirectHttpMcpClient client, ToolAction toolAction)
//    {
//        try
//        {
//            var result = await client.CallToolAsync(toolAction.ToolName, toolAction.Arguments);
//            _logger.LogInformation("✅ Tool executed successfully: {ToolName}", toolAction.ToolName);
//            return result.ToString();
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Tool execution failed: {ToolName}", toolAction.ToolName);
//            return $"Tool execution failed: {ex.Message}";
//        }
//    }

//    private async Task<string> GetOpenAIResponseWithContext(string userInput, string toolResult, string toolName)
//    {
//        var contextPrompt = $@"The user asked: ""{userInput}""

//I used the {toolName} tool and got this result:
//{toolResult}

//Please provide a helpful, natural response to the user based on this information. Be conversational and helpful.";

//        var messages = _conversationHistory.Concat(new[]
//        {
//            new { role = "user", content = contextPrompt }
//        }).ToArray();

//        return await GetOpenAIResponse(messages);
//    }

//    private async Task<string> GetOpenAIResponse(object[]? messages = null)
//    {
//        try
//        {
//            var requestMessages = messages ?? _conversationHistory.ToArray();
            
//            var payload = new
//            {
//                model = _model,
//                messages = requestMessages,
//                max_tokens = 500,
//                temperature = 0.7
//            };

//            var json = JsonSerializer.Serialize(payload);
//            var content = new StringContent(json, Encoding.UTF8, "application/json");

//            var response = await _httpClient.PostAsync("/chat/completions", content);
            
//            if (!response.IsSuccessStatusCode)
//            {
//                var errorContent = await response.Content.ReadAsStringAsync();
//                _logger.LogError("OpenAI API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
//                return "I'm having trouble connecting to the AI service right now. Please try again later.";
//            }

//            var responseJson = await response.Content.ReadAsStringAsync();
//            var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);
            
//            var choices = responseObj.GetProperty("choices");
//            if (choices.GetArrayLength() > 0)
//            {
//                var firstChoice = choices[0];
//                var message = firstChoice.GetProperty("message");
//                var responseContent = message.GetProperty("content").GetString();
//                return responseContent ?? "I apologize, but I couldn't generate a proper response.";
//            }

//            return "I apologize, but I couldn't generate a proper response.";
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Failed to get OpenAI response");
//            return "I'm having trouble connecting to the AI service right now. Please try again later.";
//        }
//    }

//    private class ToolAnalysis
//    {
//        public string? ToolName { get; set; }
//        public Dictionary<string, object?>? Arguments { get; set; }
//    }

//    private class ToolAction
//    {
//        public required string ToolName { get; set; }
//        public required Dictionary<string, object?> Arguments { get; set; }
//    }

//    public void Dispose()
//    {
//        _httpClient?.Dispose();
//    }
//}
