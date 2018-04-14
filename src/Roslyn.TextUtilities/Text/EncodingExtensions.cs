// -----------------------------------------------------------------------
// <copyright file="EncodingExtensions.cs" company="Ollon, LLC">
//     Copyright (c) 2018 Ollon, LLC. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System;
using System;
using System;
using System.Diagnostics;
using System.IO;

namespace System.Text
{
    internal static class EncodingExtensions
    {
        /// <summary>
        /// Get maximum char count needed to decode the entire stream.
        /// </summary>
        /// <param name="encoding"></param>
        /// <param name="stream"></param>
        /// <exception cref="IOException">Stream is so big that max char count can't fit in <see cref="int"/>.</exception> 
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
            throw new IOException();
#endif
        }

        public static bool TryGetMaxCharCount(this Encoding encoding, long length, out int maxCharCount)
        {
            maxCharCount = 0;
            if (length <= int.MaxValue)
            {
                try
                {
                    maxCharCount = encoding.GetMaxCharCount((int)length);
                    return true;
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Encoding does not provide a way to predict that max byte count would not
                    // fit in Int32 and we must therefore catch ArgumentOutOfRange to handle that
                    // case.
                }
            }

            return false;
        }
    }
}
