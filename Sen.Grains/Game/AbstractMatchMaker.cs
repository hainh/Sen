using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sen.Game
{
    public abstract class AbstractMatchMaker : BaseGrain, IMatchMaker
    {
        public ValueTask<object?> Register(IMatchablePlayer player)
        {
            throw new NotImplementedException();
        }
    }
}
