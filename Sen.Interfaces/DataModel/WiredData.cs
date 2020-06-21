using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sen
{
    [MessagePackObject]
    public class WiredData<TUnionDataInterface> where TUnionDataInterface : IUnionData
    {
        [Key(0)]
        public int ServiceCode { get; set; }

        [Key(1)]
        public TUnionDataInterface Data { get; set; }
    }

    /// <summary>
    /// Marker interface for data transfers back and forth between game client and game server
    /// </summary>
    public interface IUnionData
    {

    }

    [Flags]
    public enum WiredDataType
    {
        Normal = 0,
        Compressed = 1,
        Encrypted = 2,
        EncrypteCompressed = Compressed | Encrypted,
    }
}
