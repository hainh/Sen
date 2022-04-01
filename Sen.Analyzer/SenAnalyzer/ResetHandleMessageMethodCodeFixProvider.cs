using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Rename;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SenAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RenameHandleMessageCodeFixProvider)), Shared]
    public class ResetHandleMessageMethodCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(
                    HandleMessageMethodAnalyzer.HandleMessageMustBePublicDiagnosticId,
                    HandleMessageMethodAnalyzer.HandleMessageSignatureReturnTypeDiagnosticId,
                    HandleMessageMethodAnalyzer.HandleMessageSignatureArg0DiagnosticId,
                    HandleMessageMethodAnalyzer.HandleMessageSignatureArg1DiagnosticId,
                    HandleMessageMethodAnalyzer.HandleMessageSignatureArg2DiagnosticId,
                    HandleMessageMethodAnalyzer.HandleMessageSignatureArgLengthDiagnosticId,
                    HandleMessageMethodAnalyzer.HandleMessageCreateMethodDiagnosticId);
            }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var resetDiagnostic = context.Diagnostics.Where(d => FixableDiagnosticIds.IndexOf(d.Id) >= 0);
            if (resetDiagnostic.Any())
            {
                var doc = context.Document;
                var root = await doc.GetSyntaxRootAsync(context.CancellationToken);
                var location = resetDiagnostic.First().AdditionalLocations[0];
                // Find the type declaration identified by the diagnostic.
                SyntaxNode declaration = root.FindToken(location.SourceSpan.Start).Parent;
                // Get the symbol representing the type to be renamed.
                var semanticModel = await doc.GetSemanticModelAsync();
                var symbol = semanticModel.GetDeclaredSymbol(declaration);
                INamedTypeSymbol containingClass = symbol.ContainingType;
                ITypeSymbol tUnionDataType = HandleMessageMethodAnalyzer.GetUnionTypeOfPlayer(containingClass);
                ITypeSymbol unionType = tUnionDataType ?? HandleMessageMethodAnalyzer.GetUnionDataTypeInRoomType(containingClass);
                List<ISymbol> messageTypes;
                if (symbol is IFieldSymbol)
                {
                    messageTypes = await GetAllUnhandledMessageTypes(doc.Project.Solution, unionType);
                }
                else
                {
                    var methodSymbol = symbol as IMethodSymbol;
                    messageTypes = methodSymbol.Parameters.Select(para =>
                    {
                        if (para.Type.BaseType != null && HandleMessageMethodAnalyzer.HasInterface(para.Type, unionType))
                        {
                            return (ISymbol)para.Type;
                        }
                        return null;
                    }).Where(t => t != null).ToList();
                    if (messageTypes.Count == 0)
                    {
                        messageTypes = await GetAllUnhandledMessageTypes(doc.Project.Solution, unionType);
                    }
                }
                if (messageTypes.Count == 0)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create($"Create a {unionType.Name} message type first", createChangedDocument: c => Task.FromResult(context.Document)),
                        resetDiagnostic);
                    return;
                }
                messageTypes.Sort((a, b) => a.Name.CompareTo(b.Name));
                string methodStr;
                if (HandleMessageMethodAnalyzer.IsAbstractPlayer(containingClass))
                {
                    methodStr = $"public ValueTask<{unionType.Name}> HandleMessage({{message}}, NetworkOptions networkOptions)";
                }
                else
                {
                    methodStr = $"public ValueTask<{unionType.Name}> HandleMessage({{message}}, IPlayer player, NetworkOptions networkOptions)";
                }
                // Register a code action that will invoke the fix.
                string title = symbol is IFieldSymbol ? "Generate HandleMessage" : "Reset HandleMessage";
                context.RegisterCodeFix(
                    CodeAction.Create(title, CreateNestedCodeActions(messageTypes, context, location, methodStr, symbol is IFieldSymbol), true),
                    resetDiagnostic);
            }
        }

        private static ImmutableArray<CodeAction> CreateNestedCodeActions(List<ISymbol> messageTypes, CodeFixContext context, Location location, string method, bool isCreating)
        {
            return ImmutableArray.Create(messageTypes.Select(msgType =>
            {
                string paramName = (msgType as ITypeSymbol).TypeKind == TypeKind.Interface
                    ? "message"
                    : msgType.Name.Replace(msgType.Name[0], char.ToLower(msgType.Name[0]));
                string newMethod = method.Replace("{message}", $"{msgType.Name} {paramName}");
                string title = $"With parameters ({msgType.Name} {paramName}, ...)" + (isCreating ? "" : " and return type ValueTask<>");
                return CodeAction.Create(
                    title: title,
                    createChangedSolution: c => ResetHandleMessageMethodAsync(context, location, newMethod, c),
                    equivalenceKey: "For " + msgType.Name);
            }).ToArray());
        }

        private static async Task<List<ISymbol>> GetAllUnhandledMessageTypes(Solution solution, ISymbol unionType)
        {
            Project messagesProject = solution.Projects.FirstOrDefault(proj => proj.ProjectReferences.Any(senInterfacesProject));
            IImmutableSet<Project> messageProjects = messagesProject == null ? null : ImmutableHashSet.Create(messagesProject);
            Project grainsProject = solution.Projects.FirstOrDefault(proj => proj.ProjectReferences.Any(senGrainsProject));
            IImmutableSet<Document> grainsDocuments = grainsProject == null ? null : ImmutableHashSet.Create(grainsProject.Documents.Where(doc => doc.Folders.FirstOrDefault() != "obj").ToArray());
            var msgTypes = await SymbolFinder.FindImplementationsAsync(unionType, solution, messageProjects);
            List<ISymbol> result = new();
            Dictionary<DocumentId, Root> roots = new();
            foreach (var msgType in msgTypes)
            {
                var refs = await SymbolFinder.FindReferencesAsync(msgType, solution, grainsDocuments);
                bool isHandleMessageRef = false;
                foreach(var refer in refs)
                {
                    if (await IsHandleMessageRef(refer))
                    {
                        isHandleMessageRef = true;
                        break;
                    }
                }
                if (!isHandleMessageRef)
                {
                    result.Add(msgType);
                }
            }
            return result;

            bool senInterfacesProject(ProjectReference reference)
            {
                return solution.Projects.Any(p => p.Id == reference.ProjectId && p.Name == Constants.SenInterfaces);
            }
            bool senGrainsProject(ProjectReference reference)
            {
                return solution.Projects.Any(p => p.Id == reference.ProjectId && p.Name == Constants.SenGrains);
            }
            async Task<bool> IsHandleMessageRef(ReferencedSymbol referencedSymbol)
            {
                if (referencedSymbol.Definition.Kind != SymbolKind.NamedType) return false;
                foreach (var location in referencedSymbol.Locations)
                {
                    if (!roots.TryGetValue(location.Document.Id, out Root root))
                    {
                        root = new Root(await location.Document.GetSyntaxRootAsync(), await location.Document.GetSemanticModelAsync());
                        roots.Add(location.Document.Id, root);
                    }
                    var containingNode = root.SyntaxNode.FindNode(location.Location.SourceSpan).Parent;
                    var method = GetOuterMethod(root, containingNode, HandleMessageMethodAnalyzer.HandleMessage);
                    if (method != null) return true;
                }
                return false;
            }
        }

        private static SyntaxNode GetOuterMethod(Root root, SyntaxNode inner, string methodName)
        {
            if (inner == null) return null;
            ISymbol symbol = root.SemanticModel.GetDeclaredSymbol(inner);
            if (symbol is INamespaceSymbol)
            {
                return null;
            }
            if (symbol is IMethodSymbol methodSymbol)
            {
                if (HandleMessageMethodAnalyzer.IsAbstractPlayer(methodSymbol.ContainingType) || HandleMessageMethodAnalyzer.IsAbstractRoom(methodSymbol.ContainingType))
                {
                    if (methodSymbol.Name == methodName)
                    {
                        return inner;
                    }
                }
                return null;
            }
            return GetOuterMethod(root, inner.Parent, methodName);
        }

        private static async Task<Solution> RenameHandleMessageAsync(Document document, SyntaxNode methodDecl, CancellationToken cancellationToken)
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
        
        private static async Task<Solution> ResetHandleMessageMethodAsync(CodeFixContext context, Location location, string method, CancellationToken c)
        {
            var doc = context.Document;
            var root = await doc.GetSyntaxRootAsync(c);
            SyntaxNode declaration = root.FindNode(location.SourceSpan);
            if (declaration.ToFullString().Trim().ToLower() == HandleMessageMethodAnalyzer.HandleMessageShort)
            {
                declaration = declaration.Parent.Parent; // declaration is a field
                string preSpaces = string.Join("", declaration.ToFullString().TakeWhile(c => char.IsWhiteSpace(c)));
                method = preSpaces + method + getMethodBody(method, preSpaces) + System.Environment.NewLine;
            }
            else if (declaration is MethodDeclarationSyntax methodDeclaration)
            {
                string preSpaces = string.Join("", declaration.ToFullString().TakeWhile(c => char.IsWhiteSpace(c)));
                method = preSpaces + method
                    + (methodDeclaration.Body == null 
                        ? getMethodBody(method, preSpaces)
                        : System.Environment.NewLine + methodDeclaration.Body.ToFullString())
                    + System.Environment.NewLine;
            }
            var methodDeclarationSyntax = SyntaxFactory.ParseMemberDeclaration(method);
            return doc.WithSyntaxRoot(root.ReplaceNode(declaration, methodDeclarationSyntax)).Project.Solution;

            static string getMethodBody(string method, string preSpaces)
            {
                string returnType = method.Substring(method.IndexOf("ValueTask"), method.IndexOf('>') - method.IndexOf("ValueTask") + 1)
                    .Replace("<", ".FromResult<");
                return $"{preSpaces}{{{preSpaces}    return {returnType}(null);{preSpaces}}}{preSpaces}";
            }
        }

        struct Root
        {
            public Root(SyntaxNode node, SemanticModel model)
            {
                SyntaxNode = node;
                SemanticModel = model;
            }
            public SyntaxNode SyntaxNode { get; }
            public SemanticModel SemanticModel { get; }
        }
    }
}
