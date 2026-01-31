using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ZenohDotNet.Client;

namespace ZenohDotNet.Benchmarks;

/// <summary>
/// Benchmarks for Query/Reply operations measuring request-response latency.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[RankColumn]
public class QueryBenchmarks
{
    private Session _queryableSession = null!;
    private Session _querierSession = null!;
    private Client.Queryable _queryable = null!;
    private string _keyExpr = null!;
    private byte[] _replyPayload = null!;

    [Params(10, 100)]
    public int QueryCount { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        _queryableSession = await Session.OpenAsync();
        _querierSession = await Session.OpenAsync();

        _keyExpr = $"benchmark/query/{Guid.NewGuid()}";
        _replyPayload = new byte[256];
        Random.Shared.NextBytes(_replyPayload);

        _queryable = await _queryableSession.DeclareQueryableAsync(_keyExpr, query =>
        {
            query.Reply(_keyExpr, _replyPayload);
        });

        // Warmup
        await Task.Delay(200);
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _queryable.DisposeAsync();
        await _queryableSession.DisposeAsync();
        await _querierSession.DisposeAsync();
    }

    [Benchmark]
    public async Task Query_SingleRequest()
    {
        var received = new TaskCompletionSource<bool>();
        await _querierSession.GetAsync(_keyExpr, _ => received.TrySetResult(true));
        await Task.WhenAny(received.Task, Task.Delay(5000));
    }

    [Benchmark]
    public async Task Query_MultipleRequests()
    {
        for (int i = 0; i < QueryCount; i++)
        {
            var received = new TaskCompletionSource<bool>();
            await _querierSession.GetAsync(_keyExpr, _ => received.TrySetResult(true));
            await Task.WhenAny(received.Task, Task.Delay(5000));
        }
    }

    [Benchmark]
    public async Task Query_ConcurrentRequests()
    {
        var tasks = new Task[QueryCount];
        for (int i = 0; i < QueryCount; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                var received = new TaskCompletionSource<bool>();
                await _querierSession.GetAsync(_keyExpr, _ => received.TrySetResult(true));
                await Task.WhenAny(received.Task, Task.Delay(5000));
            });
        }
        await Task.WhenAll(tasks);
    }
}
