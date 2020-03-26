#ifndef _SENLA_DESERIALIZER_H
#define _SENLA_DESERIALIZER_H
#include "Value.h"
#include "DataContainer.h"
#include "CircularBuffer.h"

namespace Senla {
using namespace gamesen;

class Deserializer
{
public:
	Deserializer();
	virtual ~Deserializer();

	DataContainer* DeserializeData(CircularBuffer &rawData);
};

}
#endif //_SENLA_DESERIALIZER_H