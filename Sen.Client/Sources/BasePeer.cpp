#include "BasePeer.h"

Senla::BasePeer::BasePeer(Protocol protocol)
	: _checkPingTimeCount(0.0f)
	, _checkPingTimeInterval(0.0f)
	, _pingTime(0)
	, _heartbeat(new HeartbeatHandler(this))
	, _protocol(protocol)
	, _serializer(new Serializer())
	, _deserializer(new Deserializer())
{
}

void Senla::BasePeer::connect(const std::string & endpoint, unsigned short port, const std::string & appName)
{
	_bootstrap = new Bootstrap();
	switch (_protocol)
	{
	case Senla::Protocol::UDP:
		break;
	case Senla::Protocol::TCP:
		_bootstrap->channel<TcpSocketChannel>()
			->childHandler([this](ChannelPipline* pipeline)
		{
			pipeline->addLast(new TcpSocketHandler());
			pipeline->addLast(new ReliableSocketHandler(this));
		});
		break;
	default:
		break;
	}

	_channel = _bootstrap->connect(endpoint, port);
}

void Senla::BasePeer::update(float dt)
{
	if (_heartbeat && connectionAccepted())
		_heartbeat->update(dt);

	std::lock_guard<std::mutex> lock(_messageQueueMutex);
	MessageDispatched messageWrapper;
	while (!_messageQueue.empty())
	{
		messageWrapper = _messageQueue.front();
		_messageQueue.pop();

		ServiceType service = (ServiceType)messageWrapper.message->getServiceType();

		switch (service)
		{
		case Senla::ServiceType::EventData:
			onEvent(*(EventData*)messageWrapper.message, messageWrapper.sendParameters);
			break;
		case Senla::ServiceType::OperationData:
			onOperationResponse(*(OperationData*)messageWrapper.message, messageWrapper.sendParameters);
			break;
		case Senla::ServiceType::PingData:
			sendPingData((Ping*)messageWrapper.message);
			break;
		case Senla::ServiceType::EncryptData:
			break;
		case Senla::ServiceType::ConnectionStatus:
			onStatusChanged(((ConnectionStatusData*)messageWrapper.message)->Status());
			break;
		case Senla::ServiceType::ConfigData:
			_heartbeat->setConnectionTimeout(((ConfigData*)messageWrapper.message)->Parameters().asDict()[0].asUint32());
			break;
		default:
			break;
		}

		delete messageWrapper.message;
	}
}

void Senla::BasePeer::sendOperationRequest(OperationData & opData, unsigned char channelId, bool reliable, bool flush, bool encrypted)
{
	std::vector<unsigned char> data = _serializer->Serialize(&opData);
	MessageBuffer messageBuf(data.data(), (uint32_t)data.size());

	MessageWrapper message(&messageBuf, SendParameters(
		channelId,
		reliable,
		encrypted,
		flush
		));

	_channel->writeAsync(&message);
}

void Senla::BasePeer::setCheckPingTimeInterval(float seconds)
{
	if (seconds < 3) seconds = 3;

	_checkPingTimeInterval = seconds;

	// This make first ping time request occurs sooner
	_checkPingTimeCount = _heartbeat->timeline() - seconds - 1;
}

void Senla::BasePeer::pushDisconnectMessage(ConnectionStatus status)
{
	std::lock_guard<std::mutex> lock(_messageQueueMutex);
	_messageQueue.push(MessageDispatched{
		new ConnectionStatusData(status), SendParameters()
	});
	_messageQueue.push(MessageDispatched{
		new ConnectionStatusData(ConnectionStatus::DISCONNECTED), SendParameters()
	});
}

void Senla::BasePeer::sendPingData(Ping * pingData)
{
	auto timeline = _heartbeat->timeline();
	auto &params = pingData->Parameters();

	auto pingTime = params.asDict().find(1);
	if (pingTime != params.asDict().end())
	{
		_pingTime = pingTime->second.asInt32();
		params.asDict().erase(pingTime);
	}

	// TODO: XXX may have race condition here
	std::vector<unsigned char>& data = _serializer->Serialize(pingData);
	MessageBuffer messageBuf(data.data(), (uint32_t)data.size());

	MessageWrapper message(&messageBuf, SendParameters());

	_channel->writeAsync(&message);
}

void Senla::BasePeer::onConnected()
{
	std::lock_guard<std::mutex> lock(_messageQueueMutex);
	_messageQueue.push(MessageDispatched{
		new ConnectionStatusData(ConnectionStatus::CONNECTED), SendParameters()
	});
}

void Senla::BasePeer::updateHeartbeat()
{
	if (_heartbeat)
	{
		_heartbeat->updateHeartbeat();
	}
}

void Senla::BasePeer::releaseResources()
{
	if (_bootstrap)
	{
		delete _bootstrap;
		_bootstrap = nullptr;
	}

	if (_heartbeat)
	{
		delete _heartbeat;
		_heartbeat = nullptr;
	}
}
