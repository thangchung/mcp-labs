using A2A;
using A2A.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;
using ServiceDefaults;
using Microsoft.OpenApi.Models;
using PingService.Agents;
using PingService.Services;
using PingService.Middleware;
using System.IdentityModel.Tokens.Jwt;

namespace PingService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure structured logging with JSON formatter
        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole(options =>
        {
            options.IncludeScopes = true;
            options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
            options.UseUtcTimestamp = true;
            options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
            {
                Indented = false
            };
        });

        // Add OpenTelemetry logging
        builder.AddObservabilityLogging("PingService");

        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.AddServerHeader = false;
        });

        var services = builder.Services;
        var configuration = builder.Configuration;

        // Add observability (OpenTelemetry)
        services.AddObservability("PingService", configuration, builder.Environment);

        // Add services to the container
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddHttpContextAccessor();
        services.AddServiceDefaults(configuration);

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
                Title = "PingService API",
                Version = "v1",
                Description = "A2A MCP Authentication Ping Service API - Client service that sends messages via A2A protocol"
            });
        });

        // Register A2A Client service
        services.AddSingleton<IA2AClientService, A2AClientService>();

        // Register TaskManager as singleton
        services.AddSingleton<ITaskManager>(provider =>
        {
            var taskManager = new TaskManager();
            var a2aClientService = provider.GetRequiredService<IA2AClientService>();
            var logger = provider.GetRequiredService<ILogger<PingAgent>>();
            var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
            var pingAgent = new PingAgent(a2aClientService, logger, httpContextAccessor);
            pingAgent.Attach(taskManager);
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

        // Use HSTS in production
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
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "PingService API v1");
            });
        }

        app.UseHttpsRedirection();
        app.UseRouting();

        // Add JSON-RPC tracing middleware before authentication
        app.UseMiddleware<JsonRpcTracingMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // Get the configured TaskManager for A2A endpoints
        var taskManager = app.Services.GetRequiredService<ITaskManager>();

        // Map A2A endpoints
        app.MapA2A(taskManager, "/ping");
        app.MapHttpA2A(taskManager, "/ping");

        app.MapDefaultEndpoints("PingService");

        app.Run();
    }
}
