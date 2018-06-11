using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ConsoleTester.Refactoring
{
    public class SyntacticDocument
    {
        public readonly Document Document;
        public readonly SourceText Text;
        public readonly SyntaxTree SyntaxTree;
        public readonly SyntaxNode Root;
        public readonly CompilationUnitSyntax CompilationUnitRoot;
        public readonly SyntaxList<NamespaceDeclarationSyntax> Namespaces;

        protected SyntacticDocument(Document document, SourceText text, SyntaxTree tree, SyntaxNode root)
        {
            this.Document = document;
            this.Text = text;
            this.SyntaxTree = tree;
            this.Root = root;
            CompilationUnitRoot = Root as CompilationUnitSyntax;
            Namespaces = SyntaxFactory.List(CompilationUnitRoot?
                .DescendantNodes(_ => true).OfType<NamespaceDeclarationSyntax>());
        }

        public Project Project => this.Document?.Project;

        public Solution Solution => this.Document?.Project?.Solution;

        public IEnumerable<TNode> GetNodes<TNode>() where TNode : SyntaxNode
        {
            return Root?.DescendantNodes(_ => true).OfType<TNode>();
        }

        public static async Task<SyntacticDocument> CreateAsync(
            Document document, CancellationToken cancellationToken)
        {
            SourceText text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return new SyntacticDocument(document, text, root.SyntaxTree, root);
        }
    }
}
