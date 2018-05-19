using System;
using System.Diagnostics;
using System.IO;

namespace Roslyn.Utilities
{
    public static class StreamExtensions
    {
        public static int TryReadAll(
            this Stream stream,
            byte[] buffer,
            int offset,
            int count)
        {
            Debug.Assert(count > 0);
            int totalBytesRead;
            int bytesRead = 0;
            for (totalBytesRead = 0; totalBytesRead < count; totalBytesRead += bytesRead)
            {
                bytesRead = stream.Read(buffer,
                    offset + totalBytesRead,
                    count - totalBytesRead);
                if (bytesRead == 0)
                {
                    break;
                }
            }

            return totalBytesRead;
        }

        public static byte[] ReadAllBytes(this Stream stream)
        {
            if (stream.CanSeek)
            {
                long length = stream.Length - stream.Position;
                if (length == 0)
                {
                    return Array.Empty<byte>();
                }

                byte[] buffer = new byte[length];
                int actualLength = TryReadAll(stream, buffer, 0, buffer.Length);
                Array.Resize(ref buffer, actualLength);
                return buffer;
            }

            MemoryStream memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
}
