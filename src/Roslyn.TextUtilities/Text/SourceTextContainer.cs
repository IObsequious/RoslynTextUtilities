// -----------------------------------------------------------------------
// <copyright file="SourceTextContainer.cs" company="Ollon, LLC">
//     Copyright (c) 2018 Ollon, LLC. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System;
using System;
using System;

namespace System.Text
{
    /// <summary>
    /// An object that contains an instance of an SourceText and raises events when its current instance
    /// changes.
    /// </summary>
    public abstract class SourceTextContainer
    {
        /// <summary>
        /// The current text instance.
        /// </summary>
        public abstract SourceText CurrentText { get; }

        /// <summary>
        /// Raised when the current text instance changes.
        /// </summary>
        public abstract event EventHandler<TextChangeEventArgs> TextChanged;
    }
}
