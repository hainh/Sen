using Sen;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Demo.Interfaces
{
    public class PlayerState : IPlayerState
    {
        public string Name { get; set; }
        public IRoom Room { get; set; }
        public bool IsBot { get; set; }
        public IPEndPoint LocalAddress { get; set; }
        public IPEndPoint RemoteAddress { get; set; }
    }
}
