using Orleans;
using System.Threading.Tasks;

namespace Sen
{
    public interface IAuthService : ISingletonGrain, IGrainWithStringKey
    {
        ValueTask<bool> Login(string username, string password);
    }
}
