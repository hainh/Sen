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

namespace SenAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MessageTypeUnionAttributeCodeFix)), Shared]
    public class MessageTypeUnionAttributeCodeFix : CodeFixProvider
    {
        private const string title = "Register as a Union message type";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(MessageTypeAnalyzer.MessageTypeMustBeDeclaredInUnionDiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var unionAttrDiagnostic = context.Diagnostics.FirstOrDefault(d => d.Id == MessageTypeAnalyzer.MessageTypeMustBeDeclaredInUnionDiagnosticId);
            if (unionAttrDiagnostic != null)
            {
                var diagnosticSpan = unionAttrDiagnostic.Location.SourceSpan;
                var properties = unionAttrDiagnostic.Properties;

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedSolution: c => AddAttribute(context.Document, properties, c),
                        equivalenceKey: title),
                    unionAttrDiagnostic);
            }
            return Task.CompletedTask;
        }

        private async Task<Solution> AddAttribute(Document document, ImmutableDictionary<string, string> properties, CancellationToken cancellation)
        {
            int key = int.Parse(properties["key"]);
            string messageType = properties["type"];
            string unionType = properties["interface"];
            string file = properties["file"];
            string assembly = properties["assembly"];

            if (document.Project.AssemblyName != assembly)
            {
                var projectContainsUnion = document.Project.Solution.Projects.FirstOrDefault(p => p.AssemblyName == assembly);
                if (projectContainsUnion == null)
                {
                    return document.Project.Solution;
                }
                var documentContainsUnion = projectContainsUnion.Documents.FirstOrDefault(d => d.FilePath == file);
                if (documentContainsUnion == null)
                {
                    return document.Project.Solution;
                }
                document = documentContainsUnion;
            }
            else if (document.FilePath != file)
            {
                var documentContainsUnion = document.Project.Documents.FirstOrDefault(d => d.FilePath == file);
                if (documentContainsUnion == null)
                {
                    return document.Project.Solution;
                }
                document = documentContainsUnion;
            }
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellation);

            InterfaceDeclarationSyntax unionInterfaceNode = FindUnionInterface(root, unionType);

            AttributeListSyntax attribute = SyntaxFactory.ParseCompilationUnit($"[Union({key}, typeof({messageType}))]")
                .DescendantNodes()
                .OfType<AttributeListSyntax>()
                .First();

            return document.WithSyntaxRoot(
                root.ReplaceNode(
                    unionInterfaceNode,
                    Utils.Utils.AddAttribute(unionInterfaceNode, attribute, key)
                )).Project.Solution;
        }

        static InterfaceDeclarationSyntax FindUnionInterface(SyntaxNode root, string interfaceName)
        {
            if (root.ChildNodes().Count() == 0)
            {
                return null;
            }

            SyntaxNode node = root
                .ChildNodes()
                .FirstOrDefault(syntaxNode => syntaxNode is InterfaceDeclarationSyntax @interface
                    && @interface.Identifier.ValueText == interfaceName);
            if (node != null)
            {
                return node as InterfaceDeclarationSyntax;
            }

            foreach (var child in root.ChildNodes())
            {
                node = FindUnionInterface(child, interfaceName);
                if (node != null)
                {
                    return node as InterfaceDeclarationSyntax;
                }
            }

            return null;
        }
    }
}
