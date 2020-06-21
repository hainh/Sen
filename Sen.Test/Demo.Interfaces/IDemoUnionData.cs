using Demo.Interfaces.Message;
using MessagePack;
using Sen;
using System;
using System.Collections.Generic;
using System.Text;

namespace Demo.Interfaces
{
    [Union(0, typeof(Messages))]
    [Union(1, typeof(Hello))]
    [Union(2, typeof(JoinRoom))]
    [Union(3, typeof(HHHaa))]
    [Union(4, typeof(EEE))]
    public interface IDemoUnionData : IUnionData
    {
    }
}
