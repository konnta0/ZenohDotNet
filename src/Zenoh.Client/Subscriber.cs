using System;
using System.Threading.Tasks;

namespace Zenoh.Client;

/// <summary>
/// High-level async Zenoh subscriber.
/// </summary>
public sealed class Subscriber : IAsyncDisposable
{
    private readonly Native.Subscriber _nativeSubscriber;
    private bool _disposed;

    /// <summary>
    /// Gets the key expression this subscriber is listening on.
    /// </summary>
    public string KeyExpression => _nativeSubscriber.KeyExpression;

    internal Subscriber(Native.Session session, string keyExpr, Action<Sample> callback)
    {
        // Wrap the callback to convert Native.Sample to Client.Sample
        _nativeSubscriber = session.DeclareSubscriber(keyExpr, nativeSample =>
        {
            var clientSample = new Sample(nativeSample.KeyExpression, nativeSample.Payload);
            callback(clientSample);
        });
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <summary>
    /// Asynchronously disposes the subscriber and releases all resources.
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
