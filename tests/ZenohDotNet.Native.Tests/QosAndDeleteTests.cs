using ZenohDotNet.Native;
using Xunit;

namespace ZenohDotNet.Native.Tests;

public class QosAndDeleteTests
{
    [Fact]
    public void Session_DeclarePublisher_WithOptions_Succeeds()
    {
        // Arrange
        using var session = new Session();
        var options = new PublisherOptions
        {
            CongestionControl = CongestionControl.Block,
            Priority = Priority.RealTime,
            IsExpress = true
        };

        // Act
        using var publisher = session.DeclarePublisher("test/qos", options);

        // Assert
        Assert.NotNull(publisher);
        Assert.Equal("test/qos", publisher.KeyExpression);
    }

    [Fact]
    public void Session_Put_Succeeds()
    {
        // Arrange
        using var session = new Session();

        // Act & Assert - should not throw
        session.Put("test/direct", "Hello from session.Put");
    }

    [Fact]
    public void Session_Put_ByteArray_Succeeds()
    {
        // Arrange
        using var session = new Session();
        byte[] data = new byte[] { 1, 2, 3, 4, 5 };

        // Act & Assert - should not throw
        session.Put("test/direct", data);
    }

    [Fact]
    public void Session_Delete_Succeeds()
    {
        // Arrange
        using var session = new Session();

        // Act & Assert - should not throw
        session.Delete("test/to-delete");
    }

    [Fact]
    public void Publisher_Delete_Succeeds()
    {
        // Arrange
        using var session = new Session();
        using var publisher = session.DeclarePublisher("test/pub-delete");

        // Act & Assert - should not throw
        publisher.Delete();
    }

    [Fact]
    public void Sample_SampleKind_DefaultIsPut()
    {
        // Arrange & Act
        var sample = new Sample("test/key", new byte[] { 1, 2, 3 });

        // Assert
        Assert.Equal(SampleKind.Put, sample.Kind);
    }

    [Fact]
    public void Sample_SampleKind_Delete()
    {
        // Arrange & Act
        var sample = new Sample("test/key", Array.Empty<byte>(), SampleKind.Delete);

        // Assert
        Assert.Equal(SampleKind.Delete, sample.Kind);
    }

    [Fact]
    public void PriorityEnum_HasExpectedValues()
    {
        // Assert
        Assert.Equal(1, (int)Priority.RealTime);
        Assert.Equal(2, (int)Priority.InteractiveHigh);
        Assert.Equal(3, (int)Priority.InteractiveLow);
        Assert.Equal(4, (int)Priority.DataHigh);
        Assert.Equal(5, (int)Priority.Data);
        Assert.Equal(6, (int)Priority.DataLow);
        Assert.Equal(7, (int)Priority.Background);
    }

    [Fact]
    public void CongestionControlEnum_HasExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)CongestionControl.Block);
        Assert.Equal(1, (int)CongestionControl.Drop);
    }
}
