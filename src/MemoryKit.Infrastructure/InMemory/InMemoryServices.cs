using MemoryKit.Domain.Entities;
using MemoryKit.Domain.Interfaces;

namespace MemoryKit.Infrastructure.InMemory;

/// <summary>
/// In-memory working memory implementation for testing and MVP.
/// </summary>
public class InMemoryWorkingMemory : IMemoryLayer
{
    private readonly Dictionary<string, Queue<Message>> _storage = new();
    private const int MaxItems = 10;
    private readonly object _lock = new();

    public async Task<T[]> RetrieveAsync<T>(
        string query,
        int maxResults,
        CancellationToken cancellationToken = default)
        where T : notnull
    {
        await Task.Delay(1, cancellationToken); // Simulate async operation
        lock (_lock)
        {
            var results = new List<T>();
            if (typeof(T) == typeof(Message))
            {
                foreach (var queue in _storage.Values)
                {
                    results.AddRange(queue.Where(m =>
                        m.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
                        .Cast<T>()
                        .Take(maxResults));
                }
            }
            return results.ToArray();
        }
    }

    public async Task StoreAsync<T>(T item, CancellationToken cancellationToken = default)
        where T : notnull
    {
        await Task.Delay(1, cancellationToken);
        lock (_lock)
        {
            if (item is Message msg)
            {
                var key = $"{msg.UserId}:{msg.ConversationId}";
                if (!_storage.ContainsKey(key))
                    _storage[key] = new Queue<Message>();

                _storage[key].Enqueue(msg);
                while (_storage[key].Count > MaxItems)
                    _storage[key].Dequeue();
            }
        }
    }

    public async Task<double> GetAverageLatencyAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(1.0); // Simulated 1ms latency
    }

    public async Task PruneAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);
        lock (_lock)
        {
            var keysToRemove = _storage.Where(kvp => kvp.Value.Count == 0).Select(kvp => kvp.Key).ToList();
            foreach (var key in keysToRemove)
                _storage.Remove(key);
        }
    }
}

/// <summary>
/// In-memory storage implementation for testing.
/// </summary>
public class InMemoryStorage
{
    private readonly Dictionary<string, object> _data = new();
    private readonly object _lock = new();

    public void Store<T>(string key, T value) where T : notnull
    {
        lock (_lock)
        {
            _data[key] = value;
        }
    }

    public bool TryGet<T>(string key, out T? value) where T : notnull
    {
        lock (_lock)
        {
            if (_data.TryGetValue(key, out var obj))
            {
                value = (T?)obj;
                return value != null;
            }
            value = default(T);
            return false;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _data.Clear();
        }
    }
}
