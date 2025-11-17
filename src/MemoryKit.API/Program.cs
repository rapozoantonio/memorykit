using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// Add API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MemoryKit API",
        Version = "v1.0",
        Description = "Enterprise-grade neuroscience-inspired memory infrastructure for LLM applications",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "MemoryKit",
            Url = new Uri("https://github.com/antoniorapozo/memorykit")
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });
});

// Add logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

// Add MediatR for CQRS
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
    config.RegisterServicesFromAssemblyContaining(typeof(MemoryKit.Application.UseCases.AddMessage.AddMessageHandler));
});

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<MemoryKit.Application.Validators.CreateMessageRequestValidator>();

// Register Memory Layer Services (In-Memory implementations for MVP)
builder.Services.AddSingleton<MemoryKit.Infrastructure.Azure.IWorkingMemoryService,
    MemoryKit.Infrastructure.InMemory.InMemoryWorkingMemoryService>();
builder.Services.AddSingleton<MemoryKit.Infrastructure.Azure.IScratchpadService,
    MemoryKit.Infrastructure.InMemory.InMemoryScratchpadService>();
builder.Services.AddSingleton<MemoryKit.Infrastructure.Azure.IEpisodicMemoryService,
    MemoryKit.Infrastructure.InMemory.InMemoryEpisodicMemoryService>();
builder.Services.AddSingleton<MemoryKit.Infrastructure.Azure.IProceduralMemoryService,
    MemoryKit.Infrastructure.InMemory.InMemoryProceduralMemoryService>();

// Register Cognitive Services
builder.Services.AddSingleton<MemoryKit.Infrastructure.Cognitive.IPrefrontalController,
    MemoryKit.Infrastructure.Cognitive.PrefrontalControllerService>();
builder.Services.AddSingleton<MemoryKit.Infrastructure.Cognitive.IAmygdalaImportanceEngine,
    MemoryKit.Infrastructure.Cognitive.AmygdalaImportanceEngineService>();

// Register Semantic Kernel Service (Mock for MVP)
builder.Services.AddSingleton<MemoryKit.Infrastructure.SemanticKernel.ISemanticKernelService,
    MemoryKit.Infrastructure.SemanticKernel.MockSemanticKernelService>();

// Register Memory Orchestrator
builder.Services.AddScoped<MemoryKit.Domain.Interfaces.IMemoryOrchestrator,
    MemoryKit.Application.Services.MemoryOrchestrator>();

// Add health checks
builder.Services.AddHealthChecks();

// Build the app
var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "MemoryKit API v1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map health check
app.MapHealthChecks("/health");

// Run the application
app.Run();
