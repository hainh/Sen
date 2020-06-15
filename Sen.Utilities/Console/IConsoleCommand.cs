using System.Threading.Tasks;

namespace Sen.Utilities.Console
{
    public interface IConsoleCommand
    {
        Task RunAsync();
    }
}
