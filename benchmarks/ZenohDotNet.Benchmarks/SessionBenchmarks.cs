using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ZenohDotNet.Client;

namespace ZenohDotNet.Benchmarks;

/// <summary>
/// Benchmarks for Session operations including open/close and configuration.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[RankColumn]
public class SessionBenchmarks
{
    [Benchmark]
    public async Task Session_OpenClose()
    {
        await using var session = await Session.OpenAsync();
    }

    [Benchmark]
    public async Task Session_OpenClose_WithConfig()
    {
        var config = SessionConfig.Peer()
            .WithMulticastScouting(true)
            .WithScoutingTimeout(1000);
        await using var session = await Session.OpenAsync(config);
    }

    [Benchmark]
    public async Task Session_DeclarePublisher()
    {
        await using var session = await Session.OpenAsync();
        await using var publisher = await session.DeclarePublisherAsync($"benchmark/session/{Guid.NewGuid()}");
    }

    [Benchmark]
    public async Task Session_DeclareSubscriber()
    {
        await using var session = await Session.OpenAsync();
        await using var subscriber = await session.DeclareSubscriberAsync($"benchmark/session/{Guid.NewGuid()}", _ => { });
    }

    [Benchmark]
    public async Task Session_DeclareQueryable()
    {
        await using var session = await Session.OpenAsync();
        await using var queryable = await session.DeclareQueryableAsync($"benchmark/session/{Guid.NewGuid()}", q => q.Reply("key", Array.Empty<byte>()));
    }

    [Benchmark]
    public async Task Session_DeclareLivelinessToken()
    {
        await using var session = await Session.OpenAsync();
        await using var token = await session.DeclareLivelinessTokenAsync($"benchmark/session/{Guid.NewGuid()}");
    }
}
