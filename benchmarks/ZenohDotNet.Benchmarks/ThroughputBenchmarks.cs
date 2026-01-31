using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ZenohDotNet.Client;

namespace ZenohDotNet.Benchmarks;

/// <summary>
/// Benchmarks for throughput measurement - messages per second.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 3, iterationCount: 5)]
[MemoryDiagnoser]
public class ThroughputBenchmarks
{
    private Session _pubSession = null!;
    private Session _subSession = null!;
    private Publisher _publisher = null!;
    private Subscriber _subscriber = null!;
    private byte[] _payload = null!;
    private long _receivedCount;

    [Params(64, 512, 4096)]
    public int PayloadSize { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        _pubSession = await Session.OpenAsync();
        _subSession = await Session.OpenAsync();

        var keyExpr = $"benchmark/throughput/{Guid.NewGuid()}";

        _subscriber = await _subSession.DeclareSubscriberAsync(keyExpr, _ =>
        {
            Interlocked.Increment(ref _receivedCount);
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
        _receivedCount = 0;
    }

    /// <summary>
    /// Measure maximum achievable throughput over 1 second.
    /// </summary>
    [Benchmark]
    public async Task<long> Throughput_1Second()
    {
        var endTime = DateTime.UtcNow.AddSeconds(1);
        long sent = 0;

        while (DateTime.UtcNow < endTime)
        {
            await _publisher.PutAsync(_payload);
            sent++;
        }

        // Wait for messages to arrive
        await Task.Delay(500);
        return _receivedCount;
    }

    /// <summary>
    /// Measure sustained throughput - 10000 messages.
    /// </summary>
    [Benchmark]
    public async Task<long> Throughput_10KMessages()
    {
        const int messageCount = 10000;

        for (int i = 0; i < messageCount; i++)
        {
            await _publisher.PutAsync(_payload);
        }

        // Wait for messages
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (_receivedCount < messageCount && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }

        return _receivedCount;
    }
}
