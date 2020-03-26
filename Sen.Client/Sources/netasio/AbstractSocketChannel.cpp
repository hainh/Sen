#include "AbstractSocketChannel.h"

void Senla::AbstractSocketChannel::initialize(std::function<void(ChannelPipline*)> initialize)
{
	initialize(_pipeline);
}
