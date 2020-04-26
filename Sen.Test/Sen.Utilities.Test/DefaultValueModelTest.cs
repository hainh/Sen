using Sen.Utilities.Configuration;
using System;
using Xunit;

namespace Sen.Utilities.Test
{
    public class DefaultValueModel
    {
        [Fact]
        public void DefaultValueModelTest()
        {
            var d = new D1();
            Assert.True(d.Abc == 100);
            Assert.True(d.D2.Efg == "hahah");
            Assert.Null(d.D3);
        }

        public class D1 : DefaultValueModel<D1>
        {
            [DefaultValue(100)]
            public int Abc { get; private set; }

            [DefaultValue(typeof(D2))]
            public D2 D2 { get; private set; }

            public D3 D3 { get; private set; }
        }

        public class D2 : DefaultValueModel<D2>
        {
            [DefaultValue("hahah")]
            public string Efg { get; private set; }
        }

        public class D3 : DefaultValueModel<D3>
        {
            [DefaultValue(30)]
            public double D { get; set; }
        }
    }
}
