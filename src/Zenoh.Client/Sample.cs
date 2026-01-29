using System;
using System.Text;
using System.Text.Json;

namespace Zenoh.Client;

/// <summary>
/// Represents a sample received from a Zenoh subscriber with modern C# features.
/// </summary>
/// <param name="KeyExpression">The key expression of this sample.</param>
/// <param name="Payload">The raw payload data.</param>
public record Sample(string KeyExpression, byte[] Payload)
{
    /// <summary>
    /// Gets the payload as a UTF-8 encoded string.
    /// </summary>
    /// <returns>The payload as a string.</returns>
    public string GetPayloadAsString()
    {
        return Encoding.UTF8.GetString(Payload);
    }

    /// <summary>
    /// Tries to deserialize the payload as JSON to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
    public T? TryGetPayloadAsJson<T>()
    {
        try
        {
            return JsonSerializer.Deserialize<T>(Payload);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Gets the payload as JSON, throwing an exception if deserialization fails.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="JsonException">Thrown when deserialization fails.</exception>
    public T GetPayloadAsJson<T>()
    {
        return JsonSerializer.Deserialize<T>(Payload)
            ?? throw new JsonException("Failed to deserialize payload");
    }

    /// <summary>
    /// Returns a string representation of this sample.
    /// </summary>
    public override string ToString()
    {
        return $"Sample {{ KeyExpr = {KeyExpression}, PayloadLen = {Payload.Length} }}";
    }

    // Custom equality for byte array comparison
    public virtual bool Equals(Sample? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return KeyExpression == other.KeyExpression
            && Payload.AsSpan().SequenceEqual(other.Payload);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(KeyExpression, Payload.Length);
    }
}
