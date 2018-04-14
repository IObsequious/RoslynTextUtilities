// -----------------------------------------------------------------------
// <copyright file="TextChange.cs" company="Ollon, LLC">
//     Copyright (c) 2018 Ollon, LLC. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System;
using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;

namespace System.Text
{
    /// <summary>
    /// Describes a single change when a particular span is replaced with a new text.
    /// </summary>
    public struct TextChange : IEquatable<TextChange>
    {
        /// <summary>
        /// The original span of the changed text. 
        /// </summary>
        public TextSpan Span { get; }

        /// <summary>
        /// The new text.
        /// </summary>
        public string NewText { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="TextChange"/>
        /// </summary>
        /// <param name="span">The original span of the changed text.</param>
        /// <param name="newText">The new text.</param>
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

        /// <summary>
        /// Provides a string representation for <see cref="TextChange"/>.
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0}: {{ {1}, \"{2}\" }}", GetType().Name, Span, NewText);
        }

        public override bool Equals(object obj)
        {
            return obj is TextChange && Equals((TextChange)obj);
        }

        public bool Equals(TextChange other)
        {
            return
                EqualityComparer<TextSpan>.Default.Equals(Span, other.Span) &&
                EqualityComparer<string>.Default.Equals(NewText, other.NewText);
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

        /// <summary>
        /// Converts a <see cref="TextChange"/> to a <see cref="TextChangeRange"/>.
        /// </summary>
        /// <param name="change"></param>
        public static implicit operator TextChangeRange(TextChange change)
        {
            return new TextChangeRange(change.Span, change.NewText.Length);
        }

        /// <summary>
        /// An empty set of changes.
        /// </summary>
        public static IReadOnlyList<TextChange> NoChanges => new List<TextChange>();
    }
}
