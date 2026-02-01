using ZenohDotNet.Client;
using Xunit;

namespace ZenohDotNet.Client.Tests;

public class SessionTests
{
    [Fact]
    [Trait("Category", "RequiresNative")]
    public async Task Session_OpenAsync_Succeeds()
    {
        // Act
        await using var session = await Session.OpenAsync();

        // Assert
        Assert.NotNull(session);
    }

    [Fact]
    [Trait("Category", "RequiresNative")]
    public async Task Session_OpenAsyncWithConfig_Succeeds()
    {
        // Arrange
        string config = "{}";

        // Act
        await using var session = await Session.OpenAsync(config);

        // Assert
        Assert.NotNull(session);
    }

    [Fact]
    [Trait("Category", "RequiresNative")]
    public async Task Session_DeclarePublisherAsync_Succeeds()
    {
        // Arrange
        await using var session = await Session.OpenAsync();

        // Act
        await using var publisher = await session.DeclarePublisherAsync("test/demo");

        // Assert
        Assert.NotNull(publisher);
        Assert.Equal("test/demo", publisher.KeyExpression);
    }

    [Fact]
    [Trait("Category", "RequiresNative")]
    public async Task Session_DeclareSubscriberAsync_Succeeds()
    {
        // Arrange
        await using var session = await Session.OpenAsync();
        bool callbackInvoked = false;
        void Callback(Sample sample)
        {
            callbackInvoked = true;
        }

        // Act
        await using var subscriber = await session.DeclareSubscriberAsync("test/demo", Callback);

        // Assert
        Assert.NotNull(subscriber);
        Assert.Equal("test/demo", subscriber.KeyExpression);
    }
}
