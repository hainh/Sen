#ifndef _SENLA_ABSTRACTSOCKET_CHANNEL_H_
#define _SENLA_ABSTRACTSOCKET_CHANNEL_H_

#include <string>
#include "MessageObj.h"
#include "ChannelPipeline.h"

namespace Senla {

class ChannelPipline;

class AbstractSocketChannel
{
public:
	AbstractSocketChannel()
	{
		s_countInstances++;
	}

	virtual ~AbstractSocketChannel()
	{
		s_countInstances--;
	}

	virtual void writeAsync(MessageObj* message) = 0;

	virtual void connect(const std::string& endpoint, unsigned short port) = 0;

	virtual void disconnect() = 0;

	virtual void initialize(std::function<void(ChannelPipline*)> initialize);

	//virtual void onError(int errorCode, const char* const message) = 0;
protected:
	friend class ChannelPipline;
	virtual void writeMessageOnWire(MessageObj* message) = 0;

protected:
	ChannelPipline* _pipeline;

	static int s_countInstances;
	//ChannelPipelineInitialzeDelegate* _channelInitializer;

private:
	AbstractSocketChannel(const AbstractSocketChannel&)
	{
	}
	AbstractSocketChannel& operator=(const AbstractSocketChannel&)
	{
	}
};


}

#endif //_SENLA_ABSTRACTSOCKET_CHANNEL_H_