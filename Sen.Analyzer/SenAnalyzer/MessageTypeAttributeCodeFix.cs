using System;
using System.Composition;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using SenAnalyzer.Utils;
using Microsoft.CodeAnalysis.FindSymbols;

namespace SenAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MessageTypeAttributeCodeFix)), Shared]
    public class MessageTypeAttributeCodeFix : CodeFixProvider
    {
        private const string title = "Add MessagePackObject attribute";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(MessageTypeAnalyzer.MessageTypeHasAttributeDiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var messageTypeAttrDiagnostic = context.Diagnostics.FirstOrDefault(d => d.Id == MessageTypeAnalyzer.MessageTypeHasAttributeDiagnosticId);
            if (messageTypeAttrDiagnostic != null)
            {
                var diagnosticSpan = messageTypeAttrDiagnostic.Location.SourceSpan;

                SyntaxNode declaration = root.FindToken(diagnosticSpan.Start).Parent;

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedSolution: c => AddAttribute(context.Document, declaration, c),
                        equivalenceKey: title),
                    messageTypeAttrDiagnostic);
            }
        }

        private async Task<Solution> AddAttribute(Document document, SyntaxNode declaration, CancellationToken cancellation)
        {
            var interfaceDeclarationSyntax = declaration as ClassDeclarationSyntax;
            var root = await document.GetSyntaxRootAsync(cancellation);

            AttributeListSyntax attribute = SyntaxFactory.ParseCompilationUnit("[MessagePackObject]")
                .DescendantNodes()
                .OfType<AttributeListSyntax>()
                .First();

            return document.WithSyntaxRoot(
                root.ReplaceNode(
                    interfaceDeclarationSyntax,
                    Utils.Utils.AddAttribute(interfaceDeclarationSyntax, attribute, -1)
                )).Project.Solution;
        }
    }
}
