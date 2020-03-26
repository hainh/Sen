#ifndef _SENLA_MESSAGES_H_
#define _SENLA_MESSAGES_H_

#include "../netasio/MessageBuffer.h"
#include "SendParameters.h"
#include "../DataContainer.h"

namespace Senla {

class MessageWrapper : public MessageObj
{
public:
	MessageWrapper(MessageBuffer* messageBuffer, SendParameters sendParameters)
		: messageBuffer(messageBuffer)
		, sendParameters(sendParameters)
	{

	}

	MessageBuffer* messageBuffer;
	SendParameters sendParameters;
};

struct MessageDispatched
{
	DataContainer* message;
	SendParameters sendParameters;
};


}

#endif //_SENLA_MESSAGES_H_