using BenchmarkDotNet.Attributes;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace Benchmarking
{
    public class MessagePackBenchmark
    {
        MyData myData;
        private byte[] mspByteArray;
        private MyServiceData<IMyUnion> serviceData;
        private byte[] protoBufArray;


        [GlobalSetup]
        public void Setup()
        {
            myData = new MyData 
            {
                Aiiiii = 123456, BEnnnnnn = MyEnum.Ccc, Csssssssss = -123, Dlllllll = 0, Eggggg = true, Fgggg = "Fsdkfhkj",
                H3333 = 212, LAffffff = new long[] {232, 1298723, 8763876837},
                SomeData = new InnerData()
                {
                    Bbbbbbb = new bool[] { true, false, false, true, false, false, false } ,
                    Ffffffff = 3.2938493f,
                    SSSSss = new string[] {"sdkjhe", "sdkfjheu"}
                }
            };

            serviceData = new MyServiceData<IMyUnion>()
            {
                ServiceCode = 5,
                MyData = myData
            };

            mspByteArray = MessagePackSerializer.Serialize(myData);

            Console.WriteLine("Bare class {0} bytes, Union class {1} bytes", mspByteArray.Length, protoBufArray.Length);
        }

        [Benchmark]
        public void SerializeBareClass()
        {
            MessagePackSerializer.Serialize(myData);
        }

        [Benchmark]
        public void SerializeUnionClass()
        {
            MessagePackSerializer.Serialize(serviceData);
        }

        [Benchmark]
        public void DeserializeBareClass()
        {
            MessagePackSerializer.Deserialize<MyData>(mspByteArray);
        }

        [Benchmark]
        public void DeserializeUnionClass()
        {
            MessagePackSerializer.Deserialize<MyServiceData<IMyUnion>>(protoBufArray);
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

    [Union(0, typeof(MyData))]
    public interface IMyUnion
    {

    }

    [MessagePackObject]
    public class MyData : IMyUnion
    {
        [Key(0)]
        public int Aiiiii { get; set; }

        [Key(1)]
        public MyEnum BEnnnnnn { get; set; }

        [Key(2)]
        public short Csssssssss { get; set; }

        [Key(3)]
        public long Dlllllll { get; set; }

        [Key(4)]
        public bool Eggggg { get; set; }

        [Key(5)]
        public string Fgggg { get; set; }

        [Key(6)]
        public byte H3333 { get; set; }

        [Key(7)]
        public long[] LAffffff { get; set; }

        [Key(8)]
        public InnerData SomeData { get; set; }
    }

    [MessagePackObject]
    public class InnerData
    {
        [Key(0)]
        public string[] SSSSss { get; set; }

        [Key(1)]
        public float Ffffffff { get; set; }

        [Key(2)]
        public bool[] Bbbbbbb { get; set; }
    }

    public enum MyEnum
    {
        Aaa, Bbb, Ccc
    }
}
