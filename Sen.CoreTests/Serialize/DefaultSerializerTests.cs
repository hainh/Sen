using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senla.Core.Heartbeat;
using Senla.Core.Serialize;
using Senla.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Core.Serialize.Tests
{
    [TestClass()]
    public class DefaultSerializerTests
    {
        [TestMethod()]
        public void SerializeTest()
        {
            var serializer = new DefaultSerializer();
            var data = new Ping(0, new Dictionary<byte, object>
            {
                { 0, 12393823UL },
                { 1, 2328389238UL },
                { 2, (byte)23 },
                { 3, new byte[] {1, 4, 254} },
                { 4, (short)24256 },
                { 5, new short[] {-3847, 987, -12, 3289} },
                { 6, (ushort)3873 },
                { 7, new ushort[] {47854, 29828, 65443} },
                { 8, 45345 },
                { 9, new int[] {3423, 34534564, 234234234} },
                { 10, new long [] {237498273892, -634765982374982, 123} },
                { 11, true },
                { 12, new bool[] {true, false, false, false, true, true, true, false} },
                { 13, "gio oi" }
            });

            var raw = serializer.Serialize(data);
            string s = "";
            foreach (var i in raw)
            {
                s += (int)i + ", ";
            }
            var buffraw = new Buffer.QueueBuffer<byte>(raw);

            var deserializer = new DefaultDeserializer();
            var netData = deserializer.DeserializeData(buffraw);
            Assert.IsTrue(data.SerializeAsString().Equals(netData.SerializeAsString()));
        }

        [TestMethod()]
        public void SerializeTest2()
        {
            var raw = new byte[] { 184, 7, 0, 1, 32, 2, 240, 1, 29, 3, 195, 146, 9, 4, 115, 202, 36, 132, 189, 3, 232, 176, 244, 153, 9, 5, 7, 192, 175, 208, 172, 200, 171, 215, 15, 6, 119, 254, 165, 14, 156, 173, 221, 208, 159, 160, 5, 222, 141, 187, 212, 120, 7, 202, 1, 107, 101, 109, 107, 101, 109, 8, 90, 4, 107, 101, 109, 49, 4, 107, 101, 109, 50, 9, 8, 240, 12, 2, 70, 10, 120, 191, 35, 49, 69, 0, 241, 5, 71, 13, 69, 177, 80, 11, 9, 248, 221, 238, 238, 28, 65, 10, 67, 12, 89, 251, 162, 235, 92, 5, 209, 181, 66, 80, 208, 184, 144, 93, 238, 11, 67 };
            var buffraw = new Buffer.QueueBuffer<byte>(raw);

            var deserializer = new DefaultDeserializer();
            var netData = deserializer.DeserializeData(buffraw);

            Assert.Fail();
        }

        [TestMethod()]
        public void SerializeConfirmJsTest()
        {
            var serializer = new DefaultSerializer();
            var data = new EventData(0, new Dictionary<byte, object>
            {
                { 1, true},
	            { 2, new bool[] {true, false, true, true, true, false, false} },
	            { 3, 2341},
	            { 4, new int[] { 2341, 28482, 1235127348 }},
	            { 5, 8828376237479872UL },
	            { 6, new ulong[] { 234238, 23098234984092, 32389383902 } },
	            { 7, "kemkem" },
	            { 8, new string[] { "kem1", "kem2" } }
            });

            var raw = serializer.Serialize(data);
            string s = "";
            foreach (var i in raw)
            {
                s += (int)i + ", ";
            }
            var buffraw = new Buffer.QueueBuffer<byte>(raw);

            var deserializer = new DefaultDeserializer();
            var netData = deserializer.DeserializeData(buffraw);
        }
    }
}