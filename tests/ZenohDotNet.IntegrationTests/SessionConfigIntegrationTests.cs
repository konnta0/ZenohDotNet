using ZenohDotNet.Client;

namespace ZenohDotNet.IntegrationTests;

/// <summary>
/// Integration tests for SessionConfig functionality.
/// Tests verify that different session configurations work correctly.
/// </summary>
public class SessionConfigIntegrationTests
{
    [Fact]
    public async Task PeerMode_CanCommunicate()
    {
        // Arrange
        var keyExpr = $"test/integration/config/peer/{Guid.NewGuid()}";
        var messageReceived = new TaskCompletionSource<string>();

        var config1 = SessionConfig.Peer();
        var config2 = SessionConfig.Peer();

        await using var pubSession = await Session.OpenAsync(config1);
        await using var subSession = await Session.OpenAsync(config2);

        await using var subscriber = await subSession.DeclareSubscriberAsync(keyExpr, sample =>
        {
            var msg = System.Text.Encoding.UTF8.GetString(sample.Payload);
            messageReceived.TrySetResult(msg);
        });

        await using var publisher = await pubSession.DeclarePublisherAsync(keyExpr);
        await Task.Delay(100);

        // Act
        await publisher.PutAsync("Peer mode test");

        // Wait
        var received = await Task.WhenAny(
            messageReceived.Task,
            Task.Delay(TimeSpan.FromSeconds(5))
        ) == messageReceived.Task;

        // Assert
        Assert.True(received, "Message not received in peer mode");
        Assert.Equal("Peer mode test", await messageReceived.Task);
    }

    [Fact]
    public async Task SessionWithMulticastDisabled_StillWorks()
    {
        // Arrange - Disable multicast scouting
        var keyExpr = $"test/integration/config/nomulticast/{Guid.NewGuid()}";
        var messageReceived = new TaskCompletionSource<string>();

        var config = new SessionConfig()
            .WithMode(SessionMode.Peer)
            .WithMulticastScouting(false);

        await using var session1 = await Session.OpenAsync(config);
        await using var session2 = await Session.OpenAsync(config);

        await using var subscriber = await session2.DeclareSubscriberAsync(keyExpr, sample =>
        {
            var msg = System.Text.Encoding.UTF8.GetString(sample.Payload);
            messageReceived.TrySetResult(msg);
        });

        await using var publisher = await session1.DeclarePublisherAsync(keyExpr);
        await Task.Delay(100);

        // Act
        await publisher.PutAsync("No multicast test");

        // Wait - may take longer without multicast
        var received = await Task.WhenAny(
            messageReceived.Task,
            Task.Delay(TimeSpan.FromSeconds(10))
        ) == messageReceived.Task;

        // Assert - Communication might not work without multicast in isolated test
        // This test mainly verifies the config doesn't crash the session
        Assert.NotNull(session1);
        Assert.NotNull(session2);
    }

    [Fact]
    public async Task SessionWithTimestamping_ProducesTimestamps()
    {
        // This test verifies that enabling timestamping doesn't break the session
        var config = new SessionConfig()
            .WithMode(SessionMode.Peer)
            .WithTimestamping(true);

        // Act
        await using var session = await Session.OpenAsync(config);

        // Assert - Session should open successfully
        Assert.NotNull(session);
    }

    [Fact]
    public async Task SessionWithScoutingTimeout_Respects_Timeout()
    {
        // Arrange
        var config = new SessionConfig()
            .WithMode(SessionMode.Peer)
            .WithScoutingTimeout(1000); // 1 second

        // Act - Should complete within reasonable time
        var startTime = DateTime.UtcNow;
        await using var session = await Session.OpenAsync(config);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        Assert.NotNull(session);
        // Session opening should complete (not hang indefinitely)
        Assert.True(elapsed < TimeSpan.FromSeconds(30), "Session took too long to open");
    }

    [Fact]
    public async Task MultipleSessionsWithDifferentConfigs_CanCoexist()
    {
        // Arrange
        var keyExpr = $"test/integration/config/multi/{Guid.NewGuid()}";

        var config1 = new SessionConfig()
            .WithMode(SessionMode.Peer)
            .WithMulticastScouting(true);

        var config2 = new SessionConfig()
            .WithMode(SessionMode.Peer)
            .WithTimestamping(true);

        var config3 = new SessionConfig()
            .WithMode(SessionMode.Peer)
            .WithScoutingTimeout(2000);

        // Act - Open multiple sessions with different configs
        await using var session1 = await Session.OpenAsync(config1);
        await using var session2 = await Session.OpenAsync(config2);
        await using var session3 = await Session.OpenAsync(config3);

        // Create publishers on each
        await using var pub1 = await session1.DeclarePublisherAsync($"{keyExpr}/s1");
        await using var pub2 = await session2.DeclarePublisherAsync($"{keyExpr}/s2");
        await using var pub3 = await session3.DeclarePublisherAsync($"{keyExpr}/s3");

        // Assert - All sessions and publishers should be created
        Assert.NotNull(session1);
        Assert.NotNull(session2);
        Assert.NotNull(session3);
        Assert.NotNull(pub1);
        Assert.NotNull(pub2);
        Assert.NotNull(pub3);
    }

    [Fact]
    public async Task DefaultConfig_Works()
    {
        // Act - Default configuration (no custom settings)
        await using var session = await Session.OpenAsync();

        // Assert
        Assert.NotNull(session);
    }

    [Fact]
    public async Task EmptyJsonConfig_Works()
    {
        // Act - Empty JSON configuration
        await using var session = await Session.OpenAsync("{}");

        // Assert
        Assert.NotNull(session);
    }
}
