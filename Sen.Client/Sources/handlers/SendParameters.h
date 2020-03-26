#ifndef _SENLA_SENDPARAMETERS_H_
#define _SENLA_SENDPARAMETERS_H_

namespace Senla {


struct SendParameters
{
	unsigned char ChannelId;
	bool Reliable;
	bool Encrypted;
	bool Flush;

	SendParameters()
		: ChannelId(0)
		, Reliable(true)
		, Encrypted(false)
		, Flush(true)
	{
	}

	SendParameters(unsigned char channelId, bool reliable, bool encrypted, bool flush)
		: ChannelId(channelId)
		, Reliable(reliable)
		, Encrypted(encrypted)
		, Flush(flush)
	{
	}
};


}

#endif //_SENLA_SENDPARAMETERS_H_