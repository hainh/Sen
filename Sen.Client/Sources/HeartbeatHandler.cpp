#include "HeartbeatHandler.h"

Senla::HeartbeatHandler::HeartbeatHandler(BasePeer * peer)
	: _peer(peer)
	, _lastBeat(0)
	, _timeline(0.0f)
	, _disconnectTask(10e15f, 10, std::bind(&BasePeer::onNoHeartbeat, peer))
{

}
