using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senla.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Core.Tests
{
    [TestClass()]
    public class EventDataTests
    {
        [TestMethod()]
        public void ToStringFullTest()
        {
            var data = new EventData(123, new Dictionary<byte, object>
            {
                { 0, new string[] { "", "e", null} },
                { 2, new byte[] { 12, 55, 1} },
                { 5, 24L }
            });

            var s = data.ToStringFull();

            Assert.IsTrue(true);
        }
    }
}