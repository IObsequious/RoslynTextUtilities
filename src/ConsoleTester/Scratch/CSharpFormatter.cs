using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace ConsoleTester.Scratch
{
    public class CSharpFormatter : CSharpSyntaxRewriter
    {
        public CSharpFormatter(bool visitIntoStructuredTrivia = true) : base(visitIntoStructuredTrivia)
        {
        }

        public static TNode Format<TNode>(TNode node) where TNode : SyntaxNode
        {
            AdhocWorkspace workspace = new AdhocWorkspace();

            CSharpFormatter formatter = new CSharpFormatter();
            TNode newNode = (TNode) formatter.Visit(node);

            newNode = (TNode) Formatter.Format(newNode, Formatter.Annotation, workspace);

            return (TNode) formatter.Visit(newNode);
        }

        public static async Task<TNode> FormatAsync<TNode>(TNode node) where TNode: SyntaxNode
        {
            AdhocWorkspace workspace = new AdhocWorkspace();
            OptionSet options = workspace.Options;
            options = options.WithChangedOption(CSharpFormattingOptions.SpaceAfterComma, true);
            options = options.WithChangedOption(CSharpFormattingOptions.NewLineForMembersInObjectInit, true);
            options = options.WithChangedOption(CSharpFormattingOptions.NewLineForCatch, true);
            options = options.WithChangedOption(CSharpFormattingOptions.NewLineForClausesInQuery, true);
            options = options.WithChangedOption(CSharpFormattingOptions.NewLineForElse, true);
            options = options.WithChangedOption(CSharpFormattingOptions.NewLineForFinally, true);
            options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInAccessors, true);
            options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInProperties, true);
            options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInTypes, true);
            options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInMethods, true);
            options = options.WithChangedOption(CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine, false);
            options = options.WithChangedOption(CSharpFormattingOptions.WrappingPreserveSingleLine, false);
            options = options.WithChangedOption(CSharpFormattingOptions.NewLineForMembersInObjectInit, true);

            Solution solution = workspace.CurrentSolution;
            Project project = solution.AddProject("X", "X", LanguageNames.CSharp);
            solution = project.Solution;
            DocumentId documentId = DocumentId.CreateNewId(project.Id);
            Document document = solution.AddDocument(documentId, "Node.cs", node).GetDocument(documentId);

            document = await FormatAsync(document, CancellationToken.None).ConfigureAwait(false);

            document = await Formatter.FormatAsync(document,Formatter.Annotation,options).ConfigureAwait(false);

            return (TNode) await document.GetSyntaxRootAsync().ConfigureAwait(false);
        }

        public static async Task<Document> FormatAsync(Document document, CancellationToken cancellationToken = new CancellationToken())
        {
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            SourceText sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            IEnumerable<TextChange> textChanges = GetEmptyLines(sourceText, root, root.Span)
                .Select(line => new TextChange(line.SpanIncludingLineBreak, ""));
            SourceText newSourceText = sourceText.WithChanges(textChanges);
            return document.WithText(newSourceText);
        }

        private static IEnumerable<TextLine> GetEmptyLines(SourceText sourceText, SyntaxNode root, TextSpan span)
        {
            foreach (TextLine line in sourceText
                .Lines
                .SkipWhile(f => f.Start < span.Start)
                .TakeWhile(f => f.EndIncludingLineBreak <= span.End))
            {
                if (line.Span.Length == 0
                    || IsWhitespace(line.ToString()))
                {
                    SyntaxTrivia endOfLine = root.FindTrivia(line.End, findInsideTrivia: true);

                    if (endOfLine.Kind() == SyntaxKind.EndOfLineTrivia)
                        yield return line;
                }
            }
        }

        public static bool IsWhitespace(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsWhiteSpace(value[i]))
                    return false;
            }

            return true;
        }

        public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
        {
            switch (trivia.Kind())
            {
                case SyntaxKind.MultiLineDocumentationCommentTrivia:
                case SyntaxKind.SingleLineDocumentationCommentTrivia:
                case SyntaxKind.MultiLineCommentTrivia:
                case SyntaxKind.SingleLineCommentTrivia:
                    return default;
                case SyntaxKind.EndOfLineTrivia:
                    return trivia.WithAdditionalAnnotations(Formatter.Annotation);
            }

            return trivia;
        }
    }

    public class SyntaxLoggerVisitor : CSharpSyntaxRewriter
    {
        /// <summary>Called when the visitor visits a CompilationUnitSyntax node.</summary>
        /// <param name="node"></param>
        public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
        {
            LogNode(node);

            return base.VisitCompilationUnit(node);
        }

        /// <summary>Called when the visitor visits a NamespaceDeclarationSyntax node.</summary>
        /// <param name="node"></param>
        public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            LogNode(node);

            return base.VisitNamespaceDeclaration(node);
        }

        /// <summary>Called when the visitor visits a ClassDeclarationSyntax node.</summary>
        /// <param name="node"></param>
        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            LogNode(node);

            return base.VisitClassDeclaration(node);
        }

        /// <summary>Called when the visitor visits a MethodDeclarationSyntax node.</summary>
        /// <param name="node"></param>
        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            LogNode(node);

            return base.VisitMethodDeclaration(node);
        }

        /// <summary>Called when the visitor visits a PropertyDeclarationSyntax node.</summary>
        /// <param name="node"></param>
        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            LogNode(node);

            return base.VisitPropertyDeclaration(node);
        }

        public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
        {
            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                LogTrivia(trivia);
                trivia = trivia.WithAdditionalAnnotations(Formatter.Annotation);
            }

            switch (trivia.Kind())
            {
                case SyntaxKind.MultiLineDocumentationCommentTrivia:
                case SyntaxKind.SingleLineDocumentationCommentTrivia:
                case SyntaxKind.MultiLineCommentTrivia:
                case SyntaxKind.SingleLineCommentTrivia:
                    return default;
                case SyntaxKind.EndOfLineTrivia:
                    return trivia.WithAdditionalAnnotations(Formatter.Annotation);
            }

            return trivia;
        }

        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            // LogToken(token);

            return base.VisitToken(token);
        }

        public void LogTrivia(SyntaxTrivia trivia)
        {
            Console.WriteLine($"{trivia.Kind()} {trivia.Span}");
        }

        public void LogToken(SyntaxToken token)
        {
            Console.WriteLine($"{token.Kind()} {token.Span}");
        }

        public void LogNode(SyntaxNode node)
        {
            Console.WriteLine($"{node.Kind()} {node.Span}");
        }
    }
}
