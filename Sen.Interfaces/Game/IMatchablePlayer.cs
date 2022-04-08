using Orleans;
using System.Threading.Tasks;

namespace Sen
{
    public interface IMatchablePlayer : IGrain
    {
        ValueTask NotifyMatch(object match);
    }
}
