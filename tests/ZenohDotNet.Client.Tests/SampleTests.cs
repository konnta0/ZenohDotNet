using ZenohDotNet.Client;
using Xunit;

namespace ZenohDotNet.Client.Tests;

public class SampleTests
{
    [Fact]
    public void Sample_Constructor_CreatesInstance()
    {
        // Arrange
        var keyExpr = "test/demo";
        var payload = System.Text.Encoding.UTF8.GetBytes("test data");

        // Act
        var sample = new Sample(keyExpr, payload);

        // Assert
        Assert.Equal(keyExpr, sample.KeyExpression);
        Assert.Equal(payload, sample.Payload);
    }

    [Fact]
    public void Sample_GetPayloadAsString_ReturnsCorrectValue()
    {
        // Arrange
        var expectedString = "Hello, Zenoh!";
        var payload = System.Text.Encoding.UTF8.GetBytes(expectedString);
        var sample = new Sample("test/demo", payload);

        // Act
        var result = sample.GetPayloadAsString();

        // Assert
        Assert.Equal(expectedString, result);
    }

    [Fact]
    public void Sample_Equals_ComparesCorrectly()
    {
        // Arrange
        var payload = System.Text.Encoding.UTF8.GetBytes("test");
        var sample1 = new Sample("test/demo", payload);
        var sample2 = new Sample("test/demo", payload);
        var sample3 = new Sample("test/other", payload);

        // Act & Assert
        Assert.True(sample1.Equals(sample2));
        Assert.False(sample1.Equals(sample3));
    }
}
