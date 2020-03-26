using Senla.Core.Buffer;
using Senla.Core.Heartbeat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Core.Serialize
{
    public class DefaultDeserializer : IDeserializer
    {
        public static bool TryReadVarInt(QueueBuffer<byte> data, out ulong varInt, ref int offset)
        {
            varInt = 0;
            if (data.Count == 0)
            {
                return false;
            }
            
            int i = 0;
            int shift;
            while(true)
            {
                byte b = data[offset];
                shift = 7 * i;
                varInt = varInt | ((ulong)(b & 0x7F) << shift);
                ++offset;
                ++i;
                if (shift > 63)
                {
                    throw new OverflowException("VarInt is overflow");
                }
                if (b < 0x80)
                {
                    return true;
                }

                if (offset >= data.Count)
                {
                    offset -= i; // reset offset
                    return false;
                }
            }

        }

        public static byte ReadByte(QueueBuffer<byte> data, ref int offset)
        {
            return data[offset++];
        }

        public static long ToSignedInt(ulong num)
        {
            bool isNeg = (num & 1) == 1;
            if (isNeg)
            {
                return -(long)(num >> 1);
            }

            return (long)(num >> 1);
        }

        private byte[] bits = new byte[12];

        public float ReadFloat(QueueBuffer<byte> data, ref int offset)
        {
            bits[0] = data[offset++];
            bits[1] = data[offset++];
            bits[2] = data[offset++];
            bits[3] = data[offset++];

            return BitConverter.ToSingle(bits, 0);
        }

        public double ReadDouble(QueueBuffer<byte> data, ref int offset)
        {
            bits[0] = data[offset++];
            bits[1] = data[offset++];
            bits[2] = data[offset++];
            bits[3] = data[offset++];
            bits[4] = data[offset++];
            bits[5] = data[offset++];
            bits[6] = data[offset++];
            bits[7] = data[offset++];

            return BitConverter.ToDouble(bits, 0);
        }

        public string ReadString(QueueBuffer<byte> data, ref int offset, int length)
        {
            if (length == 0)
            {
                return string.Empty;
            }

            if (length <= bits.Length)
            {
                offset += data.CopyTo(bits, 0, offset, length);
                return Encoding.UTF8.GetString(bits, 0, length);
            }

            var bytes = new byte[length];
            offset += data.CopyTo(bytes, 0, offset, length);
            return Encoding.UTF8.GetString(bytes, 0, length);
        }

        public static bool[] ReadBoolArray(QueueBuffer<byte> data, ref int offset, int length)
        {
            // Special case that carries a null
            if (length == 0)
            {
                return null;
            }

            var bools = new bool[length];

            for (int i = 0; i < length; i++)
            {
                bools[i] = (data[offset] & (1 << (i % 8))) > 0;
                if ((i + 1) % 8 == 0)
                {
                    ++offset;
                }
                else if (i == length - 1)
                {
                    ++offset;
                }
            }

            return bools;
        }

        public static byte[] ReadByteArray(QueueBuffer<byte> data, ref int offset, int length)
        {
            var bytes = new byte[length];
            int copied = data.CopyTo(bytes, 0, offset, length);
            offset += copied;

            return bytes;
        }

        public static short[] ReadInt16Array(QueueBuffer<byte> data, ref int offset, int length)
        {
            var ss = new short[length];
            ulong value;
            for (int i = 0; i < length; i++)
            {
                TryReadVarInt(data, out value, ref offset);
                ss[i] = (short)ToSignedInt(value);
            }

            return ss;
        }

        public static int[] ReadInt32Array(QueueBuffer<byte> data, ref int offset, int length)
        {
            var ss = new int[length];
            ulong value;
            for (int i = 0; i < length; i++)
            {
                TryReadVarInt(data, out value, ref offset);
                ss[i] = (int)ToSignedInt(value);
            }

            return ss;
        }

        public static long[] ReadInt64Array(QueueBuffer<byte> data, ref int offset, int length)
        {
            var ss = new long[length];
            ulong value;
            for (int i = 0; i < length; i++)
            {
                TryReadVarInt(data, out value, ref offset);
                ss[i] = ToSignedInt(value);
            }

            return ss;
        }

        public static ushort[] ReadUint16Array(QueueBuffer<byte> data, ref int offset, int length)
        {
            var ss = new ushort[length];
            ulong value;
            for (int i = 0; i < length; i++)
            {
                TryReadVarInt(data, out value, ref offset);
                ss[i] = (ushort)(value);
            }

            return ss;
        }

        public static uint[] ReadUint32Array(QueueBuffer<byte> data, ref int offset, int length)
        {
            var ss = new uint[length];
            ulong value;
            for (int i = 0; i < length; i++)
            {
                TryReadVarInt(data, out value, ref offset);
                ss[i] = (uint)(value);
            }

            return ss;
        }

        public static ulong[] ReadUint64Array(QueueBuffer<byte> data, ref int offset, int length)
        {
            var ss = new ulong[length];
            ulong value;
            for (int i = 0; i < length; i++)
            {
                TryReadVarInt(data, out value, ref offset);
                ss[i] = (value);
            }

            return ss;
        }

        public float[] ReadFloatArray(QueueBuffer<byte> data, ref int offset, int length)
        {
            var ss = new float[length];
            for (int i = 0; i < length; i++)
            {
                ss[i] = ReadFloat(data, ref offset);
            }

            return ss;
        }

        public double[] ReadDoubleArray(QueueBuffer<byte> data, ref int offset, int length)
        {
            var ss = new double[length];
            for (int i = 0; i < length; i++)
            {
                ss[i] = ReadDouble(data, ref offset);
            }

            return ss;
        }

        public string[] ReadStringArray(QueueBuffer<byte> data, ref int offset, int length)
        {
            var ss = new string[length];
            ulong slen;
            for (int i = 0; i < length; i++)
            {
                TryReadVarInt(data, out slen, ref offset);
                ss[i] = ReadString(data, ref offset, (int)slen);
            }

            return ss;
        }

        public Dictionary<byte, object> ReadParameters(QueueBuffer<byte> data, ref int offset, int maxOffset)
        {
            var parameters = new Dictionary<byte, object>(10);
            ulong varInt;
            byte code;
            DataType type;
            ulong value;

            while (offset < maxOffset)
            {
                code = ReadByte(data, ref offset);
                TryReadVarInt(data, out varInt, ref offset);

                type = (DataType)(varInt & 0x1F); // 5 bits type
                value = varInt >> 5;
                int length = (int)value;

                switch (type)
                {
                    case DataType.BOOL:
                        parameters.Add(code, value > 0);
                        break;
                    case DataType.BYTE:
                        parameters.Add(code, (byte)value);
                        break;
                    case DataType.INT16:
                        parameters.Add(code, (short)ToSignedInt(value));
                        break;
                    case DataType.INT32:
                        parameters.Add(code, (int)ToSignedInt(value));
                        break;
                    case DataType.INT64:
                        TryReadVarInt(data, out value, ref offset);
                        parameters.Add(code, ToSignedInt(value));
                        break;
                    case DataType.UINT16:
                        parameters.Add(code, (ushort)value);
                        break;
                    case DataType.UINT32:
                        parameters.Add(code, (uint)value);
                        break;
                    case DataType.UINT64:
                        TryReadVarInt(data, out value, ref offset);
                        parameters.Add(code, value);
                        break;
                    case DataType.FLOAT:
                        parameters.Add(code, ReadFloat(data, ref offset));
                        break;
                    case DataType.DOUBLE:
                        parameters.Add(code, ReadDouble(data, ref offset));
                        break;
                    case DataType.STRING:
                        string str = ReadString(data, ref offset, (int)value);
                        parameters.Add(code, str);
                        break;
                    case DataType.DICTIONARY:
                        break;
                    case DataType.ARRAY_BOOL: // Special case that 'bools = null'
                        bool[] bools = ReadBoolArray(data, ref offset, length);
                        parameters.Add(code, bools);
                        break;
                    case DataType.ARRAY_BYTE:
                        var bytes = ReadByteArray(data, ref offset, length);
                        parameters.Add(code, bytes);
                        break;
                    case DataType.ARRAY_INT16:
                        var ss = ReadInt16Array(data, ref offset, length);
                        parameters.Add(code, ss);
                        break;
                    case DataType.ARRAY_INT32:
                        var iis = ReadInt32Array(data, ref offset, length);
                        parameters.Add(code, iis);
                        break;
                    case DataType.ARRAY_INT64:
                        var ls = ReadInt64Array(data, ref offset, length);
                        parameters.Add(code, ls);
                        break;
                    case DataType.ARRAY_UINT16:
                        var us = ReadUint16Array(data, ref offset, length);
                        parameters.Add(code, us);
                        break;
                    case DataType.ARRAY_UINT32:
                        var uis = ReadUint32Array(data, ref offset, length);
                        parameters.Add(code, uis);
                        break;
                    case DataType.ARRAY_UINT64:
                        var uls = ReadUint64Array(data, ref offset, length);
                        parameters.Add(code, uls);
                        break;
                    case DataType.ARRAY_FLOAT:
                        var fs = ReadFloatArray(data, ref offset, length);
                        parameters.Add(code, fs);
                        break;
                    case DataType.ARRAY_DOUBLE:
                        var ds = ReadDoubleArray(data, ref offset, length);
                        parameters.Add(code, ds);
                        break;
                    case DataType.ARRAY_STRING:
                        var strs = ReadStringArray(data, ref offset, length);
                        parameters.Add(code, strs);
                        break;
                    default:
                        break;
                }
            }

            if (offset != maxOffset)
            {
                throw new FrameSizeOutOfBoundException(string.Format("Deserialized frame too large {0} > {1}", offset, maxOffset));
            }

            return parameters;
        }

        public IDataContainer DeserializeData(QueueBuffer<byte> rawData)
        {
            ulong varInt;
            int offset = 0;

            if (!TryReadVarInt(rawData, out varInt, ref offset))
            {
                return null;
            }

            ServiceType type = (ServiceType)((byte)(varInt & 0x07));
            int frameSize = (int)(varInt >> 3);

            int maxSize = 65535; // 64kB
            if (frameSize > maxSize)
            {
                throw new FrameSizeOutOfBoundException(string.Format("Frame size {0}bytes > maxSize {1}bytes", frameSize, maxSize));
            }

            // Not enough data for a frame
            if (rawData.Count - offset < frameSize)
            {
                return null;
            }

            IDataContainer dataContainer = null;

            byte code = ReadByte(rawData, ref offset);
            Dictionary<byte, object> parameters = ReadParameters(rawData, ref offset, frameSize + offset - 1);

            switch (type)
            {
                case ServiceType.EventData:
                    dataContainer = new EventData(code, parameters);
                    break;
                case ServiceType.OperationData:
                    dataContainer = new OperationData(code, parameters);
                    break;
                case ServiceType.PingData:
                    dataContainer = new Ping(code, parameters);
                    break;
                case ServiceType.EncryptData:
                    break;
                default:
                    break;
            }

            rawData.DiscardDequeu(offset);

            return dataContainer;
        }
    }
}
