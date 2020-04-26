using Orleans.CodeGeneration;
using Orleans.Serialization;
using Sen.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sen.OrleansInterfaces
{
    [Obsolete("Use this will decrease performance")]
    [Serializable]
    public struct ByteArraySegment
    {
        public int Offset { get; }
        public int Count { get; }
        public byte[] Array { get; }

        public ByteArraySegment(ArraySegment<byte> origin)
        {
            Array = origin.Array;
            Offset = origin.Offset;
            Count = origin.Count;
        }

        private static readonly ObjectPool<List<ArraySegment<byte>>> ListPool
            = new ObjectPool<List<ArraySegment<byte>>>(() => new List<ArraySegment<byte>>(1));

        [CopierMethod]
        public static object DeepCopier(object obj, ICopyContext context)
        {
            return obj;
        }

        [SerializerMethod]
        public static void Serializer(object untypedInput, ISerializationContext context, Type expected)
        {
            var input = (ByteArraySegment)untypedInput;

            List<ArraySegment<byte>> list = ListPool.Allocate();
            list.Clear();
            list.Add(new ArraySegment<byte>(input.Array, input.Offset, input.Count));
            context.StreamWriter.Write(input.Count);
            context.StreamWriter.Write(list);
            ListPool.Free(list);
        }

        [DeserializerMethod]
        public static object Deserializer(Type expected, IDeserializationContext context)
        {
            var length = context.StreamReader.ReadInt();
            byte[] array = new byte[length];
            context.StreamReader.ReadByteArray(array, 0, length);
            var result = new ByteArraySegment(array);

            return result;
        }
    }
}
