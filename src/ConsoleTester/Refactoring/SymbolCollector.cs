using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ConsoleTester.Refactoring
{

    public class CollectionEntry
    {
        public INamedTypeSymbol Class;
        public ImmutableArray<IPropertySymbol> Properties;

        public CollectionEntry(INamedTypeSymbol @class)
        {
            Class = @class;
            Properties = @class.GetMembers().OfType<IPropertySymbol>().ToImmutableArray();
        }
    }

    public class SymbolCollector : SymbolVisitor
    {
        public SymbolCollector()
        {
            Types = new List<CollectionEntry>();
        }
        public List<CollectionEntry> Types { get; }

        public override void VisitNamespace(INamespaceSymbol symbol)
        {
            foreach (INamespaceOrTypeSymbol member in symbol.GetMembers())
            {
                member.Accept(this);
            }
        }

        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            Types.Add(new CollectionEntry(symbol));

            foreach (ISymbol member in symbol.GetMembers())
            {
                member.Accept(this);
            }
        }
    }
}
