using Demo.Interfaces;
using Demo.Interfaces.Message;
using Sen;
using System.Threading.Tasks;

namespace Demo.Grains
{
    public class DemoPlayer : AbstractPlayer<IDemoUnionData, PlayerState>, IDemoPlayer
    {
        public override ValueTask OnDisconnect()
        {
            return default;
        }

        public ValueTask<IDemoUnionData> HandleMessage(Hello hello, NetworkOptions networkOptions)
        {
            hello.Message = $"{hello.Message} huh?";
            networkOptions.Reliability = Reliability.Unreliable;
            return new ValueTask<IDemoUnionData>(hello);
        }

        protected override ILobby GetGameWorld()
        {
            return GrainFactory.GetGrain<IGameWorld>("GameWorld");
        }

        protected override ValueTask<bool> CheckAccessToken(string accessToken)
        {
            return new ValueTask<bool>(true);
        }
    }
}
