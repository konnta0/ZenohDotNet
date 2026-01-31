using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ZenohDotNet.Client;

/// <summary>
/// High-level async Zenoh session for .NET 8.0+.
/// </summary>
public sealed class Session : IAsyncDisposable
{
    private readonly Native.Session _nativeSession;
    private bool _disposed;

    /// <summary>
    /// Opens a new Zenoh session asynchronously with default configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation, containing the Session.</returns>
    public static async Task<Session> OpenAsync(CancellationToken cancellationToken = default)
    {
        return await OpenAsync((string?)null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Opens a new Zenoh session asynchronously with the specified configuration.
    /// </summary>
    /// <param name="config">The session configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation, containing the Session.</returns>
    public static async Task<Session> OpenAsync(SessionConfig config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        return await OpenAsync(config.ToJson(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Opens a new Zenoh session asynchronously with the specified JSON configuration.
    /// </summary>
    /// <param name="configJson">JSON configuration string, or null for default configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation, containing the Session.</returns>
    public static async Task<Session> OpenAsync(string? configJson, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => new Session(configJson), cancellationToken).ConfigureAwait(false);
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
    /// <returns>A task representing the async operation, containing the Publisher.</returns>
    /// <exception cref="ArgumentNullException">Thrown when keyExpr is null or empty.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the publisher cannot be created.</exception>
    public async Task<Publisher> DeclarePublisherAsync(string keyExpr, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyExpr);
        ThrowIfDisposed();

        return await Task.Run(() => new Publisher(_nativeSession, keyExpr), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Declares a publisher for the specified key expression with options.
    /// </summary>
    /// <param name="keyExpr">The key expression to publish on.</param>
    /// <param name="options">The publisher options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation, containing the Publisher.</returns>
    /// <exception cref="ArgumentNullException">Thrown when keyExpr or options is null.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the publisher cannot be created.</exception>
    public async Task<Publisher> DeclarePublisherAsync(string keyExpr, PublisherOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyExpr);
        ArgumentNullException.ThrowIfNull(options);
        ThrowIfDisposed();

        return await Task.Run(() => new Publisher(_nativeSession, keyExpr, options), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Declares a subscriber for the specified key expression.
    /// </summary>
    /// <param name="keyExpr">The key expression to subscribe to.</param>
    /// <param name="callback">The callback to invoke when data is received.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation, containing the Subscriber.</returns>
    /// <exception cref="ArgumentNullException">Thrown when keyExpr or callback is null.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the subscriber cannot be created.</exception>
    public async Task<Subscriber> DeclareSubscriberAsync(string keyExpr, Action<Sample> callback, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyExpr);
        ArgumentNullException.ThrowIfNull(callback);
        ThrowIfDisposed();

        return await Task.Run(() => new Subscriber(_nativeSession, keyExpr, callback), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Declares a queryable for the specified key expression.
    /// </summary>
    /// <param name="keyExpr">The key expression to listen for queries on.</param>
    /// <param name="callback">The callback to invoke when a query is received.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation, containing the Queryable.</returns>
    /// <exception cref="ArgumentNullException">Thrown when keyExpr or callback is null.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the queryable cannot be created.</exception>
    public async Task<Queryable> DeclareQueryableAsync(string keyExpr, Action<Query> callback, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyExpr);
        ArgumentNullException.ThrowIfNull(callback);
        ThrowIfDisposed();

        return await Task.Run(() => new Queryable(_nativeSession, keyExpr, callback), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs a get query (request-response pattern) asynchronously.
    /// </summary>
    /// <param name="selector">The selector (key expression) to query.</param>
    /// <param name="callback">The callback to invoke for each reply received.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when selector or callback is null.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the query fails.</exception>
    public async Task GetAsync(string selector, Action<Sample> callback, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(callback);
        ThrowIfDisposed();

        await Task.Run(() => _nativeSession.Get(selector, nativeSample =>
        {
            var sample = ConvertNativeSample(nativeSample);
            callback(sample);
        }), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs a get query with a timeout.
    /// Note: The timeout is implemented on the C# side. If the timeout expires, the task throws TimeoutException,
    /// but the underlying native query may continue running until completion.
    /// </summary>
    /// <param name="selector">The selector (key expression) to query.</param>
    /// <param name="callback">The callback to invoke for each reply received.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when selector or callback is null.</exception>
    /// <exception cref="TimeoutException">Thrown when the query exceeds the timeout.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the query fails.</exception>
    public async Task GetAsync(string selector, Action<Sample> callback, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(callback);
        ThrowIfDisposed();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            await Task.Run(() => _nativeSession.Get(selector, nativeSample =>
            {
                var sample = ConvertNativeSample(nativeSample);
                callback(sample);
            }), cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"The query for '{selector}' timed out after {timeout.TotalMilliseconds}ms.");
        }
    }

    /// <summary>
    /// Puts data directly on the session (without declaring a publisher).
    /// </summary>
    /// <param name="keyExpr">The key expression to put on.</param>
    /// <param name="data">The data to put.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when keyExpr or data is null.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the put operation fails.</exception>
    public async Task PutAsync(string keyExpr, byte[] data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyExpr);
        ArgumentNullException.ThrowIfNull(data);
        ThrowIfDisposed();

        await Task.Run(() => _nativeSession.Put(keyExpr, data), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Puts a string directly on the session (without declaring a publisher).
    /// </summary>
    /// <param name="keyExpr">The key expression to put on.</param>
    /// <param name="value">The string to put.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when keyExpr or value is null.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the put operation fails.</exception>
    public async Task PutAsync(string keyExpr, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyExpr);
        ArgumentNullException.ThrowIfNull(value);
        ThrowIfDisposed();

        await Task.Run(() => _nativeSession.Put(keyExpr, value), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Puts data with encoding directly on the session.
    /// </summary>
    /// <param name="keyExpr">The key expression to put on.</param>
    /// <param name="data">The data to put.</param>
    /// <param name="encoding">The encoding to use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when keyExpr or data is null.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the put operation fails.</exception>
    public async Task PutAsync(string keyExpr, byte[] data, Encoding encoding, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyExpr);
        ArgumentNullException.ThrowIfNull(data);
        ThrowIfDisposed();

        await Task.Run(() => _nativeSession.Put(keyExpr, data, (Native.PayloadEncoding)encoding), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Puts data with attachment directly on the session.
    /// Attachments are key-value pairs that are sent alongside the payload.
    /// </summary>
    /// <param name="keyExpr">The key expression to put on.</param>
    /// <param name="data">The data to put.</param>
    /// <param name="attachment">The attachment key-value pairs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when keyExpr, data, or attachment is null.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the put operation fails.</exception>
    public async Task PutWithAttachmentAsync(string keyExpr, byte[] data, IDictionary<string, string> attachment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyExpr);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(attachment);
        ThrowIfDisposed();

        await Task.Run(() => _nativeSession.Put(keyExpr, data, attachment), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Puts a string with attachment directly on the session.
    /// Attachments are key-value pairs that are sent alongside the payload.
    /// </summary>
    /// <param name="keyExpr">The key expression to put on.</param>
    /// <param name="value">The string to put.</param>
    /// <param name="attachment">The attachment key-value pairs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when keyExpr, value, or attachment is null.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the put operation fails.</exception>
    public async Task PutWithAttachmentAsync(string keyExpr, string value, IDictionary<string, string> attachment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyExpr);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(attachment);
        ThrowIfDisposed();

        var data = System.Text.Encoding.UTF8.GetBytes(value);
        await Task.Run(() => _nativeSession.Put(keyExpr, data, attachment), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes data for a key expression.
    /// </summary>
    /// <param name="keyExpr">The key expression to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when keyExpr is null.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the delete operation fails.</exception>
    public async Task DeleteAsync(string keyExpr, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyExpr);
        ThrowIfDisposed();

        await Task.Run(() => _nativeSession.Delete(keyExpr), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the Zenoh ID of this session.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation, containing the Zenoh ID string.</returns>
    /// <exception cref="Native.ZenohException">Thrown when the Zenoh ID cannot be retrieved.</exception>
    public async Task<string> GetZenohIdAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        return await Task.Run(() => _nativeSession.GetZenohId(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Declares a querier for repeated queries on the same key expression.
    /// </summary>
    /// <param name="keyExpr">The key expression to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation, containing the Querier.</returns>
    /// <exception cref="ArgumentNullException">Thrown when keyExpr is null.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the querier cannot be created.</exception>
    public async Task<Querier> DeclareQuerierAsync(string keyExpr, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyExpr);
        ThrowIfDisposed();

        return await Task.Run(() => new Querier(_nativeSession, keyExpr), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Declares a liveliness token for the specified key expression.
    /// The token signals that a resource is alive. When disposed, the resource is considered dead.
    /// </summary>
    /// <param name="keyExpr">The key expression for the liveliness token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation, containing the LivelinessToken.</returns>
    /// <exception cref="ArgumentNullException">Thrown when keyExpr is null.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the token cannot be created.</exception>
    public async Task<LivelinessToken> DeclareLivelinessTokenAsync(string keyExpr, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyExpr);
        ThrowIfDisposed();

        return await Task.Run(() => new LivelinessToken(_nativeSession, keyExpr), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Declares a liveliness subscriber for the specified key expression.
    /// The subscriber receives notifications when liveliness tokens are created or dropped.
    /// </summary>
    /// <param name="keyExpr">The key expression to subscribe to (supports wildcards like "my/**").</param>
    /// <param name="callback">The callback invoked when a liveliness change occurs. 
    /// Parameters are (keyExpression, isAlive) where isAlive is true when a token is declared, false when dropped.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation, containing the LivelinessSubscriber.</returns>
    /// <exception cref="ArgumentNullException">Thrown when keyExpr or callback is null.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the subscriber cannot be created.</exception>
    public async Task<LivelinessSubscriber> DeclareLivelinessSubscriberAsync(string keyExpr, Action<string, bool> callback, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyExpr);
        ArgumentNullException.ThrowIfNull(callback);
        ThrowIfDisposed();

        return await Task.Run(() => new LivelinessSubscriber(_nativeSession, keyExpr, callback), cancellationToken).ConfigureAwait(false);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    internal static Sample ConvertNativeSample(Native.Sample nativeSample)
    {
        Timestamp? timestamp = null;
        if (nativeSample.Timestamp.HasValue)
        {
            var nativeTs = nativeSample.Timestamp.Value;
            timestamp = new Timestamp(nativeTs.TimeNtp64, nativeTs.Id);
        }

        return new Sample(
            nativeSample.KeyExpression,
            nativeSample.Payload,
            (SampleKind)nativeSample.Kind,
            (Encoding)nativeSample.Encoding,
            timestamp);
    }

    /// <summary>
    /// Asynchronously disposes the session and releases all resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await Task.Run(() => _nativeSession.Dispose()).ConfigureAwait(false);
            _disposed = true;
        }
    }
}
