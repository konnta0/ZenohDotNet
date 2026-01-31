using Microsoft.CodeAnalysis;

namespace ZenohDotNet.Generator;

internal static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor TypeMustBePartial = new(
        id: "ZENOH001",
        title: "Type must be partial",
        messageFormat: "Type '{0}' marked with [ZenohMessage] must be declared as partial",
        category: "ZenohDotNet.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Types marked with [ZenohMessage] attribute must be declared as partial to allow source generation.");

    public static readonly DiagnosticDescriptor CustomSerializerRequired = new(
        id: "ZENOH002",
        title: "Custom serializer required",
        messageFormat: "Type '{0}' uses Custom encoding but no [ZenohSerializer] attribute is specified",
        category: "ZenohDotNet.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "When using Custom encoding, a [ZenohSerializer] attribute must be specified with the serializer type.");

    public static readonly DiagnosticDescriptor InvalidSerializerType = new(
        id: "ZENOH003",
        title: "Invalid serializer type",
        messageFormat: "Serializer type '{0}' does not implement IZenohSerializer<{1}>",
        category: "ZenohDotNet.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The specified serializer type must implement IZenohSerializer<T> for the message type.");

    public static readonly DiagnosticDescriptor PropertyNotSerializable = new(
        id: "ZENOH004",
        title: "Property type not serializable",
        messageFormat: "Property '{0}' of type '{1}' cannot be serialized by the selected encoder",
        category: "ZenohDotNet.Generator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The property type may not be serializable with the selected encoding format.");
}
