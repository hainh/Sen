using Demo.Interfaces;
using Demo.Interfaces.Message;
using Orleans.Runtime;
using Sen;
using System.Threading.Tasks;

namespace Demo.Grains
{
    public class DemoPlayer : AbstractPlayer<IDemoUnionData, PlayerState>, IDemoPlayer
    {
        public DemoPlayer([PersistentState("player")]IPersistentState<PlayerState> profile) : base(profile)
        {
        }

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

        public ValueTask<IDemoUnionData> HandleMessage(JoinRoom joinRoom, NetworkOptions networkOptions)
        {
            joinRoom.RoomName = "Ok to join " + joinRoom.RoomName;
            return new ValueTask<IDemoUnionData>(joinRoom);
        }

        protected override ILobby GetGameWorld()
        {
            return GrainFactory.GetGrain<IGameWorld>("GameWorld");
        }

        protected override ValueTask<bool> CheckAccessToken(string accessToken)
        {
            return new ValueTask<bool>(true);
        }

        public override Task OnActivateAsync()
        {
            return base.OnActivateAsync();
        }
    }
}
