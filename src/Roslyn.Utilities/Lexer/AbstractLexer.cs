// -----------------------------------------------------------------------
// <copyright file="AbstractLexer.cs" company="Ollon, LLC">
//     Copyright (c) 2017 Ollon, LLC. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Utilities.Lexer
{
    public abstract class AbstractLexer : IDisposable
    {
        public readonly SlidingTextWindow TextWindow;

        protected AbstractLexer(SourceText sourceText)
        {
            TextWindow = new SlidingTextWindow(sourceText);
        }

        public void Start() => TextWindow.Start();

        public void Reset(int position) => TextWindow.Reset(position);

        public virtual void Dispose()
        {
            TextWindow.Dispose();
        }
    }
}
