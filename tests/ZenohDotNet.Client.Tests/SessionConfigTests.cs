using Xunit;

namespace ZenohDotNet.Client.Tests;

public class SessionConfigTests
{
    [Fact]
    public void SessionConfig_Peer_CreatesPeerConfig()
    {
        // Arrange & Act - just verify it doesn't throw
        var config = SessionConfig.Peer();

        // Assert
        Assert.NotNull(config);
    }

    [Fact]
    public void SessionConfig_Client_CreatesClientConfig()
    {
        // Arrange & Act
        var config = SessionConfig.Client("tcp/router.example.com:7447");

        // Assert
        Assert.NotNull(config);
    }

    [Fact]
    public void SessionConfig_Router_CreatesRouterConfig()
    {
        // Arrange & Act
        var config = SessionConfig.Router("tcp/0.0.0.0:7447");

        // Assert
        Assert.NotNull(config);
    }

    [Fact]
    public void SessionConfig_ChainedConfig_Works()
    {
        // Arrange & Act - verify method chaining works
        var config = new SessionConfig()
            .WithMode(SessionMode.Client)
            .WithConnect("tcp/192.168.1.1:7447")
            .WithMulticastScouting(false)
            .WithScoutingTimeout(3000);

        // Assert
        Assert.NotNull(config);
    }

    [Fact]
    public async Task Session_OpenWithConfig_Succeeds()
    {
        // Arrange
        var config = new SessionConfig()
            .WithMode(SessionMode.Peer)
            .WithMulticastScouting(true);

        // Act
        await using var session = await Session.OpenAsync(config);

        // Assert
        Assert.NotNull(session);
    }

    [Fact]
    public void SessionConfig_WithNegativeTimeout_ThrowsException()
    {
        // Arrange
        var config = new SessionConfig();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => config.WithScoutingTimeout(-1));
    }

    [Fact]
    public void SessionConfig_WithNullEndpoint_ThrowsException()
    {
        // Arrange
        var config = new SessionConfig();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => config.WithConnect((string)null!));
        Assert.Throws<ArgumentNullException>(() => config.WithListen((string)null!));
    }

    [Fact]
    public void SessionConfig_WithMultipleEndpoints_Works()
    {
        // Arrange & Act
        var config = new SessionConfig()
            .WithConnect("tcp/192.168.1.1:7447", "tcp/192.168.1.2:7447")
            .WithListen("tcp/0.0.0.0:7447", "tcp/0.0.0.0:7448");

        // Assert
        Assert.NotNull(config);
    }

    [Fact]
    public void SessionConfig_WithAuth_Works()
    {
        // Arrange & Act
        var config = new SessionConfig()
            .WithAuth("myuser", "mypassword");

        // Assert
        Assert.NotNull(config);
    }

    [Fact]
    public void SessionConfig_WithSharedMemory_Works()
    {
        // Arrange & Act
        var config = new SessionConfig()
            .WithSharedMemory(true);

        // Assert
        Assert.NotNull(config);
    }

    [Fact]
    public void SessionConfig_WithTimestamping_Works()
    {
        // Arrange & Act
        var config = new SessionConfig()
            .WithTimestamping(true);

        // Assert
        Assert.NotNull(config);
    }

    [Fact]
    public void SessionConfig_WithMulticastInterface_Works()
    {
        // Arrange & Act
        var config = new SessionConfig()
            .WithMulticastInterface("eth0")
            .WithMulticastAddress("224.0.0.224");

        // Assert
        Assert.NotNull(config);
    }
}
