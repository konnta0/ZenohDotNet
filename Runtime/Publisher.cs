using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ZenohDotNet.Unity
{
    /// <summary>
    /// Unity-optimized Zenoh publisher with UniTask integration.
    /// </summary>
    public sealed class Publisher : IDisposable
    {
        private readonly ZenohDotNet.Native.Publisher _nativePublisher;
        private bool _disposed;

        /// <summary>
        /// Gets the key expression this publisher is bound to.
        /// </summary>
        public string KeyExpression => _nativePublisher.KeyExpression;

        internal Publisher(ZenohDotNet.Native.Session session, string keyExpr)
        {
            _nativePublisher = session.DeclarePublisher(keyExpr);
        }

        /// <summary>
        /// Publishes binary data asynchronously.
        /// </summary>
        /// <param name="data">The data to publish.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A UniTask representing the async operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
        /// <exception cref="ZenohDotNet.Native.ZenohException">Thrown when the put operation fails.</exception>
        public async UniTask PutAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            ThrowIfDisposed();

            await UniTask.RunOnThreadPool(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                _nativePublisher.Put(data);
            }, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Publishes a string as UTF-8 encoded bytes asynchronously.
        /// </summary>
        /// <param name="value">The string to publish.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A UniTask representing the async operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
        /// <exception cref="ZenohDotNet.Native.ZenohException">Thrown when the put operation fails.</exception>
        public async UniTask PutAsync(string value, CancellationToken cancellationToken = default)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            ThrowIfDisposed();

            await UniTask.RunOnThreadPool(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                _nativePublisher.Put(value);
            }, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Synchronously publishes data (for use in Unity update loops).
        /// </summary>
        /// <param name="data">The data to publish.</param>
        public void Put(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            ThrowIfDisposed();
            _nativePublisher.Put(data);
        }

        /// <summary>
        /// Synchronously publishes a string (for use in Unity update loops).
        /// </summary>
        /// <param name="value">The string to publish.</param>
        public void Put(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            ThrowIfDisposed();
            _nativePublisher.Put(value);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Publisher));
        }

        /// <summary>
        /// Disposes the publisher and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _nativePublisher?.Dispose();
                _disposed = true;
            }
        }
    }
}
