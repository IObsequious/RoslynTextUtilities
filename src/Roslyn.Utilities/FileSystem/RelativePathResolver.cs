#pragma warning disable 436 
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public class RelativePathResolver : IEquatable<RelativePathResolver>
    {
        public ImmutableArray<string> SearchPaths { get; }

        public string BaseDirectory { get; }

        public RelativePathResolver(ImmutableArray<string> searchPaths, string baseDirectory)
        {
            Debug.Assert(searchPaths.All(PathUtilities.IsAbsolute));
            Debug.Assert(baseDirectory == null || PathUtilities.GetPathKind(baseDirectory) == PathKind.Absolute);
            SearchPaths = searchPaths;
            BaseDirectory = baseDirectory;
        }

        public string ResolvePath(string reference, string baseFilePath)
        {
            string resolvedPath = FileUtilities.ResolveRelativePath(reference, baseFilePath, BaseDirectory, SearchPaths, FileExists);
            if (resolvedPath == null)
            {
                return null;
            }

            return FileUtilities.TryNormalizeAbsolutePath(resolvedPath);
        }

        protected virtual bool FileExists(string fullPath)
        {
            Debug.Assert(fullPath != null);
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));
            return File.Exists(fullPath);
        }

        public RelativePathResolver WithSearchPaths(ImmutableArray<string> searchPaths)
        {
            return new RelativePathResolver(searchPaths, BaseDirectory);
        }

        public RelativePathResolver WithBaseDirectory(string baseDirectory)
        {
            return new RelativePathResolver(SearchPaths, baseDirectory);
        }

        public bool Equals(RelativePathResolver other)
        {
            return BaseDirectory == other.BaseDirectory && SearchPaths.SequenceEqual(other.SearchPaths);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(BaseDirectory, Hash.CombineValues(SearchPaths));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RelativePathResolver);
        }
    }
}
