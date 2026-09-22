using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ZenohDotNet.Unity
{
    /// <summary>
    /// Unity-optimized Zenoh session with UniTask integration.
    /// </summary>
    public sealed class Session : IDisposable
    {
        private readonly ZenohDotNet.Native.Session _nativeSession;
        private bool _disposed;

        /// <summary>
        /// Opens a new Zenoh session asynchronously with default configuration.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A UniTask representing the async operation, containing the Session.</returns>
        public static async UniTask<Session> OpenAsync(CancellationToken cancellationToken = default)
        {
            return await OpenAsync((string)null, cancellationToken);
        }

        /// <summary>
        /// Opens a new Zenoh session asynchronously with the specified configuration.
        /// </summary>
        /// <param name="config">The session configuration.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A UniTask representing the async operation, containing the Session.</returns>
        public static async UniTask<Session> OpenAsync(ZenohDotNet.Native.SessionConfig config, CancellationToken cancellationToken = default)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            return await OpenAsync(config.ToJson(), cancellationToken);
        }

        /// <summary>
        /// Opens a new Zenoh session asynchronously with the specified JSON configuration.
        /// </summary>
        /// <param name="configJson">JSON configuration string, or null for default configuration.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A UniTask representing the async operation, containing the Session.</returns>
        public static async UniTask<Session> OpenAsync(string configJson, CancellationToken cancellationToken = default)
        {
            return await UniTask.RunOnThreadPool(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new Session(configJson);
            }, cancellationToken: cancellationToken);
        }

        private Session(string? configJson)
        {
            _nativeSession = new ZenohDotNet.Native.Session(configJson);
        }

        /// <summary>
        /// Declares a publisher for the specified key expression.
        /// </summary>
        /// <param name="keyExpr">The key expression to publish on.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A UniTask representing the async operation, containing the Publisher.</returns>
        /// <exception cref="ArgumentNullException">Thrown when keyExpr is null or empty.</exception>
        /// <exception cref="ZenohDotNet.Native.ZenohException">Thrown when the publisher cannot be created.</exception>
        public async UniTask<Publisher> DeclarePublisherAsync(string keyExpr, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(keyExpr))
                throw new ArgumentNullException(nameof(keyExpr));

            ThrowIfDisposed();

            return await UniTask.RunOnThreadPool(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new Publisher(_nativeSession, keyExpr);
            }, cancellationToken: cancellationToken);
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
        /// <exception cref="ZenohDotNet.Native.ZenohException">Thrown when the subscriber cannot be created.</exception>
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

            return await UniTask.RunOnThreadPool(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Wrap callback to execute on Unity main thread
                Action<ZenohDotNet.Native.Sample> wrappedCallback = nativeSample =>
                {
                    var sample = new Sample(nativeSample.KeyExpression, nativeSample.Payload);

                    // Dispatch callback to Unity main thread using UniTask
                    UniTask.Post(() => callback(sample));
                };

                return new Subscriber(_nativeSession, keyExpr, wrappedCallback);
            }, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Declares a queryable for the specified key expression.
        /// The callback will be invoked on the Unity main thread.
        /// </summary>
        /// <param name="keyExpr">The key expression to listen for queries on.</param>
        /// <param name="callback">The callback to invoke when a query is received (runs on main thread).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A UniTask representing the async operation, containing the Queryable.</returns>
        /// <exception cref="ArgumentNullException">Thrown when keyExpr or callback is null.</exception>
        /// <exception cref="ZenohDotNet.Native.ZenohException">Thrown when the queryable cannot be created.</exception>
        public async UniTask<Queryable> DeclareQueryableAsync(
            string keyExpr,
            Action<Query> callback,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(keyExpr))
                throw new ArgumentNullException(nameof(keyExpr));
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            ThrowIfDisposed();

            return await UniTask.RunOnThreadPool(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new Queryable(_nativeSession, keyExpr, callback);
            }, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Performs a get query (request-response pattern) asynchronously.
        /// Reply callbacks will be invoked on the Unity main thread.
        /// </summary>
        /// <param name="selector">The selector (key expression) to query.</param>
        /// <param name="callback">The callback to invoke for each reply received (runs on main thread).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A UniTask representing the async operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when selector or callback is null.</exception>
        /// <exception cref="ZenohDotNet.Native.ZenohException">Thrown when the query fails.</exception>
        public async UniTask GetAsync(
            string selector,
            Action<Sample> callback,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(selector))
                throw new ArgumentNullException(nameof(selector));
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            ThrowIfDisposed();

            await UniTask.RunOnThreadPool(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Wrap callback to execute on Unity main thread
                Action<ZenohDotNet.Native.Sample> wrappedCallback = nativeSample =>
                {
                    var sample = new Sample(nativeSample.KeyExpression, nativeSample.Payload);

                    // Dispatch callback to Unity main thread using UniTask
                    UniTask.Post(() => callback(sample));
                };

                _nativeSession.Get(selector, wrappedCallback);
            }, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Declares a liveliness token for the specified key expression.
        /// The token signals that a resource is alive. When disposed, the resource is considered dead.
        /// </summary>
        /// <param name="keyExpr">The key expression for the liveliness token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A UniTask representing the async operation, containing the LivelinessToken.</returns>
        /// <exception cref="ArgumentNullException">Thrown when keyExpr is null or empty.</exception>
        /// <exception cref="ZenohDotNet.Native.ZenohException">Thrown when the token cannot be created.</exception>
        public async UniTask<LivelinessToken> DeclareLivelinessTokenAsync(
            string keyExpr,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(keyExpr))
                throw new ArgumentNullException(nameof(keyExpr));

            ThrowIfDisposed();

            return await UniTask.RunOnThreadPool(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new LivelinessToken(_nativeSession, keyExpr);
            }, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Declares a liveliness subscriber for the specified key expression.
        /// The subscriber receives notifications when liveliness tokens are created or dropped.
        /// The callback will be invoked on the Unity main thread.
        /// </summary>
        /// <param name="keyExpr">The key expression to subscribe to (supports wildcards like "my/**").</param>
        /// <param name="callback">The callback invoked when a liveliness change occurs (runs on main thread).
        /// Parameters are (keyExpression, isAlive) where isAlive is true when a token is declared, false when dropped.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A UniTask representing the async operation, containing the LivelinessSubscriber.</returns>
        /// <exception cref="ArgumentNullException">Thrown when keyExpr or callback is null.</exception>
        /// <exception cref="ZenohDotNet.Native.ZenohException">Thrown when the subscriber cannot be created.</exception>
        public async UniTask<LivelinessSubscriber> DeclareLivelinessSubscriberAsync(
            string keyExpr,
            Action<string, bool> callback,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(keyExpr))
                throw new ArgumentNullException(nameof(keyExpr));
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            ThrowIfDisposed();

            return await UniTask.RunOnThreadPool(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Wrap callback to execute on Unity main thread
                Action<string, bool> wrappedCallback = (tokenKeyExpr, isAlive) =>
                {
                    UniTask.Post(() => callback(tokenKeyExpr, isAlive));
                };

                return new LivelinessSubscriber(_nativeSession, keyExpr, wrappedCallback);
            }, cancellationToken: cancellationToken);
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
