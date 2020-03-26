using System;
using System.Collections.Generic;

namespace Senla.Gamer.Data
{
    public interface ITransmitData : IEntity
    {
        DateTime CreateAt { get; set; }

        int Code { get; set; }

        Dictionary<byte, object> Data { get; set; }

        string Room { get; set; }

        string Players { get; set; }

        string Type { get; set; }

        long MatchId { get; set; }

        void ConvertToReadableParameters();

    }
}
