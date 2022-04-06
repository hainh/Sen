
namespace Sen
{
    /// <summary>
    /// Marker interface for data transfers back and forth between game client and game server
    /// </summary>
    public interface IUnionData
    {
    }

    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class HiddenMessageAttribute : System.Attribute
    {
    }
}
