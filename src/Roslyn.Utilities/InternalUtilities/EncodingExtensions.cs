using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Roslyn.Utilities
{
    public static class EncodingExtensions
    {
        public static int GetMaxCharCountOrThrowIfHuge(this Encoding encoding, Stream stream)
        {
            Debug.Assert(stream.CanSeek);
            long length = stream.Length;
            int maxCharCount;
            if (encoding.TryGetMaxCharCount(length, out maxCharCount))
            {
                return maxCharCount;
            }
#if WORKSPACE
            throw new IOException(WorkspacesResources.Stream_is_too_long);
#else
            throw new IOException(CodeAnalysisResources.StreamIsTooLong);
#endif
        }

        public static bool TryGetMaxCharCount(this Encoding encoding, long length, out int maxCharCount)
        {
            maxCharCount = 0;
            if (length <= int.MaxValue)
            {
                try
                {
                    maxCharCount = encoding.GetMaxCharCount((int) length);
                    return true;
                }
                catch (ArgumentOutOfRangeException)
                {
                }
            }

            return false;
        }
    }
}
