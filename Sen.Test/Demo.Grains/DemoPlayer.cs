using Demo.Interfaces;
using Demo.Interfaces.Message;
using Sen;
using System.Threading.Tasks;

namespace Demo.Grains
{
    public class DemoPlayer : AbstractPlayer<IDemoUnionData, PlayerState>, IDemoPlayer
    {
        public override string Name => State.Name;

        public override ValueTask OnDisconnect()
        {
            return default;
        }

        public ValueTask<IDemoUnionData> HandleMessage(Hello hello)
        {
            hello.Message = $"{hello.Message} huh?";
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
