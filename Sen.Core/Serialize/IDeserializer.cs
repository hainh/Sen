using Senla.Core.Buffer;
using System;

namespace Senla.Core.Serialize
{
    public interface IDeserializer
    {
        /// <summary>
        /// Try to deserialize the byte stream buffer to a <see cref="IDataContainer"/>.
        /// </summary>
        /// <remarks>This method should discard all deserialized data in rawData as deserialized successful</remarks>
        /// <param name="rawData"></param>
        /// <returns>A <see cref="IDataContainer"/> represent the request if <para name="rawData"/> contains efficient bytes or null if otherwise</returns>
        /// <exception cref="Exception">Thrown when deserialize failed</exception>
        IDataContainer DeserializeData(QueueBuffer<byte> rawData);
    }
}
