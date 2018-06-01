﻿using System;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Text
{
    public sealed class LargeTextWriter : SourceTextWriter
    {
        private readonly Encoding _encoding;
        private readonly SourceHashAlgorithm _checksumAlgorithm;
        private readonly ArrayBuilder<char[]> _chunks;
        private int _bufferSize;
        private char[] _buffer;
        private int _currentUsed;

        public LargeTextWriter(Encoding encoding, SourceHashAlgorithm checksumAlgorithm, int length)
        {
            _encoding = encoding;
            _checksumAlgorithm = checksumAlgorithm;
            _chunks = ArrayBuilder<char[]>.GetInstance(1 + length / LargeText.ChunkSize);
            _bufferSize = Math.Min(LargeText.ChunkSize, length);
        }

        public override SourceText ToSourceText()
        {
            Flush();
            return new LargeText(_chunks.ToImmutableAndFree(),
                _encoding,
                default(ImmutableArray<byte>),
                _checksumAlgorithm,
                default(ImmutableArray<byte>));
        }

        public override Encoding Encoding
        {
            get
            {
                return _encoding;
            }
        }

        public bool CanFitInAllocatedBuffer(int chars)
        {
            return _buffer != null && chars <= _buffer.Length - _currentUsed;
        }

        public override void Write(char value)
        {
            if (_buffer != null && _currentUsed < _buffer.Length)
            {
                _buffer[_currentUsed] = value;
                _currentUsed++;
            }
            else
            {
                Write(new[] {value}, 0, 1);
            }
        }

        public override void Write(string value)
        {
            if (value != null)
            {
                int count = value.Length;
                int index = 0;
                while (count > 0)
                {
                    EnsureBuffer();
                    int remaining = _buffer.Length - _currentUsed;
                    int copy = Math.Min(remaining, count);
                    value.CopyTo(index, _buffer, _currentUsed, copy);
                    _currentUsed += copy;
                    index += copy;
                    count -= copy;
                    if (_currentUsed == _buffer.Length)
                    {
                        Flush();
                    }
                }
            }
        }

        public override void Write(char[] chars, int index, int count)
        {
            if (index < 0 || index >= chars.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (count < 0 || count > chars.Length - index)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            while (count > 0)
            {
                EnsureBuffer();
                int remaining = _buffer.Length - _currentUsed;
                int copy = Math.Min(remaining, count);
                Array.Copy(chars, index, _buffer, _currentUsed, copy);
                _currentUsed += copy;
                index += copy;
                count -= copy;
                if (_currentUsed == _buffer.Length)
                {
                    Flush();
                }
            }
        }

        public void AppendChunk(char[] chunk)
        {
            if (CanFitInAllocatedBuffer(chunk.Length))
            {
                Write(chunk, 0, chunk.Length);
            }
            else
            {
                Flush();
                _chunks.Add(chunk);
            }
        }

        public override void Flush()
        {
            if (_buffer != null && _currentUsed > 0)
            {
                if (_currentUsed < _buffer.Length)
                {
                    Array.Resize(ref _buffer, _currentUsed);
                }

                _chunks.Add(_buffer);
                _buffer = null;
                _currentUsed = 0;
            }
        }

        private void EnsureBuffer()
        {
            if (_buffer == null)
            {
                _buffer = new char[_bufferSize];
            }
        }
    }
}