using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities
{
#if COMPILERCORE
    using Resources = CodeAnalysisResources;
#else
    using Resources = WorkspacesResources;
#endif

    public sealed partial class ObjectReader : IDisposable
    {
        internal const byte VersionByte1 = 0b10101010;
        internal const byte VersionByte2 = 0b00001001;
        private readonly BinaryReader _reader;
        private readonly CancellationToken _cancellationToken;
        private ReaderReferenceMap<object> _objectReferenceMap;
        private ReaderReferenceMap<string> _stringReferenceMap;
        private readonly ObjectBinderSnapshot _binderSnapshot;
        private int _recursionDepth;

        private ObjectReader(
            Stream stream,
            CancellationToken cancellationToken)
        {
            Debug.Assert(BitConverter.IsLittleEndian);
            _reader = new BinaryReader(stream, Encoding.UTF8);
            _objectReferenceMap = ReaderReferenceMap<object>.Create();
            _stringReferenceMap = ReaderReferenceMap<string>.Create();
            _binderSnapshot = ObjectBinder.GetSnapshot();
            _cancellationToken = cancellationToken;
        }

        public static ObjectReader TryGetReader(
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            if (stream == null)
            {
                return null;
            }

            if (stream.ReadByte() != VersionByte1 ||
                stream.ReadByte() != VersionByte2)
            {
                return null;
            }

            return new ObjectReader(stream, cancellationToken);
        }

        public void Dispose()
        {
            _objectReferenceMap.Dispose();
            _stringReferenceMap.Dispose();
            _recursionDepth = 0;
        }

        public bool ReadBoolean()
        {
            return _reader.ReadBoolean();
        }

        public byte ReadByte()
        {
            return _reader.ReadByte();
        }

        public char ReadChar()
        {
            return (char) _reader.ReadUInt16();
        }

        public decimal ReadDecimal()
        {
            return _reader.ReadDecimal();
        }

        public double ReadDouble()
        {
            return _reader.ReadDouble();
        }

        public float ReadSingle()
        {
            return _reader.ReadSingle();
        }

        public int ReadInt32()
        {
            return _reader.ReadInt32();
        }

        public long ReadInt64()
        {
            return _reader.ReadInt64();
        }

        public sbyte ReadSByte()
        {
            return _reader.ReadSByte();
        }

        public short ReadInt16()
        {
            return _reader.ReadInt16();
        }

        public uint ReadUInt32()
        {
            return _reader.ReadUInt32();
        }

        public ulong ReadUInt64()
        {
            return _reader.ReadUInt64();
        }

        public ushort ReadUInt16()
        {
            return _reader.ReadUInt16();
        }

        public string ReadString()
        {
            return ReadStringValue();
        }

        public Guid ReadGuid()
        {
            ObjectWriter.GuidAccessor accessor = new ObjectWriter.GuidAccessor
            {
                Low64 = ReadInt64(),
                High64 = ReadInt64()
            };
            return accessor.Guid;
        }

        public object ReadValue()
        {
            int oldDepth = _recursionDepth;
            _recursionDepth++;
            object value;
            if (_recursionDepth % ObjectWriter.MaxRecursionDepth == 0)
            {
                Task<object> task = Task.Factory.StartNew(
                    function: () => ReadValueWorker(),
                    cancellationToken: _cancellationToken,
                    creationOptions: TaskCreationOptions.LongRunning,
                    scheduler: TaskScheduler.Default);
                value = task.GetAwaiter().GetResult();
            }
            else
            {
                value = ReadValueWorker();
            }

            _recursionDepth--;
            Debug.Assert(oldDepth == _recursionDepth);
            return value;
        }

        private object ReadValueWorker()
        {
            ObjectWriter.EncodingKind kind = (ObjectWriter.EncodingKind) _reader.ReadByte();
            switch (kind)
            {
                case ObjectWriter.EncodingKind.Null:
                    return null;
                case ObjectWriter.EncodingKind.Boolean_True:
                    return true;
                case ObjectWriter.EncodingKind.Boolean_False:
                    return false;
                case ObjectWriter.EncodingKind.Int8:
                    return _reader.ReadSByte();
                case ObjectWriter.EncodingKind.UInt8:
                    return _reader.ReadByte();
                case ObjectWriter.EncodingKind.Int16:
                    return _reader.ReadInt16();
                case ObjectWriter.EncodingKind.UInt16:
                    return _reader.ReadUInt16();
                case ObjectWriter.EncodingKind.Int32:
                    return _reader.ReadInt32();
                case ObjectWriter.EncodingKind.Int32_1Byte:
                    return (int) _reader.ReadByte();
                case ObjectWriter.EncodingKind.Int32_2Bytes:
                    return (int) _reader.ReadUInt16();
                case ObjectWriter.EncodingKind.Int32_0:
                case ObjectWriter.EncodingKind.Int32_1:
                case ObjectWriter.EncodingKind.Int32_2:
                case ObjectWriter.EncodingKind.Int32_3:
                case ObjectWriter.EncodingKind.Int32_4:
                case ObjectWriter.EncodingKind.Int32_5:
                case ObjectWriter.EncodingKind.Int32_6:
                case ObjectWriter.EncodingKind.Int32_7:
                case ObjectWriter.EncodingKind.Int32_8:
                case ObjectWriter.EncodingKind.Int32_9:
                case ObjectWriter.EncodingKind.Int32_10:
                    return (int) kind - (int) ObjectWriter.EncodingKind.Int32_0;
                case ObjectWriter.EncodingKind.UInt32:
                    return _reader.ReadUInt32();
                case ObjectWriter.EncodingKind.UInt32_1Byte:
                    return (uint) _reader.ReadByte();
                case ObjectWriter.EncodingKind.UInt32_2Bytes:
                    return (uint) _reader.ReadUInt16();
                case ObjectWriter.EncodingKind.UInt32_0:
                case ObjectWriter.EncodingKind.UInt32_1:
                case ObjectWriter.EncodingKind.UInt32_2:
                case ObjectWriter.EncodingKind.UInt32_3:
                case ObjectWriter.EncodingKind.UInt32_4:
                case ObjectWriter.EncodingKind.UInt32_5:
                case ObjectWriter.EncodingKind.UInt32_6:
                case ObjectWriter.EncodingKind.UInt32_7:
                case ObjectWriter.EncodingKind.UInt32_8:
                case ObjectWriter.EncodingKind.UInt32_9:
                case ObjectWriter.EncodingKind.UInt32_10:
                    return (uint) ((int) kind - (int) ObjectWriter.EncodingKind.UInt32_0);
                case ObjectWriter.EncodingKind.Int64:
                    return _reader.ReadInt64();
                case ObjectWriter.EncodingKind.UInt64:
                    return _reader.ReadUInt64();
                case ObjectWriter.EncodingKind.Float4:
                    return _reader.ReadSingle();
                case ObjectWriter.EncodingKind.Float8:
                    return _reader.ReadDouble();
                case ObjectWriter.EncodingKind.Decimal:
                    return _reader.ReadDecimal();
                case ObjectWriter.EncodingKind.Char:
                    return (char) _reader.ReadUInt16();
                case ObjectWriter.EncodingKind.StringUtf8:
                case ObjectWriter.EncodingKind.StringUtf16:
                case ObjectWriter.EncodingKind.StringRef_4Bytes:
                case ObjectWriter.EncodingKind.StringRef_1Byte:
                case ObjectWriter.EncodingKind.StringRef_2Bytes:
                    return ReadStringValue(kind);
                case ObjectWriter.EncodingKind.ObjectRef_4Bytes:
                    return _objectReferenceMap.GetValue(_reader.ReadInt32());
                case ObjectWriter.EncodingKind.ObjectRef_1Byte:
                    return _objectReferenceMap.GetValue(_reader.ReadByte());
                case ObjectWriter.EncodingKind.ObjectRef_2Bytes:
                    return _objectReferenceMap.GetValue(_reader.ReadUInt16());
                case ObjectWriter.EncodingKind.Object:
                    return ReadObject();
                case ObjectWriter.EncodingKind.DateTime:
                    return DateTime.FromBinary(_reader.ReadInt64());
                case ObjectWriter.EncodingKind.Array:
                case ObjectWriter.EncodingKind.Array_0:
                case ObjectWriter.EncodingKind.Array_1:
                case ObjectWriter.EncodingKind.Array_2:
                case ObjectWriter.EncodingKind.Array_3:
                    return ReadArray(kind);
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private struct ReaderReferenceMap<T> where T : class
        {
            private readonly List<T> _values;
            internal static readonly ObjectPool<List<T>> s_objectListPool
                = new ObjectPool<List<T>>(factory: () => new List<T>(20));

            private ReaderReferenceMap(List<T> values)
            {
                _values = values;
            }

            public static ReaderReferenceMap<T> Create()
            {
                return new ReaderReferenceMap<T>(s_objectListPool.Allocate());
            }

            public void Dispose()
            {
                _values.Clear();
                s_objectListPool.Free(_values);
            }

            public int GetNextObjectId()
            {
                int id = _values.Count;
                _values.Add(null);
                return id;
            }

            public void AddValue(T value)
            {
                _values.Add(value);
            }

            public void AddValue(int index, T value)
            {
                _values[index] = value;
            }

            public T GetValue(int referenceId)
            {
                return _values[referenceId];
            }
        }

        public uint ReadCompressedUInt()
        {
            byte info = _reader.ReadByte();
            byte marker = (byte) (info & ObjectWriter.ByteMarkerMask);
            byte byte0 = (byte) (info & ~ObjectWriter.ByteMarkerMask);
            if (marker == ObjectWriter.Byte1Marker)
            {
                return byte0;
            }

            if (marker == ObjectWriter.Byte2Marker)
            {
                byte byte1 = _reader.ReadByte();
                return ((uint) byte0 << 8) | byte1;
            }

            if (marker == ObjectWriter.Byte4Marker)
            {
                byte byte1 = _reader.ReadByte();
                byte byte2 = _reader.ReadByte();
                byte byte3 = _reader.ReadByte();
                return ((uint) byte0 << 24) | ((uint) byte1 << 16) | ((uint) byte2 << 8) | byte3;
            }

            throw ExceptionUtilities.UnexpectedValue(marker);
        }

        private string ReadStringValue()
        {
            ObjectWriter.EncodingKind kind = (ObjectWriter.EncodingKind) _reader.ReadByte();
            return kind == ObjectWriter.EncodingKind.Null ? null : ReadStringValue(kind);
        }

        private string ReadStringValue(ObjectWriter.EncodingKind kind)
        {
            switch (kind)
            {
                case ObjectWriter.EncodingKind.StringRef_1Byte:
                    return _stringReferenceMap.GetValue(_reader.ReadByte());
                case ObjectWriter.EncodingKind.StringRef_2Bytes:
                    return _stringReferenceMap.GetValue(_reader.ReadUInt16());
                case ObjectWriter.EncodingKind.StringRef_4Bytes:
                    return _stringReferenceMap.GetValue(_reader.ReadInt32());
                case ObjectWriter.EncodingKind.StringUtf16:
                case ObjectWriter.EncodingKind.StringUtf8:
                    return ReadStringLiteral(kind);
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private unsafe string ReadStringLiteral(ObjectWriter.EncodingKind kind)
        {
            string value;
            if (kind == ObjectWriter.EncodingKind.StringUtf8)
            {
                value = _reader.ReadString();
            }
            else
            {
                int characterCount = (int) ReadCompressedUInt();
                byte[] bytes = _reader.ReadBytes(characterCount * sizeof(char));
                fixed (byte* bytesPtr = bytes)
                {
                    value = new string((char*) bytesPtr, 0, characterCount);
                }
            }

            _stringReferenceMap.AddValue(value);
            return value;
        }

        private Array ReadArray(ObjectWriter.EncodingKind kind)
        {
            int length;
            switch (kind)
            {
                case ObjectWriter.EncodingKind.Array_0:
                    length = 0;
                    break;
                case ObjectWriter.EncodingKind.Array_1:
                    length = 1;
                    break;
                case ObjectWriter.EncodingKind.Array_2:
                    length = 2;
                    break;
                case ObjectWriter.EncodingKind.Array_3:
                    length = 3;
                    break;
                default:
                    length = (int) ReadCompressedUInt();
                    break;
            }

            ObjectWriter.EncodingKind elementKind = (ObjectWriter.EncodingKind) _reader.ReadByte();
            var elementType = ObjectWriter.s_reverseTypeMap[(int) elementKind];
            if (elementType != null)
            {
                return ReadPrimitiveTypeArrayElements(elementType, elementKind, length);
            }

            elementType = ReadTypeAfterTag();
            Array array = Array.CreateInstance(elementType, length);
            for (int i = 0; i < length; ++i)
            {
                object value = ReadValue();
                array.SetValue(value, i);
            }

            return array;
        }

        private Array ReadPrimitiveTypeArrayElements(Type type, ObjectWriter.EncodingKind kind, int length)
        {
            Debug.Assert(ObjectWriter.s_reverseTypeMap[(int) kind] == type);
            if (type == typeof(byte))
            {
                return _reader.ReadBytes(length);
            }

            if (type == typeof(char))
            {
                return _reader.ReadChars(length);
            }

            if (type == typeof(string))
            {
                return ReadStringArrayElements(CreateArray<string>(length));
            }

            if (type == typeof(bool))
            {
                return ReadBooleanArrayElements(CreateArray<bool>(length));
            }

            switch (kind)
            {
                case ObjectWriter.EncodingKind.Int8:
                    return ReadInt8ArrayElements(CreateArray<sbyte>(length));
                case ObjectWriter.EncodingKind.Int16:
                    return ReadInt16ArrayElements(CreateArray<short>(length));
                case ObjectWriter.EncodingKind.Int32:
                    return ReadInt32ArrayElements(CreateArray<int>(length));
                case ObjectWriter.EncodingKind.Int64:
                    return ReadInt64ArrayElements(CreateArray<long>(length));
                case ObjectWriter.EncodingKind.UInt16:
                    return ReadUInt16ArrayElements(CreateArray<ushort>(length));
                case ObjectWriter.EncodingKind.UInt32:
                    return ReadUInt32ArrayElements(CreateArray<uint>(length));
                case ObjectWriter.EncodingKind.UInt64:
                    return ReadUInt64ArrayElements(CreateArray<ulong>(length));
                case ObjectWriter.EncodingKind.Float4:
                    return ReadFloat4ArrayElements(CreateArray<float>(length));
                case ObjectWriter.EncodingKind.Float8:
                    return ReadFloat8ArrayElements(CreateArray<double>(length));
                case ObjectWriter.EncodingKind.Decimal:
                    return ReadDecimalArrayElements(CreateArray<decimal>(length));
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private bool[] ReadBooleanArrayElements(bool[] array)
        {
            int wordLength = BitVector.WordsRequired(array.Length);
            int count = 0;
            for (int i = 0; i < wordLength; i++)
            {
                uint word = _reader.ReadUInt32();
                for (int p = 0; p < BitVector.BitsPerWord; p++)
                {
                    if (count >= array.Length)
                    {
                        return array;
                    }

                    array[count++] = BitVector.IsTrue(word, p);
                }
            }

            return array;
        }

        private static T[] CreateArray<T>(int length)
        {
            if (length == 0)
            {
                return Array.Empty<T>();
            }

            return new T[length];
        }

        private string[] ReadStringArrayElements(string[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = ReadStringValue();
            }

            return array;
        }

        private sbyte[] ReadInt8ArrayElements(sbyte[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = _reader.ReadSByte();
            }

            return array;
        }

        private short[] ReadInt16ArrayElements(short[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = _reader.ReadInt16();
            }

            return array;
        }

        private int[] ReadInt32ArrayElements(int[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = _reader.ReadInt32();
            }

            return array;
        }

        private long[] ReadInt64ArrayElements(long[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = _reader.ReadInt64();
            }

            return array;
        }

        private ushort[] ReadUInt16ArrayElements(ushort[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = _reader.ReadUInt16();
            }

            return array;
        }

        private uint[] ReadUInt32ArrayElements(uint[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = _reader.ReadUInt32();
            }

            return array;
        }

        private ulong[] ReadUInt64ArrayElements(ulong[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = _reader.ReadUInt64();
            }

            return array;
        }

        private decimal[] ReadDecimalArrayElements(decimal[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = _reader.ReadDecimal();
            }

            return array;
        }

        private float[] ReadFloat4ArrayElements(float[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = _reader.ReadSingle();
            }

            return array;
        }

        private double[] ReadFloat8ArrayElements(double[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = _reader.ReadDouble();
            }

            return array;
        }

        public Type ReadType()
        {
            _reader.ReadByte();
            return Type.GetType(ReadString());
        }

        private Type ReadTypeAfterTag()
        {
            return _binderSnapshot.GetTypeFromId(ReadInt32());
        }

        private object ReadObject()
        {
            int objectId = _objectReferenceMap.GetNextObjectId();
            Func<ObjectReader, object> typeReader = _binderSnapshot.GetTypeReaderFromId(ReadInt32());
            object instance = typeReader(this);
            _objectReferenceMap.AddValue(objectId, instance);
            return instance;
        }

        private static Exception DeserializationReadIncorrectNumberOfValuesException(string typeName)
        {
            throw new InvalidOperationException(string.Format(Resources.Deserialization_reader_for_0_read_incorrect_number_of_values,
                typeName));
        }

        private static Exception NoSerializationTypeException(string typeName)
        {
            return new InvalidOperationException(
                string.Format(Resources.The_type_0_is_not_understood_by_the_serialization_binder, typeName));
        }

        private static Exception NoSerializationReaderException(string typeName)
        {
            return new InvalidOperationException(string.Format(Resources.Cannot_serialize_type_0, typeName));
        }
    }
}
