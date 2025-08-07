using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using McpAgent.XClient.Models;

namespace McpAgent.XClient.Pages;

public partial class ChatFixed : ComponentBase, IAsyncDisposable
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    
    private HubConnection? hubConnection;
    private List<string> messages = new();
    private string userInput = string.Empty;
    private string messageInput = string.Empty;
    private ElicitationState elicitationState = new();

    public bool IsConnected =>
        hubConnection?.State == HubConnectionState.Connected;

    protected override async Task OnInitializedAsync()
    {
        hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:8007/chatHub")
            .Build();

        // Handle regular messages
        hubConnection.On<string, string>("ReceiveMessage", (user, message) =>
        {
            var encodedMsg = $"{user}: {message}";
            messages.Add(encodedMsg);
            InvokeAsync(StateHasChanged);
        });

        // Handle progress updates
        hubConnection.On<string>("ProgressUpdate", (message) =>
        {
            messages.Add($"Progress: {message}");
            InvokeAsync(StateHasChanged);
        });

        // Handle completion messages
        hubConnection.On<string>("TaskCompleted", (message) =>
        {
            messages.Add($"✅ Completed: {message}");
            InvokeAsync(StateHasChanged);
        });

        // Handle elicitation modal display
        hubConnection.On<McpElicitationRequest>("ShowElicitationModal", (request) =>
        {
            ShowElicitationModal(request);
            InvokeAsync(StateHasChanged);
        });

        // Handle elicitation completion
        hubConnection.On<string>("ElicitationCompleted", (message) =>
        {
            elicitationState.IsVisible = false;
            messages.Add($"🎯 {message}");
            InvokeAsync(StateHasChanged);
        });

        // Handle errors
        hubConnection.On<string>("Error", (errorMessage) =>
        {
            messages.Add($"❌ Error: {errorMessage}");
            InvokeAsync(StateHasChanged);
        });

        await hubConnection.StartAsync();
    }

    private async Task Send()
    {
        if (hubConnection is not null && !string.IsNullOrWhiteSpace(messageInput))
        {
            await hubConnection.SendAsync("SendMessage", userInput, messageInput);
            messageInput = string.Empty;
        }
    }

    private async Task TravelToTokyo()
    {
        if (hubConnection is not null)
        {
            await hubConnection.SendAsync("InitiateTravelBooking");
        }
    }

    private async Task ResearchAI()
    {
        if (hubConnection is not null)
        {
            await hubConnection.SendAsync("InitiateResearch");
        }
    }

    private async Task HandleKeyPress(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            await Send();
        }
    }

    private void ShowElicitationModal(McpElicitationRequest request)
    {
        elicitationState.IsVisible = true;
        elicitationState.Message = request.Message;
        elicitationState.Fields = ConvertSchemaToFields(request.RequestedSchema);
        elicitationState.RequestId = Guid.NewGuid().ToString();
    }

    private async Task HandleElicitationAction(string action)
    {
        if (hubConnection is null) return;

        var response = new McpElicitationResponse
        {
            Action = action
        };

        if (action == "accept")
        {
            // Collect form data
            response.Content = new Dictionary<string, object>();
            foreach (var field in elicitationState.Fields)
            {
                if (!string.IsNullOrEmpty(field.Value))
                {
                    response.Content[field.Name] = ConvertFieldValue(field);
                }
            }
        }

        await hubConnection.SendAsync("SubmitElicitationResponse", response);
        elicitationState.IsVisible = false;
    }

    private object ConvertFieldValue(FieldInfo field)
    {
        return field.Type switch
        {
            FieldType.Number => double.TryParse(field.Value, out var numValue) ? numValue : 0,
            FieldType.Boolean => bool.TryParse(field.Value, out var boolValue) ? boolValue : false,
            FieldType.Date => DateTime.TryParse(field.Value, out var dateValue) ? dateValue : DateTime.Now,
            _ => field.Value ?? string.Empty
        };
    }

    private List<FieldInfo> ConvertSchemaToFields(McpJsonSchema schema)
    {
        return schema.Properties.Select(prop => new FieldInfo
        {
            Name = prop.Key,
            Type = DetermineFieldType(prop.Value),
            Required = schema.Required?.Contains(prop.Key) ?? false,
            Description = prop.Value.Description ?? string.Empty,
            Value = string.Empty,
            Options = prop.Value.Enum?.ToList() ?? new List<string>(),
            MinValue = prop.Value.Minimum,
            MaxValue = prop.Value.Maximum,
            MinLength = prop.Value.MinLength,
            MaxLength = prop.Value.MaxLength,
            Pattern = prop.Value.Pattern ?? string.Empty
        }).ToList();
    }

    private FieldType DetermineFieldType(McpJsonSchemaProperty property)
    {
        if (property.Enum?.Any() == true)
            return FieldType.Enum;

        return property.Type?.ToLower() switch
        {
            "string" when property.Format == "email" => FieldType.Email,
            "string" when property.Format == "date" => FieldType.Date,
            "string" => FieldType.String,
            "number" => FieldType.Number,
            "integer" => FieldType.Number,
            "boolean" => FieldType.Boolean,
            _ => FieldType.String
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (hubConnection is not null)
        {
            await hubConnection.DisposeAsync();
        }
    }
}
