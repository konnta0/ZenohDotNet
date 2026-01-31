using System;

namespace ZenohDotNet.Abstractions;

/// <summary>
/// Marks a property as a key parameter that will be used to construct the key expression.
/// The property value replaces the corresponding placeholder in the DefaultKey.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// [ZenohMessage("game/player/{UserId}/position")]
/// public partial struct PlayerPosition
/// {
///     [ZenohKeyParameter]
///     public string UserId { get; init; }
///     
///     public float X { get; init; }
///     public float Y { get; init; }
/// }
/// 
/// // Generated: BuildKeyExpression() method
/// var key = position.BuildKeyExpression(); // "game/player/123/position"
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class ZenohKeyParameterAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the placeholder name in the key expression.
    /// If not specified, the property name is used.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets whether this property should be excluded from serialization.
    /// Default is false (included in payload).
    /// </summary>
    public bool ExcludeFromPayload { get; set; }
}

/// <summary>
/// Specifies a subscription key pattern with wildcards for subscribing.
/// Use this when the subscription key differs from the publish key.
/// </summary>
/// <remarks>
/// Zenoh wildcards:
/// - * matches any single path segment
/// - ** matches any number of path segments
/// 
/// Example:
/// <code>
/// [ZenohMessage("game/player/{UserId}/position")]
/// [ZenohSubscriptionPattern("game/player/*/position")]  // Subscribe to all players
/// public partial struct PlayerPosition { ... }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
public sealed class ZenohSubscriptionPatternAttribute : Attribute
{
    /// <summary>
    /// Gets the subscription key pattern with wildcards.
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    /// Gets or sets a name for this subscription pattern.
    /// Useful when multiple patterns are defined.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Initializes a new instance with the specified pattern.
    /// </summary>
    /// <param name="pattern">The subscription key pattern (e.g., "game/player/*/position").</param>
    public ZenohSubscriptionPatternAttribute(string pattern)
    {
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
    }
}
