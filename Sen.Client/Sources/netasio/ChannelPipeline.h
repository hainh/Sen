#ifndef _SENLA_CHANNEL_PIPELINE_H_
#define _SENLA_CHANNEL_PIPELINE_H_

#include <functional>
#include "ChannelHandler.h"
#include "AbstractSocketChannel.h"

namespace Senla {

#define Assert(condition, message) if (!condition) printf("Assert fail, %s, line %d, %s", message, __LINE__, __FILE__)

class AbstractSocketChannel;

class ChannelHandlerContext
{
public:
	ChannelHandlerContext(ChannelHandler* handler, ChannelHandlerContext* next, ChannelHandlerContext* prev)
		: _next(next)
		, _prev(prev)
		, _handler(handler)
	{
		if (next)
		{
			next->_prev = this;
		}
		if (prev)
		{
			prev->_next = this;
		}
	}

	ChannelHandlerContext* Next()
	{
		return _next;
	}

	ChannelHandlerContext* Prev()
	{
		return _prev;
	}

	ChannelHandler* Handler()
	{
		return _handler;
	}

	ChannelHandler* removeLast()
	{
		auto last = this;
		auto prev = this->_prev;
		while (last->_next)
		{
			prev = last;
			last = last->_next;
		}

		auto handler = last->_handler;
		delete last;

		if (prev)
		{
			prev->_next = nullptr;
		}

		return handler;
	}

	ChannelHandler* removeFirst()
	{
		auto first = this;
		auto next = this->_next;
		while (first->_prev)
		{
			next = first;
			first = first->_prev;
		}

		auto handler = first->_handler;
		delete first;

		if (next)
		{
			next->_prev = nullptr;
		}

		return handler;
	}

	//friend class ChannelPipeline;
private:
	ChannelHandlerContext* _next;
	ChannelHandlerContext* _prev;
	ChannelHandler* _handler;
};

class ChannelPipline
{
public:
	ChannelPipline(AbstractSocketChannel* channel)
		: _firstHandlerContext(nullptr)
		, _currentHandlerContext(nullptr)
		, _socketChannel(channel)
	{
	}

	virtual ~ChannelPipline()
	{
		auto next = _firstHandlerContext;
		while (next)
		{
			next = _firstHandlerContext->Next();
			delete _firstHandlerContext;
			_firstHandlerContext = next;
		}
	}

	virtual void addLast(ChannelHandler* handler)
	{
		if (_firstHandlerContext == nullptr)
		{
			_firstHandlerContext = new ChannelHandlerContext(handler, nullptr, nullptr);
			return;
		}
		auto last = _firstHandlerContext;
		while (last->Next())
		{
			last = last->Next();
		}

		new ChannelHandlerContext(handler, nullptr, last);
	}

	virtual void removeLast()
	{
		if (_firstHandlerContext)
		{
			auto handler = _firstHandlerContext->removeLast();
			handler->handlerRemoved(this);
		}
	}

	virtual void addFirst(ChannelHandler* handler)
	{
		if (_firstHandlerContext == nullptr)
		{
			_firstHandlerContext = new ChannelHandlerContext(handler, nullptr, nullptr);
			return;
		}

		_firstHandlerContext = new ChannelHandlerContext(handler, _firstHandlerContext, nullptr);
	}

	virtual void removeFirst()
	{
		if (_firstHandlerContext)
		{
			auto handler = _firstHandlerContext->removeFirst();
			handler->handlerRemoved(this);
		}
	}

	virtual void fireChannelRead(MessageObj* message)
	{
		if (_currentHandlerContext)
		{
			auto currentContext = _currentHandlerContext;
			_currentHandlerContext = _currentHandlerContext->Next();
			currentContext->Handler()->channelRead(this, message);
		}
	}

	virtual void fireChannelActive()
	{
		if (_currentHandlerContext)
		{
			auto currentContext = _currentHandlerContext;
			_currentHandlerContext = _currentHandlerContext->Next();
			currentContext->Handler()->channelActive(this);
		}
	}

	virtual void fireChannelInactive()
	{
		if (_currentHandlerContext)
		{
			auto currentContext = _currentHandlerContext;
			_currentHandlerContext = _currentHandlerContext->Next();
			currentContext->Handler()->channelInactive(this);
		}
	}

	virtual void fireChannelError(int error, const char* const message)
	{
		if (_currentHandlerContext)
		{
			auto currentContext = _currentHandlerContext;
			_currentHandlerContext = _currentHandlerContext->Prev();
			currentContext->Handler()->channelError(this, error, message);
		}
	}

	virtual void writeAsync(MessageObj* message);

	virtual AbstractSocketChannel* Channel()
	{
		return _socketChannel;
	}

protected:
	ChannelHandlerContext* _firstHandlerContext;
	ChannelHandlerContext* _currentHandlerContext;

	AbstractSocketChannel* _socketChannel;

private:
	ChannelPipline(ChannelPipline&)
	{
	}
};

//typedef std::function<void(ChannelPipline*)> ChannelPiplineInitializer;

class DefaultChannelPipeline : public ChannelPipline
{
public:
	DefaultChannelPipeline(AbstractSocketChannel* channel)
		: ChannelPipline(channel)
	{

	}

	void fireChannelReadEvent(MessageObj* message)
	{
		_currentHandlerContext = _firstHandlerContext;
		this->fireChannelRead(message);
	}

	void fireChannelActiveEvent()
	{
		_currentHandlerContext = _firstHandlerContext;
		this->fireChannelActive();
	}

	void fireChannelInactiveEvent()
	{
		_currentHandlerContext = _firstHandlerContext;
		this->fireChannelInactive();
	}

	void fireChannelErrorEvent(int error, const char* const message)
	{
		// Find last handler that error propagation start
		_currentHandlerContext = _firstHandlerContext;
		while (_currentHandlerContext->Next())
		{
			_currentHandlerContext = _currentHandlerContext->Next();
		}

		fireChannelError(error, message);
	}

	void writeAsyncBegin(MessageObj* message)
	{
		// Find last handler that writeAsync start
		_currentHandlerContext = _firstHandlerContext;
		while (_currentHandlerContext->Next())
		{
			_currentHandlerContext = _currentHandlerContext->Next();
		}

		// write by last hander
		writeAsync(message);
	}
};


}

#endif //_SENLA_CHANNEL_PIPELINE_H_