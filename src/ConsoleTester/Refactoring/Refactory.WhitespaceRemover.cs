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
        internal class WhitespaceRemover : CSharpSyntaxRewriter
        {
            private WhitespaceRemover(TextSpan? span = null)
            {
                Span = span;
            }

            private static WhitespaceRemover Default { get; } = new WhitespaceRemover();

            public TextSpan? Span { get; }

            public static SyntaxTrivia Replacement { get; } = SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, "");

            public static WhitespaceRemover GetInstance(TextSpan? span = null)
            {
                if (span != null)
                {
                    return new WhitespaceRemover(span);
                }
                else
                {
                    return Default;
                }
            }

            public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
            {
                if ((trivia.IsKind(SyntaxKind.WhitespaceTrivia) || trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                    && (Span == null || Span.Value.Contains(trivia.Span)))
                {
                    return Replacement;
                }

                return base.VisitTrivia(trivia);
            }
        }
    }
}
