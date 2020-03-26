using System;

namespace Senla.Gamer.Game.Turnbase
{
    public class GameStateManager
    {
        public DateTimeOffset StartTime { get; protected set; }
        public DateTimeOffset EndTime { get; protected set; }

        public StateBase CurrentState { get; set; }

        /// <summary>
        /// Time left in Milliseconds
        /// </summary>
        public int TimeLeft
        {
            get
            {
                return Math.Max(0, (int)((EndTime - DateTimeOffset.Now).TotalMilliseconds));
            }
        }

        public uint StateId { get; protected set; }

        public GameStateManager()
        {
            StateId = 0;
            CurrentState = null;
            StartTime = new DateTimeOffset();
            EndTime = new DateTimeOffset();
        }

        public void SetNewState(StateBase newState, int timeLeftInMs)
        {
            CurrentState = newState;
            StartTime = DateTimeOffset.Now;
            EndTime = StartTime + new TimeSpan(0, 0, 0, 0, timeLeftInMs);
            StateId++;
        }
    }
}
