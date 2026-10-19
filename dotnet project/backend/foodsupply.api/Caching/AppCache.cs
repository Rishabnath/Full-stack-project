using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace FoodSupply.Api.Caching;

public sealed class AppCache : IAppCache, IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _tagTokens = new();

    public AppCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        TimeSpan lifetime,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default,
        params string[] tags)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);

        if (lifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lifetime),
                "Cache lifetime must be greater than zero.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var value = await _memoryCache.GetOrCreateAsync(
            key,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = lifetime;
                entry.Size = 1;

                foreach (var tag in tags.Distinct(StringComparer.Ordinal))
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace(tag);
                    var tagToken = _tagTokens.GetOrAdd(
                        tag,
                        static _ => new CancellationTokenSource());
                    entry.AddExpirationToken(new CancellationChangeToken(tagToken.Token));
                }

                cancellationToken.ThrowIfCancellationRequested();
                return await factory(cancellationToken);
            });

        return value ?? throw new InvalidOperationException(
            $"The cache factory returned null for key '{key}'.");
    }

    public void EvictTag(string tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        if (_tagTokens.TryRemove(tag, out var tokenSource))
        {
            tokenSource.Cancel();
        }
    }

    public void Dispose()
    {
        foreach (var tokenSource in _tagTokens.Values)
        {
            tokenSource.Cancel();
        }

        _memoryCache.Dispose();

        foreach (var tokenSource in _tagTokens.Values)
        {
            tokenSource.Dispose();
        }

        _tagTokens.Clear();
    }
}
