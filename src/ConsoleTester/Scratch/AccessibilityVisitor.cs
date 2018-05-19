using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConsoleTester.Scratch
{
    public class AccessibilityVisitor : CSharpSyntaxRewriter
    {
        /// <summary>Called when the visitor visits a ClassDeclarationSyntax node.</summary>
        //public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        //{
        //    ClassDeclarationSyntax newNode = (ClassDeclarationSyntax) base.VisitClassDeclaration(node);

        //    if (!ContainsPublicModifier(newNode))
        //    {
        //        if (newNode.Modifiers.Count != 0)
        //        {
        //            SyntaxToken token = newNode.Modifiers.FirstOrDefault();
                
        //            newNode = newNode.ReplaceToken(token, VisitToken(token));
        //        }

        //    }

        //    return newNode;
        //}


        private static bool ContainsPublicModifier(BaseTypeDeclarationSyntax baseType)
        {
            var modifiers = baseType.Modifiers;

            for (int i = 0; i < modifiers.Count; i++)
            {
                SyntaxToken token = modifiers[i];
                SyntaxKind kind = token.Kind();
                if (kind == SyntaxKind.PublicKeyword)
                {
                    return true;
                }
            }
            return false;
        }


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
                    //case SyntaxKind.PrivateKeyword:
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
