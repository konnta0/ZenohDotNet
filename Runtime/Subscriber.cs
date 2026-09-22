using System;

namespace ZenohDotNet.Unity
{
    /// <summary>
    /// Unity-optimized Zenoh subscriber. Callbacks are executed on the Unity main thread.
    /// </summary>
    public sealed class Subscriber : IDisposable
    {
        private readonly ZenohDotNet.Native.Subscriber _nativeSubscriber;
        private bool _disposed;

        /// <summary>
        /// Gets the key expression this subscriber is listening on.
        /// </summary>
        public string KeyExpression => _nativeSubscriber.KeyExpression;

        internal Subscriber(ZenohDotNet.Native.Session session, string keyExpr, Action<ZenohDotNet.Native.Sample> callback)
        {
            _nativeSubscriber = session.DeclareSubscriber(keyExpr, callback);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Subscriber));
        }

        /// <summary>
        /// Disposes the subscriber and releases all resources.
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
