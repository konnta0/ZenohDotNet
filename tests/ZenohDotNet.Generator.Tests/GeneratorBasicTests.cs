using System.Text.Json;
using Xunit;
using ZenohDotNet.Abstractions;

namespace ZenohDotNet.Generator.Tests;

/// <summary>
/// Test message for verifying Source Generator output.
/// </summary>
[ZenohMessage("test/sensor/temperature")]
public partial struct SensorData
{
    public double Temperature { get; init; }
    public double Humidity { get; init; }
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Test message class (reference type).
/// </summary>
[ZenohMessage]
public partial class DeviceConfig
{
    public string DeviceId { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Enabled { get; set; }
}

/// <summary>
/// Test with custom property names.
/// </summary>
[ZenohMessage(DefaultKey = "test/events")]
public partial record EventLog
{
    [ZenohProperty(Name = "event_type")]
    public string EventType { get; init; } = "";
    
    [ZenohProperty(Order = 1)]
    public string Message { get; init; } = "";
    
    [ZenohIgnore]
    public string InternalId { get; init; } = "";
}

/// <summary>
/// Test with key parameters for dynamic keys.
/// </summary>
[ZenohMessage("game/player/{PlayerId}/position")]
[ZenohSubscriptionPattern("game/player/*/position")]
public partial struct PlayerPosition
{
    [ZenohKeyParameter]
    public string PlayerId { get; init; }
    
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }
}

/// <summary>
/// Test with multiple key parameters.
/// </summary>
[ZenohMessage("game/{GameId}/team/{TeamId}/score")]
[ZenohSubscriptionPattern("game/*/team/*/score")]
[ZenohSubscriptionPattern("game/{0}/team/*/score")]  // Game-specific
public partial struct TeamScore
{
    [ZenohKeyParameter]
    public string GameId { get; init; }
    
    [ZenohKeyParameter]
    public string TeamId { get; init; }
    
    public int Score { get; init; }
}

public class GeneratorBasicTests
{
    [Fact]
    public void SensorData_ToBytes_SerializesToJson()
    {
        var data = new SensorData
        {
            Temperature = 25.5,
            Humidity = 60.0,
            Timestamp = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        var bytes = data.ToBytes();
        
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);

        // Verify it's valid JSON
        var json = System.Text.Encoding.UTF8.GetString(bytes);
        Assert.Contains("25.5", json);
        Assert.Contains("60", json);
    }

    [Fact]
    public void SensorData_Serialize_WithInParameter_NoCopy()
    {
        var data = new SensorData
        {
            Temperature = 30.0,
            Humidity = 70.0,
            Timestamp = DateTime.UtcNow
        };

        // This uses 'in' parameter - no copy
        var bytes = SensorData.Serialize(in data);
        
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void SensorData_SerializeTo_UsesSpan()
    {
        var data = new SensorData
        {
            Temperature = 22.0,
            Humidity = 55.0,
            Timestamp = DateTime.UtcNow
        };

        Span<byte> buffer = stackalloc byte[256];
        var written = SensorData.SerializeTo(in data, buffer);
        
        Assert.True(written > 0);
        Assert.True(written < 256);
    }

    [Fact]
    public void SensorData_Deserialize_FromBytes()
    {
        var original = new SensorData
        {
            Temperature = 25.5,
            Humidity = 60.0,
            Timestamp = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        var bytes = original.ToBytes();
        var deserialized = SensorData.Deserialize(bytes);
        
        Assert.Equal(original.Temperature, deserialized.Temperature);
        Assert.Equal(original.Humidity, deserialized.Humidity);
    }

    [Fact]
    public void SensorData_TryDeserialize_ReturnsTrue_OnValidData()
    {
        var original = new SensorData
        {
            Temperature = 25.5,
            Humidity = 60.0,
            Timestamp = DateTime.UtcNow
        };

        var bytes = original.ToBytes();
        var success = SensorData.TryDeserialize(bytes, out var result);
        
        Assert.True(success);
        Assert.Equal(original.Temperature, result.Temperature);
    }

    [Fact]
    public void SensorData_TryDeserialize_ReturnsFalse_OnInvalidData()
    {
        var invalidBytes = new byte[] { 0x00, 0x01, 0x02 };
        var success = SensorData.TryDeserialize(invalidBytes, out _);
        
        Assert.False(success);
    }

    [Fact]
    public void SensorData_HasDefaultKeyExpression()
    {
        Assert.Equal("test/sensor/temperature", SensorData.DefaultKeyExpression);
    }

    [Fact]
    public void SensorData_HasMessageEncoding()
    {
        Assert.Equal(ZenohEncoding.Json, SensorData.MessageEncoding);
    }

    [Fact]
    public void DeviceConfig_Class_SerializesCorrectly()
    {
        var config = new DeviceConfig
        {
            DeviceId = "device-001",
            Name = "Test Device",
            Enabled = true
        };

        var bytes = config.ToBytes();
        var json = System.Text.Encoding.UTF8.GetString(bytes);
        
        Assert.Contains("device-001", json);
        Assert.Contains("Test Device", json);
        Assert.Contains("true", json);
    }

    [Fact]
    public void EventLog_Record_SerializesCorrectly()
    {
        var log = new EventLog
        {
            EventType = "INFO",
            Message = "Test event",
            InternalId = "internal-123" // Should be ignored
        };

        var bytes = log.ToBytes();
        var deserialized = EventLog.Deserialize(bytes);
        
        Assert.Equal("INFO", deserialized.EventType);
        Assert.Equal("Test event", deserialized.Message);
    }

    [Fact]
    public void EventLog_HasDefaultKeyExpression()
    {
        Assert.Equal("test/events", EventLog.DefaultKeyExpression);
    }

    [Fact]
    public void PlayerPosition_BuildKeyExpression_Instance()
    {
        var position = new PlayerPosition
        {
            PlayerId = "player123",
            X = 10.0f,
            Y = 20.0f,
            Z = 30.0f
        };

        var key = position.BuildKeyExpression();
        
        Assert.Equal("game/player/player123/position", key);
    }

    [Fact]
    public void PlayerPosition_BuildKeyExpression_Static()
    {
        var key = PlayerPosition.BuildKeyExpression("player456");
        
        Assert.Equal("game/player/player456/position", key);
    }

    [Fact]
    public void PlayerPosition_SubscriptionPattern()
    {
        Assert.Equal("game/player/*/position", PlayerPosition.SubscriptionPattern);
    }

    [Fact]
    public void TeamScore_MultipleKeyParameters()
    {
        var score = new TeamScore
        {
            GameId = "game001",
            TeamId = "teamA",
            Score = 42
        };

        var key = score.BuildKeyExpression();
        
        Assert.Equal("game/game001/team/teamA/score", key);
    }

    [Fact]
    public void TeamScore_BuildKeyExpression_Static()
    {
        var key = TeamScore.BuildKeyExpression("game002", "teamB");
        
        Assert.Equal("game/game002/team/teamB/score", key);
    }

    [Fact]
    public void TeamScore_MultipleSubscriptionPatterns()
    {
        // First pattern (no suffix)
        Assert.Equal("game/*/team/*/score", TeamScore.SubscriptionPattern1);
        // Second pattern
        Assert.Equal("game/{0}/team/*/score", TeamScore.SubscriptionPattern2);
    }

    [Fact]
    public void PlayerPosition_SerializesWithKeyParameter()
    {
        var position = new PlayerPosition
        {
            PlayerId = "player789",
            X = 1.0f,
            Y = 2.0f,
            Z = 3.0f
        };

        var bytes = position.ToBytes();
        var json = System.Text.Encoding.UTF8.GetString(bytes);
        
        // Key parameter is included in serialization by default
        Assert.Contains("player789", json);
        Assert.Contains("1", json);
    }
}
