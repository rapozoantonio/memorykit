using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MemoryKit.Infrastructure.Resilience;

/// <summary>
/// Resilient wrapper for working memory service with fallback support.
/// </summary>
public class ResilientWorkingMemoryService : IWorkingMemoryService
{
    private readonly IWorkingMemoryService _primaryService;
    private readonly IWorkingMemoryService _fallbackService;
    private readonly ILogger<ResilientWorkingMemoryService> _logger;
    private readonly bool _enableAutoFallback;
    private readonly int _maxRetryAttempts;

    public ResilientWorkingMemoryService(
        IWorkingMemoryService primaryService,
        IWorkingMemoryService fallbackService,
        IConfiguration configuration,
        ILogger<ResilientWorkingMemoryService> logger)
    {
        _primaryService = primaryService;
        _fallbackService = fallbackService;
        _logger = logger;
        _enableAutoFallback = configuration.GetValue("MemoryKit:EnableAutoFallback", true);
        _maxRetryAttempts = configuration.GetValue("MemoryKit:MaxRetryAttempts", 3);
    }

    public async Task AddAsync(string userId, string conversationId, Message message, CancellationToken cancellationToken = default)
    {
        await ExecuteWithFallbackAsync(
            async () => await _primaryService.AddAsync(userId, conversationId, message, cancellationToken),
            async () => await _fallbackService.AddAsync(userId, conversationId, message, cancellationToken),
            "AddAsync");
    }

    public async Task<Message[]> GetRecentAsync(string userId, string conversationId, int count = 10, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithFallbackAsync(
            async () => await _primaryService.GetRecentAsync(userId, conversationId, count, cancellationToken),
            async () => await _fallbackService.GetRecentAsync(userId, conversationId, count, cancellationToken),
            "GetRecentAsync");
    }

    public async Task ClearAsync(string userId, string conversationId, CancellationToken cancellationToken = default)
    {
        await ExecuteWithFallbackAsync(
            async () => await _primaryService.ClearAsync(userId, conversationId, cancellationToken),
            async () => await _fallbackService.ClearAsync(userId, conversationId, cancellationToken),
            "ClearAsync");
    }

    public async Task DeleteUserDataAsync(string userId, CancellationToken cancellationToken = default)
    {
        await ExecuteWithFallbackAsync(
            async () => await _primaryService.DeleteUserDataAsync(userId, cancellationToken),
            async () => await _fallbackService.DeleteUserDataAsync(userId, cancellationToken),
            "DeleteUserDataAsync");
    }

    private async Task ExecuteWithFallbackAsync(
        Func<Task> primaryAction,
        Func<Task> fallbackAction,
        string operationName)
    {
        for (int attempt = 0; attempt < _maxRetryAttempts; attempt++)
        {
            try
            {
                await primaryAction();
                return;
            }
            catch (Exception ex) when (attempt < _maxRetryAttempts - 1)
            {
                _logger.LogWarning(ex,
                    "Attempt {Attempt}/{MaxAttempts} failed for {Operation}, retrying...",
                    attempt + 1, _maxRetryAttempts, operationName);
                await Task.Delay(TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)));
            }
            catch (Exception ex) when (_enableAutoFallback)
            {
                _logger.LogError(ex,
                    "Primary service failed for {Operation} after {Attempts} attempts, falling back to secondary",
                    operationName, _maxRetryAttempts);
                
                try
                {
                    await fallbackAction();
                    _logger.LogInformation("Successfully executed {Operation} using fallback service", operationName);
                    return;
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Fallback service also failed for {Operation}", operationName);
                    throw;
                }
            }
        }
    }

    private async Task<T> ExecuteWithFallbackAsync<T>(
        Func<Task<T>> primaryAction,
        Func<Task<T>> fallbackAction,
        string operationName)
    {
        for (int attempt = 0; attempt < _maxRetryAttempts; attempt++)
        {
            try
            {
                return await primaryAction();
            }
            catch (Exception ex) when (attempt < _maxRetryAttempts - 1)
            {
                _logger.LogWarning(ex,
                    "Attempt {Attempt}/{MaxAttempts} failed for {Operation}, retrying...",
                    attempt + 1, _maxRetryAttempts, operationName);
                await Task.Delay(TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)));
            }
            catch (Exception ex) when (_enableAutoFallback)
            {
                _logger.LogError(ex,
                    "Primary service failed for {Operation} after {Attempts} attempts, falling back to secondary",
                    operationName, _maxRetryAttempts);
                
                try
                {
                    var result = await fallbackAction();
                    _logger.LogInformation("Successfully executed {Operation} using fallback service", operationName);
                    return result;
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Fallback service also failed for {Operation}", operationName);
                    throw;
                }
            }
        }

        throw new InvalidOperationException($"Should not reach here in {operationName}");
    }
}
