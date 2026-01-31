using System;
using System.Threading.Tasks;

namespace ZenohDotNet.Client;

/// <summary>
/// High-level async Zenoh liveliness token.
/// A liveliness token signals that a resource is alive.
/// When the token is disposed, the resource is considered dead.
/// </summary>
public sealed class LivelinessToken : IAsyncDisposable
{
    private readonly Native.LivelinessToken _nativeToken;
    private bool _disposed;

    /// <summary>
    /// Gets the key expression this token is bound to.
    /// </summary>
    public string KeyExpression => _nativeToken.KeyExpression;

    internal LivelinessToken(Native.Session session, string keyExpr)
    {
        _nativeToken = session.DeclareLivelinessToken(keyExpr);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <summary>
    /// Asynchronously disposes the liveliness token and releases all resources.
    /// This signals that the resource is no longer alive.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await Task.Run(() => _nativeToken.Dispose()).ConfigureAwait(false);
            _disposed = true;
        }
    }
}
