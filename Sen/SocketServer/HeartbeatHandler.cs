using System;

namespace Senla.Server.SocketServer
{
    public class HeartbeatHandler
    {
        private ReliableServerHandler _reliableHandler;
        private int _maxIdleTime;
        private int _minIdleTime;
        private int _pingTime;
        private int _lastBeat;
        private bool _disconnected;

        private int _lastPingCount;

        public HeartbeatHandler(ReliableServerHandler reliableHandler, int minIdleTime, int maxIdleTime)
        {
            _disconnected = false;
            _lastBeat = 0;
            _reliableHandler = reliableHandler;
            _maxIdleTime = Math.Max(maxIdleTime, minIdleTime);
            _minIdleTime = minIdleTime;
            _pingTime = Math.Max((maxIdleTime - 1000)/ 3, 1000);
        }

        public void Start()
        {
            SchedulePing();
            ScheduleLostConnection();
        }

        public void UpdateHeartbeat()
        {
            _lastBeat++;
            Utils.WriteLineT("New beat {0}", _lastBeat);
        }

        public void Disconnected()
        {
            _disconnected = true;
        }

        protected void SchedulePing()
        {
            var lastbeat = _lastBeat;
            Utils.WriteLineT("Schedule ping, {0}", lastbeat);
            _reliableHandler.Context.Channel.EventLoop.ScheduleAsync(() =>
            {
                if (_disconnected)
                {
                    return;
                }
                Utils.WriteLineT("Send ping {0}, {1}, {2}", lastbeat == _lastBeat, lastbeat, _pingTime);
                if (lastbeat == _lastBeat || _lastPingCount > 2)
                {
                    _reliableHandler._peer.sendPing();
                    _lastPingCount = 0;
                }
                else
                {
                    _lastPingCount++;
                }
                SchedulePing();
            }, TimeSpan.FromMilliseconds(_pingTime));
        }

        protected void ScheduleLostConnection()
        {
            if (_disconnected)
            {
                return;
            }

            // Do the scheduling
            scheduleLostConnection();

            // Repeat scheduling
            _reliableHandler.Context.Channel.EventLoop.ScheduleAsync(() =>
            {
                ScheduleLostConnection();
            }, TimeSpan.FromMilliseconds(Math.Max(_maxIdleTime - _minIdleTime, 1000)));
        }

        private void scheduleLostConnection()
        {
            var lastbeat = _lastBeat;
            //Utils.WriteLineT("Schedule connection lost, {0}, {1}", lastbeat, _minIdleTime);
            _reliableHandler.Context.Channel.EventLoop.ScheduleAsync(() =>
            {
                if (_disconnected)
                {
                    return;
                }
                //Utils.WriteLineT("Lost connection {0}, from beat {1}", lastbeat == _lastBeat, lastbeat);
                if (lastbeat == _lastBeat)
                {
                    _reliableHandler.ChannelIdleTimeout();
                    Disconnected();
                    return;
                }
            }, TimeSpan.FromMilliseconds(_minIdleTime));
        }
    }
}
