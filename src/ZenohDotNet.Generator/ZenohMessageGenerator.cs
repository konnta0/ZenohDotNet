using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ZenohDotNet.Generator;

/// <summary>
/// Incremental source generator for Zenoh message types.
/// Generates serialization methods and extension methods for types marked with [ZenohMessage].
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ZenohMessageGenerator : IIncrementalGenerator
{
    private const string ZenohMessageAttributeFullName = "ZenohDotNet.Abstractions.ZenohMessageAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register the attribute source (for IDE support)
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("ZenohMessageMarker.g.cs", SourceText.From(GenerateMarkerInterface(), Encoding.UTF8));
        });

        // Find all type declarations with [ZenohMessage] attribute
        var typeDeclarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ZenohMessageAttributeFullName,
                predicate: static (node, _) => node is TypeDeclarationSyntax,
                transform: static (ctx, _) => GetTypeInfo(ctx))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!);

        // Combine with compilation
        var compilationAndTypes = context.CompilationProvider.Combine(typeDeclarations.Collect());

        // Generate source
        context.RegisterSourceOutput(compilationAndTypes, static (ctx, source) =>
        {
            var (compilation, types) = source;
            GenerateCode(ctx, compilation, types);
        });
    }

    private static ZenohMessageTypeInfo? GetTypeInfo(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
            return null;

        var attribute = context.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString() == ZenohMessageAttributeFullName);

        if (attribute is null)
            return null;

        // Extract attribute properties
        string? defaultKey = null;
        var encoding = ZenohEncodingKind.Json;
        var generateExtensions = true;

        foreach (var namedArg in attribute.NamedArguments)
        {
            switch (namedArg.Key)
            {
                case "DefaultKey":
                    defaultKey = namedArg.Value.Value as string;
                    break;
                case "Encoding":
                    if (namedArg.Value.Value is int encodingValue)
                        encoding = (ZenohEncodingKind)encodingValue;
                    break;
                case "GenerateExtensions":
                    if (namedArg.Value.Value is bool genExt)
                        generateExtensions = genExt;
                    break;
            }
        }

        // Constructor argument for default key
        if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is string ctorKey)
        {
            defaultKey = ctorKey;
        }

        // Check if type is partial
        var isPartial = context.TargetNode is TypeDeclarationSyntax tds &&
            tds.Modifiers.Any(m => m.ValueText == "partial");

        // Check if type is a record
        var isRecord = context.TargetNode is RecordDeclarationSyntax;

        // Get properties with key parameter info
        var properties = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => !p.IsStatic && p.DeclaredAccessibility == Accessibility.Public)
            .Where(p => !HasIgnoreAttribute(p))
            .Select(p => {
                var keyParamInfo = GetKeyParameterInfo(p);
                return new PropertyInfo(
                    p.Name,
                    p.Type.ToDisplayString(),
                    GetPropertyOrder(p),
                    GetPropertySerializedName(p) ?? p.Name,
                    keyParamInfo.IsKeyParameter,
                    keyParamInfo.Name,
                    keyParamInfo.ExcludeFromPayload);
            })
            .OrderBy(p => p.Order)
            .ThenBy(p => p.Name)
            .ToImmutableArray();

        // Get subscription patterns
        var subscriptionPatterns = typeSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "ZenohSubscriptionPatternAttribute")
            .Select(a => a.ConstructorArguments.FirstOrDefault().Value as string)
            .Where(s => !string.IsNullOrEmpty(s))
            .Cast<string>()
            .ToImmutableArray();

        return new ZenohMessageTypeInfo(
            typeSymbol.ContainingNamespace.ToDisplayString(),
            typeSymbol.Name,
            typeSymbol.TypeKind,
            isPartial,
            isRecord,
            defaultKey,
            encoding,
            generateExtensions,
            properties,
            subscriptionPatterns);
    }

    private static bool HasIgnoreAttribute(IPropertySymbol property)
    {
        return property.GetAttributes().Any(a =>
            a.AttributeClass?.Name is "ZenohIgnoreAttribute" or "ZenohPropertyAttribute" &&
            a.NamedArguments.Any(n => n.Key == "Ignore" && n.Value.Value is true));
    }

    private static int GetPropertyOrder(IPropertySymbol property)
    {
        var attr = property.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.Name == "ZenohPropertyAttribute");
        if (attr is null) return 0;
        var orderArg = attr.NamedArguments.FirstOrDefault(n => n.Key == "Order");
        return orderArg.Value.Value is int order ? order : 0;
    }

    private static string? GetPropertySerializedName(IPropertySymbol property)
    {
        var attr = property.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.Name == "ZenohPropertyAttribute");
        if (attr is null) return null;
        var nameArg = attr.NamedArguments.FirstOrDefault(n => n.Key == "Name");
        return nameArg.Value.Value as string;
    }

    private static (bool IsKeyParameter, string? Name, bool ExcludeFromPayload) GetKeyParameterInfo(IPropertySymbol property)
    {
        var attr = property.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.Name == "ZenohKeyParameterAttribute");
        if (attr is null) return (false, null, false);

        string? name = null;
        var excludeFromPayload = false;

        foreach (var arg in attr.NamedArguments)
        {
            switch (arg.Key)
            {
                case "Name":
                    name = arg.Value.Value as string;
                    break;
                case "ExcludeFromPayload":
                    excludeFromPayload = arg.Value.Value is true;
                    break;
            }
        }

        return (true, name ?? property.Name, excludeFromPayload);
    }

    private static void GenerateCode(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<ZenohMessageTypeInfo> types)
    {
        if (types.IsDefaultOrEmpty)
            return;

        foreach (var typeInfo in types.Distinct())
        {
            if (!typeInfo.IsPartial)
            {
                // Report diagnostic for non-partial types
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.TypeMustBePartial,
                    Location.None,
                    typeInfo.TypeName));
                continue;
            }

            // Generate serializer partial class
            var serializerSource = SerializerGenerator.Generate(typeInfo);
            context.AddSource($"{typeInfo.TypeName}.Serializer.g.cs", SourceText.From(serializerSource, Encoding.UTF8));

            // Generate extension methods
            if (typeInfo.GenerateExtensions)
            {
                var extensionsSource = ExtensionMethodGenerator.Generate(typeInfo);
                context.AddSource($"{typeInfo.TypeName}.Extensions.g.cs", SourceText.From(extensionsSource, Encoding.UTF8));
            }
        }
    }

    private static string GenerateMarkerInterface()
    {
        return """
            // <auto-generated/>
            #nullable enable
            
            namespace ZenohDotNet.Generated
            {
                /// <summary>
                /// Marker interface for generated Zenoh message types.
                /// </summary>
                internal interface IZenohGeneratedMessage
                {
                }
            }
            """;
    }
}

internal enum ZenohEncodingKind
{
    Json = 0,
    MessagePack = 1,
    Custom = 2
}

internal sealed record ZenohMessageTypeInfo(
    string Namespace,
    string TypeName,
    TypeKind TypeKind,
    bool IsPartial,
    bool IsRecord,
    string? DefaultKey,
    ZenohEncodingKind Encoding,
    bool GenerateExtensions,
    ImmutableArray<PropertyInfo> Properties,
    ImmutableArray<string> SubscriptionPatterns);

internal sealed record PropertyInfo(
    string Name,
    string TypeName,
    int Order,
    string SerializedName,
    bool IsKeyParameter,
    string? KeyParameterName,
    bool ExcludeFromPayload);
