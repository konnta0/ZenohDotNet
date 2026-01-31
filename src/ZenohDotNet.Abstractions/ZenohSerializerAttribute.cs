using System;

namespace ZenohDotNet.Abstractions;

/// <summary>
/// Specifies a custom serializer type for a Zenoh message.
/// Use this when <see cref="ZenohEncoding.Custom"/> is specified.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class ZenohSerializerAttribute : Attribute
{
    /// <summary>
    /// Gets the type of the custom serializer.
    /// Must implement <see cref="IZenohSerializer{T}"/> for the target message type.
    /// </summary>
    public Type SerializerType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ZenohSerializerAttribute"/> class.
    /// </summary>
    /// <param name="serializerType">
    /// The type implementing <see cref="IZenohSerializer{T}"/> for the target message.
    /// </param>
    public ZenohSerializerAttribute(Type serializerType)
    {
        SerializerType = serializerType ?? throw new ArgumentNullException(nameof(serializerType));
    }
}
