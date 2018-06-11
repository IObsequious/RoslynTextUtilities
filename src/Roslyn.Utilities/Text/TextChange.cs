using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    public struct TextChange : IEquatable<TextChange>
    {
        public TextSpan Span { get; }

        public string NewText { get; }

        public TextChange(TextSpan span, string newText)
            : this()
        {
            if (newText == null)
            {
                throw new ArgumentNullException(nameof(newText));
            }

            Span = span;
            NewText = newText;
        }

        public override string ToString()
        {
            return string.Format(format: "{0}: {{ {1}, \"{2}\" }}", arg0: GetType().Name, arg1: Span, arg2: NewText);
        }

        public override bool Equals(object obj)
        {
            return obj is TextChange textChange && Equals(textChange);
        }

        public bool Equals(TextChange other)
        {
            return
                EqualityComparer<TextSpan>.Default.Equals(Span, other.Span)
                && EqualityComparer<string>.Default.Equals(NewText, other.NewText);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Span.GetHashCode(), NewText.GetHashCode());
        }

        public static bool operator ==(TextChange left, TextChange right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TextChange left, TextChange right)
        {
            return !(left == right);
        }

        public static implicit operator TextChangeRange(TextChange change)
        {
            return new TextChangeRange(change.Span, change.NewText.Length);
        }

        public static IReadOnlyList<TextChange> NoChanges
        {
            get
            {
                return SpecializedCollections.EmptyReadOnlyList<TextChange>();
            }
        }
    }
}
