using System;

namespace Microsoft.CodeAnalysis.Text
{
    public abstract class SourceTextContainer
    {
        public abstract SourceText CurrentText { get; }

        public abstract event EventHandler<TextChangeEventArgs> TextChanged;
    }
}
