using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sen
{
    public interface IMatchMaker : ISingletonGrain
    {
        ValueTask<object?> Register(IMatchablePlayer player);
    }
}
