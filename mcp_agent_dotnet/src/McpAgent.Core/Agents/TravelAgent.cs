using Microsoft.Extensions.Logging;

namespace McpAgent.Core.Agents;

/// <summary>
/// Travel agent that simulates travel booking with price confirmation via elicitation.
/// Based on the Python implementation from the MCP agents sample.
/// </summary>
public class TravelAgent : AgentBase
{
    private readonly ILogger<TravelAgent>? _logger;

    public TravelAgent(ILogger<TravelAgent>? logger = null)
    {
        _logger = logger;
    }

    public override async Task<string> ExecuteAsync(AgentContext context)
    {
        ValidateArgument(context.Arguments, "destination");
        
        var destination = context.Arguments["destination"]?.ToString()!;
        _logger?.LogInformation("Starting travel booking for destination: {Destination}", destination);

        // Define booking steps
        var steps = new[]
        {
            "Searching for flights and hotels...",
            "Comparing prices across providers...", 
            "Checking availability...",
            "Preparing booking summary..."
        };

        // Send progress notifications for each step
        for (int i = 0; i < steps.Length; i++)
        {
            await context.Session.SendProgressNotificationAsync(
                progressToken: context.RequestId,
                progress: i * 25,
                total: 100,
                message: steps[i],
                relatedRequestId: context.RequestId);

            await SimulateWork(2000); // Simulate work
        }

        // Request price confirmation via elicitation
        var estimatedPrice = GetEstimatedPrice(destination);
        _logger?.LogInformation("Requesting price confirmation for ${Price} to {Destination}", estimatedPrice, destination);

        var elicitResult = await context.Session.ElicitAsync(
            message: $"Please confirm the estimated price of ${estimatedPrice} for your trip to {destination}",
            requestedSchema: new PriceConfirmationSchema(),
            relatedRequestId: context.RequestId);

        bool bookingCancelled = false;

        if (elicitResult?.Action == "accept")
        {
            _logger?.LogInformation("User confirmed price: {Content}", elicitResult.Content);
            
            // Complete the booking
            await context.Session.SendProgressNotificationAsync(
                progressToken: context.RequestId,
                progress: 100,
                total: 100,
                message: "Finalizing booking...",
                relatedRequestId: context.RequestId);

            await SimulateWork(1000);
        }
        else if (elicitResult?.Action == "decline")
        {
            _logger?.LogInformation("User declined the booking");
            bookingCancelled = true;
        }

        var result = bookingCancelled 
            ? $"Booking cancelled by user for trip to {destination}."
            : $"Travel booking confirmed! Trip to {destination} for ${estimatedPrice} has been successfully booked. You will receive confirmation details shortly.";

        await context.Session.SendLogMessageAsync(
            level: "info",
            data: result,
            logger: "travel_agent",
            relatedRequestId: context.RequestId);

        return result;
    }

    private static int GetEstimatedPrice(string destination)
    {
        // Simple price estimation based on destination
        return destination.ToLowerInvariant() switch
        {
            "paris" => 1200,
            "tokyo" => 1500,
            "london" => 1000,
            "sydney" => 1800,
            "new york" => 800,
            _ => 1000
        };
    }
}
