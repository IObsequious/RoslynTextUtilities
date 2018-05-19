using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Roslyn.Utilities
{
    public static class PathUtilities
    {
        internal static readonly char DirectorySeparatorChar = PlatformInformation.IsUnix ? '/' : '\\';
        internal const char AltDirectorySeparatorChar = '/';
        internal const string ParentRelativeDirectory = "..";
        internal const string ThisDirectory = ".";
        internal static readonly string DirectorySeparatorStr = new string(DirectorySeparatorChar, 1);
        internal const char VolumeSeparatorChar = ':';

        public static bool IsUnixLikePlatform
        {
            get
            {
                return PlatformInformation.IsUnix;
            }
        }

        public static bool IsDirectorySeparator(char c)
        {
            return c == DirectorySeparatorChar || c == AltDirectorySeparatorChar;
        }

        public static bool IsAnyDirectorySeparator(char c)
        {
            return c == '\\' || c == '/';
        }

        public static string TrimTrailingSeparators(string s)
        {
            int lastSeparator = s.Length;
            while (lastSeparator > 0 && IsDirectorySeparator(s[lastSeparator - 1]))
            {
                lastSeparator = lastSeparator - 1;
            }

            if (lastSeparator != s.Length)
            {
                s = s.Substring(0, lastSeparator);
            }

            return s;
        }

        public static string EnsureTrailingSeparator(string s)
        {
            if (s.Length == 0 || IsAnyDirectorySeparator(s[s.Length - 1]))
            {
                return s;
            }

            bool hasSlash = s.IndexOf('/') >= 0;
            bool hasBackslash = s.IndexOf('\\') >= 0;
            if (hasSlash && !hasBackslash)
            {
                return s + '/';
            }

            if (!hasSlash && hasBackslash)
            {
                return s + '\\';
            }

            return s + DirectorySeparatorChar;
        }

        public static string GetExtension(string path)
        {
            return FileNameUtilities.GetExtension(path);
        }

        public static string ChangeExtension(string path, string extension)
        {
            return FileNameUtilities.ChangeExtension(path, extension);
        }

        public static string RemoveExtension(string path)
        {
            return FileNameUtilities.ChangeExtension(path, null);
        }

        public static string GetFileName(string path, bool includeExtension = true)
        {
            return FileNameUtilities.GetFileName(path, includeExtension);
        }

        public static string GetDirectoryName(string path)
        {
            return GetDirectoryName(path, IsUnixLikePlatform);
        }

        public static string GetDirectoryName(string path, bool isUnixLike)
        {
            if (path != null)
            {
                int rootLength = GetPathRoot(path, isUnixLike).Length;
                if (path.Length > rootLength)
                {
                    int i = path.Length;
                    while (i > rootLength)
                    {
                        i--;
                        if (IsDirectorySeparator(path[i]))
                        {
                            if (i > 0 && IsDirectorySeparator(path[i - 1]))
                            {
                                continue;
                            }

                            break;
                        }
                    }

                    return path.Substring(0, i);
                }
            }

            return null;
        }

        public static bool IsSameDirectoryOrChildOf(string child, string parent)
        {
            parent = RemoveTrailingDirectorySeparator(parent);
            while (child != null)
            {
                child = RemoveTrailingDirectorySeparator(child);
                if (child.Equals(parent, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                child = GetDirectoryName(child);
            }

            return false;
        }

        public static string GetPathRoot(string path)
        {
            return GetPathRoot(path, IsUnixLikePlatform);
        }

        private static string GetPathRoot(string path, bool isUnixLike)
        {
            if (path == null)
            {
                return null;
            }

            if (isUnixLike)
            {
                return GetUnixRoot(path);
            }

            return GetWindowsRoot(path);
        }

        private static string GetWindowsRoot(string path)
        {
            int length = path.Length;
            if (length >= 1 && IsDirectorySeparator(path[0]))
            {
                if (length < 2 || !IsDirectorySeparator(path[1]))
                {
                    return path.Substring(0, 1);
                }

                int i = 2;
                i = ConsumeDirectorySeparators(path, length, i);
                bool hitSeparator = false;
                while (true)
                {
                    if (i == length)
                    {
                        return path;
                    }

                    if (!IsDirectorySeparator(path[i]))
                    {
                        i++;
                        continue;
                    }

                    if (!hitSeparator)
                    {
                        hitSeparator = true;
                        i = ConsumeDirectorySeparators(path, length, i);
                        continue;
                    }

                    return path.Substring(0, i);
                }
            }

            if (length >= 2 && path[1] == VolumeSeparatorChar)
            {
                return length >= 3 && IsDirectorySeparator(path[2]) ? path.Substring(0, 3) : path.Substring(0, 2);
            }

            return "";
        }

        private static int ConsumeDirectorySeparators(string path, int length, int i)
        {
            while (i < length && IsDirectorySeparator(path[i]))
            {
                i++;
            }

            return i;
        }

        private static string GetUnixRoot(string path)
        {
            return path.Length > 0 && IsDirectorySeparator(path[0]) ? path.Substring(0, 1) : "";
        }

        public static PathKind GetPathKind(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return PathKind.Empty;
            }

            if (IsAbsolute(path))
            {
                return PathKind.Absolute;
            }

            if (path.Length > 0 && path[0] == '.')
            {
                if (path.Length == 1 || IsDirectorySeparator(path[1]))
                {
                    return PathKind.RelativeToCurrentDirectory;
                }

                if (path[1] == '.')
                {
                    if (path.Length == 2 || IsDirectorySeparator(path[2]))
                    {
                        return PathKind.RelativeToCurrentParent;
                    }
                }
            }

            if (!IsUnixLikePlatform)
            {
                if (path.Length >= 1 && IsDirectorySeparator(path[0]))
                {
                    return PathKind.RelativeToCurrentRoot;
                }

                if (path.Length >= 2 && path[1] == VolumeSeparatorChar && (path.Length <= 2 || !IsDirectorySeparator(path[2])))
                {
                    return PathKind.RelativeToDriveDirectory;
                }
            }

            return PathKind.Relative;
        }

        public static bool IsAbsolute(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            if (IsUnixLikePlatform)
            {
                return path[0] == DirectorySeparatorChar;
            }

            if (IsDriveRootedAbsolutePath(path))
            {
                return true;
            }

            return path.Length >= 2 &&
                   IsDirectorySeparator(path[0]) &&
                   IsDirectorySeparator(path[1]);
        }

        private static bool IsDriveRootedAbsolutePath(string path)
        {
            Debug.Assert(!IsUnixLikePlatform);
            return path.Length >= 3 && path[1] == VolumeSeparatorChar && IsDirectorySeparator(path[2]);
        }

        public static string CombineAbsoluteAndRelativePaths(string root, string relativePath)
        {
            Debug.Assert(IsAbsolute(root));
            return CombinePossiblyRelativeAndRelativePaths(root, relativePath);
        }

        public static string CombinePossiblyRelativeAndRelativePaths(string rootOpt, string relativePath)
        {
            if (string.IsNullOrEmpty(rootOpt))
            {
                return null;
            }

            switch (GetPathKind(relativePath))
            {
                case PathKind.Empty:
                    return rootOpt;
                case PathKind.Absolute:
                case PathKind.RelativeToCurrentRoot:
                case PathKind.RelativeToDriveDirectory:
                    return null;
            }

            return CombinePathsUnchecked(rootOpt, relativePath);
        }

        public static string CombinePathsUnchecked(string root, string relativePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(root));
            char c = root[root.Length - 1];
            if (!IsDirectorySeparator(c) && c != VolumeSeparatorChar)
            {
                return root + DirectorySeparatorStr + relativePath;
            }

            return root + relativePath;
        }

        private static string RemoveTrailingDirectorySeparator(string path)
        {
            if (path.Length > 0 && IsDirectorySeparator(path[path.Length - 1]))
            {
                return path.Substring(0, path.Length - 1);
            }

            return path;
        }

        public static bool IsFilePath(string assemblyDisplayNameOrPath)
        {
            Debug.Assert(assemblyDisplayNameOrPath != null);
            string extension = FileNameUtilities.GetExtension(assemblyDisplayNameOrPath);
            return string.Equals(extension, b: ".dll", comparisonType: StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, b: ".exe", comparisonType: StringComparison.OrdinalIgnoreCase) ||
                   assemblyDisplayNameOrPath.IndexOf(DirectorySeparatorChar) != -1 ||
                   assemblyDisplayNameOrPath.IndexOf(AltDirectorySeparatorChar) != -1;
        }

        public static bool ContainsPathComponent(string path, string component, bool ignoreCase)
        {
            StringComparison comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (path?.IndexOf(component, comparison) >= 0)
            {
                StringComparer comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
                int count = 0;
                string currentPath = path;
                while (currentPath != null)
                {
                    string currentName = GetFileName(currentPath);
                    if (comparer.Equals(currentName, component))
                    {
                        return true;
                    }

                    currentPath = GetDirectoryName(currentPath);
                    count++;
                }
            }

            return false;
        }

        public static string GetRelativePath(string directory, string fullPath)
        {
            string relativePath = string.Empty;
            if (IsChildPath(directory, fullPath))
            {
                return GetRelativeChildPath(directory, fullPath);
            }

            string[] directoryPathParts = GetPathParts(directory);
            string[] fullPathParts = GetPathParts(fullPath);
            if (directoryPathParts.Length == 0 || fullPathParts.Length == 0)
            {
                return fullPath;
            }

            int index = 0;
            for (; index < directoryPathParts.Length; index++)
            {
                if (!PathsEqual(directoryPathParts[index], fullPathParts[index]))
                {
                    break;
                }
            }

            if (index == 0)
            {
                return fullPath;
            }

            int remainingParts = directoryPathParts.Length - index;
            if (remainingParts > 0)
            {
                for (int i = 0; i < remainingParts; i++)
                {
                    relativePath = relativePath + ParentRelativeDirectory + DirectorySeparatorStr;
                }
            }

            for (int i = index; i < fullPathParts.Length; i++)
            {
                relativePath = CombinePathsUnchecked(relativePath, fullPathParts[i]);
            }

            return relativePath;
        }

        public static bool IsChildPath(string parentPath, string childPath)
        {
            return parentPath.Length > 0 &&
                   childPath.Length > parentPath.Length &&
                   PathsEqual(childPath, parentPath, parentPath.Length) &&
                   (IsDirectorySeparator(parentPath[parentPath.Length - 1]) || IsDirectorySeparator(childPath[parentPath.Length]));
        }

        private static string GetRelativeChildPath(string parentPath, string childPath)
        {
            string relativePath = childPath.Substring(parentPath.Length);
            int start = ConsumeDirectorySeparators(relativePath, relativePath.Length, 0);
            if (start > 0)
            {
                relativePath = relativePath.Substring(start);
            }

            return relativePath;
        }

        private static readonly char[] s_pathChars = {VolumeSeparatorChar, DirectorySeparatorChar, AltDirectorySeparatorChar};

        private static string[] GetPathParts(string path)
        {
            string[] pathParts = path.Split(s_pathChars);
            if (pathParts.Contains(ThisDirectory))
            {
                pathParts = pathParts.Where(predicate: s => s != ThisDirectory).ToArray();
            }

            return pathParts;
        }

        public static bool PathsEqual(string path1, string path2)
        {
            return PathsEqual(path1, path2, Math.Max(path1.Length, path2.Length));
        }

        private static bool PathsEqual(string path1, string path2, int length)
        {
            if (path1.Length < length || path2.Length < length)
            {
                return false;
            }

            for (int i = 0; i < length; i++)
            {
                if (!PathCharEqual(path1[i], path2[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool PathCharEqual(char x, char y)
        {
            if (IsDirectorySeparator(x) && IsDirectorySeparator(y))
            {
                return true;
            }

            return IsUnixLikePlatform ? x == y : char.ToUpperInvariant(x) == char.ToUpperInvariant(y);
        }

        private static int PathHashCode(string path)
        {
            int hc = 0;
            if (path != null)
            {
                foreach (char ch in path)
                {
                    if (!IsDirectorySeparator(ch))
                    {
                        hc = Hash.Combine((int) char.ToUpperInvariant(ch), hc);
                    }
                }
            }

            return hc;
        }

        public static string NormalizePathPrefix(string filePath, ImmutableArray<KeyValuePair<string, string>> pathMap)
        {
            if (pathMap.IsDefaultOrEmpty)
            {
                return filePath;
            }

            foreach (var kv in pathMap)
            {
                var oldPrefix = kv.Key;
                if (!(oldPrefix?.Length > 0))
                {
                    continue;
                }

                if (filePath.StartsWith(oldPrefix, StringComparison.Ordinal))
                {
                    var replacementPrefix = kv.Value;
                    string replacement = replacementPrefix + filePath.Substring(oldPrefix.Length);
                    bool hasSlash = replacementPrefix.IndexOf('/') >= 0;
                    bool hasBackslash = replacementPrefix.IndexOf('\\') >= 0;
                    return
                        hasSlash && !hasBackslash ? replacement.Replace('\\', '/') :
                        hasBackslash && !hasSlash ? replacement.Replace('/', '\\') :
                        replacement;
                }
            }

            return filePath;
        }

        public static bool IsValidFilePath(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    return false;
                }

                FileInfo fileInfo = new FileInfo(path);
                return !string.IsNullOrEmpty(fileInfo.Name);
            }
            catch (Exception ex) when (
                ex is ArgumentException ||
                ex is PathTooLongException ||
                ex is NotSupportedException)
            {
                return false;
            }
        }

        public static readonly IEqualityComparer<string> Comparer = new PathComparer();

        private class PathComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                if (x == null && y == null)
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                return PathsEqual(x, y);
            }

            public int GetHashCode(string s)
            {
                return PathHashCode(s);
            }
        }
    }
}
