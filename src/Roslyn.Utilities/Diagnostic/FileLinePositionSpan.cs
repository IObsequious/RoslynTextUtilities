using System;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public struct FileLinePositionSpan : IEquatable<FileLinePositionSpan>
    {
        public string Path { get; }

        public bool HasMappedPath { get; }

        public LinePosition StartLinePosition
        {
            get
            {
                return Span.Start;
            }
        }

        public LinePosition EndLinePosition
        {
            get
            {
                return Span.End;
            }
        }

        public LinePositionSpan Span { get; }

        public FileLinePositionSpan(string path, LinePosition start, LinePosition end)
            : this(path, new LinePositionSpan(start, end))
        {
        }

        public FileLinePositionSpan(string path, LinePositionSpan span)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Path = path;
            Span = span;
            HasMappedPath = false;
        }

        internal FileLinePositionSpan(string path, LinePositionSpan span, bool hasMappedPath)
        {
            Path = path;
            Span = span;
            HasMappedPath = hasMappedPath;
        }

        public bool IsValid
        {
            get
            {
                return Path != null;
            }
        }

        public bool Equals(FileLinePositionSpan other)
        {
            return Span.Equals(other.Span)
                   && HasMappedPath == other.HasMappedPath
                   && string.Equals(Path, other.Path, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is FileLinePositionSpan fileLinePositionSpan && Equals(fileLinePositionSpan);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Path, Hash.Combine(HasMappedPath, Span.GetHashCode()));
        }

        public override string ToString()
        {
            return Path + ": " + Span;
        }
    }
}
