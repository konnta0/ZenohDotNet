namespace ZenohDotNet.Abstractions;

/// <summary>
/// Specifies the encoding format for Zenoh message serialization.
/// </summary>
public enum ZenohEncoding
{
    /// <summary>
    /// JSON encoding using System.Text.Json.
    /// Default and most portable option.
    /// </summary>
    Json = 0,

    /// <summary>
    /// MessagePack binary encoding.
    /// Requires MessagePack package reference.
    /// More compact and faster than JSON.
    /// </summary>
    MessagePack = 1,

    /// <summary>
    /// Custom encoding using a user-defined serializer.
    /// Requires implementing <see cref="IZenohSerializer{T}"/>.
    /// </summary>
    Custom = 2
}
