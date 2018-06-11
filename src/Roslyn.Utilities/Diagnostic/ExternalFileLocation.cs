using System;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public sealed class ExternalFileLocation : Location, IEquatable<ExternalFileLocation>
    {
        private readonly FileLinePositionSpan _lineSpan;

        internal ExternalFileLocation(string filePath, TextSpan sourceSpan, LinePositionSpan lineSpan)
        {
            SourceSpan = sourceSpan;
            _lineSpan = new FileLinePositionSpan(filePath, lineSpan);
        }

        public override TextSpan SourceSpan { get; }

        public override FileLinePositionSpan GetLineSpan()
        {
            return _lineSpan;
        }

        public override FileLinePositionSpan GetMappedLineSpan()
        {
            return _lineSpan;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ExternalFileLocation);
        }

        public bool Equals(ExternalFileLocation other)
        {
            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return other != null && SourceSpan == other.SourceSpan && _lineSpan.Equals(other._lineSpan);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_lineSpan.GetHashCode(), SourceSpan.GetHashCode());
        }
    }
}
