using System;
using System.Threading.Tasks;

namespace ZenohDotNet.Client;

/// <summary>
/// High-level async Zenoh publisher.
/// </summary>
public sealed class Publisher : IAsyncDisposable
{
    private readonly Native.Publisher _nativePublisher;
    private bool _disposed;

    /// <summary>
    /// Gets the key expression this publisher is bound to.
    /// </summary>
    public string KeyExpression => _nativePublisher.KeyExpression;

    internal Publisher(Native.Session session, string keyExpr)
    {
        _nativePublisher = session.DeclarePublisher(keyExpr);
    }

    internal Publisher(Native.Session session, string keyExpr, PublisherOptions options)
    {
        var nativeOptions = new Native.PublisherOptions
        {
            CongestionControl = (Native.CongestionControl)options.CongestionControl,
            Priority = (Native.Priority)options.Priority,
            IsExpress = options.IsExpress
        };
        _nativePublisher = session.DeclarePublisher(keyExpr, nativeOptions);
    }

    /// <summary>
    /// Publishes binary data synchronously.
    /// Use when low latency is required and you're already on a dedicated thread.
    /// </summary>
    /// <param name="data">The data to publish.</param>
    /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the put operation fails.</exception>
    public void Put(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        ThrowIfDisposed();
        _nativePublisher.Put(data);
    }

    /// <summary>
    /// Publishes binary data from a span synchronously (zero-copy).
    /// Use when low latency is required and you're already on a dedicated thread.
    /// </summary>
    /// <param name="data">The data to publish.</param>
    /// <exception cref="Native.ZenohException">Thrown when the put operation fails.</exception>
    public void Put(ReadOnlySpan<byte> data)
    {
        ThrowIfDisposed();
        _nativePublisher.Put(data);
    }

    /// <summary>
    /// Publishes binary data asynchronously.
    /// </summary>
    /// <param name="data">The data to publish.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the put operation fails.</exception>
    public async Task PutAsync(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        ThrowIfDisposed();

        await Task.Run(() => _nativePublisher.Put(data)).ConfigureAwait(false);
    }

    /// <summary>
    /// Publishes binary data from a ReadOnlyMemory asynchronously (zero-copy friendly).
    /// </summary>
    /// <param name="data">The data to publish.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="Native.ZenohException">Thrown when the put operation fails.</exception>
    public async Task PutAsync(ReadOnlyMemory<byte> data)
    {
        ThrowIfDisposed();

        await Task.Run(() => _nativePublisher.Put(data.Span)).ConfigureAwait(false);
    }

    /// <summary>
    /// Publishes a string as UTF-8 encoded bytes asynchronously.
    /// </summary>
    /// <param name="value">The string to publish.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the put operation fails.</exception>
    public async Task PutAsync(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        ThrowIfDisposed();

        await Task.Run(() => _nativePublisher.Put(value)).ConfigureAwait(false);
    }

    /// <summary>
    /// Publishes data with a strongly-typed value using generic serialization.
    /// </summary>
    /// <typeparam name="T">The type of the value to publish.</typeparam>
    /// <param name="value">The value to publish.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the put operation fails.</exception>
    public async Task PutAsync<T>(T value) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(value);
        ThrowIfDisposed();

        // For simple types, use ToString
        // In a full implementation, this could use System.Text.Json or similar
        string serialized = value.ToString() ?? string.Empty;
        await PutAsync(serialized).ConfigureAwait(false);
    }

    /// <summary>
    /// Publishes binary data with encoding asynchronously.
    /// </summary>
    /// <param name="data">The data to publish.</param>
    /// <param name="encoding">The encoding to use.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the put operation fails.</exception>
    public async Task PutAsync(byte[] data, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(data);
        ThrowIfDisposed();

        await Task.Run(() => _nativePublisher.Put(data, (Native.PayloadEncoding)encoding)).ConfigureAwait(false);
    }

    /// <summary>
    /// Publishes a string with encoding asynchronously.
    /// </summary>
    /// <param name="value">The string to publish.</param>
    /// <param name="encoding">The encoding to use.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="Native.ZenohException">Thrown when the put operation fails.</exception>
    public async Task PutAsync(string value, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(value);
        ThrowIfDisposed();

        await Task.Run(() => _nativePublisher.Put(value, (Native.PayloadEncoding)encoding)).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a delete operation for the key expression asynchronously.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="Native.ZenohException">Thrown when the delete operation fails.</exception>
    public async Task DeleteAsync()
    {
        ThrowIfDisposed();

        await Task.Run(() => _nativePublisher.Delete()).ConfigureAwait(false);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <summary>
    /// Asynchronously disposes the publisher and releases all resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await Task.Run(() => _nativePublisher.Dispose()).ConfigureAwait(false);
            _disposed = true;
        }
    }
}
