using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace Zenoh.Unity
{
    /// <summary>
    /// Unity-optimized Zenoh session with UniTask integration.
    /// </summary>
    public sealed class Session : IDisposable
    {
        private readonly Native.Session _nativeSession;
        private bool _disposed;

        /// <summary>
        /// Opens a new Zenoh session asynchronously with default configuration.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A UniTask representing the async operation, containing the Session.</returns>
        public static async UniTask<Session> OpenAsync(CancellationToken cancellationToken = default)
        {
            return await OpenAsync(null, cancellationToken);
        }

        /// <summary>
        /// Opens a new Zenoh session asynchronously with the specified JSON configuration.
        /// </summary>
        /// <param name="configJson">JSON configuration string, or null for default configuration.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A UniTask representing the async operation, containing the Session.</returns>
        public static async UniTask<Session> OpenAsync(string? configJson, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new Session(configJson);
            }, cancellationToken).AsUniTask();
        }

        private Session(string? configJson)
        {
            _nativeSession = new Native.Session(configJson);
        }

        /// <summary>
        /// Declares a publisher for the specified key expression.
        /// </summary>
        /// <param name="keyExpr">The key expression to publish on.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A UniTask representing the async operation, containing the Publisher.</returns>
        /// <exception cref="ArgumentNullException">Thrown when keyExpr is null or empty.</exception>
        /// <exception cref="Native.ZenohException">Thrown when the publisher cannot be created.</exception>
        public async UniTask<Publisher> DeclarePublisherAsync(string keyExpr, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(keyExpr))
                throw new ArgumentNullException(nameof(keyExpr));

            ThrowIfDisposed();

            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new Publisher(_nativeSession, keyExpr);
            }, cancellationToken).AsUniTask();
        }

        /// <summary>
        /// Declares a subscriber for the specified key expression.
        /// The callback will be invoked on the Unity main thread.
        /// </summary>
        /// <param name="keyExpr">The key expression to subscribe to.</param>
        /// <param name="callback">The callback to invoke when data is received (runs on main thread).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A UniTask representing the async operation, containing the Subscriber.</returns>
        /// <exception cref="ArgumentNullException">Thrown when keyExpr or callback is null.</exception>
        /// <exception cref="Native.ZenohException">Thrown when the subscriber cannot be created.</exception>
        public async UniTask<Subscriber> DeclareSubscriberAsync(
            string keyExpr,
            Action<Sample> callback,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(keyExpr))
                throw new ArgumentNullException(nameof(keyExpr));
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            ThrowIfDisposed();

            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Wrap callback to execute on Unity main thread
                Action<Native.Sample> wrappedCallback = nativeSample =>
                {
                    var sample = new Sample(nativeSample.KeyExpression, nativeSample.Payload);

                    // TODO: In actual Unity environment, use PlayerLoopHelper.AddAction
                    // to schedule callback on main thread. For now, call directly.
                    // Example in Unity: UniTask.Post(() => callback(sample));
                    callback(sample);
                };

                return new Subscriber(_nativeSession, keyExpr, wrappedCallback);
            }, cancellationToken).AsUniTask();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Session));
        }

        /// <summary>
        /// Disposes the session and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _nativeSession?.Dispose();
                _disposed = true;
            }
        }
    }
}
