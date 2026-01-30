using ZenohDotNet.Native;
using Xunit;

namespace ZenohDotNet.Native.Tests;

public class SessionTests
{
    [Fact]
    public void Session_OpenAndClose_Succeeds()
    {
        // Arrange & Act
        using var session = new Session();

        // Assert
        Assert.NotNull(session);
    }

    [Fact]
    public void Session_OpenWithConfig_Succeeds()
    {
        // Arrange
        string config = "{}";

        // Act
        using var session = new Session(config);

        // Assert
        Assert.NotNull(session);
    }

    [Fact]
    public void Session_DeclarePublisher_Succeeds()
    {
        // Arrange
        using var session = new Session();

        // Act
        using var publisher = session.DeclarePublisher("test/demo");

        // Assert
        Assert.NotNull(publisher);
        Assert.Equal("test/demo", publisher.KeyExpression);
    }

    [Fact]
    public void Session_DeclarePublisher_WithNullKeyExpr_ThrowsException()
    {
        // Arrange
        using var session = new Session();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => session.DeclarePublisher(null!));
    }

    [Fact]
    public void Session_DeclareSubscriber_Succeeds()
    {
        // Arrange
        using var session = new Session();
        bool callbackInvoked = false;
        void Callback(Sample sample)
        {
            callbackInvoked = true;
        }

        // Act
        using var subscriber = session.DeclareSubscriber("test/demo", Callback);

        // Assert
        Assert.NotNull(subscriber);
        Assert.Equal("test/demo", subscriber.KeyExpression);
    }

    [Fact]
    public void Session_DeclareSubscriber_WithNullCallback_ThrowsException()
    {
        // Arrange
        using var session = new Session();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            session.DeclareSubscriber("test/demo", null!));
    }
}
