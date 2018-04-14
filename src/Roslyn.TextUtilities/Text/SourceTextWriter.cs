// -----------------------------------------------------------------------
// <copyright file="SourceTextWriter.cs" company="Ollon, LLC">
//     Copyright (c) 2018 Ollon, LLC. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.IO;

namespace System.Text
{
    internal abstract class SourceTextWriter : TextWriter
    {
        public abstract SourceText ToSourceText();

        public static SourceTextWriter Create(Encoding encoding, SourceHashAlgorithm checksumAlgorithm, int length)
        {
            if (length < SourceText.LargeObjectHeapLimitInChars)
            {
                return new StringTextWriter(encoding, checksumAlgorithm, length);
            }

            return new LargeTextWriter(encoding, checksumAlgorithm, length);
        }
    }
}
