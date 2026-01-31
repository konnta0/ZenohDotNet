using System;

namespace ZenohDotNet.Abstractions;

/// <summary>
/// Marks a property for inclusion/exclusion in Zenoh serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class ZenohPropertyAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the serialized name of this property.
    /// If not set, the property name is used.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets whether this property should be ignored during serialization.
    /// Default is false.
    /// </summary>
    public bool Ignore { get; set; }

    /// <summary>
    /// Gets or sets the order of this property in serialization.
    /// Lower values are serialized first. Default is 0.
    /// </summary>
    public int Order { get; set; }
}

/// <summary>
/// Marks a property to be ignored during Zenoh serialization.
/// Shorthand for [ZenohProperty(Ignore = true)].
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class ZenohIgnoreAttribute : Attribute
{
}
