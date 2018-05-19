using System;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public struct FileLinePositionSpan : IEquatable<FileLinePositionSpan>
    {
        private readonly string _path;
        private readonly LinePositionSpan _span;
        private readonly bool _hasMappedPath;

        public string Path
        {
            get
            {
                return _path;
            }
        }

        public bool HasMappedPath
        {
            get
            {
                return _hasMappedPath;
            }
        }

        public LinePosition StartLinePosition
        {
            get
            {
                return _span.Start;
            }
        }

        public LinePosition EndLinePosition
        {
            get
            {
                return _span.End;
            }
        }

        public LinePositionSpan Span
        {
            get
            {
                return _span;
            }
        }

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

            _path = path;
            _span = span;
            _hasMappedPath = false;
        }

        internal FileLinePositionSpan(string path, LinePositionSpan span, bool hasMappedPath)
        {
            _path = path;
            _span = span;
            _hasMappedPath = hasMappedPath;
        }

        public bool IsValid
        {
            get
            {
                return _path != null;
            }
        }

        public bool Equals(FileLinePositionSpan other)
        {
            return _span.Equals(other._span) &&
                   _hasMappedPath == other._hasMappedPath &&
                   string.Equals(_path, other._path, StringComparison.Ordinal);
        }

        public override bool Equals(object other)
        {
            return other is FileLinePositionSpan && Equals((FileLinePositionSpan) other);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_path, Hash.Combine(_hasMappedPath, _span.GetHashCode()));
        }

        public override string ToString()
        {
            return _path + ": " + _span;
        }
    }
}
