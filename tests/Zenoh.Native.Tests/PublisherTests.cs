using Zenoh.Native;
using Xunit;

namespace Zenoh.Native.Tests;

public class PublisherTests
{
    [Fact]
    public void Publisher_Put_ByteArray_Succeeds()
    {
        // Arrange
        using var session = new Session();
        using var publisher = session.DeclarePublisher("test/demo");
        byte[] data = new byte[] { 1, 2, 3, 4, 5 };

        // Act & Assert (should not throw)
        publisher.Put(data);
    }

    [Fact]
    public void Publisher_Put_String_Succeeds()
    {
        // Arrange
        using var session = new Session();
        using var publisher = session.DeclarePublisher("test/demo");

        // Act & Assert (should not throw)
        publisher.Put("Hello, Zenoh!");
    }

    [Fact]
    public void Publisher_Put_NullData_ThrowsException()
    {
        // Arrange
        using var session = new Session();
        using var publisher = session.DeclarePublisher("test/demo");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => publisher.Put((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => publisher.Put((string)null!));
    }

    [Fact]
    public void Publisher_Put_EmptyData_Succeeds()
    {
        // Arrange
        using var session = new Session();
        using var publisher = session.DeclarePublisher("test/demo");

        // Act & Assert (should not throw)
        publisher.Put(Array.Empty<byte>());
        publisher.Put(string.Empty);
    }
}
