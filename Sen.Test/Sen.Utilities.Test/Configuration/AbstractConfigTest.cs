using Xunit;
using Sen.Utilities.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Sen.Utilities.Configuration.Tests
{
    public class AbstractConfigTest
    {
        [Fact()]
        public void LoadTest()
        {
            var a1 = new A1();
            Assert.True(a1.A == 1001);
            Assert.Equal(0, a1.B.B);
            a1.Load(JsonDocument.Parse(@"{""A"": 33, ""B"": {""B"": 5}, ""D"": ""1,2,Friday,8""}").RootElement);
            Assert.Equal(33, a1.A);
            Assert.Null(a1.GetParentConfig());
            Assert.Equal(a1, a1.B.GetParentConfig());
        }
    }

    public class A1 : JsonConfig<A1>
    {
        [DefaultValue(1001)]
        public int A { get; private set; }

        [DefaultValue(typeof(A2))]
        public A2 B { get; protected set; }

        public DayOfWeek[] D { get; private set; }

        public A2[] BA { get; set; }

        public A2 N { get; set; }
    }

    public class A2 : JsonConfig<A2>
    {
        public int B { get; private set; }
    }
}