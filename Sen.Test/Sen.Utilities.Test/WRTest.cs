using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Sen.Utilities.Test
{
    public class WRTest
    {
        [Fact]
        public void Test1()
        {
            var random = new WeightedRandomizer<int>();
            for (int i = 0; i < 10; i++)
            {
                random.AddOrUpdateValue(i, (i + 1) * 10);
            }

            Assert.Equal(10, random.GetWeight(0));
            Assert.Equal(20, random.GetWeight(1));
            //Assert.Equal(10, random.GetWeight(2));
            //Assert.Equal(10, random.GetWeight(3));
            //Assert.Equal(10, random.GetWeight(4));
            //Assert.Equal(10, random.GetWeight(5));
            //Assert.Equal(10, random.GetWeight(6));
            Dictionary<int, int> a = new();
            for (int i = 0; i < 100; i++)
            {

                System.Diagnostics.Debug.WriteLine(random.GetRandom());
                Assert.InRange(random.GetRandom(), 0, 9);
            }
        }
    }
}
