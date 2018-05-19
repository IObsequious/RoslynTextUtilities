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
        private static readonly SyntaxAnnotation[] _formatterAnnotationArray = new SyntaxAnnotation[] { Formatter.Annotation };
        private static readonly SyntaxAnnotation[] _simplifierAnnotationArray = new SyntaxAnnotation[] { Simplifier.Annotation };
        private static readonly SyntaxAnnotation[] _renameAnnotationArray = new SyntaxAnnotation[] { RenameAnnotation.Create() };
        private static readonly SyntaxAnnotation[] _formatterAndSimplifierAnnotationArray = new SyntaxAnnotation[] { Formatter.Annotation, Simplifier.Annotation };

        private static SymbolDisplayFormat DefaultSymbolDisplayFormat { get; } = new SymbolDisplayFormat(
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
                                  | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

        private static SymbolDisplayFormat DisplayFormat { get; } = new SymbolDisplayFormat(
            SymbolDisplayGlobalNamespaceStyle.Omitted,
            SymbolDisplayTypeQualificationStyle.NameOnly,
            SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints,
            SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeType,
            SymbolDisplayDelegateStyle.NameOnly,
            SymbolDisplayExtensionMethodStyle.Default,
            SymbolDisplayParameterOptions.None,
            SymbolDisplayPropertyStyle.NameOnly,
            SymbolDisplayLocalOptions.IncludeType,
            SymbolDisplayKindOptions.None,
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

        public static async Task<Document> RefactorAsync(Document document, CancellationToken cancellationToken = default(CancellationToken))
        {
            Logger.LogInformation($"Processing document {document.Name}...");
            // document = await SetPublicAccessibility(document, cancellationToken).ConfigureAwait(false);
            document = await RemoveCommentsAsync(document, cancellationToken).ConfigureAwait(false);
            // document = await UseAutoProperty(document, cancellationToken).ConfigureAwait(false);
            document = await RemoveEmptyLinesAsync(document, cancellationToken).ConfigureAwait(false);
            document = await BlankLineBetweenMembersAsync(document, cancellationToken).ConfigureAwait(false);

            document = await Formatter.FormatAsync(document, FormattingOptions, cancellationToken);

            //document = await FormatDocumentAsync(document, cancellationToken).ConfigureAwait(false);
            Logger.LogInformation("");
            return document;
        }

        public static async Task<Document> FixAsync(Document document, CancellationToken cancellationToken = default(CancellationToken))
        {
            Logger.LogWarning($"    FixAsync => {document.Name}");
            SemanticDocument semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken);

            CompilationUnitSyntax newRoot = semanticDocument.CompilationUnitRoot;

            SymbolCollector collector = new SymbolCollector();

            NamespaceDeclarationSyntax firstNamespace = newRoot.DescendantNodes(_ => true).OfType<NamespaceDeclarationSyntax>().First();
            NamespaceDeclarationSyntax newNamespace = firstNamespace.WithMembers(default);
            INamespaceSymbol namespaceSymbol = semanticDocument.SemanticModel.GetDeclaredSymbol(firstNamespace);
            collector.VisitNamespace(namespaceSymbol);

            foreach (CollectionEntry entry in collector.Types)
            {
                newNamespace = newNamespace.AddMembers(CreateBaseTypeDeclaration(entry.Class));
            }

            newRoot = newRoot.ReplaceNode(firstNamespace, newNamespace);

            return await Formatter.FormatAsync(document.WithSyntaxRoot(newRoot.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System.Collections.Generic")))), FormattingOptions, cancellationToken);

        }

        private static BaseTypeDeclarationSyntax CreateBaseTypeDeclaration(INamedTypeSymbol symbol)
        {
            BaseTypeDeclarationSyntax typeDeclaration = null;

            if (symbol.TypeKind == TypeKind.Class)
            {
                ClassDeclarationSyntax node = SyntaxFactory.ClassDeclaration(symbol.Name);
                node = node.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.PartialKeyword));

                var members = symbol.GetMembers().OfType<IPropertySymbol>().ToImmutableArray();
                for (int i = 0; i < members.Length; i++)
                {
                    IPropertySymbol member = members[i];
                    if (member.Name != ".ctor" && !member.Name.EndsWith("Specified", StringComparison.CurrentCulture))
                    {
                        string propertyTypeName = member.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

                        propertyTypeName = MakePropertyTypeName(propertyTypeName);

                        node = node.AddMembers(SyntaxFactory.PropertyDeclaration(
                            default,
                            SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                            SyntaxFactory.ParseTypeName(propertyTypeName),
                            null,
                            SyntaxFactory.Identifier(member.Name),
                            AutoGetSetList,
                            null,
                            null
                        ).WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed));
                    }
                }

                typeDeclaration = node;
            }

            if (symbol.TypeKind == TypeKind.Enum)
            {
                EnumDeclarationSyntax node = SyntaxFactory.EnumDeclaration(symbol.Name);
                node = node.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
                var members = symbol.GetMembers();
                for (int i = 0; i < members.Length; i++)
                {
                    ISymbol member = members[i];
                    if (member.Name != ".ctor")
                        node = node.AddMembers(SyntaxFactory.EnumMemberDeclaration(SyntaxFactory.Identifier(member.Name)));
                }

                typeDeclaration = node;
            }

            return typeDeclaration.WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed, SyntaxFactory.CarriageReturnLineFeed)
                ;
        }

        private static string MakePropertyTypeName(string propertyTypeName)
        {
            if (propertyTypeName.EndsWith("[]", StringComparison.CurrentCulture))
            {
                string innerName = propertyTypeName.TrimEnd('[', ']');
                propertyTypeName = $"List<{innerName}>";
            }

            return propertyTypeName;
        }

        private static TNode RemoveComments<TNode>(TNode node) where TNode : SyntaxNode
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            return RemoveComments(node, node.FullSpan);
        }

        private static TNode RemoveComments<TNode>(TNode node, TextSpan span) where TNode : SyntaxNode
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            var remover = new CommentRemover(node, span);

            return (TNode)remover.Visit(node);
        }

        private static TNode BlankLineBetweenMembers<TNode>(TNode node) where TNode : SyntaxNode
        {
            BlankLineAdder adder = new BlankLineAdder();
            return (TNode)adder.Visit(node);
        }

        private static TNode SetPublicAccessibility<TNode>(TNode node) where TNode : SyntaxNode
        {
            AccessibilityVisitor visitor = new AccessibilityVisitor();
            return (TNode)visitor.Visit(node);
        }

        private static readonly SyntaxAnnotation _removeAnnotation = new SyntaxAnnotation();
        private static readonly SymbolDisplayFormat _symbolDisplayFormat = new SymbolDisplayFormat(
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

        private static int ii = 0;
        private static int jj = 0;
        private static int kk = 0;

        public static async Task<Document> UseAutoProperty(
            Document document,
            CancellationToken cancellationToken)
        {
            Logger.LogWarning($"    Use AutoProperty => {document.Name}");
            SemanticDocument semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken);

            top:

            SyntaxNode newRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            NamespaceDeclarationSyntax[] namespaces = newRoot.DescendantNodes(_ => true).OfType<NamespaceDeclarationSyntax>().ToArray();

            NamespaceDeclarationSyntax firstNamespace = namespaces.First();

            TypeDeclarationSyntax[] types = firstNamespace.DescendantNodes(_ => true).OfType<TypeDeclarationSyntax>().ToArray();

            for (int j = jj; j < types.Length; j++)
            {

                TypeDeclarationSyntax typeDeclaration = types[j];

                INamedTypeSymbol namedTypeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration);

                Console.WriteLine($"    Processing {namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}...");

                PropertyDeclarationSyntax[] properties = typeDeclaration.Members.OfType<PropertyDeclarationSyntax>().ToArray();




                for (int k = kk; k < properties.Length; k++)
                {
                    PropertyDeclarationSyntax property = properties[k];



                    IPropertySymbol propertySymbol = semanticModel.GetDeclaredSymbol(property);

                    Console.WriteLine($"        -- {propertySymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");

                    PropertyDeclarationSyntax newProperty = property.WithAccessorList(AutoGetSetList);

                    newRoot = newRoot.ReplaceNode(property, newProperty.WithIdentifier(newProperty.Identifier.WithTrailingTrivia(SyntaxFactory.Space)));

                    document = document.WithSyntaxRoot(newRoot);

                    jj = j;

                    kk++;

                    if (kk >= properties.Length)
                    {
                        kk = 0;
                        jj++;
                    }

                    goto top;

                }

                jj++;
            }


            return document.WithSyntaxRoot(newRoot);
        }

        private static AccessorListSyntax AutoGetSetList
        {
            get
            {
                return SyntaxFactory.AccessorList(
                        SyntaxFactory.List<AccessorDeclarationSyntax>(
                            new AccessorDeclarationSyntax[]
                            {
                                    SyntaxFactory.AccessorDeclaration(
                                            SyntaxKind.GetAccessorDeclaration)
                                        .WithSemicolonToken(
                                            SyntaxFactory.Token(
                                                SyntaxFactory.TriviaList(),
                                                SyntaxKind.SemicolonToken,
                                                SyntaxFactory.TriviaList(
                                                    SyntaxFactory.Space))),
                                    SyntaxFactory.AccessorDeclaration(
                                            SyntaxKind.SetAccessorDeclaration)
                                        .WithSemicolonToken(
                                            SyntaxFactory.Token(
                                                SyntaxFactory.TriviaList(),
                                                SyntaxKind.SemicolonToken,
                                                SyntaxFactory.TriviaList(
                                                    SyntaxFactory.Space)))
                            }))
                    .WithOpenBraceToken(
                        SyntaxFactory.Token(
                            SyntaxFactory.TriviaList(),
                            SyntaxKind.OpenBraceToken,
                            SyntaxFactory.TriviaList(
                                SyntaxFactory.Space)))
                    .WithCloseBraceToken(
                        SyntaxFactory.Token(
                            SyntaxFactory.TriviaList(),
                            SyntaxKind.CloseBraceToken,
                            SyntaxFactory.TriviaList(
                                SyntaxFactory.LineFeed)));
            }
        }


        public static async Task<Document> SetPublicAccessibility(Document document, CancellationToken cancellationToken = new CancellationToken())
        {
            Logger.LogWarning($"    Set Public Accessibility => {document.Name}");
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return document.WithSyntaxRoot(SetPublicAccessibility(root));
        }

        public static async Task<Document> BlankLineBetweenMembersAsync(Document document, CancellationToken cancellationToken = new CancellationToken())
        {
            Logger.LogWarning($"    Blank Line Between Members => {document.Name}");
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return document.WithSyntaxRoot(BlankLineBetweenMembers(root));
        }

        public static async Task<Document> RemoveCommentsAsync(Document document, CancellationToken cancellationToken = new CancellationToken())
        {
            Logger.LogWarning($"    Remove comments => {document.Name}");
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return document.WithSyntaxRoot(RemoveComments(root));

        }

        public static async Task<Document> FormatDocumentAsync(Document document, CancellationToken cancellationToken = new CancellationToken())
        {

            Logger.LogWarning($"    Format document => {document.Name}");

            return await FormattingVisitor.FormatAsync(document, cancellationToken);
        }
        public static async Task<Document> RemoveEmptyLinesAsync(Document document, CancellationToken cancellationToken = new CancellationToken())
        {
            Logger.LogWarning($"    Remove Empty Lines => {document.Name}");
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            SourceText sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            IEnumerable<TextChange> textChanges = GetEmptyLines(sourceText, root, root.Span)
                .Select(line => new TextChange(line.SpanIncludingLineBreak, ""));
            SourceText newSourceText = sourceText.WithChanges(textChanges);
            return document.WithText(newSourceText);
        }

        public static TNode WithFormatterAnnotation<TNode>(this TNode node) where TNode : SyntaxNode
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            return node.WithAdditionalAnnotations(_formatterAnnotationArray);
        }

        /// <summary>
        /// Returns property get accessor, if any.
        /// </summary>
        /// <param name="propertyDeclaration"></param>
        /// <returns></returns>
        public static AccessorDeclarationSyntax Getter(this PropertyDeclarationSyntax propertyDeclaration)
        {
            if (propertyDeclaration == null)
                throw new ArgumentNullException(nameof(propertyDeclaration));

            return propertyDeclaration.AccessorList?.Getter();
        }

        /// <summary>
        /// Returns property set accessor, if any.
        /// </summary>
        /// <param name="propertyDeclaration"></param>
        /// <returns></returns>
        public static AccessorDeclarationSyntax Setter(this PropertyDeclarationSyntax propertyDeclaration)
        {
            if (propertyDeclaration == null)
                throw new ArgumentNullException(nameof(propertyDeclaration));

            return propertyDeclaration.AccessorList?.Setter();
        }
        public static async Task<Document> ReplaceNodeAsync(
            this Document document,
            SyntaxNode oldNode,
            SyntaxNode newNode,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            if (oldNode == null)
                throw new ArgumentNullException(nameof(oldNode));

            if (newNode == null)
                throw new ArgumentNullException(nameof(newNode));

            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            SyntaxNode newRoot = root.ReplaceNode(oldNode, newNode);

            return document.WithSyntaxRoot(newRoot);
        }

        #region AccessorDeclarationSyntax
        /// <summary>
        /// Returns true is the specified accessor is auto-implemented accessor.
        /// </summary>
        /// <param name="accessorDeclaration"></param>
        /// <returns></returns>
        public static bool IsAutoImplemented(this AccessorDeclarationSyntax accessorDeclaration)
        {
            return accessorDeclaration?.SemicolonToken.Kind() == SyntaxKind.SemicolonToken
                && accessorDeclaration.BodyOrExpressionBody() == null;
        }

        /// <summary>
        /// Returns accessor body or an expression body if the body is null.
        /// </summary>
        /// <param name="accessorDeclaration"></param>
        /// <returns></returns>
        public static CSharpSyntaxNode BodyOrExpressionBody(this AccessorDeclarationSyntax accessorDeclaration)
        {
            if (accessorDeclaration == null)
                throw new ArgumentNullException(nameof(accessorDeclaration));

            return accessorDeclaration.Body ?? (CSharpSyntaxNode)accessorDeclaration.ExpressionBody;
        }
        #endregion AccessorDeclarationSyntax

        #region AccessorListSyntax
        /// <summary>
        /// Returns a get accessor contained in the specified list.
        /// </summary>
        /// <param name="accessorList"></param>
        /// <returns></returns>
        public static AccessorDeclarationSyntax Getter(this AccessorListSyntax accessorList)
        {
            return Accessor(accessorList, SyntaxKind.GetAccessorDeclaration);
        }

        /// <summary>
        /// Returns a set accessor contained in the specified list.
        /// </summary>
        /// <param name="accessorList"></param>
        /// <returns></returns>
        public static AccessorDeclarationSyntax Setter(this AccessorListSyntax accessorList)
        {
            return Accessor(accessorList, SyntaxKind.SetAccessorDeclaration);
        }

        private static AccessorDeclarationSyntax Accessor(this AccessorListSyntax accessorList, SyntaxKind kind)
        {
            if (accessorList == null)
                throw new ArgumentNullException(nameof(accessorList));

            return accessorList
                .Accessors
                .FirstOrDefault(accessor => accessor.IsKind(kind));
        }
        #endregion AccessorListSyntax



        private static readonly SyntaxLogger Logger = new SyntaxLogger(Console.Out);
        public static OptionSet FormattingOptions
        {
            get
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
                options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInProperties, false);
                options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInTypes, true);
                options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInMethods, true);
                options = options.WithChangedOption(CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine, true);
                options = options.WithChangedOption(CSharpFormattingOptions.WrappingPreserveSingleLine, true);
                options = options.WithChangedOption(CSharpFormattingOptions.NewLineForMembersInObjectInit, true);

                return options;
            }
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

        private static TNode AddLeadingTrivia<TNode>(this TNode node, params SyntaxTrivia[] trivias) where TNode : SyntaxNode
        {
            SyntaxTriviaList leading = node.GetLeadingTrivia();
            leading = leading.AddRange(trivias);
            return node.WithLeadingTrivia(leading);
        }

        public static TNode AddLeadingBlankLine<TNode>(this TNode node) where TNode : SyntaxNode
        {
            return node.AddLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed);
        }
    }
}
