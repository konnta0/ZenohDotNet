using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ZenohDotNet.Client;

namespace ZenohDotNet.Benchmarks;

/// <summary>
/// Benchmarks for measuring memory allocation patterns.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class AllocationBenchmarks
{
    private Session _session = null!;
    private Publisher _publisher = null!;
    private byte[] _payload = null!;
    private ReadOnlyMemory<byte> _payloadMemory;

    [GlobalSetup]
    public async Task Setup()
    {
        _session = await Session.OpenAsync();
        _publisher = await _session.DeclarePublisherAsync($"benchmark/alloc/{Guid.NewGuid()}");
        _payload = new byte[1024];
        Random.Shared.NextBytes(_payload);
        _payloadMemory = _payload;
        await Task.Delay(200);
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _publisher.DisposeAsync();
        await _session.DisposeAsync();
    }

    [Benchmark(Baseline = true)]
    public async Task Put_ByteArray()
    {
        await _publisher.PutAsync(_payload);
    }

    [Benchmark]
    public async Task Put_ReadOnlyMemory()
    {
        await _publisher.PutAsync(_payloadMemory);
    }

    [Benchmark]
    public async Task Put_StringSmall()
    {
        await _publisher.PutAsync("Hello");
    }

    [Benchmark]
    public async Task Put_StringMedium()
    {
        await _publisher.PutAsync("Hello, this is a medium length message for benchmarking purposes.");
    }
}

/// <summary>
/// Benchmarks for SessionConfig JSON serialization (using Native layer directly).
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class SessionConfigBenchmarks
{
    private Native.SessionConfig _simpleConfig = null!;
    private Native.SessionConfig _complexConfig = null!;

    [GlobalSetup]
    public void Setup()
    {
        _simpleConfig = Native.SessionConfig.Peer();

        _complexConfig = new Native.SessionConfig()
            .WithMode(Native.SessionMode.Client)
            .WithConnect("tcp/localhost:7447")
            .WithConnect("tcp/localhost:7448")
            .WithMulticastScouting(true)
            .WithScoutingTimeout(5000)
            .WithTimestamping(true)
            .WithSharedMemory(false);
    }

    [Benchmark]
    public string ToJson_Simple()
    {
        return _simpleConfig.ToJson();
    }

    [Benchmark]
    public string ToJson_Complex()
    {
        return _complexConfig.ToJson();
    }
}
