using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SenAnalyzer.Utils;

namespace SenAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class HandleMessageMethodAnalyzer : DiagnosticAnalyzer
    {
        public const string HandleMessage = "HandleMessage";
        public const string RenameToHandleMessageDiagnosticId = $"Sen01";
        public const string HandleMessageMustBePublicDiagnosticId = $"Sen02";
        public const string HandleMessageSignatureReturnTypeDiagnosticId = $"Sen03";
        public const string HandleMessageSignatureArg0DiagnosticId = $"Sen04";
        public const string HandleMessageSignatureArg1DiagnosticId = $"Sen05";
        public const string HandleMessageSignatureArg2DiagnosticId = $"Sen06";
        public const string HandleMessageSignatureArgLengthDiagnosticId = $"Sen07";
        public const string HandleMessageCreateMethodDiagnosticId = $"Sen08";

        public static readonly string HandleMessageShort;
        static HandleMessageMethodAnalyzer()
        {
            HandleMessageShort = string.Join("", HandleMessage.Where(c => c >= 'A' && c <= 'Z')).ToLower();
        }

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization

        private const string CategoryNaming = "Naming";

        private static readonly DiagnosticDescriptor HandleMessageNameRule = new(
            RenameToHandleMessageDiagnosticId,
            $"Rename {HandleMessage} method name",
            $@"Method ""{{0}}"" seem likes a message handler you may want to rename it ""{HandleMessage}""",
            CategoryNaming,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: $"{HandleMessage} method name may be miss spelled.");

        private static readonly DiagnosticDescriptor HandleMessageMustBePublicRule = new(
            HandleMessageMustBePublicDiagnosticId,
            $"{HandleMessage} must be a non-statc public method",
            $"{HandleMessage} must be a non-statc public method",
            CategoryNaming,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: $"{HandleMessage} must be a non-statc public method.");

        private static readonly DiagnosticDescriptor HandleMessageSignatureReturnTypeRule = new(
            HandleMessageSignatureReturnTypeDiagnosticId,
            $"{HandleMessage} method return type not acceptable",
            $"{HandleMessage} return type {{0}} which must be {{1}}",
            CategoryNaming,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: $"{HandleMessage} method return type not acceptable.");

        private static readonly DiagnosticDescriptor HandleMessageSignatureArgLengthRule = new (
            HandleMessageSignatureArgLengthDiagnosticId,
            $"{HandleMessage} method parameters list length",
            $"{HandleMessage} method must have exact {{0}} parameter{{1}}",
            CategoryNaming,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: $"{HandleMessage} method parameters list length is not match.");

        private static readonly DiagnosticDescriptor HandleMessageSignatureArg0Rule = new (
            HandleMessageSignatureArg0DiagnosticId,
            $"{HandleMessage} method argument type is invalid",
            $"{HandleMessage} method argument {{0}} must be a MessagePackObject implements {1}",
            CategoryNaming,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: $"{HandleMessage} method argument type is invalid.");

        private static readonly DiagnosticDescriptor HandleMessageSignatureArg1Rule = new (
            HandleMessageSignatureArg1DiagnosticId,
            $"{HandleMessage} method argument type is invalid",
            $"{HandleMessage} method argument {{0}} must be an {{01}}",
            CategoryNaming,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: $"{HandleMessage} method argument type is invalid.");

        private static readonly DiagnosticDescriptor HandleMessageSignatureArg2Rule = new(
            HandleMessageSignatureArg2DiagnosticId,
            $"{HandleMessage} method argument type is invalid",
            $"{HandleMessage} method argument {{0}} must be an {{01}}",
            CategoryNaming,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: $"{HandleMessage} method argument type is invalid.");

        private static readonly DiagnosticDescriptor HandleMessageCreateMethodRule = new (
            HandleMessageCreateMethodDiagnosticId,
            $"Create mothod {HandleMessage} for a message type",
            $"Create method {HandleMessage}",
            CategoryNaming,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: $"Create method {HandleMessage} here.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get 
            {
                return ImmutableArray.Create(
                    HandleMessageNameRule,
                    HandleMessageMustBePublicRule,
                    HandleMessageSignatureReturnTypeRule,
                    HandleMessageSignatureArg0Rule,
                    HandleMessageSignatureArg1Rule,
                    HandleMessageSignatureArg2Rule,
                    HandleMessageSignatureArgLengthRule,
                    HandleMessageCreateMethodRule); 
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            //context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterSymbolAction(AnalyzeHandleMessageMethodSymbolSignature, SymbolKind.Method);
            context.RegisterSymbolAction(AnalyzeHandleMessageCreation, SymbolKind.Field);
            //context.RegisterSemanticModelAction(AnalyzeMessageSemantic);
        }

        //private void AnalyzeMessageSemantic(SemanticModelAnalysisContext semanticModelAnalysisContext)
        //{
        //    var res = semanticModelAnalysisContext.SemanticModel.SyntaxTree.GetRoot().DescendantNodes();
        //}

        private void AnalyzeHandleMessageCreation(SymbolAnalysisContext context)
        {
            var symbol = context.Symbol;
            if (symbol.Name.ToLower() == HandleMessageShort)
            {
                var diagnostic = Diagnostic.Create(HandleMessageCreateMethodRule, symbol.Locations[0], symbol.Locations);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static void AnalyzeHandleMessageMethodSymbolSignature(SymbolAnalysisContext context)
        {
            IMethodSymbol methodSymbol = (IMethodSymbol)context.Symbol;
            INamedTypeSymbol containingClass = methodSymbol.ContainingType;

            if (!IsAbstractPlayer(containingClass) && !IsAbstractRoom(containingClass))
            {
                return;
            }
            if (methodSymbol.Name.ToLower() == HandleMessageShort)
            {
                var diagnostic = Diagnostic.Create(HandleMessageCreateMethodRule, methodSymbol.Locations[0], methodSymbol.Locations);
                context.ReportDiagnostic(diagnostic);
            }

            if (methodSymbol.Name != HandleMessage && LevenshteinDistance.Compute(methodSymbol.Name, HandleMessage) <= 3)
            {
                var diagnostic = Diagnostic.Create(HandleMessageNameRule, methodSymbol.Locations[0], methodSymbol.Name);
                context.ReportDiagnostic(diagnostic);
            }

            if (methodSymbol.Name != HandleMessage)
            {
                return;
            }

            ITypeSymbol tUnionDataType = GetUnionTypeOfPlayer(containingClass.BaseType);
            while (tUnionDataType != null)
            {
                if (methodSymbol.ReturnsVoid || !ReturnTypeIsUnionData(methodSymbol.ReturnType, tUnionDataType))
                {
                    var diagnostic = Diagnostic.Create(HandleMessageSignatureReturnTypeRule,
                        ReturnTypeLocation(methodSymbol),
                        ReturnTypeName(methodSymbol),
                        $"ValueTask<{tUnionDataType.Name}>");
                    context.ReportDiagnostic(diagnostic);
                }

                if (methodSymbol.IsStatic || methodSymbol.DeclaredAccessibility != Accessibility.Public)
                {
                    var diagnostic = Diagnostic.Create(HandleMessageMustBePublicRule, ModifiersLocation(methodSymbol), methodSymbol.Locations);
                    context.ReportDiagnostic(diagnostic);
                }

                if (methodSymbol.Parameters.Length != 2)
                {
                    var diagnostic = Diagnostic.Create(HandleMessageSignatureArgLengthRule, ParameterTypeLocation(methodSymbol, -1), methodSymbol.Locations, 2, 's');
                    context.ReportDiagnostic(diagnostic);
                }

                if (methodSymbol.Parameters.Length < 1) break;
                ITypeSymbol param0 = methodSymbol.Parameters[0].Type;
                bool paramTypeMatch = param0.BaseType != null && HasInterface(param0, tUnionDataType);
                if (!paramTypeMatch)
                {
                    var diagnostic = Diagnostic.Create(HandleMessageSignatureArg0Rule, ParameterTypeLocation(methodSymbol, 0), methodSymbol.Locations,
                        methodSymbol.Parameters[0].Name, tUnionDataType.Name);
                    context.ReportDiagnostic(diagnostic);
                }

                if (methodSymbol.Parameters.Length < 2) break;
                ITypeSymbol param1 = methodSymbol.Parameters[1].Type;
                paramTypeMatch = param1.Name == Constants.NetworkOptions && param1.ContainingAssembly.Name == Constants.SenInterfaces;
                if (!paramTypeMatch)
                {
                    var diagnostic = Diagnostic.Create(HandleMessageSignatureArg1Rule, ParameterTypeLocation(methodSymbol, 0), methodSymbol.Locations,
                        methodSymbol.Parameters[1].Name, Constants.NetworkOptions);
                    context.ReportDiagnostic(diagnostic);
                }
                break;
            }

            var unionType = GetUnionDataTypeInRoomType(containingClass);
            while (unionType != null)
            {
                if (methodSymbol.ReturnsVoid || !ReturnTypeIsUnionData(methodSymbol.ReturnType, unionType))
                {
                    var diagnostic = Diagnostic.Create(HandleMessageSignatureReturnTypeRule,
                        ReturnTypeLocation(methodSymbol),
                        ReturnTypeName(methodSymbol),
                        $"ValueTask<{unionType.Name}>");
                    context.ReportDiagnostic(diagnostic);
                }

                if (methodSymbol.IsStatic || methodSymbol.DeclaredAccessibility != Accessibility.Public)
                {
                    var diagnostic = Diagnostic.Create(HandleMessageMustBePublicRule, ModifiersLocation(methodSymbol), methodSymbol.Locations);
                    context.ReportDiagnostic(diagnostic);
                }

                if (methodSymbol.Parameters.Length != 3)
                {
                    var diagnostic = Diagnostic.Create(HandleMessageSignatureArgLengthRule,
                        ParameterTypeLocation(methodSymbol, -1), 3, 's');
                    context.ReportDiagnostic(diagnostic);
                }

                if (methodSymbol.Parameters.Length < 1) break;
                ITypeSymbol param0 = methodSymbol.Parameters[0].Type;
                bool param0TypeMatch = param0.BaseType != null && HasInterface(param0, unionType);
                if (!param0TypeMatch)
                {
                    var diagnostic = Diagnostic.Create(HandleMessageSignatureArg0Rule,
                        ParameterTypeLocation(methodSymbol, 0), methodSymbol.Locations, methodSymbol.Parameters[0].Name, unionType.Name);
                    context.ReportDiagnostic(diagnostic);
                }

                if (methodSymbol.Parameters.Length < 2) break;
                ITypeSymbol param1 = methodSymbol.Parameters[1].Type;
                bool param1TypeMatch = IsIPlayer(param1);
                if (!param1TypeMatch)
                {
                    var diagnostic = Diagnostic.Create(HandleMessageSignatureArg1Rule,
                        ParameterTypeLocation(methodSymbol, 1), methodSymbol.Locations, methodSymbol.Parameters[1].Name, Constants.IPlayer);
                    context.ReportDiagnostic(diagnostic);
                }

                if (methodSymbol.Parameters.Length < 3) break;
                ITypeSymbol param2 = methodSymbol.Parameters[2].Type;
                bool param2TypeMatch = param2.Name == Constants.NetworkOptions && param2.ContainingAssembly.Name == Constants.SenInterfaces;
                if (!param2TypeMatch)
                {
                    var diagnostic = Diagnostic.Create(HandleMessageSignatureArg1Rule, 
                        ParameterTypeLocation(methodSymbol, 2), methodSymbol.Locations, methodSymbol.Parameters[2].Name, Constants.NetworkOptions);
                    context.ReportDiagnostic(diagnostic);
                }
                break;
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

        static Location ModifiersLocation(IMethodSymbol methodSymbol)
        {
            MethodDeclarationSyntax methodNode = methodSymbol.Locations[0].SourceTree.GetRoot().FindNode(methodSymbol.Locations[0].SourceSpan) as MethodDeclarationSyntax;
            return Location.Create(methodSymbol.Locations[0].SourceTree, methodNode.Modifiers.Span);
        }

        public static bool IsIPlayer(ITypeSymbol symbol)
        {
            if (symbol.Name == Constants.IPlayer && symbol.ContainingAssembly.Name == Constants.SenInterfaces)
            {
                return true;
            }
            return false;
        }

        public static bool IsAbstractPlayer(ITypeSymbol symbol)
        {
            if (symbol == null) return false;
            if (symbol.Name == Constants.SenPlayerClassName && symbol.ContainingAssembly.Name == Constants.SenGrains)
            {
                return true;
            }
            return IsAbstractPlayer(symbol.BaseType);
        }

        public static bool IsAbstractRoom(ITypeSymbol symbol)
        {
            if (symbol == null) return false;
            if (symbol.Name == Constants.SenRoomClassName && symbol.ContainingAssembly.Name == Constants.SenGrains)
            {
                return true;
            }
            return IsAbstractRoom(symbol.BaseType);
        }

        public static ITypeSymbol GetUnionTypeOfPlayer(INamedTypeSymbol classType)
        {
            if (classType == null)
            {
                return null;
            }
            if (classType.IsGenericType && classType.Name == Constants.SenPlayerClassName && classType.ContainingAssembly.Name == Constants.SenGrains)
            {
                return classType.TypeArguments[0];
            }
            return GetUnionTypeOfPlayer(classType.BaseType);
        }

        public static bool HasInterface(ITypeSymbol type, ITypeSymbol @interface)
        {
            var cmp = SymbolEqualityComparer.Default;
            if (type.Name == @interface.Name && cmp.Equals(type.ContainingAssembly, @interface.ContainingAssembly))
            {
                return true;
            }
            if (type.AllInterfaces.Any(i => i.Name == @interface.Name && cmp.Equals(i.ContainingAssembly, @interface.ContainingAssembly)))
            {
                return true;
            }
            return false;
        }

        public static bool ReturnTypeIsUnionData(ITypeSymbol typeSymbol, ITypeSymbol tUnionDataType)
        {
            return typeSymbol.IsValueType
                   && typeSymbol is INamedTypeSymbol valueTask
                   && valueTask.IsGenericType
                   && valueTask.Name == "ValueTask"
                   && HasInterface(valueTask.TypeArguments[0], tUnionDataType);
        }

        public static ITypeSymbol GetUnionDataTypeInRoomType(ITypeSymbol typeSymbol)
        {
            if (typeSymbol == null)
            {
                return null;
            }
            var nss = typeSymbol.ContainingAssembly.GlobalNamespace.GetNamespaceMembers();
            var mems = nss.Select(ns => ns.GetTypeMembers());
            var memsA = mems.Aggregate((f1, f2) => ImmutableArray.Create(f1.Concat(f2).ToArray()));
            var type = memsA.FirstOrDefault(namedType => namedType.TypeKind == TypeKind.Class && IsAbstractPlayer(namedType));
            if (type != null)
            {
                return GetUnionTypeOfPlayer(type);
            }
            return null;

            //if (typeSymbol.Name == SenRoomClassName && typeSymbol.ContainingAssembly.Name == SenGrains)
            //{
            //    if (typeSymbol.GetMembers().FirstOrDefault(t => t.Name == HandleMessage) is IMethodSymbol method
            //        && method.ReturnType is INamedTypeSymbol valueTask)
            //    {
            //        return valueTask.TypeArguments[0];
            //    }
            //    return null;
            //}
            //return GetUnionDataTypeInRoomType(typeSymbol.BaseType);
        }
    }
}
