#ifndef _SENLA_CHANNEL_HANDLER_H_
#define _SENLA_CHANNEL_HANDLER_H_

namespace Senla {

class ChannelPipline;
class MessageObj;

class ChannelHandler
{
public:
	virtual void handlerAdded(ChannelPipline* pipeline) = 0;
	virtual void handlerRemoved(ChannelPipline* pipeline) = 0;
	virtual void channelActive(ChannelPipline* pipeline) = 0;
	virtual void channelInactive(ChannelPipline* pipeline) = 0;
	virtual void channelRead(ChannelPipline* pipeline, MessageObj* message) = 0;
	virtual void writeAsync(ChannelPipline* pipeline, MessageObj* message) = 0;
	virtual void channelError(ChannelPipline* pipeline, int error, const char* const message) = 0;
private:

};

}

#endif //_SENLA_CHANNEL_HANDLER_H_