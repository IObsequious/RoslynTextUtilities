using System.IO;

namespace Roslyn.Utilities
{
    public static class PlatformInformation
    {
        public static bool IsWindows
        {
            get
            {
                return Path.DirectorySeparatorChar == '\\';
            }
        }

        public static bool IsUnix
        {
            get
            {
                return Path.DirectorySeparatorChar == '/';
            }
        }
    }
}
