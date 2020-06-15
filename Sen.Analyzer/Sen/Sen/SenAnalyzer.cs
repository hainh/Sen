using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sen
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SenAnalyzer : DiagnosticAnalyzer
    {
        public const string RenameToHandleMessageDiagnosticId = "Sen";
        public const string HandleMessage = "HandleMessage";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private const string RenameTitle = "Rename HandleMessage method name";
        private const string RenameMessageFormat = "Method {0} seem likes a message handler of {1}";
        private const string RenameDescription = "HandleMessage method name may be miss typed";

        private const string Category = "Naming";

        private static DiagnosticDescriptor HandleMessageNameRule = new DiagnosticDescriptor(
            RenameToHandleMessageDiagnosticId,
            RenameTitle,
            RenameMessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: RenameDescription);


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(HandleMessageNameRule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(AnalyzeMethodHandleMessageSymbol, SymbolKind.Method);
            context.RegisterSymbolAction(AnalyzeIncompleteHandleMessageName, SymbolKind.Property);
        }

        private static void AnalyzeMethodHandleMessageSymbol(SymbolAnalysisContext context)
        {
            var methodSymbol = (IMethodSymbol)context.Symbol;

            if (LevenshteinDistance.Compute(methodSymbol.Name, HandleMessage) <= 3)
            {
                var diagnostic = Diagnostic.Create(HandleMessageNameRule, methodSymbol.Locations[0], methodSymbol.Name, HandleMessage);

                context.ReportDiagnostic(diagnostic);
            }
        }

        private static void AnalyzeIncompleteHandleMessageName(SymbolAnalysisContext context)
        {
            var symbol = context.Symbol;
            var name = symbol.Name;
        }
    }
}
