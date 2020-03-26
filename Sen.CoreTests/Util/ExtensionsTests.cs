using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senla.Core.Serialize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Core.Utilities.Tests
{
    [TestClass()]
    public class ExtensionsTests
    {
        [TestMethod()]
        public void ToStringTest()
        {
            var serializer = new DefaultSerializer();
            var deserializer = new DefaultDeserializer();

            var data = new OperationData(4, new Dictionary<byte, object>
            {
                {14, (byte)3 },
                {15, (short)3 },
                {16, (ushort)3090 },
                {17, 10293831 },
                {18, 252u },
                {19, 65530 },
                {20, 3986ul },
                {21, 312.0 },
                {22, 3123.0f },
                {140, false },
                {141, "xin chao server" },
                {200, new string[] { "xin chao server", "xin chao server" } },
                {201, new byte[] { 46, 46 } },
                {202, new short[] { 721, 721 } },
                {203, new ushort[] { 3722, 3722 } },
                {204, new int[] { 4029723, 4029723 } },
                {205, new uint[] { 72934u, 72934u } },
                {206, new long[] { -7223445, -7223445 } },
                {207, new ulong[] { 728982346u, 728982346u } },
                {208, new float[] { 727.0f, 727.0f } },
                {209, new double[] { 7284378.0, 7284378.0 } },
                {210, new bool[] {true, true, true, true, true, true, true, true, true}}
            });

            var raw = serializer.Serialize(data);

            var buff = new Buffer.QueueBuffer<byte>(raw);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < raw.Count; i++)
            {
                sb.Append((int)raw[i]).Append(", ");
            }

            var result = deserializer.DeserializeData(buff);

            var str1 = data.ToStringFull();
            var str2 = ((OperationData)result).ToStringFull();

            Assert.IsTrue(str1.Equals(str2));
        }
    }
}