using MessagePack;

namespace Demo.Interfaces.Message
{

    [MessagePackObject]
    public class Messages : IDemoUnionData
    {
    }

    [MessagePackObject]
    public class JoinRoom : IDemoUnionData
    {
        [Key(0)]
        public string RoomName;
    }

    [MessagePackObject]
    public class Hello : IDemoUnionData
    {
        [Key(0)]
        public string Message;
    }

    [MessagePackObject]
    public class HHHaa: IDemoUnionData 
    {
        [Key(0)]
        public string Aaa;
    }

    [MessagePackObject]
    public class EEE: IDemoUnionData
    {

    }
}
