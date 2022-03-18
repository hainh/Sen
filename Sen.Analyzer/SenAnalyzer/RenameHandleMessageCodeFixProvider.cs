using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace SenAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RenameHandleMessageCodeFixProvider)), Shared]
    public class RenameHandleMessageCodeFixProvider : CodeFixProvider
    {
        private const string title = "Change {} to HandleMessage";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get 
            { 
                return ImmutableArray.Create(
                    HandleMessageMethodAnalyzer.RenameToHandleMessageDiagnosticId);
            }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var renameDiagnostic = context.Diagnostics.FirstOrDefault(d => d.Id == HandleMessageMethodAnalyzer.RenameToHandleMessageDiagnosticId);
            if (renameDiagnostic != null)
            {
                var diagnosticSpan = renameDiagnostic.Location.SourceSpan;

                // Find the type declaration identified by the diagnostic.
                SyntaxNode declaration = root.FindToken(diagnosticSpan.Start).Parent;

                string title = string.Format(RenameHandleMessageCodeFixProvider.title, ((MethodDeclarationSyntax)declaration).Identifier.Value);
                // Register a code action that will invoke the fix.
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedSolution: c => RenameHandleMessageAsync(context.Document, declaration, c),
                        equivalenceKey: title),
                    renameDiagnostic);
            }

        }

        private async Task<Solution> RenameHandleMessageAsync(Document document, SyntaxNode methodDecl, CancellationToken cancellationToken)
        {
            var newName = HandleMessageMethodAnalyzer.HandleMessage;

            // Get the symbol representing the type to be renamed.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetDeclaredSymbol(methodDecl, cancellationToken);

            // Produce a new solution that has all references to that type renamed, including the declaration.
            var originalSolution = document.Project.Solution;
            var optionSet = originalSolution.Workspace.Options;
            var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, newName, optionSet, cancellationToken).ConfigureAwait(false);

            // Return the new solution with the now-renamed type name.
            return newSolution;
        }
    }
}
