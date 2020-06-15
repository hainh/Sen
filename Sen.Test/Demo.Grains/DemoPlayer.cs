using Demo.Interfaces;
using Orleans;
using Sen.Game;
using System;
using System.Threading.Tasks;

namespace Demo.Grains
{
    public class DemoPlayer : Player<IDemoUnionData, PlayerState>, IDemoPlayer
    {
        public override ValueTask<string> GetName() => new ValueTask<string>(State.Name);

        public override ValueTask OnDisconnect()
        {
            return default;
        }

        public ValueTask<IDemoUnionData> HandleMessage()
    }
}
