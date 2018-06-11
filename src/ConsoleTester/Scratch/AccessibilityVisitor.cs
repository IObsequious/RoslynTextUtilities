using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConsoleTester.Scratch
{
    public class AccessibilityVisitor : CSharpSyntaxRewriter
    {
        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            SyntaxToken newToken = base.VisitToken(token);

            SyntaxNode parentNode = newToken.Parent;

            if (parentNode.IsKind(SyntaxKind.ClassDeclaration)
                || parentNode.IsKind(SyntaxKind.InterfaceDeclaration)
                || parentNode.IsKind(SyntaxKind.StructDeclaration)
                || parentNode.IsKind(SyntaxKind.MethodDeclaration)
                || parentNode.IsKind(SyntaxKind.PropertyDeclaration)
                || parentNode.IsKind(SyntaxKind.ConstructorDeclaration)
                || parentNode.IsKind(SyntaxKind.DestructorDeclaration))
            {
                switch (newToken.Kind())
                {
                    case SyntaxKind.InternalKeyword:
                    {
                        SyntaxTriviaList leading = newToken.LeadingTrivia;
                        SyntaxTriviaList trailing = newToken.TrailingTrivia;
                        newToken = SyntaxFactory.Token(leading, SyntaxKind.PublicKeyword, trailing);
                        Console.WriteLine($"   --> Replacing {token.Span} '{token.ToFullString()}' with '{newToken.ToFullString()}'");
                        break;
                    }
                }
            }

            return newToken;
        }
    }
}
