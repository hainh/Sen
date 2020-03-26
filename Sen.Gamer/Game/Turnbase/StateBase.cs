namespace Senla.Gamer.Game.Turnbase
{
    public class StateBase
    {
        private string _name;

        public StateBase(string name)
        {
            _name = name;
        }

        public override string ToString()
        {
            return _name;
        }

        public static readonly StateBase IDLE = new StateBase("idle");
        public static readonly StateBase WAITING = new StateBase("waiting");
        public static readonly StateBase PLAYING = new StateBase("playing");
    }
}
