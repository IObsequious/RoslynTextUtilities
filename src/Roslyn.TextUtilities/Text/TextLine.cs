// -----------------------------------------------------------------------
// <copyright file="TextLine.cs" company="Ollon, LLC">
//     Copyright (c) 2018 Ollon, LLC. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System;
using System;
using System;

namespace System.Text
{
    /// <summary>
    /// Information about the character boundaries of a single line of text.
    /// </summary>
    public struct TextLine : IEquatable<TextLine>
    {
        private readonly int _endIncludingBreaks;

        private TextLine(SourceText text, int start, int endIncludingBreaks)
        {
            Text = text;
            Start = start;
            _endIncludingBreaks = endIncludingBreaks;
        }

        /// <summary>
        /// Creates a <see cref="TextLine"/> instance.
        /// </summary>
        /// <param name="text">The source text.</param>
        /// <param name="span">The span of the line.</param>
        /// <returns>An instance of <see cref="TextLine"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The span does not represent a text line.</exception>
        public static TextLine FromSpan(SourceText text, TextSpan span)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (span.Start > text.Length || span.Start < 0 || span.End > text.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(span));
            }

            if (text.Length > 0)
            {
                // check span is start of line
                if (span.Start > 0 && !TextUtilities.IsAnyLineBreakCharacter(text[span.Start - 1]))
                {
                    throw new ArgumentOutOfRangeException(nameof(span));
                }

                bool endIncludesLineBreak = false;
                if (span.End > span.Start)
                {
                    endIncludesLineBreak = TextUtilities.IsAnyLineBreakCharacter(text[span.End - 1]);
                }

                if (!endIncludesLineBreak && span.End < text.Length)
                {
                    var lineBreakLen = TextUtilities.GetLengthOfLineBreak(text, span.End);
                    if (lineBreakLen > 0)
                    {
                        // adjust span to include line breaks
                        endIncludesLineBreak = true;
                        span = new TextSpan(span.Start, span.Length + lineBreakLen);
                    }
                }

                // check end of span is at end of line
                if (span.End < text.Length && !endIncludesLineBreak)
                {
                    throw new ArgumentOutOfRangeException(nameof(span));
                }

                return new TextLine(text, span.Start, span.End);
            }

            return new TextLine(text, 0, 0);
        }

        /// <summary>
        /// Gets the source text.
        /// </summary>
        public SourceText Text { get; }

        /// <summary>
        /// Gets the zero-based line number.
        /// </summary>
        public int LineNumber
        {
            get
            {
                return Text?.Lines.IndexOf(Start) ?? 0;
            }
        }

        /// <summary>
        /// Gets the start position of the line.
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// Gets the end position of the line not including the line break.
        /// </summary>
        public int End
        {
            get
            {
                return _endIncludingBreaks - LineBreakLength;
            }
        }

        private int LineBreakLength
        {
            get
            {
                if (Text == null || Text.Length == 0 || _endIncludingBreaks == Start)
                {
                    return 0;
                }

                int startLineBreak;
                int lineBreakLength;
                TextUtilities.GetStartAndLengthOfLineBreakEndingAt(Text, _endIncludingBreaks - 1, out startLineBreak, out lineBreakLength);
                return lineBreakLength;
            }
        }

        /// <summary>
        /// Gets the end position of the line including the line break.
        /// </summary>
        public int EndIncludingLineBreak
        {
            get
            {
                return _endIncludingBreaks;
            }
        }

        /// <summary>
        /// Gets the line span not including the line break.
        /// </summary>
        public TextSpan Span
        {
            get
            {
                return TextSpan.FromBounds(Start, End);
            }
        }

        /// <summary>
        /// Gets the line span including the line break.
        /// </summary>
        public TextSpan SpanIncludingLineBreak
        {
            get
            {
                return TextSpan.FromBounds(Start, EndIncludingLineBreak);
            }
        }

        public override string ToString()
        {
            if (Text == null || Text.Length == 0)
            {
                return string.Empty;
            }

            return Text.ToString(Span);
        }

        public static bool operator ==(TextLine left, TextLine right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TextLine left, TextLine right)
        {
            return !left.Equals(right);
        }

        public bool Equals(TextLine other)
        {
            return other.Text == Text && other.Start == Start && other._endIncludingBreaks == _endIncludingBreaks;
        }

        public override bool Equals(object obj)
        {
            if (obj is TextLine)
            {
                return Equals((TextLine)obj);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Text, Hash.Combine(Start, _endIncludingBreaks));
        }
    }
}
