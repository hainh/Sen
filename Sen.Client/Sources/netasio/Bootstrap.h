#ifndef _SENLA_BOOTSTRAP_H_
#define _SENLA_BOOTSTRAP_H_

#include "ChannelPipeline.h"
#include "ChannelOptions.h"

namespace Senla {

class Bootstrap
{
public:
	Bootstrap()
	{
	}
	~Bootstrap()
	{
		if (_channel)
		{
			delete _channel;
			_channel = nullptr;
		}
}

	template<class Channel>
	Bootstrap* channel()
	{
		_channel = new Channel();

		return this;
	}

	Bootstrap* childHandler(std::function<void(ChannelPipline*)> initializer)
	{
		_channelInitialzer = initializer;

		return this;
	}

	template<typename T>
	Bootstrap* option(ChannelOption<T> option)
	{

		return this;
	}

	AbstractSocketChannel* connect(const std::string& ip, unsigned short port)
	{
		_channel->initialize(_channelInitialzer);
		_channel->connect(ip, port);

		return _channel;
	}

	void shutdown()
	{
		_channel->disconnect();
	}

protected:
	std::function<void(ChannelPipline*)> _channelInitialzer;
	AbstractSocketChannel* _channel;
};


}

#endif //_SENLA_BOOTSTRAP_H_