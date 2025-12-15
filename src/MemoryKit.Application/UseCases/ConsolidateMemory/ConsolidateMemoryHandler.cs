using MediatR;
using Microsoft.Extensions.Logging;
using MemoryKit.Application.DTOs;
using MemoryKit.Domain.Interfaces;
using System.Diagnostics;

namespace MemoryKit.Application.UseCases.ConsolidateMemory;

/// <summary>
/// Handler for triggering memory consolidation between layers.
/// </summary>
public class ConsolidateMemoryHandler : IRequestHandler<ConsolidateMemoryCommand, ConsolidateMemoryResponse>
{
    private readonly IHippocampusIndexer _hippocampus;
    private readonly ILogger<ConsolidateMemoryHandler> _logger;

    public ConsolidateMemoryHandler(
        IHippocampusIndexer hippocampus,
        ILogger<ConsolidateMemoryHandler> _logger)
    {
        _hippocampus = hippocampus;
        this._logger = _logger;
    }

    public async Task<ConsolidateMemoryResponse> Handle(ConsolidateMemoryCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting memory consolidation for conversation {ConversationId}, Force: {Force}",
            request.ConversationId, request.Force);

        var stopwatch = Stopwatch.StartNew();
        var consolidatedAt = DateTime.UtcNow;

        try
        {
            // Perform consolidation for the specific conversation
            await _hippocampus.ConsolidateAsync(
                request.UserId,
                request.ConversationId,
                cancellationToken);

            // Get consolidation metrics
            var metrics = await _hippocampus.GetConsolidationMetricsAsync(
                request.UserId,
                cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "Consolidation completed in {DurationMs}ms, processed {MessagesConsolidated} messages",
                stopwatch.ElapsedMilliseconds, metrics.MessagesConsolidated);

            // For v0.1, we return simplified metrics
            // In future versions, track detailed layer transitions
            return new ConsolidateMemoryResponse
            {
                ConsolidatedAt = consolidatedAt,
                ItemsProcessed = metrics.MessagesConsolidated,
                WorkingToSemantic = metrics.MessagesConsolidated / 2, // Simplified estimate
                SemanticToEpisodic = metrics.MessagesConsolidated / 4, // Simplified estimate
                CompressionRatio = metrics.AverageImportanceScore, // Using importance as proxy
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error during consolidation for conversation {ConversationId}", request.ConversationId);
            throw;
        }
    }
}
