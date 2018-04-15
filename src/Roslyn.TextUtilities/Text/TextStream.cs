using System.Collections.Generic;

namespace System.Text
{
    public class TextStream : IStream<char>
    {
        private int _offset;
        private readonly List<char> _characters;

        public TextStream()
        {
            _offset = 0;
            _characters = new List<char>();
        }

        public TextStream(IEnumerable<char> characters)
        {
            _offset = 0;
            _characters = new List<char>(characters);
        }

        public char this[int position]
        {
            get
            {
                return _characters[position];
            }
        }

        public int Length
        {
            get
            {
                return _characters.Count;
            }
        }

        public int Position
        {
            get
            {
                return _offset;
            }
        }

        public void Advance()
        {
            _offset++;
        }

        public char Peek()
        {
            char ch = char.MaxValue;
            int position = Position;
            if (position < Length)
            {
                ch = this[position];
            }

            return ch;
        }

        public char Read()
        {
            char ch = char.MaxValue;
            int position = Position;
            if (position < Length)
            {
                ch = this[position];
                _offset++;
            }

            return ch;
        }

        public void Write(char item)
        {
            _characters.Add(item);
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return new string(_characters.ToArray());
        }
    }
}