using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senla.Core.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Core.Buffer.Tests
{
    [TestClass()]
    public class QueueBufferTests
    {
        [TestMethod()]
        public void EnqueueTest()
        {
            var buf = new QueueBuffer<byte>(100);

            buf.Enqueue(1);

            var dat1 = new byte[500];
            dat1[0] = 2;
            dat1[499] = 3;

            buf.Enqueue(dat1);

            for (int i = 0; i < 400; i++)
            {
                buf.Dequeue();
            }

            dat1 = new byte[400];
            dat1[399] = 4;
            buf.Enqueue(dat1);

            for (int i = 0; i < 450; i++)
            {
                buf.Dequeue();
            }

            Assert.Fail();
        }

        [TestMethod()]
        public void DiscardDequeuTest()
        {
            var buf = new QueueBuffer<byte>(100);

            buf.Enqueue(1);

            var dat1 = new byte[500];
            dat1[0] = 2;
            dat1[499] = 3;

            buf.Enqueue(dat1);

            buf.DiscardDequeu(400);

            dat1 = new byte[400];
            dat1[399] = 4;
            buf.Enqueue(dat1);

            buf.DiscardDequeu(450);

            Assert.Fail();
        }
    }
}