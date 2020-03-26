#include "ChannelPipeline.h"

void Senla::ChannelPipline::writeAsync(MessageObj * message)
{
	if (_currentHandlerContext)
	{
		auto currentContext = _currentHandlerContext;
		_currentHandlerContext = _currentHandlerContext->Prev();
		currentContext->Handler()->writeAsync(this, message);
	}
	else
	{
		_socketChannel->writeMessageOnWire(message);
	}
}
