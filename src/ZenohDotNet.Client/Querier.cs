using System;
using System.Threading.Tasks;

namespace ZenohDotNet.Client;

/// <summary>
/// High-level async Zenoh querier for repeated queries on the same key expression.
/// </summary>
public sealed class Querier : IAsyncDisposable
{
    private readonly Native.Querier _nativeQuerier;
    private bool _disposed;

    /// <summary>
    /// Gets the key expression this querier is bound to.
    /// </summary>
    public string KeyExpression => _nativeQuerier.KeyExpression;

    internal Querier(Native.Session session, string keyExpr)
    {
        _nativeQuerier = session.DeclareQuerier(keyExpr);
    }

    /// <summary>
    /// Performs a get query and invokes the callback for each reply asynchronously.
    /// </summary>
    /// <param name="callback">Callback invoked for each reply sample.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when callback is null.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the query fails.</exception>
    public async Task GetAsync(Action<Sample> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ThrowIfDisposed();

        await Task.Run(() => _nativeQuerier.Get(nativeSample =>
        {
            var sample = Session.ConvertNativeSample(nativeSample);
            callback(sample);
        })).ConfigureAwait(false);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <summary>
    /// Asynchronously disposes the querier and releases all resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await Task.Run(() => _nativeQuerier.Dispose()).ConfigureAwait(false);
            _disposed = true;
        }
    }
}
