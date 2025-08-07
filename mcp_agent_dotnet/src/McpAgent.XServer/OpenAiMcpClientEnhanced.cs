using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;

namespace McpAgent.Client;

public class OpenAiMcpClientEnhanced
{
    private readonly string _serverUrl;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<OpenAiMcpClientEnhanced> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly SessionManager _sessionManager;
    private readonly List<object> _conversationHistory;
    private readonly List<NotificationMessage> _notifications;
    private int _elicitationCount;
    private int _samplingCount;
    private DateTime _sessionStartTime;
    private int _commandCount;

    public OpenAiMcpClientEnhanced(string serverUrl, ILoggerFactory loggerFactory, string apiKey, string? model = null, string? endpoint = null)
    {
        if (string.IsNullOrWhiteSpace(serverUrl))
            throw new ArgumentException("Server URL cannot be null or empty", nameof(serverUrl));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("OpenAI API key cannot be null or empty", nameof(apiKey));

        _serverUrl = serverUrl;
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<OpenAiMcpClientEnhanced>();
        _model = model ?? "gpt-4o-mini";
        _sessionManager = new SessionManager(_loggerFactory);
        _notifications = new List<NotificationMessage>();
        _elicitationCount = 0;
        _samplingCount = 0;
        _commandCount = 0;

        // Configure HTTP client for OpenAI API
        _httpClient = new HttpClient();
        var baseEndpoint = endpoint ?? "https://api.openai.com/v1";
        _httpClient.BaseAddress = new Uri(baseEndpoint);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        // Initialize conversation history with enhanced system prompt
        _conversationHistory = new List<object>
        {
            new { role = "system", content = "You are an advanced AI assistant with enhanced MCP (Model Context Protocol) capabilities. " +
                "You have access to travel booking and research tools through an MCP server with session management, " +
                "progress tracking, intelligent elicitation, and MCP sampling for complex decisions. You can handle complex multi-step requests, " +
                "maintain context across sessions, provide detailed progress updates, and use MCP sampling for AI-assisted decision making. " +
                "Available tools: travel_agent (with enhanced booking features), research_agent (with deep analysis). " +
                "Use MCP sampling when you need help making complex decisions during tool execution. " +
                "Always be helpful, detailed, and proactive in gathering needed information." }
        };

        _logger.LogInformation("🤖 Enhanced OpenAI client configured with model: {Model}, endpoint: {Endpoint}", _model, baseEndpoint);
    }

    /// <summary>
    /// Runs the enhanced AI-powered interactive client session
    /// </summary>
    public async Task RunAsync()
    {
        try
        {
            _sessionStartTime = DateTime.UtcNow;
            await ConnectAndRunEnhanced();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during enhanced OpenAI client execution");
            throw;
        }
        finally
        {
            _httpClient.Dispose();
        }
    }

    private async Task ConnectAndRunEnhanced()
    {
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(_serverUrl.Replace("/mcp", ""));
        httpClient.Timeout = TimeSpan.FromMinutes(15); // Extended timeout for AI processing
        
        var mcpClient = new DirectHttpMcpClient(httpClient, _serverUrl, _loggerFactory);
        
        // Enhanced initialization with progress tracking
        _logger.LogInformation("🤖 Initializing Enhanced OpenAI MCP Client...");
        await DisplayProgressBar("Connecting to MCP server", 0.3);
        
        await mcpClient.InitializeAsync();
        await DisplayProgressBar("Establishing AI connection", 0.6);
        
        // Test OpenAI connection
        await TestOpenAIConnection();
        await DisplayProgressBar("Loading enhanced capabilities", 1.0);
        
        _logger.LogInformation("✅ Enhanced OpenAI MCP Client ready!");
        _logger.LogInformation("🧠 AI Model: OpenAI {Model}", _model);
        _logger.LogInformation("🔧 Enhanced Features: Session Management, Notifications, Elicitation, Progress Tracking, MCP Sampling");

        try
        {
            // Display enhanced server info with AI context
            await DisplayEnhancedServerInfo(mcpClient);
            
            // Check for existing session with AI context restoration
            var existingSession = await _sessionManager.LoadExistingSessionAsync();
            
            if (existingSession != null)
            {
                _logger.LogInformation("🔄 Resuming AI-enhanced session...");
                await ResumeEnhancedSessionWithAI(mcpClient, existingSession);
            }
            else
            {
                await StartEnhancedAISession(mcpClient);
            }
        }
        finally
        {
            mcpClient.Dispose();
        }
    }

    private async Task TestOpenAIConnection()
    {
        var testPayload = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "user", content = "Hello, this is a connection test. Please respond with 'Connection successful'." }
            },
            max_tokens = 50
        };

        var json = JsonSerializer.Serialize(testPayload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/chat/completions", content);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"OpenAI API test failed: {response.StatusCode} - {errorContent}");
        }

        _logger.LogInformation("🔗 OpenAI connection test successful");
    }

    private async Task DisplayEnhancedServerInfo(DirectHttpMcpClient client)
    {
        _logger.LogInformation("🚀 Enhanced OpenAI MCP Client Features:");
        _logger.LogInformation("  • 🧠 OpenAI {Model} integration with natural language processing", _model);
        _logger.LogInformation("  • 📊 Advanced progress tracking with AI-guided updates");
        _logger.LogInformation("  • 🔄 Intelligent session persistence with context restoration");
        _logger.LogInformation("  • 📬 MCP notification system with AI interpretation");
        _logger.LogInformation("  • 🎯 Smart elicitation for missing information");
        _logger.LogInformation("  • 🎲 MCP sampling for AI-assisted complex decision making");
        _logger.LogInformation("  • 📋 AI-powered result analysis and insights");
        _logger.LogInformation("");

        // Get tools with AI-enhanced descriptions
        var toolsResult = await client.ListToolsAsync();
        var tools = toolsResult.GetProperty("tools").EnumerateArray();

        _logger.LogInformation("🛠️ AI-Enhanced Tool Capabilities:");
        foreach (var tool in tools)
        {
            var name = tool.GetProperty("name").GetString();
            var description = tool.GetProperty("description").GetString();
            
            _logger.LogInformation("  🤖 {ToolName} (AI-Enhanced)", name);
            _logger.LogInformation("     Original: {Description}", description);
            _logger.LogInformation("     AI Features: Smart parameter elicitation, context awareness, progress tracking, MCP sampling");
            _logger.LogInformation("     Status: ✅ Ready with OpenAI integration");
        }
        _logger.LogInformation("");
    }

    private async Task ResumeEnhancedSessionWithAI(DirectHttpMcpClient client, SessionInfo sessionInfo)
    {
        _logger.LogInformation("📋 AI is resuming {ToolName} with enhanced context...", sessionInfo.LastTool);
        
        // Add session context to conversation history
        _conversationHistory.Add(new { role = "system", content = $"Resuming previous session - Tool: {sessionInfo.LastTool}, Args: {JsonSerializer.Serialize(sessionInfo.LastArgs)}" });
        
        // Get AI analysis of the resumption
        var resumeAnalysis = await GetOpenAIResponse(new[]
        {
            new { role = "system", content = "You are analyzing a session resumption." },
            new { role = "user", content = $"Analyze this session resumption: Tool '{sessionInfo.LastTool}' with arguments {JsonSerializer.Serialize(sessionInfo.LastArgs)}. Provide a brief status update." }
        });
        
        _logger.LogInformation("🧠 AI Analysis: {Analysis}", resumeAnalysis);
        
        try
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("⏱️ AI-guided execution starting at {StartTime}", startTime.ToString("HH:mm:ss"));
            
            var result = await ExecuteToolWithAIGuidedTracking(client, sessionInfo.LastTool, sessionInfo.LastArgs);
            
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;
            _logger.LogInformation("✅ AI-guided execution completed in {Duration:F2} seconds", duration.TotalSeconds);
            
            await DisplayAIEnhancedResult(result, sessionInfo.LastTool);
            await _sessionManager.ClearSessionAsync();
            await StartEnhancedAISession(client);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ AI-enhanced session resumption failed, starting fresh");
            await _sessionManager.ClearSessionAsync();
            await StartEnhancedAISession(client);
        }
    }

    private async Task StartEnhancedAISession(DirectHttpMcpClient client)
    {
        _logger.LogInformation("🎮 Enhanced AI Interactive Mode Started");
        _logger.LogInformation("🧠 Natural Language Commands Supported:");
        _logger.LogInformation("  💬 Conversational interaction (e.g., 'I want to travel to Paris')");
        _logger.LogInformation("  🎯 Smart elicitation (AI will ask for missing details)");
        _logger.LogInformation("  🎲 MCP sampling (server AI assists with complex decisions)");
        _logger.LogInformation("  📊 Progress tracking (real-time updates during execution)");
        _logger.LogInformation("  📬 Notification handling (AI interprets system messages)");
        _logger.LogInformation("");
        _logger.LogInformation("📝 Enhanced Commands:");
        _logger.LogInformation("  help                    - Show AI-enhanced help with examples");
        _logger.LogInformation("  tools                   - List tools with AI-powered descriptions");
        _logger.LogInformation("  status                  - Show AI session analytics");
        _logger.LogInformation("  notifications           - View and manage MCP notifications");
        _logger.LogInformation("  clear                   - Clear session with AI confirmation");
        _logger.LogInformation("  exit                    - Exit with AI session summary");
        _logger.LogInformation("");

        await RunEnhancedAIInteractiveLoop(client);
    }

    private async Task RunEnhancedAIInteractiveLoop(DirectHttpMcpClient client)
    {
        while (true)
        {
            Console.Write("🧠 OpenAI> ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
                continue;

            _commandCount++;
            await AddNotification($"User input received: {input}", NotificationType.UserAction);

            try
            {
                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleEnhancedExit();
                    break;
                }
                else if (input.Equals("help", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowAIEnhancedHelp();
                }
                else if (input.Equals("tools", StringComparison.OrdinalIgnoreCase))
                {
                    await DisplayAIEnhancedToolsList(client);
                }
                else if (input.Equals("status", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowAISessionStatus();
                }
                else if (input.Equals("notifications", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowNotifications();
                }
                else if (input.Equals("clear", StringComparison.OrdinalIgnoreCase))
                {
                    await ClearSessionWithAIConfirmation();
                }
                else
                {
                    // AI-powered natural language processing
                    await ProcessNaturalLanguageInput(client, input);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error in AI-enhanced command processing: {Command}", input);
                await AddNotification($"Error processing command: {ex.Message}", NotificationType.Error);
            }
        }
    }

    private async Task ProcessNaturalLanguageInput(DirectHttpMcpClient client, string userInput)
    {
        // Add user input to conversation history
        _conversationHistory.Add(new { role = "user", content = userInput });

        // Get AI analysis and intent detection
        var intentAnalysis = "Intent analysis unavailable";
        try
        {
            intentAnalysis = await GetOpenAIResponse(new[]
            {
                new { role = "system", content = "You are an intent analysis assistant. Analyze user inputs and determine appropriate MCP tool usage." },
                new { role = "user", content = $"Analyze this user input and determine intent: '{userInput}'. " +
                    "Respond with: 1) Intent (travel/research/question/other), 2) Required tool if any, 3) Missing information that needs elicitation, 4) Confidence level" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get AI intent analysis, using fallback");
            intentAnalysis = GetFallbackIntentAnalysis(userInput);
        }

        _logger.LogInformation("🧠 AI Intent Analysis: {Analysis}", intentAnalysis);
        await AddNotification($"AI analyzed intent: {intentAnalysis}", NotificationType.AIAnalysis);

        // Determine tool action based on AI analysis
        var toolAction = await DetermineToolActionWithAI(userInput, intentAnalysis);

        if (toolAction != null)
        {
            // Check if MCP sampling is needed for complex decisions
            var needsSampling = await CheckIfSamplingNeeded(toolAction, userInput);
            
            if (needsSampling)
            {
                await PerformMCPSampling(client, toolAction, userInput);
            }
            
            // Check if elicitation is needed
            var missingInfo = await CheckForMissingInformation(toolAction, userInput);
            
            if (missingInfo.Any())
            {
                await PerformElicitation(client, toolAction, missingInfo, userInput);
            }
            else
            {
                await ExecuteToolWithAI(client, toolAction, userInput);
            }
        }
        else
        {
            // Direct AI conversation with fallback
            try
            {
                var aiResponse = await GetOpenAIResponse(_conversationHistory.ToArray());
                Console.WriteLine($"🤖 OpenAI: {aiResponse}");
                _conversationHistory.Add(new { role = "assistant", content = aiResponse });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get AI conversation response, using fallback");
                var fallbackResponse = await GetFallbackResponse(userInput);
                Console.WriteLine($"🤖 Assistant: {fallbackResponse}");
                _conversationHistory.Add(new { role = "assistant", content = fallbackResponse });
            }
        }
    }

    /// <summary>
    /// Enhanced MCP client with sampling support
    /// </summary>
    public async Task<JsonElement> CreateSampleAsync(string prompt, int maxTokens = 500, double temperature = 0.7, Dictionary<string, object?>? metadata = null)
    {
        _samplingCount++;
        _logger.LogInformation("🎲 Creating MCP sampling request #{Count}", _samplingCount);
        
        // In a real implementation, this would call the MCP server's sampling endpoint
        // For now, we'll simulate a sampling response
        await Task.Delay(1000);
        
        var response = await GenerateSimulatedSamplingResponse(prompt, metadata);
        
        var result = JsonSerializer.SerializeToElement(new
        {
            content = new[]
            {
                new { type = "text", text = response }
            },
            model = "mcp-sampling-ai",
            stop_reason = "stop_sequence",
            usage = new
            {
                input_tokens = EstimateTokens(prompt),
                output_tokens = EstimateTokens(response)
            }
        });
        
        return result;
    }
    
    private async Task<string> GenerateSimulatedSamplingResponse(string prompt, Dictionary<string, object?>? metadata)
    {
        await Task.Delay(500);
        
        var promptLower = prompt.ToLowerInvariant();
        
        if (promptLower.Contains("travel"))
        {
            return @"🎲 **MCP Sampling Analysis - Travel Decision**

**Parameter Assessment:**
✅ Destination identified successfully
⚡ Enhanced recommendations available

**AI Optimization Suggestions:**
• Consider seasonal weather patterns for optimal timing
• Cross-reference local events and festivals
• Evaluate budget-friendly alternatives in nearby regions
• Factor in travel duration for activity planning

**Risk Analysis:**
⚠️ Verify current travel restrictions and requirements
⚠️ Check local safety conditions and health advisories
⚠️ Consider currency fluctuations and local pricing

**Execution Strategy:**
1. Prioritize user safety and satisfaction
2. Provide multiple options for flexibility
3. Include practical logistics and local insights
4. Offer real-time updates and support

**Confidence Assessment:** High - Comprehensive travel analysis completed";
        }
        else if (promptLower.Contains("research"))
        {
            return @"🎲 **MCP Sampling Analysis - Research Decision**

**Topic Analysis:**
✅ Research scope clearly defined
📊 Multiple information sources available

**Methodology Optimization:**
• Prioritize authoritative and recent sources
• Include diverse perspectives and viewpoints
• Cross-validate information across sources
• Structure findings for maximum clarity

**Quality Assurance:**
⚡ Source credibility verification recommended
⚡ Bias detection and mitigation strategies
⚡ Currency and relevance assessment

**Delivery Strategy:**
1. Executive summary with key findings
2. Detailed analysis with supporting evidence
3. Implications and future considerations
4. Actionable recommendations

**Confidence Assessment:** High - Structured research approach validated";
        }
        else
        {
            return @"🎲 **MCP Sampling Analysis - General Decision**

**Context Assessment:**
📋 Request parameters analyzed
🎯 Optimization opportunities identified

**Strategic Recommendations:**
• Ensure comprehensive approach to the task
• Consider user expectations and requirements
• Evaluate multiple solution pathways
• Prioritize quality and accuracy

**Implementation Notes:**
⚡ Follow best practices for the domain
⚡ Maintain clear communication throughout
⚡ Provide actionable and practical outcomes

**Confidence Assessment:** Medium - General analysis framework applied";
        }
    }
    
    private int EstimateTokens(string text)
    {
        return Math.Max(1, text.Length / 4);
    }

    private async Task<ToolAction?> DetermineToolActionWithAI(string userInput, string intentAnalysis)
    {
        var input = userInput.ToLowerInvariant();
        var analysis = intentAnalysis.ToLowerInvariant();

        // AI-guided tool selection
        if (analysis.Contains("travel") || input.Contains("travel") || input.Contains("book") || 
            input.Contains("trip") || input.Contains("visit") || input.Contains("go to"))
        {
            var destination = await ExtractParameterWithAI(userInput, "destination", "travel location");
            return new ToolAction("travel_agent", new Dictionary<string, object?> { ["destination"] = destination });
        }
        
        if (analysis.Contains("research") || input.Contains("research") || input.Contains("study") || 
            input.Contains("investigate") || input.Contains("learn about") || input.Contains("find out"))
        {
            var topic = await ExtractParameterWithAI(userInput, "topic", "research subject");
            return new ToolAction("research_agent", new Dictionary<string, object?> { ["topic"] = topic });
        }
        
        return null;
    }

    private async Task<string> ExtractParameterWithAI(string userInput, string parameterName, string parameterDescription)
    {
        try
        {
            var extractionResponse = await GetOpenAIResponse(new[]
            {
                new { role = "system", content = "You are a parameter extraction assistant. Extract specific information from user input." },
                new { role = "user", content = $"Extract the {parameterDescription} from this user input: '{userInput}'. " +
                    $"If no clear {parameterDescription} is specified, return 'ELICIT_NEEDED'. " +
                    $"Only return the extracted value or 'ELICIT_NEEDED'." }
            });

            return extractionResponse.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract parameter with AI, using fallback logic");
            
            // Fallback: simple keyword extraction
            if (parameterName == "destination")
            {
                var destinations = ExtractDestinationFallback(userInput);
                return string.IsNullOrEmpty(destinations) ? "ELICIT_NEEDED" : destinations;
            }
            else if (parameterName == "topic")
            {
                var topic = ExtractTopicFallback(userInput);
                return string.IsNullOrEmpty(topic) ? "ELICIT_NEEDED" : topic;
            }
            
            return "ELICIT_NEEDED";
        }
    }

    private string ExtractDestinationFallback(string input)
    {
        var patterns = new[]
        {
            @"(?:go to|visit|travel to|trip to|book.*to)\s+([A-Za-z\s]+)(?:\s|$|,|\.|!|\?)",
            @"destination.*?([A-Za-z\s]{3,})(?:\s|$|,|\.|!|\?)",
            @"([A-Z][a-z]+(?:\s+[A-Z][a-z]+)*)" // Capitalized words
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(input, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                var destination = match.Groups[1].Value.Trim();
                if (destination.Length > 2 && !string.Equals(destination, "to", StringComparison.OrdinalIgnoreCase))
                {
                    return destination;
                }
            }
        }

        return "";
    }

    private string ExtractTopicFallback(string input)
    {
        var patterns = new[]
        {
            @"(?:research|study|investigate|learn about|find out about)\s+([A-Za-z\s]+)(?:\s|$|,|\.|!|\?)",
            @"topic.*?([A-Za-z\s]{3,})(?:\s|$|,|\.|!|\?)",
            @"about\s+([A-Za-z\s]+)(?:\s|$|,|\.|!|\?)"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(input, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                var topic = match.Groups[1].Value.Trim();
                if (topic.Length > 2)
                {
                    return topic;
                }
            }
        }

        return "";
    }

    private string GetFallbackIntentAnalysis(string userInput)
    {
        var input = userInput.ToLowerInvariant();
        
        if (input.Contains("travel") || input.Contains("go to") || input.Contains("visit") || 
            input.Contains("trip") || input.Contains("book"))
        {
            return "Intent: travel, Required tool: travel_agent, Confidence: medium (fallback analysis)";
        }
        else if (input.Contains("research") || input.Contains("study") || input.Contains("investigate") || 
                 input.Contains("learn about") || input.Contains("find out"))
        {
            return "Intent: research, Required tool: research_agent, Confidence: medium (fallback analysis)";
        }
        else if (input.Contains("help") || input.Contains("command") || input.Contains("?"))
        {
            return "Intent: question, Required tool: none, Confidence: high (fallback analysis)";
        }
        else
        {
            return "Intent: other, Required tool: none, Confidence: low (fallback analysis)";
        }
    }

    private async Task<bool> CheckIfSamplingNeeded(ToolAction toolAction, string userInput)
    {
        try
        {
            // Use OpenAI to determine if MCP sampling would be beneficial
            var decision = await GetOpenAIResponse(new[]
            {
                new { role = "system", content = "You are a decision assistant. Determine if MCP sampling (server-side AI assistance) would be helpful for tool execution scenarios." },
                new { role = "user", content = $@"Analyze this tool execution scenario and determine if MCP sampling (server-side AI assistance) would be helpful:

Tool: {toolAction.ToolName}
Arguments: {JsonSerializer.Serialize(toolAction.Arguments)}
User Input: '{userInput}'

MCP sampling is useful when:
- The decision requires complex reasoning beyond parameter extraction
- Multiple valid options exist and server-side AI could provide better judgment
- The tool execution involves trade-offs that need expert evaluation
- Context from the server could improve the decision quality

Respond with only 'YES' if MCP sampling would be beneficial, or 'NO' if the current information is sufficient." }
            });
            
            return decision.Trim().ToUpperInvariant().Contains("YES");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check if sampling is needed, using fallback logic");
            
            // Fallback logic: use sampling for complex scenarios
            var complexScenarios = new[] { "multiple", "complex", "best", "recommend", "optimize", "compare", "choose" };
            var inputLower = userInput.ToLowerInvariant();
            
            return complexScenarios.Any(keyword => inputLower.Contains(keyword));
        }
    }

    private async Task PerformMCPSampling(DirectHttpMcpClient client, ToolAction toolAction, string userInput)
    {
        _samplingCount++;
        _logger.LogInformation("🎲 Initiating MCP sampling #{SamplingCount} for {ToolName}", _samplingCount, toolAction.ToolName);
        await AddNotification($"MCP sampling #{_samplingCount}: Requesting server AI assistance for {toolAction.ToolName}", NotificationType.Sampling);

        try
        {
            // Prepare sampling prompt for the MCP server
            var samplingPrompt = $@"AI Sampling Request for Tool Execution:

Tool: {toolAction.ToolName}
Current Arguments: {JsonSerializer.Serialize(toolAction.Arguments)}
User's Original Request: '{userInput}'
Context: The user wants to execute this tool, but there are complex decisions that could benefit from your AI expertise.

Please provide:
1. Analysis of the current parameters and their suitability
2. Suggestions for optimization or alternative approaches
3. Potential risks or considerations to be aware of
4. Recommended execution strategy

Your response will guide the enhanced execution of this tool.";

            // Request MCP sampling from the server - but handle if server doesn't support it
            var samplingResult = await client.CreateSampleAsync(
                prompt: samplingPrompt,
                maxTokens: 500,
                temperature: 0.7,
                metadata: new Dictionary<string, object?>
                {
                    ["tool"] = toolAction.ToolName,
                    ["sampling_count"] = _samplingCount,
                    ["user_input"] = userInput
                }
            );

            // Process the sampling result
            var serverAdvice = "";
            if (samplingResult.TryGetProperty("content", out var content))
            {
                foreach (var item in content.EnumerateArray())
                {
                    if (item.TryGetProperty("text", out var text))
                    {
                        serverAdvice = text.GetString() ?? "";
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(serverAdvice))
            {
                _logger.LogInformation("🧠 MCP Server AI Advice received");
                Console.WriteLine();
                Console.WriteLine("🎲 MCP Sampling Result:");
                Console.WriteLine("════════════════════════");
                Console.WriteLine($"🤖 Server AI: {serverAdvice}");
                Console.WriteLine("════════════════════════");
                Console.WriteLine();

                // Add server advice to conversation history
                _conversationHistory.Add(new { role = "system", content = $"MCP Server AI Advice for {toolAction.ToolName}: {serverAdvice}" });
                
                // Ask user if they want to proceed with modifications based on server advice
                Console.WriteLine("🔄 Would you like to modify your request based on this AI advice? (y/n)");
                var modifyResponse = Console.ReadLine()?.Trim().ToLowerInvariant();
                
                if (modifyResponse == "y" || modifyResponse == "yes")
                {
                    Console.WriteLine("💭 Please describe how you'd like to modify your request:");
                    var modification = Console.ReadLine()?.Trim();
                    
                    if (!string.IsNullOrEmpty(modification))
                    {
                        // Re-analyze with the modification
                        var modifiedInput = $"{userInput} (Modified: {modification})";
                        _conversationHistory.Add(new { role = "user", content = $"User modification: {modification}" });
                        
                        // Update tool action based on modification
                        await UpdateToolActionWithModification(toolAction, modification, serverAdvice);
                    }
                }

                await AddNotification($"MCP sampling completed for {toolAction.ToolName}", NotificationType.Success);
            }
            else
            {
                _logger.LogWarning("⚠️ MCP sampling returned empty response");
                await AddNotification("MCP sampling returned empty response", NotificationType.Warning);
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not implemented") || ex.Message.Contains("-32603"))
        {
            _logger.LogInformation("ℹ️ MCP server doesn't support sampling, using client-side AI analysis instead");
            await AddNotification("Server doesn't support MCP sampling, using client-side AI analysis", NotificationType.Info);
            
            // Fallback to client-side OpenAI analysis when server doesn't support sampling
            await PerformClientSideAnalysis(toolAction, userInput);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ MCP sampling failed for {ToolName}", toolAction.ToolName);
            await AddNotification($"MCP sampling failed: {ex.Message}", NotificationType.Error);
            
            // Continue with original execution despite sampling failure
            Console.WriteLine("⚠️ MCP sampling failed, continuing with original parameters...");
        }
    }

    private async Task PerformClientSideAnalysis(ToolAction toolAction, string userInput)
    {
        try
        {
            Console.WriteLine("🤖 Performing client-side AI analysis instead of MCP sampling...");
            
            var analysisPrompt = $@"As an AI assistant, analyze this tool execution scenario and provide advice:

Tool: {toolAction.ToolName}
Current Arguments: {JsonSerializer.Serialize(toolAction.Arguments)}
User's Original Request: '{userInput}'

Please provide:
1. Analysis of the current parameters and their suitability
2. Suggestions for optimization or alternative approaches
3. Potential risks or considerations to be aware of
4. Recommended execution strategy

Keep your response concise but helpful.";

            var clientAnalysis = await GetOpenAIResponse(new[]
            {
                new { role = "system", content = "You are a helpful AI assistant providing tool execution analysis and recommendations." },
                new { role = "user", content = analysisPrompt }
            });

            Console.WriteLine();
            Console.WriteLine("🤖 Client-Side AI Analysis:");
            Console.WriteLine("════════════════════════════");
            Console.WriteLine($"🧠 AI: {clientAnalysis}");
            Console.WriteLine("════════════════════════════");
            Console.WriteLine();

            // Add analysis to conversation history
            _conversationHistory.Add(new { role = "system", content = $"Client AI Analysis for {toolAction.ToolName}: {clientAnalysis}" });
            
            // Ask user if they want to proceed with modifications based on analysis
            Console.WriteLine("🔄 Would you like to modify your request based on this analysis? (y/n)");
            var modifyResponse = Console.ReadLine()?.Trim().ToLowerInvariant();
            
            if (modifyResponse == "y" || modifyResponse == "yes")
            {
                Console.WriteLine("💭 Please describe how you'd like to modify your request:");
                var modification = Console.ReadLine()?.Trim();
                
                if (!string.IsNullOrEmpty(modification))
                {
                    var modifiedInput = $"{userInput} (Modified: {modification})";
                    _conversationHistory.Add(new { role = "user", content = $"User modification: {modification}" });
                    
                    // Update tool action based on modification
                    await UpdateToolActionWithModification(toolAction, modification, clientAnalysis);
                }
            }

            await AddNotification($"Client-side AI analysis completed for {toolAction.ToolName}", NotificationType.Success);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Client-side analysis also failed, proceeding with original parameters");
            Console.WriteLine("⚠️ AI analysis unavailable, proceeding with original parameters...");
        }
    }

    private async Task UpdateToolActionWithModification(ToolAction toolAction, string modification, string serverAdvice)
    {
        // Use OpenAI to interpret the modification and update tool parameters
        try
        {
            var updatedParamsResponse = await GetOpenAIResponse(new[]
            {
                new { role = "system", content = "You are a parameter update assistant. Based on user modifications and server advice, update tool parameters. Return only valid JSON." },
                new { role = "user", content = $@"Based on this modification request and server advice, update the tool parameters:

Original Tool: {toolAction.ToolName}
Original Arguments: {JsonSerializer.Serialize(toolAction.Arguments)}
User Modification: '{modification}'
Server AI Advice: '{serverAdvice}'

Provide updated arguments in JSON format. Only return the JSON object, nothing else." }
            });
            
            // Try to parse the response as JSON and update tool action
            var cleanResponse = updatedParamsResponse.Trim();
            if (cleanResponse.StartsWith("```json"))
            {
                cleanResponse = cleanResponse.Substring(7);
            }
            if (cleanResponse.EndsWith("```"))
            {
                cleanResponse = cleanResponse.Substring(0, cleanResponse.Length - 3);
            }
            
            var updatedParams = JsonSerializer.Deserialize<Dictionary<string, object?>>(cleanResponse);
            if (updatedParams != null)
            {
                foreach (var param in updatedParams)
                {
                    toolAction.Arguments[param.Key] = param.Value;
                }
                
                _logger.LogInformation("✅ Tool parameters updated based on user modification and server advice");
                await AddNotification("Tool parameters updated with AI guidance", NotificationType.Success);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Failed to parse parameter updates, using original parameters");
        }
    }

    private Task<List<string>> CheckForMissingInformation(ToolAction toolAction, string userInput)
    {
        var missingInfo = new List<string>();

        foreach (var arg in toolAction.Arguments)
        {
            if (arg.Value?.ToString() == "ELICIT_NEEDED" || string.IsNullOrWhiteSpace(arg.Value?.ToString()))
            {
                missingInfo.Add(arg.Key);
            }
        }

        return Task.FromResult(missingInfo);
    }

    private async Task PerformElicitation(DirectHttpMcpClient client, ToolAction toolAction, List<string> missingInfo, string originalInput)
    {
        _elicitationCount++;
        await AddNotification($"Starting elicitation #{_elicitationCount} for missing: {string.Join(", ", missingInfo)}", NotificationType.Elicitation);

        foreach (var missingParam in missingInfo)
        {
            var question = await GetOpenAIResponse(new[]
            {
                new { role = "system", content = "You are a helpful assistant that asks clarifying questions to gather missing information." },
                new { role = "user", content = $"The user wants to use {toolAction.ToolName} but didn't specify the {missingParam}. " +
                    $"Generate a friendly, helpful question to ask them for this information. " +
                    $"Original request: '{originalInput}'" }
            });
            
            Console.WriteLine($"🤖 OpenAI: {question}");
            Console.Write($"💭 You ({missingParam}): ");
            
            var userResponse = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(userResponse))
            {
                toolAction.Arguments[missingParam] = userResponse;
                _conversationHistory.Add(new { role = "user", content = $"User provided {missingParam}: {userResponse}" });
            }
        }

        // Now execute with complete information
        await ExecuteToolWithAI(client, toolAction, originalInput);
    }

    private async Task ExecuteToolWithAI(DirectHttpMcpClient client, ToolAction toolAction, string originalInput)
    {
        _logger.LogInformation("🚀 AI-guided execution of {ToolName}...", toolAction.ToolName);
        await AddNotification($"Executing {toolAction.ToolName} with AI guidance", NotificationType.ToolExecution);

        try
        {
            // Save session
            await _sessionManager.SaveSessionAsync(toolAction.ToolName, toolAction.Arguments);

            var result = await ExecuteToolWithAIGuidedTracking(client, toolAction.ToolName, toolAction.Arguments);
            await DisplayAIEnhancedResult(result, toolAction.ToolName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to execute tool {ToolName} with AI guidance", toolAction.ToolName);
            await AddNotification($"Tool execution failed: {ex.Message}", NotificationType.Error);
            
            // Try to provide helpful suggestions based on the error
            var suggestion = await GetAIErrorSuggestion(toolAction.ToolName, ex.Message, originalInput);
            Console.WriteLine($"💡 AI Suggestion: {suggestion}");
            
            // Ask if user wants to try again with different parameters
            Console.WriteLine("🔄 Would you like to try again? (y/n)");
            var retry = Console.ReadLine();
            if (retry?.ToLowerInvariant() == "y" || retry?.ToLowerInvariant() == "yes")
            {
                Console.WriteLine("Please provide more details or try rephrasing your request:");
                // The user can enter a new request in the main loop
            }
        }
    }

    private async Task<JsonElement> ExecuteToolWithAIGuidedTracking(DirectHttpMcpClient client, string toolName, Dictionary<string, object?> args)
    {
        _logger.LogInformation("🤖 Starting AI-guided execution of {ToolName}", toolName);
        
        try
        {
            // AI-generated progress messages
            var progressMessages = await GenerateProgressMessages(toolName, args);
            
            var startTime = DateTime.UtcNow;
            
            // Enhanced progress tracking with AI-generated messages
            for (int i = 0; i < progressMessages.Count; i++)
            {
                var progress = (i + 1) * (100 / progressMessages.Count);
                await DisplayProgressWithAI(progressMessages[i], progress / 100.0);
                await Task.Delay(300);
            }
            
            var result = await client.CallToolAsync(toolName, args);
            
            await DisplayProgressWithAI("✅ Execution completed successfully!", 1.0);
            
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("✅ AI-guided tool execution completed in {Duration:F2} seconds", duration.TotalSeconds);
            
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "🌐 Network error during tool execution: {ToolName}", toolName);
            await DisplayProgressWithAI("❌ Network error occurred", 1.0);
            throw new InvalidOperationException($"Network error calling {toolName}: {ex.Message}", ex);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Server error"))
        {
            _logger.LogError(ex, "🔧 Server error during tool execution: {ToolName}", toolName);
            await DisplayProgressWithAI("❌ Server error occurred", 1.0);
            throw new InvalidOperationException($"Server error calling {toolName}: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Unexpected error during tool execution: {ToolName}", toolName);
            await DisplayProgressWithAI("❌ Unexpected error occurred", 1.0);
            throw new InvalidOperationException($"Unexpected error calling {toolName}: {ex.Message}", ex);
        }
    }

    private async Task<List<string>> GenerateProgressMessages(string toolName, Dictionary<string, object?> args)
    {
        try
        {
            var response = await GetOpenAIResponse(new[]
            {
                new { role = "system", content = "You are a progress message generator. Generate realistic, tool-specific progress messages." },
                new { role = "user", content = $"Generate 4 short, specific progress messages for executing {toolName} with arguments {JsonSerializer.Serialize(args)}. " +
                    "Make them realistic and tool-specific. Return only the messages, one per line." }
            });

            return response.Split('\n', StringSplitOptions.RemoveEmptyEntries).Take(4).ToList();
        }
        catch
        {
            // Fallback progress messages
            return new List<string>
            {
                $"🔄 Initializing {toolName}...",
                $"⚙️ Processing parameters...",
                $"🔍 Executing {toolName}...",
                $"📊 Finalizing results..."
            };
        }
    }

    private async Task DisplayAIEnhancedResult(JsonElement result, string toolName)
    {
        Console.WriteLine();
        Console.WriteLine("📋 AI-Enhanced Tool Result:");
        Console.WriteLine("═══════════════════════════════");
        
        var resultText = "";
        if (result.TryGetProperty("content", out var content))
        {
            foreach (var item in content.EnumerateArray())
            {
                if (item.TryGetProperty("text", out var text))
                {
                    resultText = text.GetString() ?? "";
                    Console.WriteLine(resultText);
                }
            }
        }

        // AI analysis of the result
        try
        {
            var aiAnalysis = await GetOpenAIResponse(new[]
            {
                new { role = "system", content = "You are a result analysis assistant. Analyze tool execution results and provide insights." },
                new { role = "user", content = $"Analyze this {toolName} result and provide insights: '{resultText}'. " +
                    "Identify key information, potential next steps, and any important details to highlight." }
            });
            
            Console.WriteLine();
            Console.WriteLine("🧠 AI Analysis:");
            Console.WriteLine(aiAnalysis);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get AI analysis of result");
            Console.WriteLine();
            Console.WriteLine("🧠 AI Analysis: Analysis temporarily unavailable");
        }
        
        Console.WriteLine("═══════════════════════════════");
        Console.WriteLine();

        await AddNotification($"Tool result analyzed by AI: {toolName} execution completed", NotificationType.Success);
    }

    private async Task<string> GetOpenAIResponse(object[] messages)
    {
        try
        {
            var payload = new
            {
                model = _model,
                messages = messages,
                max_tokens = 500,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/chat/completions", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("OpenAI API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                throw new Exception($"OpenAI API error: {response.StatusCode} - {errorContent}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);
            
            var choices = responseObj.GetProperty("choices");
            if (choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                var message = firstChoice.GetProperty("message");
                var responseContent = message.GetProperty("content").GetString();
                return responseContent ?? "I apologize, but I couldn't generate a proper response.";
            }

            return "I apologize, but I couldn't generate a proper response.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get OpenAI response");
            throw;
        }
    }

    private Task<string> GetFallbackResponse(string context)
    {
        // Provide intelligent fallbacks when OpenAI API is unavailable
        var contextLower = context.ToLowerInvariant();
        
        if (contextLower.Contains("travel") || contextLower.Contains("destination"))
        {
            return Task.FromResult("I can help you with travel planning. Please provide your destination and I'll assist with the travel_agent tool.");
        }
        else if (contextLower.Contains("research") || contextLower.Contains("study") || contextLower.Contains("topic"))
        {
            return Task.FromResult("I can help you with research. Please provide your research topic and I'll assist with the research_agent tool.");
        }
        else if (contextLower.Contains("help") || contextLower.Contains("command"))
        {
            return Task.FromResult("Available commands: help, tools, status, notifications, clear, exit. For natural language, try: 'I want to travel to [destination]' or 'Research [topic]'.");
        }
        else if (contextLower.Contains("tool") || contextLower.Contains("available"))
        {
            return Task.FromResult("Available tools: travel_agent (for booking travel) and research_agent (for conducting research). Try natural language like 'I want to go to Paris'.");
        }
        else
        {
            return Task.FromResult("I'm currently unable to access AI services, but I can still help you use the available tools. Try 'help' for more information.");
        }
    }

    private async Task DisplayProgressBar(string message, double progress)
    {
        var barLength = 20;
        var filledLength = (int)(barLength * progress);
        var bar = new string('▓', filledLength) + new string('░', barLength - filledLength);
        
        Console.Write($"\r📊 {message}: [{bar}] {progress:P0}");
        if (progress >= 1.0)
        {
            Console.WriteLine();
        }
        
        await Task.Delay(200);
    }

    private async Task DisplayProgressWithAI(string message, double progress)
    {
        var barLength = 25;
        var filledLength = (int)(barLength * progress);
        var bar = new string('▓', filledLength) + new string('░', barLength - filledLength);
        
        Console.Write($"\r🤖 {message}: [{bar}] {progress:P0}");
        if (progress >= 1.0)
        {
            Console.WriteLine();
        }
        
        await Task.Delay(100);
    }

    private Task ShowAIEnhancedHelp()
    {
        Console.WriteLine();
        Console.WriteLine("📖 Enhanced OpenAI MCP Client Help");
        Console.WriteLine("═══════════════════════════════════");
        Console.WriteLine($"🧠 AI-Powered Features (using {_model}):");
        Console.WriteLine("  • Natural language conversation with OpenAI");
        Console.WriteLine("  • Smart parameter elicitation for incomplete requests");
        Console.WriteLine("  • MCP sampling for server-side AI assistance with complex decisions");
        Console.WriteLine("  • AI-guided progress tracking and execution");
        Console.WriteLine("  • Intelligent result analysis and insights");
        Console.WriteLine("  • Context-aware session management");
        Console.WriteLine("  • Support for GitHub AI Models endpoint");
        Console.WriteLine();
        Console.WriteLine("💬 Natural Language Examples:");
        Console.WriteLine("  'I want to travel to Tokyo next month'");
        Console.WriteLine("  'Can you research machine learning trends?'");
        Console.WriteLine("  'Find information about renewable energy'");
        Console.WriteLine("  'Book a trip to Paris'");
        Console.WriteLine();
        Console.WriteLine("📝 Command Examples:");
        Console.WriteLine("  help          - Show this AI-enhanced help");
        Console.WriteLine("  tools         - List tools with AI descriptions");
        Console.WriteLine("  status        - Show AI session analytics");
        Console.WriteLine("  notifications - View MCP notification history");
        Console.WriteLine("  clear         - Clear session with AI confirmation");
        Console.WriteLine("  exit          - Exit with AI session summary");
        Console.WriteLine("═══════════════════════════════════");
        Console.WriteLine();
        return Task.CompletedTask;
    }

    private async Task DisplayAIEnhancedToolsList(DirectHttpMcpClient client)
    {
        var toolsResult = await client.ListToolsAsync();
        var tools = toolsResult.GetProperty("tools").EnumerateArray();

        Console.WriteLine();
        Console.WriteLine("🛠️ AI-Enhanced Tools Information");
        Console.WriteLine("════════════════════════════════════");
        
        foreach (var tool in tools)
        {
            var name = tool.GetProperty("name").GetString();
            var description = tool.GetProperty("description").GetString();
            
            // Get AI enhancement for tool description
            try
            {
                var aiEnhancement = await GetOpenAIResponse(new[]
                {
                    new { role = "system", content = "You are a tool description enhancer. Describe how AI capabilities enhance tool functionality." },
                    new { role = "user", content = $"Enhance this tool description with AI capabilities: '{description}' for tool '{name}'. " +
                        "Mention smart elicitation, progress tracking, MCP sampling for complex decisions, and AI analysis features." }
                });
                
                Console.WriteLine($"🤖 {name} (AI-Enhanced)");
                Console.WriteLine($"   Original: {description}");
                Console.WriteLine($"   AI Features: {aiEnhancement}");
                Console.WriteLine($"   Status: ✅ Ready with OpenAI integration");
                Console.WriteLine();
            }
            catch
            {
                Console.WriteLine($"🤖 {name} (AI-Enhanced)");
                Console.WriteLine($"   Original: {description}");
                Console.WriteLine($"   AI Features: Smart elicitation, progress tracking, MCP sampling, AI analysis");
                Console.WriteLine($"   Status: ✅ Ready with OpenAI integration");
                Console.WriteLine();
            }
        }
        Console.WriteLine("════════════════════════════════════");
        Console.WriteLine();
    }

    private async Task ShowAISessionStatus()
    {
        var duration = DateTime.UtcNow - _sessionStartTime;
        
        Console.WriteLine();
        Console.WriteLine("📊 Enhanced AI Session Analytics");
        Console.WriteLine("════════════════════════════════");
        Console.WriteLine($"⏰ Session Duration: {duration:hh\\:mm\\:ss}");
        Console.WriteLine($"📝 Commands Processed: {_commandCount}");
        Console.WriteLine($"🧠 AI Interactions: {_conversationHistory.Count - 1}"); // Exclude system prompt
        Console.WriteLine($"🎯 Elicitations Performed: {_elicitationCount}");
        Console.WriteLine($"🎲 MCP Samplings Performed: {_samplingCount}");
        Console.WriteLine($"📬 Notifications: {_notifications.Count}");
        Console.WriteLine($"✅ Connection Status: Active with OpenAI {_model}");
        Console.WriteLine($"💾 Session Persistence: AI-Enhanced");
        
        // AI-generated session insights
        try
        {
            var insights = await GetOpenAIResponse(new[]
            {
                new { role = "system", content = "You are a session analytics assistant. Generate insights about user sessions." },
                new { role = "user", content = $"Generate insights about this session: {_commandCount} commands, {_elicitationCount} elicitations, " +
                    $"{_samplingCount} MCP samplings, {duration.TotalMinutes:F1} minutes duration. Provide 2-3 key observations." }
            });
            
            Console.WriteLine($"🧠 AI Insights: {insights}");
        }
        catch
        {
            Console.WriteLine($"🧠 AI Insights: Session running smoothly with {_commandCount} commands processed");
        }
        
        Console.WriteLine("════════════════════════════════");
        Console.WriteLine();
    }

    private Task ShowNotifications()
    {
        Console.WriteLine();
        Console.WriteLine("📬 MCP Notification History");
        Console.WriteLine("═══════════════════════════");
        
        if (!_notifications.Any())
        {
            Console.WriteLine("📭 No notifications yet");
        }
        else
        {
            foreach (var notification in _notifications.TakeLast(10)) // Show last 10
            {
                var icon = notification.Type switch
                {
                    NotificationType.Success => "✅",
                    NotificationType.Error => "❌",
                    NotificationType.Warning => "⚠️",
                    NotificationType.Info => "ℹ️",
                    NotificationType.UserAction => "👤",
                    NotificationType.ToolExecution => "🔧",
                    NotificationType.AIAnalysis => "🧠",
                    NotificationType.Elicitation => "❓",
                    NotificationType.Sampling => "🎲",
                    _ => "📝"
                };
                
                Console.WriteLine($"{icon} [{notification.Timestamp:HH:mm:ss}] {notification.Message}");
            }
        }
        
        Console.WriteLine("═══════════════════════════");
        Console.WriteLine();
        return Task.CompletedTask;
    }

    private async Task ClearSessionWithAIConfirmation()
    {
        try
        {
            var confirmationMessage = await GetOpenAIResponse(new[]
            {
                new { role = "system", content = "You are a helpful assistant that asks for confirmation when clearing sessions." },
                new { role = "user", content = "Generate a friendly confirmation message asking if the user really wants to clear their session. " +
                    "Mention they'll lose conversation history and session state." }
            });
            
            Console.WriteLine($"🤖 OpenAI: {confirmationMessage}");
        }
        catch
        {
            Console.WriteLine("🤖 Are you sure you want to clear your session? This will remove all conversation history and session state. (yes/no)");
        }
        
        Console.Write("💭 Your response (yes/no): ");
        
        var response = Console.ReadLine()?.Trim().ToLowerInvariant();
        
        if (response == "yes" || response == "y")
        {
            await _sessionManager.ClearSessionAsync();
            _conversationHistory.Clear();
            _conversationHistory.Add(_conversationHistory[0]); // Keep system prompt
            _notifications.Clear();
            _elicitationCount = 0;
            _samplingCount = 0;
            _commandCount = 0;
            
            _logger.LogInformation("✅ AI-enhanced session cleared successfully");
            await AddNotification("Session cleared by user request", NotificationType.UserAction);
        }
        else
        {
            _logger.LogInformation("❌ Session clear cancelled");
        }
    }

    private async Task HandleEnhancedExit()
    {
        var sessionDuration = DateTime.UtcNow - _sessionStartTime;
        
        // Generate AI summary
        try
        {
            var aiSummary = await GetOpenAIResponse(new[]
            {
                new { role = "system", content = "You are a session summary assistant. Generate friendly, conversational session summaries." },
                new { role = "user", content = $"Generate a friendly session summary: {_commandCount} commands, {_elicitationCount} elicitations, " +
                    $"{_samplingCount} MCP samplings, {sessionDuration.TotalMinutes:F1} minutes, {_notifications.Count} notifications. " +
                    "Make it conversational and positive." }
            });
            
            Console.WriteLine();
            Console.WriteLine("👋 Enhanced OpenAI MCP Session Ending");
            Console.WriteLine("════════════════════════════════════");
            Console.WriteLine($"🤖 OpenAI: {aiSummary}");
            Console.WriteLine("════════════════════════════════════");
        }
        catch
        {
            Console.WriteLine();
            Console.WriteLine("👋 Enhanced OpenAI MCP Session Ending");
            Console.WriteLine("════════════════════════════════════");
            Console.WriteLine($"🤖 Thank you for using the enhanced OpenAI MCP client!");
            Console.WriteLine($"📊 Session summary: {_commandCount} commands, {sessionDuration.TotalMinutes:F1} minutes");
            Console.WriteLine("════════════════════════════════════");
        }
    }

    private Task AddNotification(string message, NotificationType type)
    {
        if (_notifications.Count > 50)
        {
            _notifications.RemoveAt(0);
        }

        _notifications.Add(new NotificationMessage
        {
            Timestamp = DateTime.UtcNow,
            Message = message,
            Type = type
        });

        return Task.CompletedTask;
    }

    private async Task<string> GetAIErrorSuggestion(string toolName, string errorMessage, string originalInput)
    {
        try
        {
            return await GetOpenAIResponse(new[]
            {
                new { role = "system", content = "You are an error analysis assistant. Provide helpful suggestions when tool execution fails." },
                new { role = "user", content = $"Tool '{toolName}' failed with error: '{errorMessage}'. " +
                    $"Original user input: '{originalInput}'. " +
                    "Provide a helpful suggestion for what the user might try differently." }
            });
        }
        catch
        {
            return $"There was an error with {toolName}. You might want to try rephrasing your request or checking if all required information was provided.";
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    // Helper classes
    internal class ToolAction
    {
        public string ToolName { get; set; } = "";
        public Dictionary<string, object?> Arguments { get; set; } = new();

        public ToolAction(string toolName, Dictionary<string, object?> arguments)
        {
            ToolName = toolName;
            Arguments = arguments;
        }
    }

    internal class ToolAnalysis
    {
        public string? ToolName { get; set; }
        public Dictionary<string, object?>? Arguments { get; set; }
    }
}
