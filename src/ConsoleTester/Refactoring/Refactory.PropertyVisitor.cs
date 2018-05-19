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
        private class PropertyVisitor : CSharpSyntaxRewriter
        {
            public List<PropertyDeclarationSyntax> List { get; } = new List<PropertyDeclarationSyntax>();

            public PropertyVisitor() : base(true)
            {

            }

            private static AccessorDeclarationSyntax AutoGetAccessor
            {
                get
                {
                    AccessorDeclarationSyntax accessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration);
                    accessor = accessor.WithBody(null);
                    accessor = accessor.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                    return accessor;
                }
            }

            private static AccessorDeclarationSyntax AutoSetAccessor
            {
                get
                {
                    AccessorDeclarationSyntax accessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration);
                    accessor = accessor.WithBody(null);
                    accessor = accessor.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                    return accessor;
                }
            }

            private static AccessorListSyntax AutoGetList
            {
                get
                {
                    return SyntaxFactory.AccessorList(
                            SyntaxFactory.SingletonList<AccessorDeclarationSyntax>(
                                SyntaxFactory.AccessorDeclaration(
                                        SyntaxKind.GetAccessorDeclaration)
                                    .WithSemicolonToken(
                                        SyntaxFactory.Token(
                                            SyntaxFactory.TriviaList(),
                                            SyntaxKind.SemicolonToken,
                                            SyntaxFactory.TriviaList(
                                                SyntaxFactory.Space)))))
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

            private static AccessorListSyntax AutoSetList
            {
                get
                {
                    return SyntaxFactory.AccessorList(
                            SyntaxFactory.SingletonList<AccessorDeclarationSyntax>(
                                SyntaxFactory.AccessorDeclaration(
                                        SyntaxKind.GetAccessorDeclaration)
                                    .WithSemicolonToken(
                                        SyntaxFactory.Token(
                                            SyntaxFactory.TriviaList(),
                                            SyntaxKind.SemicolonToken,
                                            SyntaxFactory.TriviaList(
                                                SyntaxFactory.Space)))))
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


            [Flags]
            private enum AutoPropertyKind
            {
                None = 0,
                Read = 1 << 0,
                Write = 1 << 1,
                ReadWrite = Read | Write

            }

            public static async Task<Document> UseAutoProperties(Document document, CancellationToken cancellationToken = new CancellationToken())
            {
                PropertyVisitor visitor = new PropertyVisitor();
                SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                root = visitor.Visit(root);

                foreach (PropertyDeclarationSyntax baseProperty in visitor.List)
                {

                    AccessorListSyntax accessorList = SyntaxFactory.AccessorList().WithLeadingTrivia(baseProperty.AccessorList.GetLeadingTrivia())
                        .WithTrailingTrivia(baseProperty.AccessorList.GetTrailingTrivia());
                    PropertyDeclarationSyntax newPropertyDeclaration = baseProperty;
                    foreach (AccessorDeclarationSyntax accessor in baseProperty.AccessorList.Accessors)
                    {
                        AutoPropertyKind kind = AutoPropertyKind.None;
                        if (accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
                        {
                            kind = kind | AutoPropertyKind.Read;
                        }

                        if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration))
                        {
                            kind = kind | AutoPropertyKind.Write;
                        }


                        if (kind.HasFlag(AutoPropertyKind.Read) && !kind.HasFlag(AutoPropertyKind.Write))
                        {
                            accessorList = accessorList.AddAccessors(AutoGetList.Accessors.ToArray());
                        }
                        else if (kind.HasFlag(AutoPropertyKind.ReadWrite))
                        {
                            accessorList = accessorList.AddAccessors(AutoGetSetList.Accessors.ToArray());
                        }
                        else
                        {
                            accessorList = baseProperty.AccessorList;
                        }

                        root = root.ReplaceNode(baseProperty.AccessorList, accessorList);

                    }

                }

                return document.WithSyntaxRoot(root);
            }

            /// <summary>Called when the visitor visits a CompilationUnitSyntax node.</summary>
            public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
            {

                Stack<TypeDeclarationSyntax> stack = new Stack<TypeDeclarationSyntax>();

                CompilationUnitSyntax compilationUnit = (CompilationUnitSyntax)base.VisitCompilationUnit(node);

                foreach (NamespaceDeclarationSyntax namespaceDeclaration in compilationUnit.Members.OfType<NamespaceDeclarationSyntax>())
                {
                    foreach (TypeDeclarationSyntax typeDeclaration in namespaceDeclaration.Members.OfType<TypeDeclarationSyntax>())
                    {
                        stack.Push(typeDeclaration);

                    }
                }

                ProcessStack(stack, List);

                return node;
            }

            private static void ProcessStack(Stack<TypeDeclarationSyntax> stack, List<PropertyDeclarationSyntax> list)
            {
                while (stack.Count != 0)
                {
                    TypeDeclarationSyntax typeDeclaration = stack.Pop();

                    foreach (PropertyDeclarationSyntax propertyDeclaration in typeDeclaration.Members.OfType<PropertyDeclarationSyntax>())
                    {
                        list.Add(propertyDeclaration);
                    }

                    foreach (MemberDeclarationSyntax memberDeclaration in typeDeclaration.Members)
                    {
                        switch (memberDeclaration)
                        {
                            case ClassDeclarationSyntax classDeclarationSyntax:
                                stack.Push(classDeclarationSyntax);
                                break;
                            case StructDeclarationSyntax structDeclarationSyntax:
                                stack.Push(structDeclarationSyntax);
                                break;
                        }
                    }
                }
            }
        }
    }
}
