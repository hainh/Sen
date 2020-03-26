#ifndef _SENLA_CHANNEL_OPTIONS_H_
#define _SENLA_CHANNEL_OPTIONS_H_

namespace Senla {

template<typename T>
class ChannelOption
{
public:

	ChannelOption(T value)
	{
		Value = value;
	}

	T Value;

	virtual ~ChannelOption()
	{
	}
};

#define ConstructorOption(_class, tname) _class(tname value) : ChannelOption<tname>(value) {}
class ChannelOptions
{
public:
	class SoBacklog : public ChannelOption<int>
	{
		ConstructorOption(SoBacklog, int)
	};

};


}

#endif //_SENLA_CHANNEL_OPTIONS_H_