using A2A;
using A2A.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;
using Microsoft.OpenApi.Models;
using PongService.Agents;
using PongService.Services;
using System.IdentityModel.Tokens.Jwt;

namespace PongService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.AddServerHeader = false;
        });

        var services = builder.Services;
        var configuration = builder.Configuration;

        // Add services to the container
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddHttpContextAccessor();

        // Configure Microsoft Identity Web API authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(options =>
            {
                configuration.Bind("AzureAd", options);
                options.TokenValidationParameters.ValidateIssuer = true;
                options.TokenValidationParameters.ValidateAudience = true;
                options.TokenValidationParameters.ValidateLifetime = true;
                options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(5);
                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = "role";
            }, options =>
            {
                configuration.Bind("AzureAd", options);
            });

        // Configure controllers with authorization
        services.AddControllers(options =>
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .Build();
            options.Filters.Add(new AuthorizeFilter(policy));
        });

        // Configure Swagger with JWT authentication
        services.AddSwaggerGen(c =>
        {
            c.EnableAnnotations();

            // Add JWT Authentication
            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "JWT Authentication",
                Description = "Enter JWT Bearer token **_only_**",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer", // must be lower case
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Id = JwtBearerDefaults.AuthenticationScheme,
                    Type = ReferenceType.SecurityScheme
                }
            };
            c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {securityScheme, Array.Empty<string>() }
            });
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "PongService API",
                Version = "v1",
                Description = "A2A MCP Authentication Pong Service API"
            });
        });

        // Register HTTP client for MCP communication
        services.AddHttpClient<McpClientService>(client =>
        {
            client.BaseAddress = new Uri(configuration["McpServer:Url"] ?? "http://localhost:5002");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Register MCP client service with proper dependencies
        services.AddSingleton<IMcpClientService>(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var logger = provider.GetRequiredService<ILogger<McpClientService>>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(McpClientService));
            
            return new McpClientService(config, logger, loggerFactory, httpClient);
        });

        // Register TaskManager as singleton
        services.AddSingleton<ITaskManager>(provider =>
        {
            var taskManager = new TaskManager();
            var mcpClientService = provider.GetRequiredService<IMcpClientService>();
            var logger = provider.GetRequiredService<ILogger<PongAgent>>();
            var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
            var pongAgent = new PongAgent(mcpClientService, logger, httpContextAccessor);
            pongAgent.Attach(taskManager);
            return taskManager;
        });

        // Configure logging
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        var app = builder.Build();

        // Clear default claim type mappings and enable PII for development
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        if (app.Environment.IsDevelopment())
        {
            IdentityModelEventSource.ShowPII = true;
        }

        // Use security headers (simplified approach)
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "PongService API v1");
            });
        }

        app.UseHttpsRedirection();
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // Get the configured TaskManager for A2A endpoints
        var taskManager = app.Services.GetRequiredService<ITaskManager>();

        // Map A2A endpoints
        app.MapA2A(taskManager, "/pong");
        app.MapHttpA2A(taskManager, "/pong");

        // Health check endpoint (allow anonymous access)
        app.MapGet("/health", () => new { Status = "Healthy", Service = "PongService", Timestamp = DateTime.UtcNow })
           .AllowAnonymous();

        app.Run();
    }
}
