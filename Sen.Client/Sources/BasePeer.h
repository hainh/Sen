#ifndef _SENLA_BASEPEER_H_
#define _SENLA_BASEPEER_H_

#include <mutex>
#include "DataContainer.h"
#include "CircularBuffer.h"
#include "Serializer.h"
#include "Deserializer.h"
#include "HeartbeatHandler.h"
#include "netasio/AsioSocketChannel.h"
#include "netasio/Bootstrap.h"
#include "handlers/TcpSockethandler.h"
#include "handlers/ReliableSocketHandler.h"

namespace Senla {

class ReliableSocketHandler;
class HeartbeatHandler;

class ConnectionStatusData : public DataContainer
{
public:
	ConnectionStatusData(ConnectionStatus status)
		: _status(status)
	{
	}

	~ConnectionStatusData()
	{
	}

	// Inherited via DataContainer
	virtual byte getServiceType()
	{
		return byte(ServiceType::ConnectionStatus);
	}

	ConnectionStatus Status()
	{
		return _status;
	}

private:
	ConnectionStatus _status;
};

enum class Protocol
{
	UDP,
	TCP
};

class BasePeer
{
public:
	BasePeer(Protocol protocol);

	virtual ~BasePeer()
	{
		releaseResources();
	}

	void connect(const std::string& endpoint, unsigned short port, const std::string& appName);

	void disconnect()
	{
		_channel->disconnect();
		pushDisconnectMessage(ConnectionStatus::DISCONNECTED_BY_CLIENT);
	}

	/*
	Called in user's thread to dispatch message.
	dt is in seconds
	*/
	void update(float dt);

	void sendOperationRequest(OperationData& opData, unsigned char channelId = 0,
		bool reliable = true, bool flush = true, bool encrypted = false);

	virtual void onStatusChanged(ConnectionStatus status) = 0;

	virtual void onOperationResponse(OperationData& opData, SendParameters sendParameters) = 0;

	virtual void onEvent(EventData& eventData, SendParameters sendParameters) = 0;

	bool isDisconnected()
	{
		return ((AsioSocketChannel*)_channel)->isDisconnected();
	}

	bool connectionAccepted()
	{
		return ((AsioSocketChannel*)_channel)->connectionAccepted();
	}

	void setCheckPingTimeInterval(float seconds);

	int GetPingTime()
	{
		return _pingTime;
	}
	
private:
	friend class HeartbeatHandler;
	void onNoHeartbeat()
	{
		_channel->disconnect();
		pushDisconnectMessage(ConnectionStatus::DISCONNECTED_CONNECTION_LOST);
	}

	void pushDisconnectMessage(ConnectionStatus status);

	void sendPingData(Ping *pingData);

	friend class ReliableSocketHandler;
	void onMessageDispatched(DataContainer* data, const SendParameters& sendParameters, int error)
	{
		std::lock_guard<std::mutex> lock(_messageQueueMutex);
		_messageQueue.push(MessageDispatched{
			data, sendParameters
		});
	}

	void onError(int error, const char* const message)
	{
		pushDisconnectMessage((ConnectionStatus)error);
	}

	void onConnected();

	void updateHeartbeat();
	Deserializer* deserializer() { return _deserializer; }
	Serializer* serializer() { return _serializer; }

	void releaseResources();
private:

	float _checkPingTimeInterval;
	float _checkPingTimeCount;
	int _pingTime;

	Protocol _protocol;

	HeartbeatHandler* _heartbeat;

	Deserializer* _deserializer;

	Serializer* _serializer;

	AbstractSocketChannel* _channel;
	Bootstrap* _bootstrap;

	std::mutex _messageQueueMutex;
	std::queue<MessageDispatched> _messageQueue;
};

}

#endif // _SENLA_BASEPEER_H_