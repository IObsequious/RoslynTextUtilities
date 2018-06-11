using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.CodeAnalysis.Text
{
    public class StringTextWriter : SourceTextWriter
    {
        private readonly StringBuilder _builder;
        private readonly SourceHashAlgorithm _checksumAlgorithm;

        public StringTextWriter(Encoding encoding, SourceHashAlgorithm checksumAlgorithm, int capacity)
        {
            _builder = new StringBuilder(capacity);
            Encoding = encoding;
            _checksumAlgorithm = checksumAlgorithm;
        }

        public override Encoding Encoding { get; }

        public override SourceText ToSourceText()
        {
            return new StringText(_builder.ToString(), Encoding, checksumAlgorithm: _checksumAlgorithm);
        }

        public override void Write(char value)
        {
            _builder.Append(value);
        }

        public override void Write(string value)
        {
            _builder.Append(value);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            _builder.Append(buffer, index, count);
        }
    }
}
