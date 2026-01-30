using ZenohDotNet.Native;
using Xunit;

namespace ZenohDotNet.Native.Tests;

public class LivelinessTests
{
    [Fact]
    public void Session_DeclareLivelinessToken_Succeeds()
    {
        // Arrange
        using var session = new Session();

        // Act
        using var token = session.DeclareLivelinessToken("test/liveliness/token1");

        // Assert
        Assert.NotNull(token);
        Assert.Equal("test/liveliness/token1", token.KeyExpression);
    }

    [Fact]
    public void Session_DeclareLivelinessSubscriber_Succeeds()
    {
        // Arrange
        using var session = new Session();
        bool callbackInvoked = false;
        void Callback(string keyExpr, bool isAlive)
        {
            callbackInvoked = true;
        }

        // Act
        using var subscriber = session.DeclareLivelinessSubscriber("test/liveliness/**", Callback);

        // Assert
        Assert.NotNull(subscriber);
        Assert.Equal("test/liveliness/**", subscriber.KeyExpression);
    }

    [Fact]
    public void Session_GetZenohId_ReturnsNonEmptyString()
    {
        // Arrange
        using var session = new Session();

        // Act
        var zid = session.GetZenohId();

        // Assert
        Assert.NotNull(zid);
        Assert.NotEmpty(zid);
        // ZID is a 32-character hex string
        Assert.True(zid.Length >= 32, $"ZID should be at least 32 characters, but was {zid.Length}: {zid}");
    }
}
