// -----------------------------------------------------------------------
// <copyright file="Refactory.cs" company="Ollon, LLC">
//     Copyright (c) 2018 Ollon, LLC. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ConsoleTester.Logging;
using ConsoleTester.Scratch;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace ConsoleTester.Refactoring
{
    public static partial class Refactory
    {
        private class CommentRemover : CSharpSyntaxRewriter
        {
            internal CommentRemover(SyntaxNode node, TextSpan span)
                : base(visitIntoStructuredTrivia: true)
            {
                Node = node;
                Span = span;
            }

            public SyntaxNode Node { get; }

            public TextSpan Span { get; }


            private SyntaxTrivia EmptyWhitespace() => SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, string.Empty);

            public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
            {
                TextSpan span = trivia.Span;

                if (Span.Contains(span))
                {
                    switch (trivia.Kind())
                    {
                        case SyntaxKind.SingleLineCommentTrivia:
                        case SyntaxKind.MultiLineCommentTrivia:
                        case SyntaxKind.SingleLineDocumentationCommentTrivia:
                        case SyntaxKind.MultiLineDocumentationCommentTrivia:
                            {
                                return EmptyWhitespace();
                            }
                        case SyntaxKind.EndOfLineTrivia:
                            {
                                if (ShouldRemoveEndOfLine(SyntaxKind.SingleLineCommentTrivia)
                                    && ShouldRemoveEndOfLine(SyntaxKind.WhitespaceTrivia)
                                    && ShouldRemoveEndOfLine(SyntaxKind.EndOfLineTrivia))
                                {
                                    return EmptyWhitespace();
                                }

                                break;
                            }
                    }
                }

                return base.VisitTrivia(trivia);

                bool ShouldRemoveEndOfLine(SyntaxKind kind)
                {
                    if (span.Start > 0)
                    {
                        SyntaxTrivia t = Node.FindTrivia(span.Start - 1);

                        if (t.Kind() != kind)
                            return false;

                        span = t.Span;
                    }

                    return true;
                }
            }
        }
    }
}
