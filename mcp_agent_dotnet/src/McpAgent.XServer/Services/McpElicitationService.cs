using Microsoft.Extensions.Logging;

namespace McpAgent.XServer.Services;

/// <summary>
/// Service for handling MCP elicitation - intelligent information gathering
/// </summary>
public class McpElicitationService
{
    private readonly ILogger<McpElicitationService> _logger;
    private readonly List<ElicitationSession> _sessions;
    private int _sessionId;

    public McpElicitationService(ILogger<McpElicitationService> logger)
    {
        _logger = logger;
        _sessions = new List<ElicitationSession>();
        _sessionId = 0;
    }

    /// <summary>
    /// Start an elicitation session to gather missing information
    /// </summary>
    public async Task<ElicitationSession> StartElicitationAsync(
        string toolName,
        Dictionary<string, object?> currentArgs,
        List<string> missingParameters,
        string? context = null)
    {
        var session = new ElicitationSession
        {
            Id = ++_sessionId,
            ToolName = toolName,
            CurrentArguments = new Dictionary<string, object?>(currentArgs),
            MissingParameters = new List<string>(missingParameters),
            Context = context,
            StartTime = DateTime.UtcNow,
            Status = ElicitationStatus.Active,
            Questions = new List<ElicitationQuestion>()
        };

        _sessions.Add(session);
        _logger.LogInformation("🎯 Started elicitation session #{Id} for {ToolName}", session.Id, toolName);

        // Generate initial questions
        await GenerateQuestionsAsync(session);

        return session;
    }

    /// <summary>
    /// Generate intelligent questions for missing parameters
    /// </summary>
    private async Task GenerateQuestionsAsync(ElicitationSession session)
    {
        foreach (var parameter in session.MissingParameters)
        {
            var question = await GenerateQuestionForParameter(session.ToolName, parameter, session.Context);
            session.Questions.Add(question);
        }

        _logger.LogInformation("🎯 Generated {Count} questions for elicitation session #{Id}", 
            session.Questions.Count, session.Id);
    }

    /// <summary>
    /// Generate a specific question for a parameter
    /// </summary>
    private async Task<ElicitationQuestion> GenerateQuestionForParameter(string toolName, string parameter, string? context)
    {
        await Task.Delay(100); // Simulate AI processing

        var question = new ElicitationQuestion
        {
            Parameter = parameter,
            QuestionText = GenerateQuestionText(toolName, parameter, context),
            QuestionType = DetermineQuestionType(parameter),
            ValidationRules = GetValidationRules(toolName, parameter),
            Suggestions = GetSuggestions(toolName, parameter),
            IsRequired = true
        };

        return question;
    }

    /// <summary>
    /// Generate question text based on tool and parameter
    /// </summary>
    private string GenerateQuestionText(string toolName, string parameter, string? context)
    {
        if (toolName.ToLowerInvariant().Contains("travel"))
        {
            return parameter.ToLowerInvariant() switch
            {
                "destination" => "🌍 Where would you like to travel? (e.g., Paris, Tokyo, New York)",
                "budget" => "💰 What's your travel budget? (e.g., $1000, $5000, unlimited)",
                "dates" => "📅 When would you like to travel? (e.g., next month, December 2024)",
                "duration" => "⏱️ How long is your trip? (e.g., 3 days, 1 week, 2 weeks)",
                "travelers" => "👥 How many travelers? (e.g., 1 person, 2 adults, family of 4)",
                "interests" => "🎯 What are your interests? (e.g., culture, adventure, relaxation)",
                "accommodation" => "🏨 What type of accommodation do you prefer? (e.g., hotel, airbnb, hostel)",
                _ => $"Please provide the {parameter} for your travel request."
            };
        }
        else if (toolName.ToLowerInvariant().Contains("research"))
        {
            return parameter.ToLowerInvariant() switch
            {
                "topic" => "🔍 What topic would you like to research? (be as specific as possible)",
                "scope" => "📏 What's the scope of research? (e.g., overview, deep dive, comparison)",
                "sources" => "📚 Any preferred sources? (e.g., academic, news, industry reports)",
                "timeframe" => "⏰ What timeframe are you interested in? (e.g., recent, historical, all time)",
                "format" => "📋 What format would you like? (e.g., summary, detailed report, bullet points)",
                "audience" => "👥 Who is the target audience? (e.g., general public, experts, students)",
                _ => $"Please provide the {parameter} for your research request."
            };
        }
        else
        {
            return $"Please provide the {parameter} parameter.";
        }
    }

    /// <summary>
    /// Determine the type of question based on parameter
    /// </summary>
    private ElicitationQuestionType DetermineQuestionType(string parameter)
    {
        return parameter.ToLowerInvariant() switch
        {
            "budget" or "cost" or "price" => ElicitationQuestionType.Numeric,
            "date" or "dates" or "when" => ElicitationQuestionType.Date,
            "email" => ElicitationQuestionType.Email,
            "phone" => ElicitationQuestionType.Phone,
            "interests" or "preferences" or "categories" => ElicitationQuestionType.MultipleChoice,
            "agree" or "confirm" or "accept" => ElicitationQuestionType.Boolean,
            _ => ElicitationQuestionType.Text
        };
    }

    /// <summary>
    /// Get validation rules for a parameter
    /// </summary>
    private List<string> GetValidationRules(string toolName, string parameter)
    {
        var rules = new List<string>();

        switch (parameter.ToLowerInvariant())
        {
            case "destination":
                rules.Add("Must be a valid location name");
                rules.Add("Minimum 2 characters");
                break;
                
            case "budget":
                rules.Add("Must be a positive number");
                rules.Add("Can include currency symbol");
                break;
                
            case "email":
                rules.Add("Must be a valid email format");
                break;
                
            case "phone":
                rules.Add("Must be a valid phone number");
                break;
                
            case "dates":
                rules.Add("Must be a future date");
                rules.Add("Use standard date format");
                break;
        }

        return rules;
    }

    /// <summary>
    /// Get suggestions for a parameter
    /// </summary>
    private List<string> GetSuggestions(string toolName, string parameter)
    {
        if (toolName.ToLowerInvariant().Contains("travel"))
        {
            return parameter.ToLowerInvariant() switch
            {
                "destination" => new List<string> { "Paris", "Tokyo", "New York", "London", "Rome", "Barcelona" },
                "budget" => new List<string> { "$1000", "$2500", "$5000", "$10000+" },
                "duration" => new List<string> { "3 days", "1 week", "2 weeks", "1 month" },
                "interests" => new List<string> { "Culture", "Adventure", "Relaxation", "Food", "History", "Nature" },
                "accommodation" => new List<string> { "Hotel", "Airbnb", "Resort", "Hostel", "Boutique Hotel" },
                _ => new List<string>()
            };
        }
        else if (toolName.ToLowerInvariant().Contains("research"))
        {
            return parameter.ToLowerInvariant() switch
            {
                "scope" => new List<string> { "Overview", "Deep dive", "Comparison", "Analysis", "Summary" },
                "sources" => new List<string> { "Academic papers", "Industry reports", "News articles", "Government data" },
                "format" => new List<string> { "Executive summary", "Detailed report", "Bullet points", "Infographic" },
                _ => new List<string>()
            };
        }

        return new List<string>();
    }

    /// <summary>
    /// Submit an answer to an elicitation question
    /// </summary>
    public async Task<ElicitationResult> SubmitAnswerAsync(int sessionId, string parameter, string answer)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session == null)
        {
            throw new InvalidOperationException($"Elicitation session {sessionId} not found");
        }

        if (session.Status != ElicitationStatus.Active)
        {
            throw new InvalidOperationException($"Elicitation session {sessionId} is not active");
        }

        var question = session.Questions.FirstOrDefault(q => q.Parameter == parameter);
        if (question == null)
        {
            throw new InvalidOperationException($"Question for parameter '{parameter}' not found in session {sessionId}");
        }

        // Validate the answer
        var validationResult = await ValidateAnswerAsync(question, answer);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("🎯 Invalid answer for {Parameter} in session #{SessionId}: {ValidationError}", 
                parameter, sessionId, validationResult.ErrorMessage);
            
            return new ElicitationResult
            {
                IsValid = false,
                ErrorMessage = validationResult.ErrorMessage,
                Suggestions = question.Suggestions
            };
        }

        // Store the answer
        question.Answer = answer;
        question.IsAnswered = true;
        session.CurrentArguments[parameter] = ProcessAnswer(question, answer);

        _logger.LogInformation("🎯 Answer submitted for {Parameter} in session #{SessionId}: {Answer}", 
            parameter, sessionId, answer);

        // Check if all questions are answered
        var allAnswered = session.Questions.All(q => q.IsAnswered);
        if (allAnswered)
        {
            session.Status = ElicitationStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;
            
            _logger.LogInformation("✅ Elicitation session #{SessionId} completed for {ToolName}", 
                sessionId, session.ToolName);
        }

        return new ElicitationResult
        {
            IsValid = true,
            IsComplete = allAnswered,
            RemainingQuestions = session.Questions.Where(q => !q.IsAnswered).ToList()
        };
    }

    /// <summary>
    /// Submit multiple answers at once (MCP-compliant batch processing)
    /// </summary>
    public async Task<ElicitationResult> SubmitAnswerAsync(int sessionId, Dictionary<string, object?> answers)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session == null)
            return new ElicitationResult { IsValid = false, ErrorMessage = "Session not found" };

        if (session.Status != ElicitationStatus.Active)
            return new ElicitationResult { IsValid = false, ErrorMessage = "Session is not active" };

        var result = new ElicitationResult { IsValid = true };

        // Update current arguments with provided answers
        foreach (var answer in answers)
        {
            session.CurrentArguments[answer.Key] = answer.Value;
            
            // Mark corresponding question as answered
            var question = session.Questions.FirstOrDefault(q => q.Parameter.Equals(answer.Key, StringComparison.OrdinalIgnoreCase));
            if (question != null)
            {
                question.IsAnswered = true;
                question.Answer = answer.Value?.ToString();
            }
        }

        // Validate all answers
        var validationErrors = new List<string>();
        foreach (var answer in answers)
        {
            var validation = await ValidateAnswerAsync(session.ToolName, answer.Key, answer.Value?.ToString() ?? "");
            if (!validation.IsValid)
            {
                validationErrors.Add($"{answer.Key}: {validation.ErrorMessage}");
            }
        }

        if (validationErrors.Any())
        {
            result.IsValid = false;
            result.ErrorMessage = string.Join("; ", validationErrors);
            return result;
        }

        // Check if all required parameters are now provided
        var remainingRequired = session.MissingParameters.Where(p => 
            !session.CurrentArguments.ContainsKey(p) || 
            session.CurrentArguments[p] == null || 
            string.IsNullOrWhiteSpace(session.CurrentArguments[p]?.ToString())).ToList();

        if (!remainingRequired.Any())
        {
            session.Status = ElicitationStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;
            result.IsComplete = true;
            _logger.LogInformation("✅ Elicitation session #{Id} completed successfully", sessionId);
        }
        else
        {
            result.RemainingQuestions = session.Questions
                .Where(q => remainingRequired.Contains(q.Parameter, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        return result;
    }

    /// <summary>
    /// Get elicitation session by ID
    /// </summary>
    public async Task<ElicitationSession?> GetSessionAsync(int sessionId)
    {
        await Task.CompletedTask; // For async compliance
        return _sessions.FirstOrDefault(s => s.Id == sessionId);
    }

    /// <summary>
    /// Validate answer for tool parameter (simplified validation)
    /// </summary>
    private async Task<ValidationResult> ValidateAnswerAsync(string toolName, string parameter, string answer)
    {
        await Task.Delay(50); // Simulate validation processing

        if (string.IsNullOrWhiteSpace(answer))
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = "Answer cannot be empty"
            };
        }

        // Add specific validation logic based on parameter type
        switch (parameter.ToLowerInvariant())
        {
            case "budget":
                if (!decimal.TryParse(answer.Replace("$", "").Replace(",", ""), out var budget) || budget <= 0)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Budget must be a positive number"
                    };
                }
                break;
            case "duration":
                if (!answer.ToLowerInvariant().Contains("day") && !answer.ToLowerInvariant().Contains("week") && !answer.ToLowerInvariant().Contains("month"))
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Duration should include 'day', 'week', or 'month'"
                    };
                }
                break;
        }

        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Validate an answer against question rules
    /// </summary>
    private async Task<ValidationResult> ValidateAnswerAsync(ElicitationQuestion question, string answer)
    {
        await Task.Delay(50); // Simulate validation processing

        if (string.IsNullOrWhiteSpace(answer))
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "Answer cannot be empty" };
        }

        switch (question.QuestionType)
        {
            case ElicitationQuestionType.Email:
                if (!IsValidEmail(answer))
                {
                    return new ValidationResult { IsValid = false, ErrorMessage = "Please provide a valid email address" };
                }
                break;

            case ElicitationQuestionType.Numeric:
                if (!IsValidNumeric(answer))
                {
                    return new ValidationResult { IsValid = false, ErrorMessage = "Please provide a valid number" };
                }
                break;

            case ElicitationQuestionType.Date:
                if (!IsValidDate(answer))
                {
                    return new ValidationResult { IsValid = false, ErrorMessage = "Please provide a valid date" };
                }
                break;

            case ElicitationQuestionType.Phone:
                if (!IsValidPhone(answer))
                {
                    return new ValidationResult { IsValid = false, ErrorMessage = "Please provide a valid phone number" };
                }
                break;
        }

        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Process the answer based on question type
    /// </summary>
    private object ProcessAnswer(ElicitationQuestion question, string answer)
    {
        return question.QuestionType switch
        {
            ElicitationQuestionType.Numeric => ParseNumeric(answer),
            ElicitationQuestionType.Boolean => ParseBoolean(answer),
            ElicitationQuestionType.Date => ParseDate(answer),
            ElicitationQuestionType.MultipleChoice => answer.Split(',').Select(s => s.Trim()).ToArray(),
            _ => answer
        };
    }

    /// <summary>
    /// Get elicitation session by ID
    /// </summary>
    public ElicitationSession? GetSession(int sessionId)
    {
        return _sessions.FirstOrDefault(s => s.Id == sessionId);
    }

    /// <summary>
    /// Get active elicitation sessions
    /// </summary>
    public IEnumerable<ElicitationSession> GetActiveSessions()
    {
        return _sessions.Where(s => s.Status == ElicitationStatus.Active);
    }

    /// <summary>
    /// Cancel an elicitation session
    /// </summary>
    public async Task CancelSessionAsync(int sessionId)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session != null && session.Status == ElicitationStatus.Active)
        {
            session.Status = ElicitationStatus.Cancelled;
            session.CompletedAt = DateTime.UtcNow;
            
            _logger.LogInformation("❌ Elicitation session #{SessionId} cancelled", sessionId);
        }
    }

    // Helper validation methods
    private bool IsValidEmail(string email) => email.Contains("@") && email.Contains(".");
    private bool IsValidNumeric(string value) => double.TryParse(value.Replace("$", "").Replace(",", ""), out _);
    private bool IsValidDate(string date) => DateTime.TryParse(date, out _);
    private bool IsValidPhone(string phone) => phone.Length >= 10 && phone.All(c => char.IsDigit(c) || c == '-' || c == '(' || c == ')' || c == ' ');

    private object ParseNumeric(string value) => double.TryParse(value.Replace("$", "").Replace(",", ""), out var result) ? result : 0;
    private object ParseBoolean(string value) => value.ToLowerInvariant() is "yes" or "true" or "1" or "y";
    private object ParseDate(string value) => DateTime.TryParse(value, out var result) ? result : DateTime.Now;
}

/// <summary>
/// Represents an elicitation session
/// </summary>
public class ElicitationSession
{
    public int Id { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public Dictionary<string, object?> CurrentArguments { get; set; } = new();
    public List<string> MissingParameters { get; set; } = new();
    public string? Context { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ElicitationStatus Status { get; set; }
    public List<ElicitationQuestion> Questions { get; set; } = new();
}

/// <summary>
/// Represents an elicitation question
/// </summary>
public class ElicitationQuestion
{
    public string Parameter { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public ElicitationQuestionType QuestionType { get; set; }
    public List<string> ValidationRules { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public bool IsRequired { get; set; }
    public bool IsAnswered { get; set; }
    public string? Answer { get; set; }
}

/// <summary>
/// Elicitation status enumeration
/// </summary>
public enum ElicitationStatus
{
    Active,
    Completed,
    Cancelled
}

/// <summary>
/// Elicitation question types
/// </summary>
public enum ElicitationQuestionType
{
    Text,
    Numeric,
    Boolean,
    Date,
    Email,
    Phone,
    MultipleChoice
}

/// <summary>
/// Elicitation result
/// </summary>
public class ElicitationResult
{
    public bool IsValid { get; set; }
    public bool IsComplete { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Suggestions { get; set; } = new();
    public List<ElicitationQuestion> RemainingQuestions { get; set; } = new();
}

/// <summary>
/// Validation result
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}
