using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FindSymbols;

namespace SenAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MessageTypeAnalyzer : DiagnosticAnalyzer
    {
        public const string MessageTypeImplementDiagnosticId = "Sen20";
        public const string MessageTypeHasAttributeDiagnosticId = "Sen21";
        public const string MessageTypeMustBeDeclaredInUnionDiagnosticId = "Sen22";
        public const string MessageTypeLackAHandleMessageMethodDiagnosticId = "Sen23";
        public const string TooMuchHandleMessageMethodForMessageTypeDiagnosticId = "Sen24";

        private const string CategoryMessageType = "Message";

        private static readonly DiagnosticDescriptor MessageTypeImplementInterfaceDerectlyRule = new (
            MessageTypeImplementDiagnosticId,
            @"Message type must implement a Union interface derived from ""IUnionData""",
            @"Message type ""{0}"" must implement an interface derived from ""IUnionData""",
            CategoryMessageType,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Message type must implement a Union interface.");

        private static readonly DiagnosticDescriptor MessageTypeMustHaveAttributeRule = new (
            MessageTypeHasAttributeDiagnosticId,
            @"Message type must have ""MessagePackObject"" attribute",
            @"Message type ""{0}"" must have ""MessagePackObject"" attribute",
            CategoryMessageType,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Message type must have \"MessagePackObject\" attribute.");

        private static readonly DiagnosticDescriptor MessageTypeMustBeDeclaredInUnionTypeRule = new (
            MessageTypeMustBeDeclaredInUnionDiagnosticId,
            "Message type must be declared in Union type",
            @"Message type ""{0}"" must be declared in ""{1}"" interface as a UnionAttribute's parameter",
            CategoryMessageType,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Message type must be declared in this Union interface.");

        private static readonly DiagnosticDescriptor MessageTypeLackAHandleMessageMethodRule = new (
            MessageTypeLackAHandleMessageMethodDiagnosticId,
            $"Message type has no {HandleMessageMethodAnalyzer.HandleMessage} method yet",
            @$"Message type ""{{0}}"" should have a {HandleMessageMethodAnalyzer.HandleMessage} method",
            CategoryMessageType,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: $"Message type should have a {HandleMessageMethodAnalyzer.HandleMessage} method.");

        private static readonly DiagnosticDescriptor TooMuchHandleMessageMethodForMessageTypeRule = new(
            TooMuchHandleMessageMethodForMessageTypeDiagnosticId,
            $"There are {{0}} {HandleMessageMethodAnalyzer.HandleMessage} methods for {{1}} message",
            @$"There are {{0}} {HandleMessageMethodAnalyzer.HandleMessage} methods for {{1}} message",
            CategoryMessageType,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: $"Message type should have only one {HandleMessageMethodAnalyzer.HandleMessage} method.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(
                    MessageTypeImplementInterfaceDerectlyRule,
                    MessageTypeMustHaveAttributeRule,
                    MessageTypeMustBeDeclaredInUnionTypeRule,
                    MessageTypeLackAHandleMessageMethodRule,
                    TooMuchHandleMessageMethodForMessageTypeRule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterSymbolAction(AnalyzeMessageType, SymbolKind.NamedType);
            //context.RegisterSemanticModelAction(AnalyzeHandlerOfMessageType);
        }

        private static void AnalyzeMessageType(SymbolAnalysisContext context)
        {
            if (context.Symbol is INamedTypeSymbol typeSymbol)
            {
                if (typeSymbol.GetAttributes().Any(IsMessagePackObjectAttribute))
                {
                    if (typeSymbol.Interfaces.Length == 0 || !typeSymbol.Interfaces.Any(IsImplemetationOfIUnionData))
                    {
                        var diagnostic = Diagnostic.Create(MessageTypeImplementInterfaceDerectlyRule, typeSymbol.Locations[0], typeSymbol.Name);
                        context.ReportDiagnostic(diagnostic);
                    }
                    else
                    {
                        var unionType = typeSymbol.Interfaces.First(IsImplemetationOfIUnionData);
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
                else if (typeSymbol.Interfaces.Any(IsImplemetationOfIUnionData))
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
                && SymbolEqualityComparer.Default.Equals(type, typeSymbol);
        }

        static bool IsName(ISymbol symbol, string name, string assemblyName)
        {
            return symbol.Name == name && symbol.ContainingAssembly.Name == assemblyName;
        }

        static bool IsMessagePackObjectAttribute(AttributeData attributeData)
        {
            return IsName(attributeData.AttributeClass, "MessagePackObjectAttribute", "MessagePack.Annotations");
        }

        static bool IsImplemetationOfIUnionData(INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.AllInterfaces.Any(IsIUnionDataInterface);
        }

        static bool IsIUnionDataInterface(INamedTypeSymbol typeSymbol)
        {
            return IsName(typeSymbol, "IUnionData", Constants.SenInterfaces);
        }

        private void AnalyzeHandlerOfMessageType(SemanticModelAnalysisContext obj)
        {
        }
    }
}
