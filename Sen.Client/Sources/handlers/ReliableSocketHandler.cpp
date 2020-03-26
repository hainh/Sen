#include "ReliableSocketHandler.h"

Senla::ReliableSocketHandler::ReliableSocketHandler(BasePeer * peer)
	: _peer(peer)
{
}

void Senla::ReliableSocketHandler::channelActive(ChannelPipline * pipeline)
{
	_peer->onConnected();
}

void Senla::ReliableSocketHandler::channelRead(ChannelPipline * pipeline, MessageObj * message)
{
	auto aMessage = dynamic_cast<MessageWrapper*>(message);
	if (aMessage)
	{
		auto messageBuffer = aMessage->messageBuffer;
		auto channelId = aMessage->sendParameters.ChannelId;

		auto it = _receivedBuffer.find(channelId);
		if (it == _receivedBuffer.end())
		{
			_receivedBuffer[channelId] = gamesen::CircularBuffer(messageBuffer->data(), messageBuffer->size());
			it = _receivedBuffer.find(aMessage->sendParameters.ChannelId);
		}
		else
		{
			it->second.enqueue(messageBuffer->data(), messageBuffer->size());
		}

		try
		{
			auto dataContainer = _peer->deserializer()->DeserializeData(it->second);
			if (dataContainer)
			{
				_peer->onMessageDispatched(dataContainer, aMessage->sendParameters, 0);
			}
		}
		catch (const std::exception& e)
		{
			_peer->onMessageDispatched(nullptr, aMessage->sendParameters, 1);
		}
	}

	_peer->updateHeartbeat();
}

void Senla::ReliableSocketHandler::channelError(ChannelPipline * pipeline, int error, const char * const message)
{
	_peer->onError(error, message);
}
