using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;

namespace Roslyn.Utilities
{
    public static class FileUtilities
    {
        public static string ResolveRelativePath(
            string path,
            string basePath,
            string baseDirectory,
            IEnumerable<string> searchPaths,
            Func<string, bool> fileExists)
        {
            Debug.Assert(baseDirectory == null || searchPaths != null || PathUtilities.IsAbsolute(baseDirectory));
            Debug.Assert(searchPaths != null);
            Debug.Assert(fileExists != null);
            string combinedPath;
            PathKind kind = PathUtilities.GetPathKind(path);
            if (kind == PathKind.Relative)
            {
                baseDirectory = GetBaseDirectory(basePath, baseDirectory);
                if (baseDirectory != null)
                {
                    combinedPath = PathUtilities.CombinePathsUnchecked(baseDirectory, path);
                    Debug.Assert(PathUtilities.IsAbsolute(combinedPath));
                    if (fileExists(combinedPath))
                    {
                        return combinedPath;
                    }
                }

                foreach (string searchPath in searchPaths)
                {
                    combinedPath = PathUtilities.CombinePathsUnchecked(searchPath, path);
                    Debug.Assert(PathUtilities.IsAbsolute(combinedPath));
                    if (fileExists(combinedPath))
                    {
                        return combinedPath;
                    }
                }

                return null;
            }

            combinedPath = ResolveRelativePath(kind, path, basePath, baseDirectory);
            if (combinedPath != null)
            {
                Debug.Assert(PathUtilities.IsAbsolute(combinedPath));
                if (fileExists(combinedPath))
                {
                    return combinedPath;
                }
            }

            return null;
        }

        public static string ResolveRelativePath(string path, string baseDirectory)
        {
            return ResolveRelativePath(path, null, baseDirectory);
        }

        public static string ResolveRelativePath(string path, string basePath, string baseDirectory)
        {
            Debug.Assert(baseDirectory == null || PathUtilities.IsAbsolute(baseDirectory));
            return ResolveRelativePath(PathUtilities.GetPathKind(path), path, basePath, baseDirectory);
        }

        private static string ResolveRelativePath(PathKind kind, string path, string basePath, string baseDirectory)
        {
            switch (kind)
            {
                case PathKind.Empty:
                    return null;
                case PathKind.Relative:
                    baseDirectory = GetBaseDirectory(basePath, baseDirectory);
                    if (baseDirectory == null)
                    {
                        return null;
                    }

                    return PathUtilities.CombinePathsUnchecked(baseDirectory, path);
                case PathKind.RelativeToCurrentDirectory:
                    baseDirectory = GetBaseDirectory(basePath, baseDirectory);
                    if (baseDirectory == null)
                    {
                        return null;
                    }

                    if (path.Length == 1)
                    {
                        return baseDirectory;
                    }
                    else
                    {
                        return PathUtilities.CombinePathsUnchecked(baseDirectory, path);
                    }
                case PathKind.RelativeToCurrentParent:
                    baseDirectory = GetBaseDirectory(basePath, baseDirectory);
                    if (baseDirectory == null)
                    {
                        return null;
                    }

                    return PathUtilities.CombinePathsUnchecked(baseDirectory, path);
                case PathKind.RelativeToCurrentRoot:
                    string baseRoot;
                    if (basePath != null)
                    {
                        baseRoot = PathUtilities.GetPathRoot(basePath);
                    }
                    else if (baseDirectory != null)
                    {
                        baseRoot = PathUtilities.GetPathRoot(baseDirectory);
                    }
                    else
                    {
                        return null;
                    }

                    if (string.IsNullOrEmpty(baseRoot))
                    {
                        return null;
                    }

                    Debug.Assert(PathUtilities.IsDirectorySeparator(path[0]));
                    Debug.Assert(path.Length == 1 || !PathUtilities.IsDirectorySeparator(path[1]));
                    return PathUtilities.CombinePathsUnchecked(baseRoot, path.Substring(1));
                case PathKind.RelativeToDriveDirectory:
                    return null;
                case PathKind.Absolute:
                    return path;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private static string GetBaseDirectory(string basePath, string baseDirectory)
        {
            string resolvedBasePath = ResolveRelativePath(basePath, baseDirectory);
            if (resolvedBasePath == null)
            {
                return baseDirectory;
            }

            Debug.Assert(PathUtilities.IsAbsolute(resolvedBasePath));
            try
            {
                return Path.GetDirectoryName(resolvedBasePath);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static readonly char[] s_invalidPathChars = Path.GetInvalidPathChars();

        public static string NormalizeRelativePath(string path, string basePath, string baseDirectory)
        {
            if (path.IndexOf(value: "://", comparisonType: StringComparison.Ordinal) >= 0 || path.IndexOfAny(s_invalidPathChars) >= 0)
            {
                return null;
            }

            string resolvedPath = ResolveRelativePath(path, basePath, baseDirectory);
            if (resolvedPath == null)
            {
                return null;
            }

            string normalizedPath = TryNormalizeAbsolutePath(resolvedPath);
            if (normalizedPath == null)
            {
                return null;
            }

            return normalizedPath;
        }

        public static string NormalizeAbsolutePath(string path)
        {
            Debug.Assert(PathUtilities.IsAbsolute(path));
            try
            {
                return Path.GetFullPath(path);
            }
            catch (ArgumentException e)
            {
                throw new IOException(e.Message, e);
            }
            catch (SecurityException e)
            {
                throw new IOException(e.Message, e);
            }
            catch (NotSupportedException e)
            {
                throw new IOException(e.Message, e);
            }
        }

        public static string NormalizeDirectoryPath(string path)
        {
            return NormalizeAbsolutePath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public static string TryNormalizeAbsolutePath(string path)
        {
            Debug.Assert(PathUtilities.IsAbsolute(path));
            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                return null;
            }
        }

        public static Stream OpenRead(string fullPath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));
            try
            {
                return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new IOException(e.Message, e);
            }
        }

        public static Stream OpenAsyncRead(string fullPath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));
            return RethrowExceptionsAsIOException(operation: () =>
                new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous));
        }

        public static T RethrowExceptionsAsIOException<T>(Func<T> operation)
        {
            try
            {
                return operation();
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new IOException(e.Message, e);
            }
        }

        public static Stream CreateFileStreamChecked(Func<string, Stream> factory, string path, string paramName = null)
        {
            try
            {
                return factory(path);
            }
            catch (ArgumentNullException)
            {
                if (paramName == null)
                {
                    throw;
                }

                throw new ArgumentNullException(paramName);
            }
            catch (ArgumentException e)
            {
                if (paramName == null)
                {
                    throw;
                }

                throw new ArgumentException(e.Message, paramName);
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new IOException(e.Message, e);
            }
        }

        public static DateTime GetFileTimeStamp(string fullPath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));
            try
            {
                return File.GetLastWriteTimeUtc(fullPath);
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new IOException(e.Message, e);
            }
        }

        public static long GetFileLength(string fullPath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));
            try
            {
                FileInfo info = new FileInfo(fullPath);
                return info.Length;
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new IOException(e.Message, e);
            }
        }

        public static Stream OpenFileStream(string path)
        {
            try
            {
                return File.OpenRead(path);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (IOException e)
            {
                if (e.GetType().Name == "DirectoryNotFoundException")
                {
                    throw new FileNotFoundException(e.Message, path, e);
                }

                throw;
            }
            catch (Exception e)
            {
                throw new IOException(e.Message, e);
            }
        }
    }
}
