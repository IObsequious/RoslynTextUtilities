using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    public partial class IdentifierCollection
    {
        private readonly Dictionary<string, object> _map = new Dictionary<string, object>(
            StringComparer.OrdinalIgnoreCase);

        public IdentifierCollection()
        {
        }

        public IdentifierCollection(IEnumerable<string> identifiers)
        {
            AddIdentifiers(identifiers);
        }

        public void AddIdentifiers(IEnumerable<string> identifiers)
        {
            foreach (string identifier in identifiers)
            {
                AddIdentifier(identifier);
            }
        }

        public void AddIdentifier(string identifier)
        {
            Debug.Assert(identifier != null);
            object value;
            if (!_map.TryGetValue(identifier, out value))
            {
                AddInitialSpelling(identifier);
            }
            else
            {
                AddAdditionalSpelling(identifier, value);
            }
        }

        private void AddAdditionalSpelling(string identifier, object value)
        {
            string strValue = value as string;
            if (strValue != null)
            {
                if (!string.Equals(identifier, strValue, StringComparison.Ordinal))
                {
                    _map[identifier] = new HashSet<string> {identifier, strValue};
                }
            }
            else
            {
                HashSet<string> spellings = (HashSet<string>) value;
                spellings.Add(identifier);
            }
        }

        private void AddInitialSpelling(string identifier)
        {
            _map.Add(identifier, identifier);
        }

        public bool ContainsIdentifier(string identifier, bool caseSensitive)
        {
            Debug.Assert(identifier != null);
            if (caseSensitive)
            {
                return CaseSensitiveContains(identifier);
            }

            return CaseInsensitiveContains(identifier);
        }

        private bool CaseInsensitiveContains(string identifier)
        {
            return _map.ContainsKey(identifier);
        }

        private bool CaseSensitiveContains(string identifier)
        {
            object spellings;
            if (_map.TryGetValue(identifier, out spellings))
            {
                string spelling = spellings as string;
                if (spelling != null)
                {
                    return string.Equals(identifier, spelling, StringComparison.Ordinal);
                }

                HashSet<string> set = (HashSet<string>) spellings;
                return set.Contains(identifier);
            }

            return false;
        }

        public ICollection<string> AsCaseSensitiveCollection()
        {
            return new CaseSensitiveCollection(this);
        }

        public ICollection<string> AsCaseInsensitiveCollection()
        {
            return new CaseInsensitiveCollection(this);
        }
    }
}
