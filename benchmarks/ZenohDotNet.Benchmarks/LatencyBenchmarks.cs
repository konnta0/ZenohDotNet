using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ZenohDotNet.Client;

namespace ZenohDotNet.Benchmarks;

/// <summary>
/// Benchmarks for measuring end-to-end latency of pub/sub operations.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class LatencyBenchmarks
{
    private Session _pubSession = null!;
    private Session _subSession = null!;
    private Publisher _publisher = null!;
    private Subscriber _subscriber = null!;
    private byte[] _payload = null!;
    private TaskCompletionSource<long>? _receiveTcs;
    private long _sendTimestamp;

    [Params(64, 1024)]
    public int PayloadSize { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        _pubSession = await Session.OpenAsync();
        _subSession = await Session.OpenAsync();

        var keyExpr = $"benchmark/latency/{Guid.NewGuid()}";

        _subscriber = await _subSession.DeclareSubscriberAsync(keyExpr, _ =>
        {
            var receiveTime = DateTime.UtcNow.Ticks;
            _receiveTcs?.TrySetResult(receiveTime - _sendTimestamp);
        });

        _publisher = await _pubSession.DeclarePublisherAsync(keyExpr);

        _payload = new byte[PayloadSize];
        Random.Shared.NextBytes(_payload);

        await Task.Delay(200);
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _publisher.DisposeAsync();
        await _subscriber.DisposeAsync();
        await _pubSession.DisposeAsync();
        await _subSession.DisposeAsync();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _receiveTcs = new TaskCompletionSource<long>();
    }

    /// <summary>
    /// Measure single message round-trip latency (publish to callback).
    /// </summary>
    [Benchmark]
    public async Task<long> Latency_SingleMessage()
    {
        _sendTimestamp = DateTime.UtcNow.Ticks;
        await _publisher.PutAsync(_payload);
        
        var latencyTicks = await Task.WhenAny(_receiveTcs!.Task, Task.Delay(5000)) == _receiveTcs.Task
            ? await _receiveTcs.Task
            : -1;
        
        return latencyTicks;
    }

    /// <summary>
    /// Measure average latency over multiple messages.
    /// </summary>
    [Benchmark]
    public async Task<double> Latency_Average100Messages()
    {
        const int count = 100;
        long totalLatency = 0;

        for (int i = 0; i < count; i++)
        {
            _receiveTcs = new TaskCompletionSource<long>();
            _sendTimestamp = DateTime.UtcNow.Ticks;
            await _publisher.PutAsync(_payload);

            if (await Task.WhenAny(_receiveTcs.Task, Task.Delay(1000)) == _receiveTcs.Task)
            {
                totalLatency += await _receiveTcs.Task;
            }
        }

        return totalLatency / (double)count / TimeSpan.TicksPerMillisecond; // Return average in milliseconds
    }
}
