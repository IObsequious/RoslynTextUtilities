using System.CodeDom;
using System.IO;

namespace System.Text
{
    public class CharStream : Stream
    {
        private int _capacity;
        private int _charOffset;
        private readonly int _charLength;
        private readonly char[] _charBuffer;

        private int _byteOffset;
        private int _byteLength;
        private byte[] _byteBuffer;

        public CharStream()
        {
            _capacity = 0;
            _charLength = 0;
            _charOffset = 0;
            _charBuffer = new char[] { };
            Init();
        }

        public CharStream(string text)
        {
            _charBuffer = text.ToCharArray();
            _charOffset = 0;
            _charLength = text.Length;
            Init();
        }

        public void Init()
        {
            _capacity = _charLength;
            _byteBuffer = TextUtilities.ConvertToBytes(_charBuffer);
            _byteOffset = _charOffset * 2;
            _byteLength = _byteBuffer.Length;
        }

        /// <summary>When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.</summary>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        public override void Flush()
        {
        }

        /// <summary>When overridden in a derived class, sets the position within the current stream.</summary>
        /// <param name="offset">A byte offset relative to the <paramref name="origin" /> parameter. </param>
        /// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin" /> indicating the reference point used to obtain the new position. </param>
        /// <returns>The new position within the current stream.</returns>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.NotSupportedException">The stream does not support seeking, such as if the stream is constructed from a pipe or console output. </exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (offset > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(offset), "ArgumentOutOfRange_StreamLength");
            switch (origin)
            {
                case SeekOrigin.Begin:
                {
                    int tempPosition = unchecked((int)offset);
                    if (offset < 0)
                        throw new IOException("IO.IO_SeekBeforeBegin");
                    _byteOffset = tempPosition;
                    break;
                }
                case SeekOrigin.Current:
                {
                    int tempPosition = unchecked(_byteOffset + (int)offset);
                    _byteOffset = tempPosition;
                    break;
                }
                case SeekOrigin.End:
                {
                    int tempPosition = unchecked(_byteLength + (int)offset);
                    _byteOffset = tempPosition;
                    break;
                }
                default:
                    throw new ArgumentException("Argument_InvalidSeekOrigin");
            }

            return _byteOffset;
        }

        /// <summary>When overridden in a derived class, sets the length of the current stream.</summary>
        /// <param name="value">The desired length of the current stream in bytes. </param>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.NotSupportedException">The stream does not support both writing and seeking, such as if the stream is constructed from a pipe or console output. </exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        public override void SetLength(long value)
        {
        }

        /// <summary>When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.</summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset" /> and (<paramref name="offset" /> + <paramref name="count" /> - 1) replaced by the bytes read from the current source. </param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer" /> at which to begin storing the data read from the current stream. </param>
        /// <param name="count">The maximum number of bytes to be read from the current stream. </param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
        /// <exception cref="T:System.ArgumentException">The sum of <paramref name="offset" /> and <paramref name="count" /> is larger than the buffer length. </exception>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="buffer" /> is <see langword="null" />. </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="offset" /> or <paramref name="count" /> is negative. </exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading. </exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int n = _byteLength - _byteOffset;
            if (n > count) n = count;
            if (n <= 0)
                return 0;

            int byteCount = n;

            while (--byteCount >= 0)
            {
                buffer[offset + byteCount] = _byteBuffer[_byteOffset + byteCount];
            }

            _byteOffset += n;

            _charOffset += (n / 2);

            return n;
        }

        /// <summary>When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.</summary>
        /// <param name="buffer">An array of bytes. This method copies <paramref name="count" /> bytes from <paramref name="buffer" /> to the current stream. </param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer" /> at which to begin copying bytes to the current stream. </param>
        /// <param name="count">The number of bytes to be written to the current stream. </param>
        /// <exception cref="T:System.ArgumentException">The sum of <paramref name="offset" /> and <paramref name="count" /> is greater than the buffer length.</exception>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="buffer" />  is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="offset" /> or <paramref name="count" /> is negative.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occured, such as the specified file cannot be found.</exception>
        /// <exception cref="T:System.NotSupportedException">The stream does not support writing.</exception>
        /// <exception cref="T:System.ObjectDisposedException">
        /// <see cref="M:System.IO.Stream.Write(System.Byte[],System.Int32,System.Int32)" /> was called after the stream was closed.</exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            int newSize = _byteOffset + count;
            if (_byteBuffer.Length < newSize)
            {
                Array.Resize(ref _byteBuffer, newSize);
                _byteLength = _byteBuffer.Length;
            }

            int byteCount = count;
            while (--byteCount >= 0)
                _byteBuffer[_byteOffset + byteCount] = buffer[offset + byteCount];

            _byteOffset = _byteLength;
        }

        /// <summary>When overridden in a derived class, gets a value indicating whether the current stream supports reading.</summary>
        /// <returns>
        /// <see langword="true" /> if the stream supports reading; otherwise, <see langword="false" />.</returns>
        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        /// <summary>When overridden in a derived class, gets a value indicating whether the current stream supports seeking.</summary>
        /// <returns>
        /// <see langword="true" /> if the stream supports seeking; otherwise, <see langword="false" />.</returns>
        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        /// <summary>When overridden in a derived class, gets a value indicating whether the current stream supports writing.</summary>
        /// <returns>
        /// <see langword="true" /> if the stream supports writing; otherwise, <see langword="false" />.</returns>
        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        /// <summary>When overridden in a derived class, gets the length in bytes of the stream.</summary>
        /// <returns>A long value representing the length of the stream in bytes.</returns>
        /// <exception cref="T:System.NotSupportedException">A class derived from <see langword="Stream" /> does not support seeking. </exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        public override long Length
        {
            get
            {
                return _charLength;
            }
        }

        /// <summary>When overridden in a derived class, gets or sets the position within the current stream.</summary>
        /// <returns>The current position within the stream.</returns>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.NotSupportedException">The stream does not support seeking. </exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        public override long Position
        {
            get
            {
                return _charOffset;
            }
            set
            {

            }
        }

        public int CharPosition => _charOffset;

        public int BytePosition => _byteOffset;

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return TextUtilities.ConvertToString(_byteBuffer);
        }
    }
}
