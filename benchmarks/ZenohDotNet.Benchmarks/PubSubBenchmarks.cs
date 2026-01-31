using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ZenohDotNet.Client;

namespace ZenohDotNet.Benchmarks;

/// <summary>
/// Benchmarks for Pub/Sub operations measuring throughput and latency.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[RankColumn]
public class PubSubBenchmarks
{
    private Session _pubSession = null!;
    private Session _subSession = null!;
    private Publisher _publisher = null!;
    private Subscriber _subscriber = null!;
    private byte[] _smallPayload = null!;
    private byte[] _mediumPayload = null!;
    private byte[] _largePayload = null!;
    private int _receivedCount;
    private TaskCompletionSource<bool>? _receiveComplete;

    [Params(100, 1000)]
    public int MessageCount { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        _pubSession = await Session.OpenAsync();
        _subSession = await Session.OpenAsync();

        var keyExpr = $"benchmark/pubsub/{Guid.NewGuid()}";

        _subscriber = await _subSession.DeclareSubscriberAsync(keyExpr, _ =>
        {
            Interlocked.Increment(ref _receivedCount);
            if (_receivedCount >= MessageCount)
            {
                _receiveComplete?.TrySetResult(true);
            }
        });

        _publisher = await _pubSession.DeclarePublisherAsync(keyExpr);

        // Prepare payloads
        _smallPayload = new byte[64];        // 64 bytes
        _mediumPayload = new byte[1024];     // 1 KB
        _largePayload = new byte[65536];     // 64 KB
        Random.Shared.NextBytes(_smallPayload);
        Random.Shared.NextBytes(_mediumPayload);
        Random.Shared.NextBytes(_largePayload);

        // Warmup
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
        _receivedCount = 0;
        _receiveComplete = new TaskCompletionSource<bool>();
    }

    [Benchmark(Baseline = true)]
    public async Task Publish_SmallPayload()
    {
        for (int i = 0; i < MessageCount; i++)
        {
            await _publisher.PutAsync(_smallPayload);
        }
        await WaitForMessages();
    }

    [Benchmark]
    public async Task Publish_MediumPayload()
    {
        for (int i = 0; i < MessageCount; i++)
        {
            await _publisher.PutAsync(_mediumPayload);
        }
        await WaitForMessages();
    }

    [Benchmark]
    public async Task Publish_LargePayload()
    {
        for (int i = 0; i < MessageCount; i++)
        {
            await _publisher.PutAsync(_largePayload);
        }
        await WaitForMessages();
    }

    private async Task WaitForMessages()
    {
        var timeout = Task.Delay(TimeSpan.FromSeconds(30));
        await Task.WhenAny(_receiveComplete!.Task, timeout);
    }
}
