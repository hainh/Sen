using MessagePack;
using Sen.DataModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Trial
{
    class MessagePack
    {
        public static void Attemp()
        {
            IMyUnion myData = new MyData { A = 123456, B = "78910", C = -123 };
            byte[] data = MessagePackSerializer.Typeless.Serialize(myData);
            byte[] data2 = MessagePackSerializer.Serialize(myData);
            byte[] data20 = MessagePackSerializer.Serialize<IMyUnion>(myData);
            Console.WriteLine(string.Join(',', data2));
            Console.WriteLine(string.Join(',', data20));
            object obj = MessagePackSerializer.Deserialize<IMyUnion>(data20);

            var serviceData = new MyServiceData<IMyUnion>()
            {
                ServiceCode = 55,
                MyData = myData
            };
            byte[] data3 = MessagePackSerializer.Serialize(serviceData);
            Console.WriteLine(string.Join(',', data3));

            var serviceData2 = MessagePackSerializer.Deserialize<MyServiceData<IMyUnion>>(data3);

            var sd2 = new MyServiceData<IMyUnion>
            {
                ServiceCode = 44,
                MyData = new MyData2 { A = true, O = MyEnum.Bbb }
            };
            byte[] data4 = MessagePackSerializer.Serialize(sd2);
            Console.WriteLine(string.Join(',', data4));

            var sd3 = MessagePackSerializer.Deserialize<MyServiceData<IMyUnion>>(data4);

        }
    }

    [MessagePackObject]
    public class MyServiceData<TUnion>
    {
        [Key(1)]
        public TUnion MyData { get; set; }

        [Key(0)]
        public int ServiceCode { get; set; }
    }

    //[Union(10001, typeof(MyData))]
    [Union(10000, typeof(MyData2))]
    public interface IMyUnion : IUnionData
    {

    }

    [MessagePackObject]
    public class MyData : IMyUnion
    {
        [Key(0)]
        public int A { get; set; }

        [Key(1)]
        public string B { get; set; }

        [Key(2)]
        public short C { get; set; }
    }

    [MessagePackObject]
    public class MyData2 : IMyUnion
    {
        [Key(0)]
        public bool A;
        [Key(1)]
        public byte B;
        [Key(2)]
        public sbyte C;
        [Key(3)]
        public char D;
        [Key(4)]
        public decimal E;
        [Key(5)]
        public double F;
        [Key(6)]
        public float G;
        [Key(7)]
        public int H;
        [Key(8)]
        public uint I;
        [Key(9)]
        public long J;
        [Key(10)]
        public ulong K;
        [Key(11)]
        public short L;
        [Key(12)]
        public ushort M;
        [Key(13)]
        public string N;
        [Key(14)]
        public MyEnum O;
    }

    public enum MyEnum
    {
        Aaa, Bbb, Ccc
    }


}
