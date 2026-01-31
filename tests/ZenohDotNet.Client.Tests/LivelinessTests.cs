using Xunit;

namespace ZenohDotNet.Client.Tests;

public class LivelinessTests
{
    [Fact]
    public async Task Session_DeclareLivelinessTokenAsync_Succeeds()
    {
        // Arrange
        await using var session = await Session.OpenAsync();

        // Act
        await using var token = await session.DeclareLivelinessTokenAsync("test/liveliness/token1");

        // Assert
        Assert.NotNull(token);
        Assert.Equal("test/liveliness/token1", token.KeyExpression);
    }

    [Fact]
    public async Task Session_DeclareLivelinessSubscriberAsync_Succeeds()
    {
        // Arrange
        await using var session = await Session.OpenAsync();
        bool callbackInvoked = false;
        void Callback(string keyExpr, bool isAlive)
        {
            callbackInvoked = true;
        }

        // Act
        await using var subscriber = await session.DeclareLivelinessSubscriberAsync("test/liveliness/**", Callback);

        // Assert
        Assert.NotNull(subscriber);
        Assert.Equal("test/liveliness/**", subscriber.KeyExpression);
    }

    [Fact]
    public async Task LivelinessToken_DisposesCorrectly()
    {
        // Arrange
        await using var session = await Session.OpenAsync();
        var token = await session.DeclareLivelinessTokenAsync("test/liveliness/dispose-test");
        
        // Act & Assert - should not throw
        await token.DisposeAsync();
    }

    [Fact]
    public async Task LivelinessSubscriber_DisposesCorrectly()
    {
        // Arrange
        await using var session = await Session.OpenAsync();
        var subscriber = await session.DeclareLivelinessSubscriberAsync("test/liveliness/**", (_, _) => { });
        
        // Act & Assert - should not throw
        await subscriber.DisposeAsync();
    }
}
