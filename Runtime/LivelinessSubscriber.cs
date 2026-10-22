using System;

namespace ZenohDotNet.Unity
{
    /// <summary>
    /// Unity-optimized Zenoh liveliness subscriber.
    /// Receives notifications when liveliness tokens are created or dropped.
    /// Callbacks are executed on the Unity main thread.
    /// </summary>
    public sealed class LivelinessSubscriber : IDisposable
    {
        private readonly ZenohDotNet.Native.LivelinessSubscriber _nativeSubscriber;
        private bool _disposed;

        /// <summary>
        /// Gets the key expression this subscriber is listening on.
        /// </summary>
        public string KeyExpression => _nativeSubscriber.KeyExpression;

        internal LivelinessSubscriber(ZenohDotNet.Native.Session session, string keyExpr, Action<string, bool> callback)
        {
            _nativeSubscriber = session.DeclareLivelinessSubscriber(keyExpr, callback);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LivelinessSubscriber));
        }

        /// <summary>
        /// Disposes the liveliness subscriber and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _nativeSubscriber?.Dispose();
                _disposed = true;
            }
        }
    }
}
