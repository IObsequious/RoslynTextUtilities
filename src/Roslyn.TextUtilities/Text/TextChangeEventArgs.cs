// -----------------------------------------------------------------------
// <copyright file="TextChangeEventArgs.cs" company="Ollon, LLC">
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
using System.Collections.Immutable;
using System.Linq;
using System.Linq;
using System.Linq;
using System.Linq;

namespace System.Text
{
    /// <summary>
    /// Represents state for a TextChanged event.
    /// </summary>
    public class TextChangeEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes an instance of <see cref="TextChangeEventArgs"/>.
        /// </summary>
        /// <param name="oldText">The text before the change.</param>
        /// <param name="newText">The text after the change.</param>
        /// <param name="changes">A non-empty set of ranges for the change.</param>
        public TextChangeEventArgs(SourceText oldText, SourceText newText, IEnumerable<TextChangeRange> changes)
        {
            if (changes == null || !changes.Any())
            {
                throw new ArgumentException("changes");
            }

            OldText = oldText;
            NewText = newText;
            Changes = changes.ToImmutableArray();
        }

        /// <summary>
        /// Initializes an instance of <see cref="TextChangeEventArgs"/>.
        /// </summary>
        /// <param name="oldText">The text before the change.</param>
        /// <param name="newText">The text after the change.</param>
        /// <param name="changes">A non-empty set of ranges for the change.</param>
        public TextChangeEventArgs(SourceText oldText, SourceText newText, params TextChangeRange[] changes)
            : this(oldText, newText, (IEnumerable<TextChangeRange>)changes)
        {
        }

        /// <summary>
        /// Gets the text before the change.
        /// </summary>
        public SourceText OldText { get; }

        /// <summary>
        /// Gets the text after the change.
        /// </summary>
        public SourceText NewText { get; }

        /// <summary>
        /// Gets the set of ranges for the change.
        /// </summary>
        public IReadOnlyList<TextChangeRange> Changes { get; }
    }
}
