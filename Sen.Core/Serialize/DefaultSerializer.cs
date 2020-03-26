using Senla.Core.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Core.Serialize
{
    /// <summary>
    /// Little-endian serializer.
    /// </summary>
    public class DefaultSerializer : ISerializer
    {

        DequeBuffer<byte> buffer = new DequeBuffer<byte>(512);
        List<byte> encodedBuffer = new List<byte>(10);

        static byte hasMoreByte(ulong value)
        {
            return (byte)(value | 128);
        }
        /// <summary>
        /// Encode integer types of 1 to 8-byte length.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        static List<byte> encodeVarInt(ulong value, List<byte> output)
        {
            output.Clear();
            if (value < (1 << 7))
            {
                output.Add((byte)value);
            }
            else if (value < (1UL << 14))
            {
                output.Add(hasMoreByte(value & 0x7F));
                output.Add((byte)(value >> 7));
            }
            else if (value < (1UL << 21))
            {
                output.Add(hasMoreByte(value & 0x7F));
                output.Add(hasMoreByte((value >> 7) & 0x7F));
                output.Add((byte)(value >> 14));
            }
            else if (value < (1UL << 28))
            {
                output.Add(hasMoreByte(value & 0x7F));
                output.Add(hasMoreByte((value >> 7) & 0x7F));
                output.Add(hasMoreByte((value >> 14) & 0x7F));
                output.Add((byte)(value >> 21));
            }
            else if (value < (1UL << 35))
            {
                output.Add(hasMoreByte(value & 0x7F));
                output.Add(hasMoreByte((value >> 7) & 0x7F));
                output.Add(hasMoreByte((value >> 14) & 0x7F));
                output.Add(hasMoreByte((value >> 21) & 0x7F));
                output.Add((byte)(value >> 28));
            }
            else if (value < (1UL << 42))
            {
                output.Add(hasMoreByte(value & 0x7F));
                output.Add(hasMoreByte((value >> 7) & 0x7F));
                output.Add(hasMoreByte((value >> 14) & 0x7F));
                output.Add(hasMoreByte((value >> 21) & 0x7F));
                output.Add(hasMoreByte((value >> 28) & 0x7F));
                output.Add((byte)(value >> 35));
            }
            else if (value < (1UL << 49))
            {
                output.Add(hasMoreByte(value & 0x7F));
                output.Add(hasMoreByte((value >> 7) & 0x7F));
                output.Add(hasMoreByte((value >> 14) & 0x7F));
                output.Add(hasMoreByte((value >> 21) & 0x7F));
                output.Add(hasMoreByte((value >> 28) & 0x7F));
                output.Add(hasMoreByte((value >> 35) & 0x7F));
                output.Add((byte)(value >> 42));
            }
            else if (value < (1UL << 56))
            {
                output.Add(hasMoreByte(value & 0x7F));
                output.Add(hasMoreByte((value >> 7) & 0x7F));
                output.Add(hasMoreByte((value >> 14) & 0x7F));
                output.Add(hasMoreByte((value >> 21) & 0x7F));
                output.Add(hasMoreByte((value >> 28) & 0x7F));
                output.Add(hasMoreByte((value >> 35) & 0x7F));
                output.Add(hasMoreByte((value >> 42) & 0x7F));
                output.Add((byte)(value >> 49));
            }
            else if (value < (1UL << 63))
            {
                output.Add(hasMoreByte(value & 0x7F));
                output.Add(hasMoreByte((value >> 7) & 0x7F));
                output.Add(hasMoreByte((value >> 14) & 0x7F));
                output.Add(hasMoreByte((value >> 21) & 0x7F));
                output.Add(hasMoreByte((value >> 28) & 0x7F));
                output.Add(hasMoreByte((value >> 35) & 0x7F));
                output.Add(hasMoreByte((value >> 42) & 0x7F));
                output.Add(hasMoreByte((value >> 49) & 0x7F));
                output.Add((byte)(value >> 56));
            }
            else
            {
                output.Add(hasMoreByte(value & 0x7F));
                output.Add(hasMoreByte((value >> 7) & 0x7F));
                output.Add(hasMoreByte((value >> 14) & 0x7F));
                output.Add(hasMoreByte((value >> 21) & 0x7F));
                output.Add(hasMoreByte((value >> 28) & 0x7F));
                output.Add(hasMoreByte((value >> 35) & 0x7F));
                output.Add(hasMoreByte((value >> 42) & 0x7F));
                output.Add(hasMoreByte((value >> 49) & 0x7F));
                output.Add(hasMoreByte((value >> 56) & 0x7F));
                output.Add((byte)(value >> 63));
            }

            return output;
        }

        /// <summary>
        /// Encode numberic type that equal or more than 2 bytes length as a "jagged" value
        /// </summary>
        /// <param name="originValue"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        static List<byte> encodeVarInt(long originValue, List<byte> output)
        {
            // To "jagged" value
            ulong value = getJaggedValue(originValue);
            return encodeVarInt(value, output);
        }

        static ulong getJaggedValue(long originValue)
        {
            ulong value = originValue < 0 ? (((ulong)(-originValue)) << 1) | 0x01 : (ulong)originValue << 1;
            return value;
        }

        public DequeBuffer<byte> Serialize(IDataContainer data)
        {
            buffer.Clear();

            // Put data code
            buffer.Enqueue(data.Code);

            if (data.Parameters == null)
            {
                buffer.PushHead((byte)(data.ServiceCode | (1 << 3)));

                // return the buffer array
                return buffer;
            }

            ulong param = 0;
            foreach (var item in data.Parameters)
            {
                // Put parameter code
                buffer.Enqueue(item.Key);

                if (item.Value is byte)
                {
                    param = (byte)DataType.BYTE | (ulong)((byte)(item.Value) << 5);
                    buffer.Enqueue(encodeVarInt(param, encodedBuffer));
                }
                else if (item.Value is bool)
                {
                    param = (byte)DataType.BOOL | ((bool)item.Value ? 1UL << 5 : 0);
                    buffer.Enqueue(encodeVarInt(param, encodedBuffer));
                }
                else if (item.Value is short)
                {
                    param = (byte)DataType.INT16 | (getJaggedValue((short)item.Value) << 5);
                    buffer.Enqueue(encodeVarInt(param, encodedBuffer));
                }
                else if (item.Value is int)
                {
                    param = (byte)DataType.INT32 | (getJaggedValue((int)item.Value) << 5);
                    buffer.Enqueue(encodeVarInt(param, encodedBuffer));
                }
                else if (item.Value is long)
                {
                    buffer.Enqueue((byte)DataType.INT64);
                    param = getJaggedValue((long)item.Value);
                    buffer.Enqueue(encodeVarInt(param, encodedBuffer));
                }
                else if (item.Value is ushort)
                {
                    param = (byte)DataType.UINT16 | (ulong)(((ushort)item.Value) << 5);
                    buffer.Enqueue(encodeVarInt(param, encodedBuffer));
                }
                else if (item.Value is uint)
                {
                    param = (byte)DataType.UINT32 | (((uint)item.Value) << 5);
                    buffer.Enqueue(encodeVarInt(param, encodedBuffer));
                }
                else if (item.Value is ulong)
                {
                    buffer.Enqueue((byte)DataType.UINT64);
                    param = (ulong)item.Value;
                    buffer.Enqueue(encodeVarInt(param, encodedBuffer));
                }
                else if (item.Value is float)
                {
                    buffer.Enqueue((byte)DataType.FLOAT);
                    buffer.Enqueue(BitConverter.GetBytes((float)item.Value));
                }
                else if (item.Value is double)
                {
                    buffer.Enqueue((byte)DataType.DOUBLE);
                    buffer.Enqueue(BitConverter.GetBytes((double)item.Value));
                }
                else if (item.Value is string)
                {
                    string str = item.Value as string;
                    byte[] bytes = Encoding.UTF8.GetBytes(str);
                    param = (byte)DataType.STRING | (((uint)bytes.Length << 5));
                    buffer.Enqueue(encodeVarInt(param, encodedBuffer));
                    buffer.Enqueue(bytes);
                }
                else if (item.Value is Array)
                {

                    Type type = item.Value.GetType().GetElementType();

                    if (item.Value is Array && (item.Value as Array).Length == 0)
                    {
                        buffer.Enqueue((byte)DataType.ARRAY); // make it null
                    }
                    else if (type == typeof(bool))
                    {
                        // Pack 8 values of boolean in 1 byte
                        var bools = (bool[])item.Value;
                        param = (byte)DataType.ARRAY_BOOL | ((uint)(bools.Length) << 5);
                        buffer.Enqueue(encodeVarInt(param, encodedBuffer));

                        int bytesNeeded = bools.Length / 8 + (bools.Length % 8 > 0 ? 1 : 0);
                        byte b = 0;
                        for (int i = 0; i < bools.Length; i++)
                        {
                            if (bools[i])
                            {
                                b = (byte)(b | (1 << (i % 8)));
                            }

                            if ((i + 1) % 8 == 0)
                            {
                                buffer.Enqueue(b);
                                b = 0;
                            }
                            else if (i == bools.Length - 1)
                            {
                                buffer.Enqueue(b);
                            }
                        }
                    }
                    else if (type == typeof(byte))
                    {
                        var array = (byte[])item.Value;
                        param = (byte)DataType.ARRAY_BYTE | ((uint)(array.Length) << 5);
                        buffer.Enqueue(encodeVarInt(param, encodedBuffer));
                        buffer.Enqueue(array);
                    }
                    else if (type == typeof(short))
                    {
                        var array = (short[])item.Value;
                        param = (byte)DataType.ARRAY_INT16 | ((uint)(array.Length) << 5);
                        buffer.Enqueue(encodeVarInt(param, encodedBuffer));
                        for (int i = 0; i < array.Length; i++)
                        {
                            buffer.Enqueue(encodeVarInt((long)array[i], encodedBuffer));
                        }
                    }
                    else if (type == typeof(int))
                    {
                        var array = (int[])item.Value;
                        param = (byte)DataType.ARRAY_INT32 | ((uint)(array.Length) << 5);
                        buffer.Enqueue(encodeVarInt(param, encodedBuffer));
                        for (int i = 0; i < array.Length; i++)
                        {
                            buffer.Enqueue(encodeVarInt((long)array[i], encodedBuffer));
                        }
                    }
                    else if (type == typeof(long))
                    {
                        var array = (long[])item.Value;
                        param = (byte)DataType.ARRAY_INT64 | ((uint)(array.Length) << 5);
                        buffer.Enqueue(encodeVarInt(param, encodedBuffer));
                        for (int i = 0; i < array.Length; i++)
                        {
                            buffer.Enqueue(encodeVarInt(array[i], encodedBuffer));
                        }
                    }
                    else if (type == typeof(ushort))
                    {
                        var array = (ushort[])item.Value;
                        param = (byte)DataType.ARRAY_UINT16 | ((uint)(array.Length) << 5);
                        buffer.Enqueue(encodeVarInt(param, encodedBuffer));
                        for (int i = 0; i < array.Length; i++)
                        {
                            buffer.Enqueue(encodeVarInt((ulong)array[i], encodedBuffer));
                        }
                    }
                    else if (type == typeof(uint))
                    {
                        var array = (uint[])item.Value;
                        param = (byte)DataType.ARRAY_UINT32 | ((uint)(array.Length) << 5);
                        buffer.Enqueue(encodeVarInt(param, encodedBuffer));
                        for (int i = 0; i < array.Length; i++)
                        {
                            buffer.Enqueue(encodeVarInt((ulong)array[i], encodedBuffer));
                        }
                    }
                    else if (type == typeof(ulong))
                    {
                        var array = (ulong[])item.Value;
                        param = (byte)DataType.ARRAY_UINT64 | ((uint)(array.Length) << 5);
                        buffer.Enqueue(encodeVarInt(param, encodedBuffer));
                        for (int i = 0; i < array.Length; i++)
                        {
                            buffer.Enqueue(encodeVarInt(array[i], encodedBuffer));
                        }
                    }
                    else if (type == typeof(float))
                    {
                        var array = (float[])item.Value;
                        param = (byte)DataType.ARRAY_FLOAT | ((uint)(array.Length) << 5);
                        buffer.Enqueue(encodeVarInt(param, encodedBuffer));
                        for (int i = 0; i < array.Length; i++)
                        {
                            buffer.Enqueue(BitConverter.GetBytes(array[i]));
                        }
                    }
                    else if (type == typeof(double))
                    {
                        var array = (double[])item.Value;
                        param = (byte)DataType.ARRAY_DOUBLE | ((uint)(array.Length) << 5);
                        buffer.Enqueue(encodeVarInt(param, encodedBuffer));
                        for (int i = 0; i < array.Length; i++)
                        {
                            buffer.Enqueue(BitConverter.GetBytes(array[i]));
                        }
                    }
                    else if (type == typeof(string))
                    {
                        var array = (string[])item.Value;
                        param = (byte)DataType.ARRAY_STRING | ((uint)(array.Length) << 5);
                        buffer.Enqueue(encodeVarInt(param, encodedBuffer));
                        string s;
                        for (int i = 0; i < array.Length; i++)
                        {
                            s = array[i];
                            if (s == null) s = string.Empty;
                            byte[] bytes = Encoding.UTF8.GetBytes(s);
                            buffer.Enqueue(encodeVarInt((ulong)bytes.Length, encodedBuffer));
                            buffer.Enqueue(bytes);
                        }
                    }
                    else
                    {
                        throw new NotSupportedException(string.Format("Not support array of type \"{0}\"", type.FullName));
                    }
                }
                else if (item.Value == null)
                {
                    buffer.Enqueue((byte)DataType.ARRAY);
                }
                else
                {
                    throw new NotSupportedException(string.Format("Not support type \"{0}\"", item.Value.GetType().FullName));
                }
            }

            ulong size = (ulong)buffer.Count;

            param = (size << 3) | data.ServiceCode;
            encodeVarInt(param, encodedBuffer);
            buffer.PushHead(encodedBuffer);

            //Console.Write("Send: ");
            //for (int i = 0; i < buffer.Count; i++)
            //{
            //    Console.Write("{0}, ", (int)buffer[i]);
            //}
            //Console.WriteLine();

            // return the buffer array
            return buffer;
        }
    }
}
