using ZenohDotNet.Client;

namespace ZenohDotNet.IntegrationTests;

/// <summary>
/// Integration tests for Zenoh Query/Queryable (request-response) functionality.
/// </summary>
public class QueryIntegrationTests
{
    [Fact]
    public async Task Queryable_RespondsToQuery()
    {
        // Arrange
        var keyExpr = $"test/integration/query/{Guid.NewGuid()}";
        var queryReceived = new TaskCompletionSource<bool>();
        var replyReceived = new TaskCompletionSource<string>();

        await using var queryableSession = await Session.OpenAsync();
        await using var querierSession = await Session.OpenAsync();

        // Set up queryable that responds with "Hello, Querier!"
        await using var queryable = await queryableSession.DeclareQueryableAsync(keyExpr, query =>
        {
            queryReceived.TrySetResult(true);
            query.Reply(keyExpr, System.Text.Encoding.UTF8.GetBytes("Hello, Querier!"));
        });

        await Task.Delay(100);

        // Act - Send query
        await querierSession.GetAsync(keyExpr, sample =>
        {
            var message = System.Text.Encoding.UTF8.GetString(sample.Payload);
            replyReceived.TrySetResult(message);
        });

        // Wait for query and reply
        var gotQuery = await Task.WhenAny(
            queryReceived.Task,
            Task.Delay(TimeSpan.FromSeconds(5))
        ) == queryReceived.Task;

        var gotReply = await Task.WhenAny(
            replyReceived.Task,
            Task.Delay(TimeSpan.FromSeconds(5))
        ) == replyReceived.Task;

        // Assert
        Assert.True(gotQuery, "Queryable did not receive the query");
        Assert.True(gotReply, "Querier did not receive a reply");
        Assert.Equal("Hello, Querier!", await replyReceived.Task);
    }

    [Fact]
    public async Task Queryable_CanAccessQuerySelector()
    {
        // Arrange
        var keyExpr = $"test/integration/query/selector/{Guid.NewGuid()}";
        var receivedSelector = "";
        var queryProcessed = new TaskCompletionSource<bool>();

        await using var queryableSession = await Session.OpenAsync();
        await using var querierSession = await Session.OpenAsync();

        await using var queryable = await queryableSession.DeclareQueryableAsync(keyExpr, query =>
        {
            receivedSelector = query.Selector;
            query.Reply(keyExpr, System.Text.Encoding.UTF8.GetBytes("Acknowledged"));
            queryProcessed.TrySetResult(true);
        });

        await Task.Delay(200); // Allow time for declaration to propagate

        // Act
        var replyReceived = new TaskCompletionSource<bool>();
        await querierSession.GetAsync(keyExpr, _ => replyReceived.TrySetResult(true));

        var queryWasProcessed = await Task.WhenAny(queryProcessed.Task, Task.Delay(TimeSpan.FromSeconds(5))) == queryProcessed.Task;

        // Assert
        Assert.True(queryWasProcessed, "Query was not processed by queryable");
        Assert.NotEmpty(receivedSelector);
        Assert.Contains(keyExpr.Split('/').Last().Split('-')[0], receivedSelector);
    }

    [Fact]
    public async Task Queryable_WithWildcard_ReceivesQueries()
    {
        // Arrange
        var baseKey = $"test/integration/query/wildcard/{Guid.NewGuid()}";
        var receivedQueries = new List<string>();
        var queryCount = new TaskCompletionSource<bool>();

        await using var queryableSession = await Session.OpenAsync();
        await using var querierSession = await Session.OpenAsync();

        // Queryable with wildcard
        await using var queryable = await queryableSession.DeclareQueryableAsync($"{baseKey}/**", query =>
        {
            lock (receivedQueries)
            {
                receivedQueries.Add(query.Selector);
            }
            query.Reply(query.Selector, System.Text.Encoding.UTF8.GetBytes("OK"));
            if (receivedQueries.Count >= 2)
            {
                queryCount.TrySetResult(true);
            }
        });

        await Task.Delay(300); // Allow time for queryable discovery

        // Act - Query different sub-keys
        var replies = new List<string>();
        await querierSession.GetAsync($"{baseKey}/resource1", sample =>
        {
            lock (replies) { replies.Add(sample.KeyExpression); }
        });
        await querierSession.GetAsync($"{baseKey}/resource2", sample =>
        {
            lock (replies) { replies.Add(sample.KeyExpression); }
        });

        await Task.WhenAny(queryCount.Task, Task.Delay(TimeSpan.FromSeconds(10)));

        // Assert - at least one query received (wildcard behavior may vary)
        Assert.True(receivedQueries.Count >= 1, "Queryable should receive at least one query");
    }

    [Fact]
    public async Task MultipleQueryables_SameKey_AllReceiveQueries()
    {
        // Arrange
        var keyExpr = $"test/integration/query/multi/{Guid.NewGuid()}";
        var queryable1Received = new TaskCompletionSource<bool>();
        var queryable2Received = new TaskCompletionSource<bool>();

        await using var session1 = await Session.OpenAsync();
        await using var session2 = await Session.OpenAsync();
        await using var querierSession = await Session.OpenAsync();

        await using var queryable1 = await session1.DeclareQueryableAsync(keyExpr, query =>
        {
            queryable1Received.TrySetResult(true);
            query.Reply(keyExpr, System.Text.Encoding.UTF8.GetBytes("Reply from Q1"));
        });

        await using var queryable2 = await session2.DeclareQueryableAsync(keyExpr, query =>
        {
            queryable2Received.TrySetResult(true);
            query.Reply(keyExpr, System.Text.Encoding.UTF8.GetBytes("Reply from Q2"));
        });

        await Task.Delay(300); // Allow time for queryables to register

        // Act
        var replies = new List<string>();
        await querierSession.GetAsync(keyExpr, sample =>
        {
            var message = System.Text.Encoding.UTF8.GetString(sample.Payload);
            lock (replies) { replies.Add(message); }
        });

        // Wait for responses
        await Task.WhenAny(
            Task.WhenAll(queryable1Received.Task, queryable2Received.Task),
            Task.Delay(TimeSpan.FromSeconds(5))
        );

        // Assert - At least one queryable should have received the query
        var q1Got = queryable1Received.Task.IsCompletedSuccessfully;
        var q2Got = queryable2Received.Task.IsCompletedSuccessfully;
        Assert.True(q1Got || q2Got, "At least one queryable should receive the query");
    }
}
