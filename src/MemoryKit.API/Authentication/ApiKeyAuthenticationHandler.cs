using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace MemoryKit.API.Authentication;

/// <summary>
/// API Key authentication handler for production security.
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyAuthenticationHandler> _logger;
    private const string ApiKeyHeaderName = "X-API-Key";

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, loggerFactory, encoder)
    {
        _configuration = configuration;
        _logger = loggerFactory.CreateLogger<ApiKeyAuthenticationHandler>();
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if API key is in header
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeaderValues))
        {
            _logger.LogWarning("API key header missing for request {Path}", Request.Path);
            return AuthenticateResult.Fail("Missing API Key");
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            _logger.LogWarning("Empty API key provided for request {Path}", Request.Path);
            return AuthenticateResult.Fail("Invalid API Key");
        }

        // Validate API key
        var isValid = await ValidateApiKeyAsync(providedApiKey);

        if (!isValid)
        {
            _logger.LogWarning("Invalid API key attempt: {ApiKey}", providedApiKey.Substring(0, Math.Min(8, providedApiKey.Length)) + "...");
            return AuthenticateResult.Fail("Invalid API Key");
        }

        // Extract user ID from API key (for demo, we'll use a simple mapping)
        var userId = GetUserIdFromApiKey(providedApiKey);

        // Create claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("sub", userId),
            new("apikey", providedApiKey),
            new(ClaimTypes.Role, "User")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        _logger.LogInformation("API key authentication successful for user {UserId}", userId);

        return AuthenticateResult.Success(ticket);
    }

    /// <summary>
    /// Validates the API key against configured valid keys.
    /// In production, this should check against a database or secure store.
    /// </summary>
    private async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        // For MVP: Check against configured valid keys
        var validKeys = _configuration.GetSection("ApiKeys:ValidKeys").Get<string[]>()
            ?? Array.Empty<string>();

        // In production, you would:
        // 1. Hash the API key
        // 2. Check against database/Azure Key Vault
        // 3. Verify key hasn't expired
        // 4. Check rate limits per key
        // 5. Log access for audit

        return await Task.FromResult(validKeys.Contains(apiKey));
    }

    /// <summary>
    /// Extracts user ID from API key.
    /// In production, this should lookup from a secure mapping.
    /// </summary>
    private string GetUserIdFromApiKey(string apiKey)
    {
        // For MVP: Use a simple hash of the API key as user ID
        // In production: Lookup from database/cache
        var userMappings = _configuration.GetSection("ApiKeys:UserMappings").Get<Dictionary<string, string>>()
            ?? new Dictionary<string, string>();

        if (userMappings.TryGetValue(apiKey, out var userId))
        {
            return userId;
        }

        // Fallback: generate deterministic user ID from API key
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToBase64String(hash).Substring(0, 16);
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.Add("WWW-Authenticate", $"ApiKey realm=\"MemoryKit API\"");
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        await Response.WriteAsJsonAsync(new
        {
            error = "Unauthorized",
            message = "Valid API key required. Include X-API-Key header with your request.",
            documentation = "https://github.com/rapozoantonio/memorykit/docs/authentication.md"
        });
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        await Response.WriteAsJsonAsync(new
        {
            error = "Forbidden",
            message = "You do not have permission to access this resource."
        });
    }
}
