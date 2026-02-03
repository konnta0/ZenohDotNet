using System;

namespace ZenohDotNet.Unity
{
    /// <summary>
    /// Unity-optimized Zenoh liveliness token.
    /// A liveliness token signals that a resource is alive.
    /// When the token is disposed, the resource is considered dead.
    /// </summary>
    public sealed class LivelinessToken : IDisposable
    {
        private readonly ZenohDotNet.Native.LivelinessToken _nativeToken;
        private bool _disposed;

        /// <summary>
        /// Gets the key expression this token is bound to.
        /// </summary>
        public string KeyExpression => _nativeToken.KeyExpression;

        internal LivelinessToken(ZenohDotNet.Native.Session session, string keyExpr)
        {
            _nativeToken = session.DeclareLivelinessToken(keyExpr);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LivelinessToken));
        }

        /// <summary>
        /// Disposes the liveliness token and releases all resources.
        /// This signals that the resource is no longer alive.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _nativeToken?.Dispose();
                _disposed = true;
            }
        }
    }
}
