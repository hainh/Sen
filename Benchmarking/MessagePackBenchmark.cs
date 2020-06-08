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
        private byte[] data2;
        private MyServiceData<IMyUnion> serviceData;
        private byte[] sd3;

        [GlobalSetup]
        public void Setup()
        {
            myData = new MyData { A = 123456, B = MyEnum.Ccc, C = -123, D = 0, E = true, F = "F" };
            data2 = MessagePackSerializer.Serialize(myData);

            serviceData = new MyServiceData<IMyUnion>()
            {
                ServiceCode = 5,
                MyData = new MyData2 { A = 12312, MyEnum = MyEnum.Bbb, E = true, F = "F" }
            };

            sd3 = MessagePackSerializer.Serialize(serviceData);

            Console.WriteLine("Bare class {0} bytes, Union class {1} bytes", data2.Length, sd3.Length);
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
            MessagePackSerializer.Deserialize<MyData>(data2);
        }

        [Benchmark]
        public void DeserializeUnionClass()
        {
            MessagePackSerializer.Deserialize<MyServiceData<IMyUnion>>(sd3);
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

    [Union(10000, typeof(MyData2))]
    public interface IMyUnion
    {

    }

    [MessagePackObject]
    public class MyData
    {
        [Key(0)]
        public int A { get; set; }

        [Key(1)]
        public MyEnum B { get; set; }

        [Key(2)]
        public short C { get; set; }

        [Key(3)]
        public long D { get; set; }

        [Key(4)]
        public bool E { get; set; }

        [Key(5)]
        public string F { get; set; }
    }

    [MessagePackObject]
    public class MyData2 : IMyUnion
    {
        [Key(0)]
        public long A { get; set; }

        [Key(1)]
        public MyEnum MyEnum { get; set; }

        [Key(2)]
        public string F { get; set; }

        [Key(3)]
        public bool E { get; set; }
    }

    public enum MyEnum
    {
        Aaa, Bbb, Ccc
    }
}
