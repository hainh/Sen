#ifndef _SENLA_HEARTBEAT_HANDLER_H_
#define _SENLA_HEARTBEAT_HANDLER_H_

#include <functional>
#include "BasePeer.h"

namespace Senla {

class BasePeer;

class HeartbeatHandler
{
	class Task
	{
		typedef std::function<void()> Callback0;
		float _invokeTime;
		float _delay;
		Callback0 _callback;

	public:
		Task(float startTime, float delay, const Callback0& callback)
			: _invokeTime(startTime + delay)
			, _delay(delay)
			, _callback(callback)
		{
		}

		void invoke(float timeline)
		{
			if (timeline >= _invokeTime)
			{
				_callback();
				_invokeTime = timeline + _delay;
			}
		}

		void cancelNextInvocation(float timeline)
		{
			_invokeTime = timeline + _delay;
		}

		void setDelay(float delay, float timeline)
		{
			_invokeTime = timeline + delay;
			_delay = delay;
		}
};
public:
	HeartbeatHandler(BasePeer* peer);

	~HeartbeatHandler()
	{
	}

	void update(float dt)
	{
		_timeline += dt;

		_disconnectTask.invoke(_timeline);
	}

	float timeline()
	{
		return _timeline;
	}

	void updateHeartbeat()
	{
		//printf("\nUpdate heartbeat");
		_disconnectTask.cancelNextInvocation(_timeline);
	}

	void setConnectionTimeout(Senla::uint32 timeout)
	{
		_disconnectTask.setDelay((float)timeout, _timeline);
	}

private:
	BasePeer *_peer;
	int _lastBeat;

	float _timeline;

	Task _disconnectTask;
};


}

#endif //_SENLA_HEARTBEAT_HANDLER_H_