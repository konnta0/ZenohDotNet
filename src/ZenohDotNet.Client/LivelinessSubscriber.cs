using System;
using System.Threading.Tasks;

namespace ZenohDotNet.Client;

/// <summary>
/// High-level async Zenoh liveliness subscriber.
/// Receives notifications when liveliness tokens are created or dropped.
/// </summary>
public sealed class LivelinessSubscriber : IAsyncDisposable
{
    private readonly Native.LivelinessSubscriber _nativeSubscriber;
    private bool _disposed;

    /// <summary>
    /// Gets the key expression this subscriber is listening on.
    /// </summary>
    public string KeyExpression => _nativeSubscriber.KeyExpression;

    internal LivelinessSubscriber(Native.Session session, string keyExpr, Action<string, bool> callback)
    {
        _nativeSubscriber = session.DeclareLivelinessSubscriber(keyExpr, callback);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <summary>
    /// Asynchronously disposes the liveliness subscriber and releases all resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await Task.Run(() => _nativeSubscriber.Dispose()).ConfigureAwait(false);
            _disposed = true;
        }
    }
}
