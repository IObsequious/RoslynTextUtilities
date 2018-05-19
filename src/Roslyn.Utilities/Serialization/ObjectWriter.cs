using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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

    public sealed partial class ObjectWriter : IDisposable
    {
        private readonly BinaryWriter _writer;
        private readonly CancellationToken _cancellationToken;
        private WriterReferenceMap _objectReferenceMap;
        private WriterReferenceMap _stringReferenceMap;
        private readonly ObjectBinderSnapshot _binderSnapshot;
        private int _recursionDepth;
        internal const int MaxRecursionDepth = 50;

        public ObjectWriter(
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(BitConverter.IsLittleEndian);
            _writer = new BinaryWriter(stream, Encoding.UTF8);
            _objectReferenceMap = new WriterReferenceMap(false);
            _stringReferenceMap = new WriterReferenceMap(true);
            _cancellationToken = cancellationToken;
            _binderSnapshot = ObjectBinder.GetSnapshot();
            WriteVersion();
        }

        private void WriteVersion()
        {
            _writer.Write(ObjectReader.VersionByte1);
            _writer.Write(ObjectReader.VersionByte2);
        }

        public void Dispose()
        {
            _objectReferenceMap.Dispose();
            _stringReferenceMap.Dispose();
            _recursionDepth = 0;
        }

        public void WriteBoolean(bool value)
        {
            _writer.Write(value);
        }

        public void WriteByte(byte value)
        {
            _writer.Write(value);
        }

        public void WriteChar(char ch)
        {
            _writer.Write((ushort) ch);
        }

        public void WriteDecimal(decimal value)
        {
            _writer.Write(value);
        }

        public void WriteDouble(double value)
        {
            _writer.Write(value);
        }

        public void WriteSingle(float value)
        {
            _writer.Write(value);
        }

        public void WriteInt32(int value)
        {
            _writer.Write(value);
        }

        public void WriteInt64(long value)
        {
            _writer.Write(value);
        }

        public void WriteSByte(sbyte value)
        {
            _writer.Write(value);
        }

        public void WriteInt16(short value)
        {
            _writer.Write(value);
        }

        public void WriteUInt32(uint value)
        {
            _writer.Write(value);
        }

        public void WriteUInt64(ulong value)
        {
            _writer.Write(value);
        }

        public void WriteUInt16(ushort value)
        {
            _writer.Write(value);
        }

        public void WriteString(string value)
        {
            WriteStringValue(value);
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct GuidAccessor
        {
            [FieldOffset(0)] public Guid Guid;
            [FieldOffset(0)] public long Low64;
            [FieldOffset(8)] public long High64;
        }

        public void WriteGuid(Guid guid)
        {
            GuidAccessor accessor = new GuidAccessor {Guid = guid};
            WriteInt64(accessor.Low64);
            WriteInt64(accessor.High64);
        }

        public void WriteValue(object value)
        {
            Debug.Assert(value == null || !value.GetType().GetTypeInfo().IsEnum,
                message: "Enum should not be written with WriteValue.  Write them as ints instead.");
            if (value == null)
            {
                _writer.Write((byte) EncodingKind.Null);
                return;
            }

            Type type = value.GetType();
            TypeInfo typeInfo = type.GetTypeInfo();
            Debug.Assert(!typeInfo.IsEnum, message: "Enums should not be written with WriteObject.  Write them out as integers instead.");
            if (typeInfo.IsPrimitive)
            {
                if (value.GetType() == typeof(int))
                {
                    WriteEncodedInt32((int) value);
                }
                else if (value.GetType() == typeof(double))
                {
                    _writer.Write((byte) EncodingKind.Float8);
                    _writer.Write((double) value);
                }
                else if (value.GetType() == typeof(bool))
                {
                    _writer.Write((byte) ((bool) value ? EncodingKind.Boolean_True : EncodingKind.Boolean_False));
                }
                else if (value.GetType() == typeof(char))
                {
                    _writer.Write((byte) EncodingKind.Char);
                    _writer.Write((ushort) (char) value);
                }
                else if (value.GetType() == typeof(byte))
                {
                    _writer.Write((byte) EncodingKind.UInt8);
                    _writer.Write((byte) value);
                }
                else if (value.GetType() == typeof(short))
                {
                    _writer.Write((byte) EncodingKind.Int16);
                    _writer.Write((short) value);
                }
                else if (value.GetType() == typeof(long))
                {
                    _writer.Write((byte) EncodingKind.Int64);
                    _writer.Write((long) value);
                }
                else if (value.GetType() == typeof(sbyte))
                {
                    _writer.Write((byte) EncodingKind.Int8);
                    _writer.Write((sbyte) value);
                }
                else if (value.GetType() == typeof(float))
                {
                    _writer.Write((byte) EncodingKind.Float4);
                    _writer.Write((float) value);
                }
                else if (value.GetType() == typeof(ushort))
                {
                    _writer.Write((byte) EncodingKind.UInt16);
                    _writer.Write((ushort) value);
                }
                else if (value.GetType() == typeof(uint))
                {
                    WriteEncodedUInt32((uint) value);
                }
                else if (value.GetType() == typeof(ulong))
                {
                    _writer.Write((byte) EncodingKind.UInt64);
                    _writer.Write((ulong) value);
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(value.GetType());
                }
            }
            else if (value.GetType() == typeof(decimal))
            {
                _writer.Write((byte) EncodingKind.Decimal);
                _writer.Write((decimal) value);
            }
            else if (value.GetType() == typeof(DateTime))
            {
                _writer.Write((byte) EncodingKind.DateTime);
                _writer.Write(((DateTime) value).ToBinary());
            }
            else if (value.GetType() == typeof(string))
            {
                WriteStringValue((string) value);
            }
            else if (type.IsArray)
            {
                Array instance = (Array) value;
                if (instance.Rank > 1)
                {
                    throw new InvalidOperationException(Resources.Arrays_with_more_than_one_dimension_cannot_be_serialized);
                }

                WriteArray(instance);
            }
            else
            {
                WriteObject(value, null);
            }
        }

        public void WriteValue(IObjectWritable value)
        {
            if (value == null)
            {
                _writer.Write((byte) EncodingKind.Null);
                return;
            }

            WriteObject(value, value);
        }

        private void WriteEncodedInt32(int v)
        {
            if (v >= 0 && v <= 10)
            {
                _writer.Write((byte) ((int) EncodingKind.Int32_0 + v));
            }
            else if (v >= 0 && v < byte.MaxValue)
            {
                _writer.Write((byte) EncodingKind.Int32_1Byte);
                _writer.Write((byte) v);
            }
            else if (v >= 0 && v < ushort.MaxValue)
            {
                _writer.Write((byte) EncodingKind.Int32_2Bytes);
                _writer.Write((ushort) v);
            }
            else
            {
                _writer.Write((byte) EncodingKind.Int32);
                _writer.Write(v);
            }
        }

        private void WriteEncodedUInt32(uint v)
        {
            if (v >= 0 && v <= 10)
            {
                _writer.Write((byte) ((int) EncodingKind.UInt32_0 + v));
            }
            else if (v >= 0 && v < byte.MaxValue)
            {
                _writer.Write((byte) EncodingKind.UInt32_1Byte);
                _writer.Write((byte) v);
            }
            else if (v >= 0 && v < ushort.MaxValue)
            {
                _writer.Write((byte) EncodingKind.UInt32_2Bytes);
                _writer.Write((ushort) v);
            }
            else
            {
                _writer.Write((byte) EncodingKind.UInt32);
                _writer.Write(v);
            }
        }

        private struct WriterReferenceMap
        {
            private readonly Dictionary<object, int> _valueToIdMap;
            private readonly bool _valueEquality;
            private int _nextId;
            private static readonly ObjectPool<Dictionary<object, int>> s_referenceDictionaryPool =
                new ObjectPool<Dictionary<object, int>>(factory: () =>
                    new Dictionary<object, int>(128, ReferenceEqualityComparer.Instance));
            private static readonly ObjectPool<Dictionary<object, int>> s_valueDictionaryPool =
                new ObjectPool<Dictionary<object, int>>(factory: () => new Dictionary<object, int>(128));

            public WriterReferenceMap(bool valueEquality)
            {
                _valueEquality = valueEquality;
                _valueToIdMap = GetDictionaryPool(valueEquality).Allocate();
                _nextId = 0;
            }

            private static ObjectPool<Dictionary<object, int>> GetDictionaryPool(bool valueEquality)
            {
                return valueEquality ? s_valueDictionaryPool : s_referenceDictionaryPool;
            }

            public void Dispose()
            {
                ObjectPool<Dictionary<object, int>> pool = GetDictionaryPool(_valueEquality);
                if (_valueToIdMap.Count > 1024)
                {
                    pool.ForgetTrackedObject(_valueToIdMap);
                }
                else
                {
                    _valueToIdMap.Clear();
                    pool.Free(_valueToIdMap);
                }
            }

            public bool TryGetReferenceId(object value, out int referenceId)
            {
                return _valueToIdMap.TryGetValue(value, out referenceId);
            }

            public void Add(object value)
            {
                int id = _nextId++;
                _valueToIdMap.Add(value, id);
            }
        }

        public void WriteCompressedUInt(uint value)
        {
            if (value <= byte.MaxValue >> 2)
            {
                _writer.Write((byte) value);
            }
            else if (value <= ushort.MaxValue >> 2)
            {
                byte byte0 = (byte) (((value >> 8) & 0xFFu) | Byte2Marker);
                byte byte1 = (byte) (value & 0xFFu);
                _writer.Write(byte0);
                _writer.Write(byte1);
            }
            else if (value <= uint.MaxValue >> 2)
            {
                byte byte0 = (byte) (((value >> 24) & 0xFFu) | Byte4Marker);
                byte byte1 = (byte) ((value >> 16) & 0xFFu);
                byte byte2 = (byte) ((value >> 8) & 0xFFu);
                byte byte3 = (byte) (value & 0xFFu);
                _writer.Write(byte0);
                _writer.Write(byte1);
                _writer.Write(byte2);
                _writer.Write(byte3);
            }
            else
            {
                throw new ArgumentException(Resources.Value_too_large_to_be_represented_as_a_30_bit_unsigned_integer);
            }
        }

        private unsafe void WriteStringValue(string value)
        {
            if (value == null)
            {
                _writer.Write((byte) EncodingKind.Null);
            }
            else
            {
                int id;
                if (_stringReferenceMap.TryGetReferenceId(value, out id))
                {
                    Debug.Assert(id >= 0);
                    if (id <= byte.MaxValue)
                    {
                        _writer.Write((byte) EncodingKind.StringRef_1Byte);
                        _writer.Write((byte) id);
                    }
                    else if (id <= ushort.MaxValue)
                    {
                        _writer.Write((byte) EncodingKind.StringRef_2Bytes);
                        _writer.Write((ushort) id);
                    }
                    else
                    {
                        _writer.Write((byte) EncodingKind.StringRef_4Bytes);
                        _writer.Write(id);
                    }
                }
                else
                {
                    _stringReferenceMap.Add(value);
                    if (value.IsValidUnicodeString())
                    {
                        _writer.Write((byte) EncodingKind.StringUtf8);
                        _writer.Write(value);
                    }
                    else
                    {
                        _writer.Write((byte) EncodingKind.StringUtf16);
                        byte[] bytes = new byte[(uint) value.Length * sizeof(char)];
                        fixed (char* valuePtr = value)
                        {
                            Marshal.Copy((IntPtr) valuePtr, bytes, 0, bytes.Length);
                        }

                        WriteCompressedUInt((uint) value.Length);
                        _writer.Write(bytes);
                    }
                }
            }
        }

        private void WriteArray(Array array)
        {
            int length = array.GetLength(0);
            switch (length)
            {
                case 0:
                    _writer.Write((byte) EncodingKind.Array_0);
                    break;
                case 1:
                    _writer.Write((byte) EncodingKind.Array_1);
                    break;
                case 2:
                    _writer.Write((byte) EncodingKind.Array_2);
                    break;
                case 3:
                    _writer.Write((byte) EncodingKind.Array_3);
                    break;
                default:
                    _writer.Write((byte) EncodingKind.Array);
                    WriteCompressedUInt((uint) length);
                    break;
            }

            Type elementType = array.GetType().GetElementType();
            if (s_typeMap.TryGetValue(elementType, out EncodingKind elementKind))
            {
                WritePrimitiveType(elementType, elementKind);
                WritePrimitiveTypeArrayElements(elementType, elementKind, array);
            }
            else
            {
                WriteKnownType(elementType);
                int oldDepth = _recursionDepth;
                _recursionDepth++;
                if (_recursionDepth % MaxRecursionDepth == 0)
                {
                    Task task = Task.Factory.StartNew(
                        action: a => WriteArrayValues((Array) a),
                        state: array,
                        cancellationToken: _cancellationToken,
                        creationOptions: TaskCreationOptions.LongRunning,
                        scheduler: TaskScheduler.Default);
                    task.GetAwaiter().GetResult();
                }
                else
                {
                    WriteArrayValues(array);
                }

                _recursionDepth--;
                Debug.Assert(_recursionDepth == oldDepth);
            }
        }

        private void WriteArrayValues(Array array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                WriteValue(array.GetValue(i));
            }
        }

        private void WritePrimitiveTypeArrayElements(Type type, EncodingKind kind, Array instance)
        {
            Debug.Assert(s_typeMap[type] == kind);
            if (type == typeof(byte))
            {
                _writer.Write((byte[]) instance);
            }
            else if (type == typeof(char))
            {
                _writer.Write((char[]) instance);
            }
            else if (type == typeof(string))
            {
                WriteStringArrayElements((string[]) instance);
            }
            else if (type == typeof(bool))
            {
                WriteBooleanArrayElements((bool[]) instance);
            }
            else
            {
                switch (kind)
                {
                    case EncodingKind.Int8:
                        WriteInt8ArrayElements((sbyte[]) instance);
                        return;
                    case EncodingKind.Int16:
                        WriteInt16ArrayElements((short[]) instance);
                        return;
                    case EncodingKind.Int32:
                        WriteInt32ArrayElements((int[]) instance);
                        return;
                    case EncodingKind.Int64:
                        WriteInt64ArrayElements((long[]) instance);
                        return;
                    case EncodingKind.UInt16:
                        WriteUInt16ArrayElements((ushort[]) instance);
                        return;
                    case EncodingKind.UInt32:
                        WriteUInt32ArrayElements((uint[]) instance);
                        return;
                    case EncodingKind.UInt64:
                        WriteUInt64ArrayElements((ulong[]) instance);
                        return;
                    case EncodingKind.Float4:
                        WriteFloat4ArrayElements((float[]) instance);
                        return;
                    case EncodingKind.Float8:
                        WriteFloat8ArrayElements((double[]) instance);
                        return;
                    case EncodingKind.Decimal:
                        WriteDecimalArrayElements((decimal[]) instance);
                        return;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(kind);
                }
            }
        }

        private void WriteBooleanArrayElements(bool[] array)
        {
            BitVector bits = BitVector.Create(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                bits[i] = array[i];
            }

            foreach (uint word in bits.Words())
            {
                _writer.Write(word);
            }
        }

        private void WriteStringArrayElements(string[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                WriteStringValue(array[i]);
            }
        }

        private void WriteInt8ArrayElements(sbyte[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                _writer.Write(array[i]);
            }
        }

        private void WriteInt16ArrayElements(short[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                _writer.Write(array[i]);
            }
        }

        private void WriteInt32ArrayElements(int[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                _writer.Write(array[i]);
            }
        }

        private void WriteInt64ArrayElements(long[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                _writer.Write(array[i]);
            }
        }

        private void WriteUInt16ArrayElements(ushort[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                _writer.Write(array[i]);
            }
        }

        private void WriteUInt32ArrayElements(uint[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                _writer.Write(array[i]);
            }
        }

        private void WriteUInt64ArrayElements(ulong[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                _writer.Write(array[i]);
            }
        }

        private void WriteDecimalArrayElements(decimal[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                _writer.Write(array[i]);
            }
        }

        private void WriteFloat4ArrayElements(float[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                _writer.Write(array[i]);
            }
        }

        private void WriteFloat8ArrayElements(double[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                _writer.Write(array[i]);
            }
        }

        private void WritePrimitiveType(Type type, EncodingKind kind)
        {
            Debug.Assert(s_typeMap[type] == kind);
            _writer.Write((byte) kind);
        }

        public void WriteType(Type type)
        {
            _writer.Write((byte) EncodingKind.Type);
            WriteString(type.AssemblyQualifiedName);
        }

        private void WriteKnownType(Type type)
        {
            _writer.Write((byte) EncodingKind.Type);
            WriteInt32(_binderSnapshot.GetTypeId(type));
        }

        private void WriteObject(object instance, IObjectWritable instanceAsWritableOpt)
        {
            Debug.Assert(instance != null);
            Debug.Assert(instanceAsWritableOpt == null || instance == instanceAsWritableOpt);
            _cancellationToken.ThrowIfCancellationRequested();
            if (_objectReferenceMap.TryGetReferenceId(instance, out int id))
            {
                Debug.Assert(id >= 0);
                if (id <= byte.MaxValue)
                {
                    _writer.Write((byte) EncodingKind.ObjectRef_1Byte);
                    _writer.Write((byte) id);
                }
                else if (id <= ushort.MaxValue)
                {
                    _writer.Write((byte) EncodingKind.ObjectRef_2Bytes);
                    _writer.Write((ushort) id);
                }
                else
                {
                    _writer.Write((byte) EncodingKind.ObjectRef_4Bytes);
                    _writer.Write(id);
                }
            }
            else
            {
                IObjectWritable writable = instanceAsWritableOpt;
                if (writable == null)
                {
                    writable = instance as IObjectWritable;
                    if (writable == null)
                    {
                        throw NoSerializationWriterException($"{instance.GetType()} must implement {nameof(IObjectWritable)}");
                    }
                }

                int oldDepth = _recursionDepth;
                _recursionDepth++;
                if (_recursionDepth % MaxRecursionDepth == 0)
                {
                    Task task = Task.Factory.StartNew(
                        action: obj => WriteObjectWorker((IObjectWritable) obj),
                        state: writable,
                        cancellationToken: _cancellationToken,
                        creationOptions: TaskCreationOptions.LongRunning,
                        scheduler: TaskScheduler.Default);
                    task.GetAwaiter().GetResult();
                }
                else
                {
                    WriteObjectWorker(writable);
                }

                _recursionDepth--;
                Debug.Assert(_recursionDepth == oldDepth);
            }
        }

        private void WriteObjectWorker(IObjectWritable writable)
        {
            _objectReferenceMap.Add(writable);
            _writer.Write((byte) EncodingKind.Object);
            WriteInt32(_binderSnapshot.GetTypeId(writable.GetType()));
            writable.WriteTo(this);
        }

        private static Exception NoSerializationTypeException(string typeName)
        {
            return new InvalidOperationException(
                string.Format(Resources.The_type_0_is_not_understood_by_the_serialization_binder, typeName));
        }

        private static Exception NoSerializationWriterException(string typeName)
        {
            return new InvalidOperationException(string.Format(Resources.Cannot_serialize_type_0, typeName));
        }

        internal static readonly Dictionary<Type, EncodingKind> s_typeMap;
        internal static readonly ImmutableArray<Type> s_reverseTypeMap;

        static ObjectWriter()
        {
            s_typeMap = new Dictionary<Type, EncodingKind>
            {
                {typeof(bool), EncodingKind.BooleanType},
                {typeof(char), EncodingKind.Char},
                {typeof(string), EncodingKind.StringType},
                {typeof(sbyte), EncodingKind.Int8},
                {typeof(short), EncodingKind.Int16},
                {typeof(int), EncodingKind.Int32},
                {typeof(long), EncodingKind.Int64},
                {typeof(byte), EncodingKind.UInt8},
                {typeof(ushort), EncodingKind.UInt16},
                {typeof(uint), EncodingKind.UInt32},
                {typeof(ulong), EncodingKind.UInt64},
                {typeof(float), EncodingKind.Float4},
                {typeof(double), EncodingKind.Float8},
                {typeof(decimal), EncodingKind.Decimal}
            };
            Type[] temp = new Type[(int) EncodingKind.Last];
            foreach (KeyValuePair<Type, EncodingKind> kvp in s_typeMap)
            {
                temp[(int) kvp.Value] = kvp.Key;
            }

            s_reverseTypeMap = ImmutableArray.Create(temp);
        }

        internal const byte ByteMarkerMask = 3 << 6;
        internal const byte Byte1Marker = 0;
        internal const byte Byte2Marker = 1 << 6;
        internal const byte Byte4Marker = 2 << 6;

        internal enum EncodingKind : byte
        {
            Null,
            Type,
            Object,
            ObjectRef_1Byte,
            ObjectRef_2Bytes,
            ObjectRef_4Bytes,
            StringUtf8,
            StringUtf16,
            StringRef_1Byte,
            StringRef_2Bytes,
            StringRef_4Bytes,
            Boolean_True,
            Boolean_False,
            Char,
            Int8,
            Int16,
            Int32,
            Int32_1Byte,
            Int32_2Bytes,
            Int32_0,
            Int32_1,
            Int32_2,
            Int32_3,
            Int32_4,
            Int32_5,
            Int32_6,
            Int32_7,
            Int32_8,
            Int32_9,
            Int32_10,
            Int64,
            UInt8,
            UInt16,
            UInt32,
            UInt32_1Byte,
            UInt32_2Bytes,
            UInt32_0,
            UInt32_1,
            UInt32_2,
            UInt32_3,
            UInt32_4,
            UInt32_5,
            UInt32_6,
            UInt32_7,
            UInt32_8,
            UInt32_9,
            UInt32_10,
            UInt64,
            Float4,
            Float8,
            Decimal,
            DateTime,
            Array,
            Array_0,
            Array_1,
            Array_2,
            Array_3,
            BooleanType,
            StringType,
            Last = StringType + 1
        }
    }
}
