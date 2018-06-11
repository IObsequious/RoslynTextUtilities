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
        private class BlankLineAdder : CSharpSyntaxRewriter
        {
            /// <summary>Called when the visitor visits a ClassDeclarationSyntax node.</summary>
            /// <param name="node"></param>
            public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                return base.VisitClassDeclaration(node).AddLeadingBlankLine();
            }

            /// <summary>Called when the visitor visits a StructDeclarationSyntax node.</summary>
            /// <param name="node"></param>
            public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
            {
                return base.VisitStructDeclaration(node).AddLeadingBlankLine();
            }

            /// <summary>Called when the visitor visits a InterfaceDeclarationSyntax node.</summary>
            /// <param name="node"></param>
            public override SyntaxNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
            {
                return base.VisitInterfaceDeclaration(node).AddLeadingBlankLine();
            }

            /// <summary>Called when the visitor visits a EnumDeclarationSyntax node.</summary>
            /// <param name="node"></param>
            public override SyntaxNode VisitEnumDeclaration(EnumDeclarationSyntax node)
            {
                return base.VisitEnumDeclaration(node).AddLeadingBlankLine();
            }

            /// <summary>Called when the visitor visits a EnumMemberDeclarationSyntax node.</summary>
            /// <param name="node"></param>
            public override SyntaxNode VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
            {
                return base.VisitEnumMemberDeclaration(node).AddLeadingBlankLine();
            }

            /// <summary>Called when the visitor visits a MethodDeclarationSyntax node.</summary>
            /// <param name="node"></param>
            public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                return base.VisitMethodDeclaration(node).AddLeadingBlankLine();
            }

            /// <summary>Called when the visitor visits a PropertyDeclarationSyntax node.</summary>
            /// <param name="node"></param>
            public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                return base.VisitPropertyDeclaration(node).AddLeadingBlankLine();
            }

            /// <summary>Called when the visitor visits a ConstructorDeclarationSyntax node.</summary>
            /// <param name="node"></param>
            public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
            {
                return base.VisitConstructorDeclaration(node).AddLeadingBlankLine();
            }

            /// <summary>Called when the visitor visits a DestructorDeclarationSyntax node.</summary>
            /// <param name="node"></param>
            public override SyntaxNode VisitDestructorDeclaration(DestructorDeclarationSyntax node)
            {
                return base.VisitDestructorDeclaration(node).AddLeadingBlankLine();
            }

            /// <summary>Called when the visitor visits a NamespaceDeclarationSyntax node.</summary>
            /// <param name="node"></param>
            public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            {
                return base.VisitNamespaceDeclaration(node).AddLeadingBlankLine();
            }
        }
    }
}
