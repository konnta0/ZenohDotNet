using System;
using System.Threading.Tasks;

namespace ZenohDotNet.Client;

/// <summary>
/// High-level async Zenoh session for .NET 8.0+.
/// </summary>
public sealed class Session : IAsyncDisposable
{
    private readonly Native.Session _nativeSession;
    private bool _disposed;

    /// <summary>
    /// Opens a new Zenoh session asynchronously with default configuration.
    /// </summary>
    /// <returns>A task representing the async operation, containing the Session.</returns>
    public static async Task<Session> OpenAsync()
    {
        return await OpenAsync(null).ConfigureAwait(false);
    }

    /// <summary>
    /// Opens a new Zenoh session asynchronously with the specified JSON configuration.
    /// </summary>
    /// <param name="configJson">JSON configuration string, or null for default configuration.</param>
    /// <returns>A task representing the async operation, containing the Session.</returns>
    public static async Task<Session> OpenAsync(string? configJson)
    {
        return await Task.Run(() => new Session(configJson)).ConfigureAwait(false);
    }

    private Session(string? configJson)
    {
        _nativeSession = new Native.Session(configJson);
    }

    /// <summary>
    /// Declares a publisher for the specified key expression.
    /// </summary>
    /// <param name="keyExpr">The key expression to publish on.</param>
    /// <returns>A task representing the async operation, containing the Publisher.</returns>
    /// <exception cref="ArgumentNullException">Thrown when keyExpr is null or empty.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the publisher cannot be created.</exception>
    public async Task<Publisher> DeclarePublisherAsync(string keyExpr)
    {
        ArgumentNullException.ThrowIfNull(keyExpr);
        ThrowIfDisposed();

        return await Task.Run(() => new Publisher(_nativeSession, keyExpr)).ConfigureAwait(false);
    }

    /// <summary>
    /// Declares a subscriber for the specified key expression.
    /// </summary>
    /// <param name="keyExpr">The key expression to subscribe to.</param>
    /// <param name="callback">The callback to invoke when data is received.</param>
    /// <returns>A task representing the async operation, containing the Subscriber.</returns>
    /// <exception cref="ArgumentNullException">Thrown when keyExpr or callback is null.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the subscriber cannot be created.</exception>
    public async Task<Subscriber> DeclareSubscriberAsync(string keyExpr, Action<Sample> callback)
    {
        ArgumentNullException.ThrowIfNull(keyExpr);
        ArgumentNullException.ThrowIfNull(callback);
        ThrowIfDisposed();

        return await Task.Run(() => new Subscriber(_nativeSession, keyExpr, callback)).ConfigureAwait(false);
    }

    /// <summary>
    /// Declares a queryable for the specified key expression.
    /// </summary>
    /// <param name="keyExpr">The key expression to listen for queries on.</param>
    /// <param name="callback">The callback to invoke when a query is received.</param>
    /// <returns>A task representing the async operation, containing the Queryable.</returns>
    /// <exception cref="ArgumentNullException">Thrown when keyExpr or callback is null.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the queryable cannot be created.</exception>
    public async Task<Queryable> DeclareQueryableAsync(string keyExpr, Action<Query> callback)
    {
        ArgumentNullException.ThrowIfNull(keyExpr);
        ArgumentNullException.ThrowIfNull(callback);
        ThrowIfDisposed();

        return await Task.Run(() => new Queryable(_nativeSession, keyExpr, callback)).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs a get query (request-response pattern) asynchronously.
    /// </summary>
    /// <param name="selector">The selector (key expression) to query.</param>
    /// <param name="callback">The callback to invoke for each reply received.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when selector or callback is null.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the query fails.</exception>
    public async Task GetAsync(string selector, Action<Sample> callback)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(callback);
        ThrowIfDisposed();

        await Task.Run(() => _nativeSession.Get(selector, nativeSample =>
        {
            var sample = new Sample(nativeSample.KeyExpression, nativeSample.Payload);
            callback(sample);
        })).ConfigureAwait(false);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <summary>
    /// Asynchronously disposes the session and releases all resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await Task.Run(() => _nativeSession.Dispose()).ConfigureAwait(false);
            _disposed = true;
        }
    }
}
