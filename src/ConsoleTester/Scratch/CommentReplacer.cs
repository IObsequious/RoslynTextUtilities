using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConsoleTester.Scratch
{
    public class CommentReplacer : CSharpSyntaxRewriter
    {
        public static TNode ReplaceComments<TNode>(TNode node) where TNode: SyntaxNode
        {
            if (node is null) throw new ArgumentNullException(nameof(node));
            CommentReplacer replacer = new CommentReplacer();
            return (TNode) replacer.Visit(node);
        }

        //public override SyntaxToken VisitToken(SyntaxToken token)
        //{
        //    SyntaxToken newToken = base.VisitToken(token);

        //    if (newToken.HasTrailingTrivia)
        //    {
        //        foreach (SyntaxTrivia trivia in newToken.TrailingTrivia)
        //        {
        //            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
        //            {
        //                SyntaxToken nextToken = newToken.GetNextToken();

        //                if (nextToken.HasLeadingTrivia)
        //                {
        //                    foreach (SyntaxTrivia x_trivia in nextToken.LeadingTrivia)
        //                    {
        //                        if (x_trivia.IsKind(SyntaxKind.EndOfLineTrivia))
        //                        {
        //                            Console.WriteLine($"    Removing extra line break.");
        //                            return newToken.WithTrailingTrivia(SyntaxTriviaList.Empty);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    return newToken;
        //}

        public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
        {
            SyntaxTrivia newTrivia = base.VisitTrivia(trivia);

            switch (newTrivia.Kind())
            {
                case SyntaxKind.MultiLineDocumentationCommentTrivia:
                case SyntaxKind.SingleLineDocumentationCommentTrivia:
                case SyntaxKind.MultiLineCommentTrivia:
                case SyntaxKind.SingleLineCommentTrivia:
                    return default(SyntaxTrivia);
            }

            return newTrivia;
        }
    }
}

