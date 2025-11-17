using FluentValidation.AspNetCore;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// 1. LOGGING & MONITORING (Application Insights)
// ============================================================================
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
    config.AddApplicationInsights();
});

// Add Application Insights telemetry
var appInsightsKey = builder.Configuration["ApplicationInsights:InstrumentationKey"];
if (!string.IsNullOrEmpty(appInsightsKey))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
        options.EnableAdaptiveSampling = true;
        options.EnableQuickPulseMetricStream = true;
    });
}

// ============================================================================
// 2. AUTHENTICATION & AUTHORIZATION
// ============================================================================
builder.Services.AddAuthentication("ApiKey")
    .AddScheme<AuthenticationSchemeOptions, MemoryKit.API.Authentication.ApiKeyAuthenticationHandler>("ApiKey", null);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiKeyPolicy", policy =>
    {
        policy.AuthenticationSchemes.Add("ApiKey");
        policy.RequireAuthenticatedUser();
    });
});

// ============================================================================
// 3. RATE LIMITING
// ============================================================================
builder.Services.AddRateLimiter(options =>
{
    // Fixed window rate limiter: 100 requests per minute
    options.AddFixedWindowLimiter("fixed", options =>
    {
        options.PermitLimit = int.Parse(builder.Configuration["RateLimiting:PermitLimit"] ?? "100");
        options.Window = TimeSpan.Parse(builder.Configuration["RateLimiting:Window"] ?? "00:01:00");
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 20;
    });

    // Sliding window rate limiter for bursts
    options.AddSlidingWindowLimiter("sliding", options =>
    {
        options.PermitLimit = 200;
        options.Window = TimeSpan.FromMinutes(1);
        options.SegmentsPerWindow = 4;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 10;
    });

    // Per-user concurrency limiter
    options.AddConcurrencyLimiter("concurrent", options =>
    {
        options.PermitLimit = 10;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 5;
    });

    // Global partition by API key
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault() ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(apiKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 1000,
            Window = TimeSpan.FromHours(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            await context.HttpContext.Response.WriteAsync(
                $"Too many requests. Please try again after {retryAfter.TotalSeconds} seconds.",
                cancellationToken);
        }
        else
        {
            await context.HttpContext.Response.WriteAsync(
                "Too many requests. Please try again later.",
                cancellationToken);
        }
    };
});

// ============================================================================
// 4. CORS CONFIGURATION
// ============================================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000", "http://localhost:5173" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ============================================================================
// 5. API CONTROLLERS & VALIDATION
// ============================================================================
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<MemoryKit.Application.Validators.CreateMessageRequestValidator>();

// ============================================================================
// 6. API DOCUMENTATION (Swagger/OpenAPI)
// ============================================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MemoryKit API",
        Version = "v1.0.0",
        Description = "Enterprise-grade neuroscience-inspired memory infrastructure for LLM applications",
        Contact = new OpenApiContact
        {
            Name = "Antonio Rapozo",
            Url = new Uri("https://github.com/rapozoantonio/memorykit"),
            Email = "antonio@memorykit.dev"
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Add API Key security definition
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-API-Key",
        Description = "API Key authentication. Obtain your key from the MemoryKit dashboard."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// ============================================================================
// 7. MEDIATR (CQRS)
// ============================================================================
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
    config.RegisterServicesFromAssemblyContaining(typeof(MemoryKit.Application.UseCases.AddMessage.AddMessageHandler));
});

// ============================================================================
// 8. HEALTH CHECKS
// ============================================================================
builder.Services.AddHealthChecks()
    .AddCheck<MemoryKit.API.HealthChecks.MemoryServicesHealthCheck>("memory_services")
    .AddCheck<MemoryKit.API.HealthChecks.CognitiveServicesHealthCheck>("cognitive_services");

// ============================================================================
// 9. MEMORY LAYER SERVICES (Clean registration - no duplicates)
// ============================================================================

// Register Semantic Kernel Service
builder.Services.AddSingleton<MemoryKit.Infrastructure.SemanticKernel.ISemanticKernelService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<MemoryKit.Infrastructure.SemanticKernel.SemanticKernelService>>();

    // Use real Semantic Kernel if Azure OpenAI is configured, otherwise use mock
    var endpoint = config["AzureOpenAI:Endpoint"];
    if (!string.IsNullOrEmpty(endpoint))
    {
        return new MemoryKit.Infrastructure.SemanticKernel.SemanticKernelService(config, logger);
    }
    else
    {
        logger.LogWarning("Azure OpenAI not configured. Using mock Semantic Kernel service.");
        return new MemoryKit.Infrastructure.SemanticKernel.MockSemanticKernelService(logger);
    }
});

// Register Memory Services (In-Memory implementations for MVP)
builder.Services.AddSingleton<MemoryKit.Infrastructure.Azure.IWorkingMemoryService,
    MemoryKit.Infrastructure.InMemory.InMemoryWorkingMemoryService>();
builder.Services.AddSingleton<MemoryKit.Infrastructure.Azure.IScratchpadService,
    MemoryKit.Infrastructure.InMemory.InMemoryScratchpadService>();
builder.Services.AddSingleton<MemoryKit.Infrastructure.Azure.IEpisodicMemoryService,
    MemoryKit.Infrastructure.InMemory.InMemoryEpisodicMemoryService>();

// Register Enhanced Procedural Memory Service with AI support
builder.Services.AddSingleton<MemoryKit.Infrastructure.Azure.IProceduralMemoryService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<MemoryKit.Infrastructure.InMemory.EnhancedInMemoryProceduralMemoryService>>();
    var semanticKernel = sp.GetService<MemoryKit.Infrastructure.SemanticKernel.ISemanticKernelService>();
    return new MemoryKit.Infrastructure.InMemory.EnhancedInMemoryProceduralMemoryService(logger, semanticKernel);
});

// Register Cognitive Services
builder.Services.AddSingleton<MemoryKit.Infrastructure.Cognitive.IPrefrontalController,
    MemoryKit.Application.Services.PrefrontalController>();
builder.Services.AddSingleton<MemoryKit.Infrastructure.Cognitive.IAmygdalaImportanceEngine,
    MemoryKit.Application.Services.AmygdalaImportanceEngine>();

// Register Hippocampus Indexer for memory consolidation
builder.Services.AddSingleton<MemoryKit.Infrastructure.Cognitive.IHippocampusIndexer>(sp =>
{
    var workingMemory = sp.GetRequiredService<MemoryKit.Infrastructure.Azure.IWorkingMemoryService>();
    var scratchpad = sp.GetRequiredService<MemoryKit.Infrastructure.Azure.IScratchpadService>();
    var episodic = sp.GetRequiredService<MemoryKit.Infrastructure.Azure.IEpisodicMemoryService>();
    var amygdala = sp.GetRequiredService<MemoryKit.Infrastructure.Cognitive.IAmygdalaImportanceEngine>();
    var logger = sp.GetRequiredService<ILogger<MemoryKit.Infrastructure.Cognitive.HippocampusIndexer>>();
    return new MemoryKit.Infrastructure.Cognitive.HippocampusIndexer(workingMemory, scratchpad, episodic, amygdala, logger);
});

// Register Memory Orchestrator
builder.Services.AddSingleton<MemoryKit.Domain.Interfaces.IMemoryOrchestrator>(sp =>
{
    var workingMemory = sp.GetRequiredService<MemoryKit.Infrastructure.Azure.IWorkingMemoryService>();
    var scratchpad = sp.GetRequiredService<MemoryKit.Infrastructure.Azure.IScratchpadService>();
    var episodic = sp.GetRequiredService<MemoryKit.Infrastructure.Azure.IEpisodicMemoryService>();
    var procedural = sp.GetRequiredService<MemoryKit.Infrastructure.Azure.IProceduralMemoryService>();
    var prefrontal = sp.GetRequiredService<MemoryKit.Infrastructure.Cognitive.IPrefrontalController>();
    var amygdala = sp.GetRequiredService<MemoryKit.Infrastructure.Cognitive.IAmygdalaImportanceEngine>();
    var logger = sp.GetRequiredService<ILogger<MemoryKit.Application.Services.MemoryOrchestrator>>();
    var semanticKernel = sp.GetService<MemoryKit.Infrastructure.SemanticKernel.ISemanticKernelService>();

    return new MemoryKit.Application.Services.MemoryOrchestrator(
        workingMemory, scratchpad, episodic, procedural, prefrontal, amygdala, logger, semanticKernel);
});

// ============================================================================
// BUILD APPLICATION
// ============================================================================
var app = builder.Build();

// ============================================================================
// MIDDLEWARE PIPELINE CONFIGURATION
// ============================================================================

// 1. Exception handling
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// 2. Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Permissions-Policy", "accelerometer=(), camera=(), geolocation=(), microphone=()");

    // Remove server header
    context.Response.Headers.Remove("Server");

    await next();
});

// 3. Swagger (in all environments for open source)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "MemoryKit API v1");
    options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    options.DocumentTitle = "MemoryKit API Documentation";
    options.DefaultModelsExpandDepth(-1); // Hide schemas section by default
});

// 4. CORS
app.UseCors("AllowSpecificOrigins");

// 5. HTTPS Redirection
app.UseHttpsRedirection();

// 6. Rate Limiting
app.UseRateLimiter();

// 7. Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// 8. Request logging
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Request: {Method} {Path}", context.Request.Method, context.Request.Path);

    await next();

    logger.LogInformation("Response: {StatusCode}", context.Response.StatusCode);
});

// ============================================================================
// ENDPOINT MAPPING
// ============================================================================

// Map controllers
app.MapControllers()
    .RequireRateLimiting("fixed");

// Map health checks
app.MapHealthChecks("/health")
    .AllowAnonymous();

app.MapHealthChecks("/health/live")
    .AllowAnonymous();

app.MapHealthChecks("/health/ready")
    .AllowAnonymous();

// Metrics endpoint (for monitoring)
app.MapGet("/metrics", () => new
{
    uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime(),
    timestamp = DateTime.UtcNow,
    version = "1.0.0",
    environment = app.Environment.EnvironmentName
})
.AllowAnonymous();

// Error handling endpoint
app.MapGet("/error", () => Results.Problem("An error occurred processing your request."))
    .ExcludeFromDescription();

// ============================================================================
// RUN APPLICATION
// ============================================================================
app.Run();
