using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Sen
{
    public interface IPlayerState
    {
        string Name { get; set; }
        IRoom Room { get; set; }
        bool IsBot { get; set; }
        IPEndPoint LocalAddress { get; set; }
        IPEndPoint RemoteAddress { get; set; }
    }
}
