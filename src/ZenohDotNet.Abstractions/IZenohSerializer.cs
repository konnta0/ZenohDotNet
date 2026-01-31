using System;

namespace ZenohDotNet.Abstractions;

/// <summary>
/// Interface for custom Zenoh message serialization.
/// Implement this interface to provide custom serialization logic for a message type.
/// </summary>
/// <typeparam name="T">The message type to serialize.</typeparam>
public interface IZenohSerializer<T>
{
    /// <summary>
    /// Gets the maximum size in bytes that the serialized data can occupy.
    /// Used for stack allocation optimization. Return -1 if unknown.
    /// </summary>
    int MaxSerializedSize { get; }

    /// <summary>
    /// Serializes the message to the destination span.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>The number of bytes written.</returns>
    int Serialize(in T value, Span<byte> destination);

    /// <summary>
    /// Deserializes the message from the source span.
    /// </summary>
    /// <param name="source">The source buffer containing serialized data.</param>
    /// <param name="value">The deserialized value.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    bool TryDeserialize(ReadOnlySpan<byte> source, out T value);
}
