using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SenAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MessageTypeAnalyzer : DiagnosticAnalyzer
    {
        public const string MessageTypeImplementDiagnosticId = "MessageTypeMustNotImplementIUnionDataDirectly";
        public const string MessageTypeHasAttributeDiagnosticId = "MessageTypeMustHasMessagePackObjectAttribute";
        public const string MessageTypeMustBeDeclaredInUnionDiagnosticId = "MessageTypeMustBeDeclaredInUnion";

        private const string CategoryMessageType = "Message";

        private static readonly DiagnosticDescriptor MessageTypeImplementInterfaceDerectlyRule = new DiagnosticDescriptor(
            MessageTypeImplementDiagnosticId,
            @"Message type must implement a Union interface derived from ""IUnionData""",
            @"Message type ""{0}"" must implement an interface derived from ""IUnionData""",
            CategoryMessageType,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Message type must implement a Union interface");

        private static readonly DiagnosticDescriptor MessageTypeMustHaveAttributeRule = new DiagnosticDescriptor(
            MessageTypeHasAttributeDiagnosticId,
            @"Message type must have ""MessagePackObject"" attribute",
            @"Message type ""{0}"" must have ""MessagePackObject"" attribute",
            CategoryMessageType,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: @"Message type must have ""MessagePackObject"" attribute");

        private static readonly DiagnosticDescriptor MessageTypeMustBeDeclaredInUnionTypeRule = new DiagnosticDescriptor(
            MessageTypeMustBeDeclaredInUnionDiagnosticId,
            "Message type must be declared in Union type",
            @"Message type ""{0}"" must be declared in ""{1}"" interface as a UnionAttribute's parameter",
            CategoryMessageType,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Message type must be declared in this Union interface");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(
                    MessageTypeImplementInterfaceDerectlyRule,
                    MessageTypeMustHaveAttributeRule,
                    MessageTypeMustBeDeclaredInUnionTypeRule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(AnalyzeMessageType, SymbolKind.NamedType);
        }

        private static void AnalyzeMessageType(SymbolAnalysisContext context)
        {
            if (context.Symbol is INamedTypeSymbol typeSymbol)
            {
                if (typeSymbol.GetAttributes().Any(IsMessagePackObjectAttribute))
                {
                    if (typeSymbol.Interfaces.Length == 0 || !typeSymbol.Interfaces.Any(IsDerivedFromIUnionData))
                    {
                        var diagnostic = Diagnostic.Create(MessageTypeImplementInterfaceDerectlyRule, typeSymbol.Locations[0], typeSymbol.Name);
                        context.ReportDiagnostic(diagnostic);
                    }
                    else
                    {
                        var unionType = typeSymbol.Interfaces.First(IsDerivedFromIUnionData);
                        if (!unionType.GetAttributes().Any(attr => IsUnionAttributeOf(typeSymbol, attr)))
                        {
                            int key = 0;
                            foreach (var item in unionType.GetAttributes().Where(attr => !IsUnionAttributeOf(typeSymbol, attr)).OrderBy(attr => (int)attr.ConstructorArguments[0].Value))
                            {
                                if ((int)item.ConstructorArguments[0].Value == key)
                                {
                                    key++;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            var properties = new Dictionary<string, string>
                            {
                                { "key", key.ToString() },
                                { "type", typeSymbol.Name },
                                { "interface", unionType.Name },
                                { "file", unionType.Locations[0].SourceTree.FilePath },
                                { "assembly", unionType.ContainingAssembly.Name }
                            }.ToImmutableDictionary();
                            var diagnostic = Diagnostic.Create(MessageTypeMustBeDeclaredInUnionTypeRule, typeSymbol.Locations[0], properties, typeSymbol.Name, unionType.Name);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
                else if (typeSymbol.Interfaces.Any(IsDerivedFromIUnionData))
                {
                    if (!typeSymbol.GetAttributes().Any(IsMessagePackObjectAttribute))
                    {
                        var diagnostic = Diagnostic.Create(MessageTypeMustHaveAttributeRule, typeSymbol.Locations[0], typeSymbol.Name);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        static bool IsUnionAttributeOf(INamedTypeSymbol typeSymbol, AttributeData attributeData)
        {
            return IsName(attributeData.AttributeClass, "UnionAttribute", "MessagePack.Annotations")
                && !attributeData.ConstructorArguments[1].IsNull
                && attributeData.ConstructorArguments[1].Value is ITypeSymbol type
                && type == typeSymbol;
        }

        static bool IsName(ISymbol symbol, string name, string assemblyName)
        {
            return symbol.Name == name && symbol.ContainingAssembly.Name == assemblyName;
        }

        static bool IsMessagePackObjectAttribute(AttributeData attributeData)
        {
            return IsName(attributeData.AttributeClass, "MessagePackObjectAttribute", "MessagePack.Annotations");
        }

        static bool IsDerivedFromIUnionData(INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.AllInterfaces.Any(IsIUnionDataInterface);
        }

        static bool IsIUnionDataInterface(INamedTypeSymbol typeSymbol)
        {
            return IsName(typeSymbol, "IUnionData", "Sen.Interfaces");
        }

    }
}
