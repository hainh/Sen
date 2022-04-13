
namespace Sen
{
    /// <summary>
    /// Marker interface for data transfers back and forth between game client and game server
    /// </summary>
    public interface IUnionData
    {
    }

    [MessagePack.MessagePackObject]
    public class RpcMessage<TUnionData> where TUnionData : IUnionData
    {
        [MessagePack.Key(0)]
        public uint Id { get; set; }

        [MessagePack.Key(1)]
#if UNITY
        public TUnionData UnionData { get; set; }
#else
        public TUnionData? UnionData { get; set; }
#endif
    }

    public enum MessageType
    {
        Normal,
        Rpc,
        MaxValue = 8
    }

    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class NotHandleMessageAttribute : System.Attribute
    {
    }
}
