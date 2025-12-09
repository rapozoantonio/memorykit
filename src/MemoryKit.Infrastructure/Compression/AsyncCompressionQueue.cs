using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace MemoryKit.Infrastructure.Compression;

/// <summary>
/// Async queue for non-blocking compression operations.
/// Compression happens on background threads to avoid blocking the hot path.
/// </summary>
public class AsyncCompressionQueue : IAsyncDisposable
{
    private readonly ICompressionService _compressionService;
    private readonly ILogger<AsyncCompressionQueue> _logger;
    private readonly ConcurrentQueue<CompressionTask> _queue;
    private readonly SemaphoreSlim _workAvailable;
    private readonly CancellationTokenSource _shutdownTokenSource;
    private readonly Task[] _workerTasks;
    private readonly int _maxQueueSize;

    public int QueuedCount => _queue.Count;

    public AsyncCompressionQueue(
        ICompressionService compressionService,
        ILogger<AsyncCompressionQueue> logger,
        int workerCount = 2,
        int maxQueueSize = 1000)
    {
        _compressionService = compressionService ?? throw new ArgumentNullException(nameof(compressionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _queue = new ConcurrentQueue<CompressionTask>();
        _workAvailable = new SemaphoreSlim(0);
        _shutdownTokenSource = new CancellationTokenSource();
        _maxQueueSize = maxQueueSize;

        // Start worker tasks
        _workerTasks = new Task[workerCount];
        for (int i = 0; i < workerCount; i++)
        {
            _workerTasks[i] = Task.Run(() => WorkerLoop(_shutdownTokenSource.Token));
        }

        _logger.LogInformation(
            "AsyncCompressionQueue started with {WorkerCount} workers, algorithm: {Algorithm}",
            workerCount,
            _compressionService.AlgorithmName);
    }

    /// <summary>
    /// Enqueues data for compression.
    /// </summary>
    public Task<byte[]> EnqueueCompressionAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (data == null || data.Length == 0)
            return Task.FromResult(Array.Empty<byte>());

        if (_queue.Count >= _maxQueueSize)
        {
            _logger.LogWarning("Compression queue full ({Count} items), compressing synchronously", _queue.Count);
            return _compressionService.CompressAsync(data, cancellationToken);
        }

        var tcs = new TaskCompletionSource<byte[]>();
        var task = new CompressionTask
        {
            Data = data,
            CompletionSource = tcs,
            EnqueuedAt = Stopwatch.GetTimestamp()
        };

        _queue.Enqueue(task);
        _workAvailable.Release();

        return tcs.Task;
    }

    /// <summary>
    /// Enqueues a string for compression.
    /// </summary>
    public Task<byte[]> EnqueueCompressionAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text))
            return Task.FromResult(Array.Empty<byte>());

        var data = System.Text.Encoding.UTF8.GetBytes(text);
        return EnqueueCompressionAsync(data, cancellationToken);
    }

    private async Task WorkerLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _workAvailable.WaitAsync(cancellationToken);

                if (_queue.TryDequeue(out var task))
                {
                    var queueTime = Stopwatch.GetElapsedTime(task.EnqueuedAt);

                    try
                    {
                        var compressed = await _compressionService.CompressAsync(task.Data, cancellationToken);
                        task.CompletionSource.SetResult(compressed);

                        var compressionRatio = (1.0 - (double)compressed.Length / task.Data.Length) * 100;
                        _logger.LogDebug(
                            "Compressed {OriginalSize} → {CompressedSize} bytes ({Ratio:F1}% reduction) in {Duration}ms (queued {QueueTime}ms)",
                            task.Data.Length,
                            compressed.Length,
                            compressionRatio,
                            Stopwatch.GetElapsedTime(task.EnqueuedAt).TotalMilliseconds,
                            queueTime.TotalMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Compression failed for {Size} bytes", task.Data.Length);
                        task.CompletionSource.SetException(ex);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker loop error");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Shutting down AsyncCompressionQueue, {Pending} items pending", _queue.Count);

        _shutdownTokenSource.Cancel();

        // Wait for workers to finish
        await Task.WhenAll(_workerTasks);

        // Process remaining items
        while (_queue.TryDequeue(out var task))
        {
            task.CompletionSource.TrySetCanceled();
        }

        _workAvailable.Dispose();
        _shutdownTokenSource.Dispose();

        _logger.LogInformation("AsyncCompressionQueue shutdown complete");
    }

    private class CompressionTask
    {
        public required byte[] Data { get; init; }
        public required TaskCompletionSource<byte[]> CompletionSource { get; init; }
        public required long EnqueuedAt { get; init; }
    }
}
