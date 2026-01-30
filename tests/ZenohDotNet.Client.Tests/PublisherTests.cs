using ZenohDotNet.Client;
using Xunit;

namespace ZenohDotNet.Client.Tests;

public class PublisherTests
{
    [Fact]
    public async Task Publisher_PutAsync_ByteArray_Succeeds()
    {
        // Arrange
        await using var session = await Session.OpenAsync();
        await using var publisher = await session.DeclarePublisherAsync("test/demo");
        byte[] data = new byte[] { 1, 2, 3, 4, 5 };

        // Act & Assert (should not throw)
        await publisher.PutAsync(data);
    }

    [Fact]
    public async Task Publisher_PutAsync_String_Succeeds()
    {
        // Arrange
        await using var session = await Session.OpenAsync();
        await using var publisher = await session.DeclarePublisherAsync("test/demo");

        // Act & Assert (should not throw)
        await publisher.PutAsync("Hello, Zenoh!");
    }

    [Fact]
    public async Task Publisher_PutAsync_GenericType_Succeeds()
    {
        // Arrange
        await using var session = await Session.OpenAsync();
        await using var publisher = await session.DeclarePublisherAsync("test/demo");

        // Act & Assert (should not throw)
        await publisher.PutAsync(42);
        await publisher.PutAsync(DateTime.Now);
    }
}
