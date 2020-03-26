#ifndef _SENLA_RELIABLE_SOCKET_HANDLER_H_
#define _SENLA_RELIABLE_SOCKET_HANDLER_H_

#include "../netasio/ChannelPipeline.h"
#include "../netasio/AbstractSocketChannel.h"
#include "../BasePeer.h"
#include "Messages.h"
#include "../CircularBuffer.h"

namespace Senla {

class BasePeer;

class ReliableSocketHandler : public ChannelHandler
{
public:
	ReliableSocketHandler(BasePeer* peer);

	// Inherited via ChannelHandler
	virtual void handlerAdded(ChannelPipline * pipeline) override
	{
		_channel = pipeline->Channel();
	}
	virtual void handlerRemoved(ChannelPipline * pipeline) override
	{
	}
	virtual void channelActive(ChannelPipline * pipeline) override;

	virtual void channelInactive(ChannelPipline * pipeline) override
	{
	}

	virtual void channelRead(ChannelPipline * pipeline, MessageObj * message) override;

	virtual void writeAsync(ChannelPipline * pipeline, MessageObj * message) override
	{
		pipeline->writeAsync(message);
	}

	virtual void channelError(ChannelPipline * pipeline, int error, const char * const message) override;

private:


	/*
	buffer to queue all received data
	*/
	std::map<unsigned char, gamesen::CircularBuffer> _receivedBuffer;

	BasePeer* _peer;

	AbstractSocketChannel* _channel;
};


}

#endif //