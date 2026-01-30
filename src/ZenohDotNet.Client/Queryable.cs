using System;

namespace ZenohDotNet.Client;

/// <summary>
/// High-level queryable that responds to get queries.
/// </summary>
public sealed class Queryable : IAsyncDisposable
{
    private readonly Native.Queryable _nativeQueryable;
    private bool _disposed;

    internal Queryable(Native.Session nativeSession, string keyExpr, Action<Query> callback)
    {
        _nativeQueryable = nativeSession.DeclareQueryable(keyExpr, nativeQuery =>
        {
            var query = new Query(nativeQuery);
            callback(query);
        });
    }

    /// <summary>
    /// Gets the key expression this queryable is listening on.
    /// </summary>
    public string KeyExpression => _nativeQueryable.KeyExpression;

    /// <summary>
    /// Asynchronously disposes the queryable and releases all resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await Task.Run(() => _nativeQueryable.Dispose()).ConfigureAwait(false);
            _disposed = true;
        }
    }
}

/// <summary>
/// Represents a query received by a queryable.
/// </summary>
public sealed class Query
{
    private readonly Native.Query _nativeQuery;

    internal Query(Native.Query nativeQuery)
    {
        _nativeQuery = nativeQuery;
    }

    /// <summary>
    /// Gets the selector (key expression) of the query.
    /// </summary>
    public string Selector => _nativeQuery.Selector;

    /// <summary>
    /// Replies to the query with data.
    /// </summary>
    /// <param name="keyExpr">The key expression for the reply.</param>
    /// <param name="data">The payload data.</param>
    public void Reply(string keyExpr, byte[] data)
    {
        _nativeQuery.Reply(keyExpr, data);
    }

    /// <summary>
    /// Replies to the query with a string.
    /// </summary>
    /// <param name="keyExpr">The key expression for the reply.</param>
    /// <param name="value">The payload string.</param>
    public void Reply(string keyExpr, string value)
    {
        _nativeQuery.Reply(keyExpr, value);
    }
}
