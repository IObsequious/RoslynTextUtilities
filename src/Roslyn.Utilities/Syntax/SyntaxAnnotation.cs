
using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{

    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public sealed class SyntaxAnnotation : IObjectWritable, IEquatable<SyntaxAnnotation>
    {

        static SyntaxAnnotation()
        {
            ObjectBinder.RegisterTypeReader(typeof(SyntaxAnnotation), r => new SyntaxAnnotation(r));
        }

        public static SyntaxAnnotation ElasticAnnotation { get; } = new SyntaxAnnotation();
        private readonly long _id;
        private static long s_nextId;

        public string Kind
        {
            get;
        }

        public string Data
        {
            get;
        }

        public SyntaxAnnotation()
        {
            _id = System.Threading.Interlocked.Increment(ref s_nextId);
        }

        public SyntaxAnnotation(string kind)
                    : this()
        {
            Kind = kind;
        }

        public SyntaxAnnotation(string kind, string data)
                    : this(kind)
        {
            Data = data;
        }

        private SyntaxAnnotation(ObjectReader reader)
        {
            _id = reader.ReadInt64();
            Kind = reader.ReadString();
            Data = reader.ReadString();
        }

        void IObjectWritable.WriteTo(ObjectWriter writer)
        {
            writer.WriteInt64(_id);
            writer.WriteString(Kind);
            writer.WriteString(Data);
        }

        private string GetDebuggerDisplay()
        {
            return string.Format("Annotation: Kind='{0}' Data='{1}'", Kind ?? "", Data ?? "");
        }

        public bool Equals(SyntaxAnnotation other)
        {
            return (object)other != null && _id == other._id;
        }
        public static bool operator ==(SyntaxAnnotation left, SyntaxAnnotation right)
        {
            if ((object)left == (object)right)
            {
                return true;
            }
            if ((object)left == null || (object)right == null)
            {
                return false;
            }
            return left.Equals(right);
        }
        public static bool operator !=(SyntaxAnnotation left, SyntaxAnnotation right)
        {
            if ((object)left == (object)right)
            {
                return false;
            }
            if ((object)left == null || (object)right == null)
            {
                return true;
            }
            return !left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SyntaxAnnotation);
        }

        public override int GetHashCode()
        {
            return _id.GetHashCode();
        }
    }
}
