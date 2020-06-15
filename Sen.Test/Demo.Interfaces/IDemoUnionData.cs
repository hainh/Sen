using Demo.Interfaces.Message;
using MessagePack;
using Sen.DataModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Demo.Interfaces
{
    [Union(0, typeof(JoinRoom))]
    public interface IDemoUnionData : IUnionData
    {
    }
}
