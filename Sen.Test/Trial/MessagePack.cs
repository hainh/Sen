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

            var serviceData = new WiredData<IMyUnion>()
            {
                ServiceCode = 55,
                Data = myData
            };
            byte[] data3 = MessagePackSerializer.Serialize(serviceData);
            Console.WriteLine(string.Join(',', data3));

            var serviceData2 = MessagePackSerializer.Deserialize<WiredData<IMyUnion>>(data3);

            var sd2 = new WiredData<IMyUnion>
            {
                ServiceCode = 44,
                Data = new MyData2 { A = true, O = MyEnum.Bbb, MyData = (MyData)myData, MyDatas = new[] { (MyData)myData } }
            };
            byte[] data4 = MessagePackSerializer.Serialize(sd2);
            Console.WriteLine(string.Join(',', data4));

            var sd3 = MessagePackSerializer.Deserialize<WiredData<IMyUnion>>(data4);

        }
    }

    [Union(1, typeof(MyData))]
    [Union(2, typeof(MyData2))]
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

        [Key(15)]
        public bool[] AA;
        [Key(16)]
        public byte[] AB;
        [Key(17)]
        public sbyte[] AC;
        [Key(18)]
        public char[] AD;
        [Key(19)]
        public decimal[] AE;
        [Key(20)]
        public double[] AF;
        [Key(21)]
        public float[] AG;
        [Key(22)]
        public int[] AH;
        [Key(23)]
        public uint[] AI;
        [Key(24)]
        public long[] AJ;
        [Key(25)]
        public ulong[] AK;
        [Key(26)]
        public short[] AL;
        [Key(27)]
        public ushort[] AM;
        [Key(28)]
        public string[] AN;
        [Key(29)]
        public MyData MyData;
        [Key(30)]
        public MyData[] MyDatas;
    }

    [MessagePackObject]
    public class WiredData<TUnionDataInterface> where TUnionDataInterface : IUnionData
    {
        [Key(0)]
        public int ServiceCode { get; set; }

        [Key(1)]
        public TUnionDataInterface Data { get; set; }
    }

    /// <summary>
    /// Marker interface for data transfers back and forth between game client and game server
    /// </summary>
    public interface IUnionData
    {

    }


    public enum MyEnum
    {
        Aaa, Bbb, Ccc
    }


}
