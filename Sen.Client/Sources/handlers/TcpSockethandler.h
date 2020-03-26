#ifndef _SENLA_TCP_SOCKET_HANDLER_H_
#define _SENLA_TCP_SOCKET_HANDLER_H_

#include "../netasio/ChannelPipeline.h"
#include "Messages.h"

namespace Senla {


class TcpSocketHandler : public ChannelHandler
{
public:
	// Inherited via ChannelHandler
	virtual void handlerAdded(ChannelPipline * pipeline) override
	{
}
	virtual void handlerRemoved(ChannelPipline * pipeline) override
	{
	}
	virtual void channelActive(ChannelPipline * pipeline) override
	{
		pipeline->fireChannelActive();
	}
	virtual void channelInactive(ChannelPipline * pipeline) override
	{
		pipeline->fireChannelInactive();
	}
	virtual void channelRead(ChannelPipline * pipeline, MessageObj * message) override
	{
		MessageWrapper messageWrapper((MessageBuffer*)message, SendParameters());
		pipeline->fireChannelRead(&messageWrapper);
	}
	virtual void writeAsync(ChannelPipline * pipeline, MessageObj * message) override
	{
		pipeline->writeAsync(((MessageWrapper*)message)->messageBuffer);
	}
	virtual void channelError(ChannelPipline * pipeline, int error, const char * const message) override
	{
		pipeline->fireChannelError(error, message);
	}
};


}

#endif //_SENLA_TCP_SOCKET_HANDLER_H_