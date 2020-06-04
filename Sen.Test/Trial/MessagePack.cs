using MessagePack;
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
                MyData = new MyData2 { A = 9-9, MyEnum = MyEnum.Bbb }
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

    [Union(10001, typeof(MyData))]
    [Union(10000, typeof(MyData2))]
    public interface IMyUnion
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
        public long A { get; set; }

        [Key(1)]
        public MyEnum MyEnum { get; set; }
    }

    public enum MyEnum
    {
        Aaa, Bbb, Ccc
    }
}
