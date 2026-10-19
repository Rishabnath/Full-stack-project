namespace FoodSupply.Api.Caching;

/// <summary>
/// Typed application cache with expiration and tag-based invalidation.
/// </summary>
public interface IAppCache
{
    Task<T> GetOrCreateAsync<T>(
        string key,
        TimeSpan lifetime,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default,
        params string[] tags)
        where T : class;

    void EvictTag(string tag);
}
