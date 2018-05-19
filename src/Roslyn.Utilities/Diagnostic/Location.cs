using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public abstract class Location
    {
        internal Location()
        {
        }

        public virtual TextSpan SourceSpan
        {
            get
            {
                return default(TextSpan);
            }
        }

        public virtual FileLinePositionSpan GetLineSpan()
        {
            return default(FileLinePositionSpan);
        }

        public virtual FileLinePositionSpan GetMappedLineSpan()
        {
            return default(FileLinePositionSpan);
        }

        public abstract override bool Equals(object obj);

        public abstract override int GetHashCode();

        public static bool operator ==(Location left, Location right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(Location left, Location right)
        {
            return !(left == right);
        }

        protected virtual string GetDebuggerDisplay()
        {
            string result = GetType().Name;
            FileLinePositionSpan pos = GetLineSpan();
            if (pos.Path != null)
            {
                result += "(" + pos.Path + "@" + (pos.StartLinePosition.Line + 1) + ":" + (pos.StartLinePosition.Character + 1) + ")";
            }

            return result;
        }

        public static Location None
        {
            get
            {
                return NoLocation.Singleton;
            }
        }

        //public virtual SyntaxTree SourceTree { get; }

        //public static Location Create(SyntaxTree syntaxTree, TextSpan textSpan)
        //{
        //    if (syntaxTree == null)
        //    {
        //        throw new ArgumentNullException(nameof(syntaxTree));
        //    }

        //    return new SourceLocation(syntaxTree, textSpan);
        //}

        public static Location Create(string filePath, TextSpan textSpan, LinePositionSpan lineSpan)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            return new ExternalFileLocation(filePath, textSpan, lineSpan);
        }
    }
}
