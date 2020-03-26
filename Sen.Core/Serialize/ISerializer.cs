using Senla.Core.Buffer;

namespace Senla.Core.Serialize
{
    public interface ISerializer
    {
        DequeBuffer<byte> Serialize(IDataContainer data);
    }
}