using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace Demo.Interfaces.Message
{
    public class Messages
    {
    }

    [MessagePackObject]
    public class JoinRoom : IDemoUnionData
    {
        [Key(0)]
        public string RoomName { get; set; }
    }

    [MessagePackObject]
    public class Hello : IDemoUnionData
    {
        [Key(0)]
        public string Message { get; set; }
    }
}
