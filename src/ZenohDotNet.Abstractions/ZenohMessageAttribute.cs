using System;

namespace ZenohDotNet.Abstractions;

/// <summary>
/// Marks a type as a Zenoh message that can be serialized and deserialized automatically.
/// The type must be declared as partial to allow source generation.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class ZenohMessageAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the default key expression for this message type.
    /// Can be overridden at runtime when publishing.
    /// </summary>
    public string? DefaultKey { get; set; }

    /// <summary>
    /// Gets or sets the default encoding format for this message type.
    /// Default is <see cref="ZenohEncoding.Json"/>.
    /// </summary>
    public ZenohEncoding Encoding { get; set; } = ZenohEncoding.Json;

    /// <summary>
    /// Gets or sets whether to generate extension methods for this type.
    /// Default is true.
    /// </summary>
    public bool GenerateExtensions { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZenohMessageAttribute"/> class.
    /// </summary>
    public ZenohMessageAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ZenohMessageAttribute"/> class
    /// with a default key expression.
    /// </summary>
    /// <param name="defaultKey">The default key expression for publishing.</param>
    public ZenohMessageAttribute(string defaultKey)
    {
        DefaultKey = defaultKey;
    }
}
