using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenAnalyzer.Utils
{
    class Utils
    {
        public static BaseTypeDeclarationSyntax AddAttribute(BaseTypeDeclarationSyntax declarationSyntax, AttributeListSyntax attributeList, int index)
        {
            bool hasLeadNewLine = declarationSyntax.GetLeadingTrivia().Any(SyntaxKind.EndOfLineTrivia);
            SyntaxTrivia newLineTrivia = SyntaxFactory.SyntaxTrivia(SyntaxKind.EndOfLineTrivia, Environment.NewLine);
            SyntaxTrivia spacesTrivia = declarationSyntax.GetLeadingTrivia().Last();
            if (index <= 0 || !hasLeadNewLine)
            {
                attributeList = attributeList.NormalizeWhitespace().WithLeadingTrivia(spacesTrivia);
            }
            else
            {
                attributeList = attributeList.NormalizeWhitespace().WithLeadingTrivia(newLineTrivia, spacesTrivia);
            }

            var attrLists = declarationSyntax.AttributeLists;
            declarationSyntax = declarationSyntax
                .RemoveNodes(attrLists, SyntaxRemoveOptions.KeepTrailingTrivia)
                .WithLeadingTrivia(newLineTrivia, spacesTrivia);
            if (index < 0)
            {
                attrLists = attrLists.Add(attributeList);
            }
            else
            {
                attrLists = attrLists.Insert(index, attributeList);
            }

            return declarationSyntax.WithAttributeLists(attrLists);
        }
    }
}
