using System;
using System.Collections.Immutable;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConsoleTester.Refactoring
{
    public class FormattingVisitor : CSharpSyntaxRewriter
    {
        private int _namespaceNestLevel = 0;
        private int _typeNestLevel = 0;
        private SemanticModel _semanticModel;

        private static readonly SymbolDisplayFormat TypeNameFormat = new SymbolDisplayFormat(
            SymbolDisplayGlobalNamespaceStyle.Omitted,
            SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            SymbolDisplayGenericsOptions.None,
            SymbolDisplayMemberOptions.None,
            SymbolDisplayDelegateStyle.NameOnly,
            SymbolDisplayExtensionMethodStyle.Default,
            SymbolDisplayParameterOptions.None,
            SymbolDisplayPropertyStyle.NameOnly,
            SymbolDisplayLocalOptions.None,
            SymbolDisplayKindOptions.None,
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        public FormattingVisitor(SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
        }

        public static async Task<Document> FormatAsync(Document document, CancellationToken cancellationToken = default)
        {
            SemanticDocument semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken);

            FormattingVisitor visitor = new FormattingVisitor(semanticDocument.SemanticModel);

            SyntaxNode newRoot = visitor.Visit(semanticDocument.Root);

            return document.WithSyntaxRoot(newRoot);
        }

        /// <summary>Called when the visitor visits a NamespaceDeclarationSyntax node.</summary>
        public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            _namespaceNestLevel++;
            NamespaceDeclarationSyntax newNode = (NamespaceDeclarationSyntax) base.VisitNamespaceDeclaration(node);
            _namespaceNestLevel--;

            return newNode;
        }

        /// <summary>Called when the visitor visits a ClassDeclarationSyntax node.</summary>
        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            _typeNestLevel++;

            INamedTypeSymbol classSymbol = _semanticModel.GetDeclaredSymbol(node);

            ClassDeclarationSyntax newNode = (ClassDeclarationSyntax) base.VisitClassDeclaration(node);

            newNode = newNode.WithAttributeLists(SyntaxFactory.List<AttributeListSyntax>());

            newNode = newNode.WithMembers(VisitClassMembers(newNode.Members));

            newNode = newNode.AddMembers(CreateConstructor(classSymbol));

            _typeNestLevel--;
            return newNode;
        }


        private ConstructorDeclarationSyntax CreateConstructor(INamedTypeSymbol symbol)
        {
            ConstructorDeclarationSyntax ctor = SyntaxFactory.ConstructorDeclaration(SyntaxFactory.Identifier(symbol.Name));
            ctor = ctor.WithModifiers(SyntaxFactory.TokenList(WithIndentation(SyntaxFactory.Token(SyntaxKind.PublicKeyword))));
            BlockSyntax body = SyntaxFactory.Block();
            SyntaxList<StatementSyntax> bodyStatements = new SyntaxList<StatementSyntax>();
            ImmutableArray<IPropertySymbol> properties = symbol.GetMembers().OfType<IPropertySymbol>().ToImmutableArray();

            foreach (IPropertySymbol property in properties)
            {

                var expression = GetExpression(property);

                string expressionString = expression.ToFullString();

                ExpressionStatementSyntax statement = SyntaxFactory.ExpressionStatement(expression, SemicolonToken());

                bodyStatements = bodyStatements.Add(statement);
            }

            body = body.WithStatements(bodyStatements);

            ctor = ctor.WithBody(body);

            return ctor.NormalizeWhitespace();
        }

        private static ExpressionSyntax DefaultExpression(string typeName) =>
            SyntaxFactory.DefaultExpression(SyntaxFactory.ParseTypeName(typeName));

        private static ExpressionSyntax GetExpression(IPropertySymbol property)
        {
            string fullString = property.Type.ToDisplayString(TypeNameFormat);
            string minimalString = property.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            ExpressionSyntax rightExpression = null;

            switch (minimalString)
            {
                case "long":
                case "short":
                case "double":
                case "float":
                case "int":
                    rightExpression = SyntaxFactory.ParseExpression("0");
                    break;
                case "string":
                    rightExpression = SyntaxFactory.ParseExpression("string.Empty");
                    break;
                case "bool":
                    rightExpression = SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
                    break;
                default:
                    rightExpression = SyntaxFactory.ObjectCreationExpression(
                        WithTrailingSpace(SyntaxFactory.Token(SyntaxKind.NewKeyword)),
                        SyntaxFactory.ParseTypeName(minimalString),
                        SyntaxFactory.ArgumentList().WithLeadingTrivia(),
                        null);
                    break;

            }


            return SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(property.Name),
                WithLeadingSpace(SyntaxFactory.Token(SyntaxKind.EqualsToken)),
                rightExpression);
        }

        private static LiteralExpressionSyntax StringLiteralExpression(string text = "")
        {
            SyntaxToken literalToken = SyntaxFactory.Token(
                SyntaxFactory.TriviaList(),
                SyntaxKind.StringLiteralToken,
                text,
                text,
                SyntaxFactory.TriviaList()
            );

            return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, literalToken);
        }

        private static LiteralExpressionSyntax NumericLiteralExpression(int value = 0)
        {
            SyntaxToken literalToken = SyntaxFactory.Token(
                SyntaxFactory.TriviaList(),
                SyntaxKind.NumericLiteralToken,
                value.ToString(),
                value.ToString(),
                SyntaxFactory.TriviaList()
            );

            return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, literalToken);
        }

        private SyntaxList<MemberDeclarationSyntax> VisitClassMembers(SyntaxList<MemberDeclarationSyntax> members)
        {
            members = VisitList(members);

            SyntaxList<MemberDeclarationSyntax> newList = SyntaxFactory.List<MemberDeclarationSyntax>();

            foreach (MemberDeclarationSyntax member in members)
            {
                if (!member.IsKind(SyntaxKind.FieldDeclaration))
                {
                    newList = newList.Add(member);
                }
            }

            return newList;
        }

        private SyntaxTokenList VisitModifiers(SyntaxTokenList modifiers)
        {
            SyntaxTokenList newList = SyntaxFactory.TokenList();
            for (int i = 0; i < modifiers.Count; i++)
            {
                SyntaxToken modifier = modifiers[i];

                if (i == 0)
                {
                    modifier = modifier.WithoutTrivia();

                    modifier = modifier.WithLeadingTrivia(GetIndentation());

                    modifier = modifier.WithTrailingTrivia(SyntaxFactory.Space);
                }

                newList = newList.Add(modifier);
            }

            return newList;
        }

        private string GetAnchorIndentation()
        {
            StringBuilder sb = new StringBuilder();

            for (int j = 0; j < _namespaceNestLevel; j++)
            {
                sb.Append("    ");
            }

            int x = _typeNestLevel + 1;

            for (int j = 0; j < _typeNestLevel; j++)
            {
                sb.Append("    ");
            }

            return sb.ToString();
        }

        private SyntaxTrivia GetIndentation()
        {
            return SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, GetAnchorIndentation());
        }

        private SyntaxToken WithIndentation(SyntaxToken token)
        {
            return token.WithLeadingTrivia(GetIndentation());
        }

        private SyntaxToken SurroundTokenWithSpace(SyntaxToken token)
        {
            return token.WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(SyntaxFactory.Space);
        }

        private static SyntaxToken WithLeadingSpace(SyntaxToken token)
        {
            return token.WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(EmptyTrivia());
        }

        private static SyntaxToken WithTrailingSpace(SyntaxToken token)
        {
            return token.WithLeadingTrivia(EmptyTrivia()).WithTrailingTrivia(SyntaxFactory.Space);
        }

        private static SyntaxTrivia EmptyTrivia()
        {
            return SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, string.Empty);
        }

        private static SyntaxToken SemicolonToken()
        {
            return WithTrailingSpace(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        /// <summary>Called when the visitor visits a PropertyDeclarationSyntax node.</summary>
        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            PropertyDeclarationSyntax newNode = (PropertyDeclarationSyntax) base.VisitPropertyDeclaration(node);

            newNode = newNode.WithAttributeLists(SyntaxFactory.List<AttributeListSyntax>());

            newNode = newNode.WithIdentifier(WithTrailingSpace(newNode.Identifier));

            newNode = newNode.WithModifiers(VisitModifiers(newNode.Modifiers));

            newNode = newNode.WithAccessorList((AccessorListSyntax) VisitAccessorList(newNode.AccessorList));

            return newNode;
        }

        /// <summary>Called when the visitor visits a AccessorListSyntax node.</summary>
        public override SyntaxNode VisitAccessorList(AccessorListSyntax node)
        {
            AccessorListSyntax newNode = (AccessorListSyntax) base.VisitAccessorList(node);

            newNode = newNode.WithAccessors(VisitList(newNode.Accessors));

            return newNode;
        }

        /// <summary>Called when the visitor visits a AccessorDeclarationSyntax node.</summary>
        public override SyntaxNode VisitAccessorDeclaration(AccessorDeclarationSyntax node)
        {
            AccessorDeclarationSyntax newNode = (AccessorDeclarationSyntax) base.VisitAccessorDeclaration(node);

            newNode = newNode.WithAttributeLists(SyntaxFactory.List<AttributeListSyntax>());
            newNode = newNode.WithKeyword(newNode.Keyword.WithoutTrivia());
            newNode = newNode.WithBody(null);
            newNode = newNode.WithSemicolonToken(SemicolonToken());

            return newNode;
        }
    }
}
