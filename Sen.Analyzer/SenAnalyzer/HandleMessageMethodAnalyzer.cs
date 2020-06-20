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
    public class HandleMessageMethodAnalyzer : DiagnosticAnalyzer
    {
        public const string RenameToHandleMessageDiagnosticId = "SenHandleMessageRename";
        public const string HandleMessageSignatureReturnTypeDiagnosticId = "SenHandleMessageSignatureReturnType";
        public const string HandleMessageSignatureArg0DiagnosticId = "SenHandleMessageSignatureArg0";
        public const string HandleMessageSignatureArg1DiagnosticId = "SenHandleMessageSignatureArg1";
        public const string HandleMessageSignatureArgLengthDiagnosticId = "SenHandleMessageSignatureArgLength";
        public const string HandleMessage = "HandleMessage";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization

        private const string CategoryNaming = "Naming";

        private static readonly DiagnosticDescriptor HandleMessageNameRule = new DiagnosticDescriptor(
            RenameToHandleMessageDiagnosticId,
            "Rename HandleMessage method name",
            $@"Method ""{{0}}"" seem likes a message handler you may want to rename it ""{HandleMessage}""",
            CategoryNaming,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "HandleMessage method name may be miss spelled");

        private static readonly DiagnosticDescriptor HandleMessageSignatureReturnTypeRule = new DiagnosticDescriptor(
            HandleMessageSignatureReturnTypeDiagnosticId,
            "HandleMessage method return type not acceptable",
            "HandleMessage return type {0} which must be {1}",
            CategoryNaming,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "HandleMessage method return type not acceptable");

        private static readonly DiagnosticDescriptor HandleMessageSignatureArgLengthRule = new DiagnosticDescriptor(
            HandleMessageSignatureArgLengthDiagnosticId,
            "HandleMessage method parameters list length",
            "HandleMessage method must have exact {0} parameter{1}",
            CategoryNaming,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "HandleMessage method parameters list length is not match");

        private static readonly DiagnosticDescriptor HandleMessageSignatureArg0Rule = new DiagnosticDescriptor(
            HandleMessageSignatureArg0DiagnosticId,
            "HandleMessage method argument type is invalid",
            "HandleMessage method argument {0} must be a MessagePackObject implements {1}",
            CategoryNaming,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "HandleMessage method argument type is invalid");

        private static readonly DiagnosticDescriptor HandleMessageSignatureArg1Rule = new DiagnosticDescriptor(
            HandleMessageSignatureArg1DiagnosticId,
            "HandleMessage method argument type is invalid",
            "HandleMessage method argument {0} must be an {1}",
            CategoryNaming,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "HandleMessage method argument type is invalid");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get 
            {
                return ImmutableArray.Create(
                    HandleMessageNameRule,
                    HandleMessageSignatureReturnTypeRule,
                    HandleMessageSignatureArg0Rule,
                    HandleMessageSignatureArg1Rule,
                    HandleMessageSignatureArgLengthRule); 
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(AnalyzeHandleMessageMethodSymbol, SymbolKind.Method);
            context.RegisterSymbolAction(AnalyzeHandleMessageMethodSignature, SymbolKind.Method);
        }

        private static void AnalyzeHandleMessageMethodSymbol(SymbolAnalysisContext context)
        {
            var methodSymbol = (IMethodSymbol)context.Symbol;
            if (methodSymbol.Name == HandleMessage)
            {
                return;
            }
            if (LevenshteinDistance.Compute(methodSymbol.Name, HandleMessage) <= 3)
            {
                var diagnostic = Diagnostic.Create(HandleMessageNameRule, methodSymbol.Locations[0], methodSymbol.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static void AnalyzeHandleMessageMethodSignature(SymbolAnalysisContext context)
        {
            IMethodSymbol methodSymbol = (IMethodSymbol)context.Symbol;
            if (methodSymbol.Name != HandleMessage)
            {
                return;
            }
            INamedTypeSymbol containingClass = methodSymbol.ContainingType;
            
            ITypeSymbol tUnionDataType = GetUnionTypeOfPlayer(containingClass.BaseType);
            if (tUnionDataType != null)
            {
                if (methodSymbol.ReturnsVoid || !ReturnTypeIsUnionData(methodSymbol.ReturnType, tUnionDataType))
                {
                    var diagnostic = Diagnostic.Create(HandleMessageSignatureReturnTypeRule,
                        ReturnTypeLocation(methodSymbol),
                        ReturnTypeName(methodSymbol),
                        $"ValueTask<{tUnionDataType.Name}>");
                    context.ReportDiagnostic(diagnostic);
                }

                if (methodSymbol.Parameters.Length != 1)
                {
                    var diagnostic = Diagnostic.Create(HandleMessageSignatureArgLengthRule, ParameterTypeLocation(methodSymbol, -1), 1, string.Empty);
                    context.ReportDiagnostic(diagnostic);
                }
                else
                {
                    ITypeSymbol param0 = methodSymbol.Parameters[0].Type;
                    bool paramTypeMatch = param0.BaseType != null && HasInterface(param0, tUnionDataType);
                    if (!paramTypeMatch)
                    {
                        var diagnostic = Diagnostic.Create(HandleMessageSignatureArg0Rule, ParameterTypeLocation(methodSymbol, 0),
                            methodSymbol.Parameters[0].Name, tUnionDataType.Name);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }

            var unionType = GetUnionDataTypeInRoomType(containingClass.BaseType);
            if (unionType != null)
            {
                if (methodSymbol.ReturnsVoid || !ReturnTypeIsUnionData(methodSymbol.ReturnType, unionType))
                {
                    var diagnostic = Diagnostic.Create(HandleMessageSignatureReturnTypeRule,
                        ReturnTypeLocation(methodSymbol),
                        ReturnTypeName(methodSymbol),
                        $"ValueTask<{unionType.Name}>");
                    context.ReportDiagnostic(diagnostic);
                }


                if (methodSymbol.Parameters.Length != 2)
                {
                    var diagnostic = Diagnostic.Create(HandleMessageSignatureArgLengthRule,
                        ParameterTypeLocation(methodSymbol, -1), 2, 's');
                    context.ReportDiagnostic(diagnostic);
                }
                else
                {
                    ITypeSymbol param0 = methodSymbol.Parameters[0].Type;
                    bool param0TypeMatch = param0.BaseType != null && HasInterface(param0, unionType);
                    if (!param0TypeMatch)
                    {
                        var diagnostic = Diagnostic.Create(HandleMessageSignatureArg0Rule,
                            ParameterTypeLocation(methodSymbol, 0), methodSymbol.Parameters[0].Name, unionType.Name);
                        context.ReportDiagnostic(diagnostic);
                    }

                    ITypeSymbol param1 = methodSymbol.Parameters[1].Type;
                    bool param1TypeMatch = IsOfIPlayer(param1);
                    if (!param1TypeMatch)
                    {
                        var diagnostic = Diagnostic.Create(HandleMessageSignatureArg1Rule,
                            ParameterTypeLocation(methodSymbol, 1), methodSymbol.Parameters[1].Name, "IPlayer");
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        static Location ReturnTypeLocation(IMethodSymbol methodSymbol)
        {
            MethodDeclarationSyntax methodNode = methodSymbol.Locations[0].SourceTree.GetRoot().FindNode(methodSymbol.Locations[0].SourceSpan) as MethodDeclarationSyntax;
            return methodNode.ReturnType.GetLocation();
        }

        static string ReturnTypeName(IMethodSymbol methodSymbol)
        {
            return methodSymbol.ReturnsVoid
                        ? "void"
                        : methodSymbol.ReturnType is INamedTypeSymbol vt && vt.Name == "ValueTask" && vt.IsGenericType
                            ? $"ValueTask<{vt.TypeArguments[0].Name}>"
                            : $"{methodSymbol.ReturnType.Name}";
        }

        static Location ParameterTypeLocation(IMethodSymbol methodSymbol, int paramIndex)
        {
            MethodDeclarationSyntax methodNode = methodSymbol.Locations[0].SourceTree.GetRoot().FindNode(methodSymbol.Locations[0].SourceSpan) as MethodDeclarationSyntax;
            if (paramIndex < 0)
            {
                return methodNode.ParameterList.GetLocation();
            }
            return methodNode.ParameterList.Parameters[paramIndex].GetLocation();
        }

        static bool IsOfIPlayer(ITypeSymbol param1)
        {
            if (param1.Name == "IPlayer" && param1.ContainingAssembly.Name == "Sen.Interfaces")
            {
                return true;
            }
            if (param1.AllInterfaces.Any(i => i.Name == "IPlayer" && i.ContainingAssembly.Name == "Sen.Interfaces"))
            {
                return true;
            }
            return false;
        }

        static ITypeSymbol GetUnionTypeOfPlayer(INamedTypeSymbol classType)
        {
            if (classType == null)
            {
                return null;
            }
            if (classType.IsGenericType && classType.Name == "Player" && classType.ContainingAssembly.Name == "Sen.Grains")
            {
                return classType.TypeArguments[0];
            }
            return GetUnionTypeOfPlayer(classType.BaseType);
        }

        static bool HasInterface(ITypeSymbol type, ITypeSymbol @interface)
        {
            if (type.Name == @interface.Name && type.ContainingAssembly == @interface.ContainingAssembly)
            {
                return true;
            }
            if (type.AllInterfaces.Any(i => i.Name == @interface.Name && i.ContainingAssembly == @interface.ContainingAssembly))
            {
                return true;
            }
            return false;
        }

        static bool ReturnTypeIsUnionData(ITypeSymbol typeSymbol, ITypeSymbol tUnionDataType)
        {
            return typeSymbol.IsValueType
                   && typeSymbol is INamedTypeSymbol valueTask
                   && valueTask.IsGenericType
                   && valueTask.Name == "ValueTask"
                   && HasInterface(valueTask.TypeArguments[0], tUnionDataType);
        }

        static ITypeSymbol GetUnionDataTypeInRoomType(ITypeSymbol typeSymbol)
        {
            if (typeSymbol == null)
            {
                return null;
            }
            if (typeSymbol.Name == "Room" && typeSymbol.ContainingAssembly.Name == "Sen.Grains")
            {
                if (typeSymbol.GetMembers().FirstOrDefault(t => t.Name == HandleMessage) is IMethodSymbol method
                    && method.ReturnType is INamedTypeSymbol valueTask)
                {
                    return valueTask.TypeArguments[0];
                }
                return null;
            }
            return GetUnionDataTypeInRoomType(typeSymbol.BaseType);
        }
    }
}
