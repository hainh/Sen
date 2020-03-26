#ifndef _SENLA_SERIALIZER_H_
#define _SENLA_SERIALIZER_H_

#include <vector>
#include "Value.h"
#include "DataContainer.h"

namespace Senla {

class Serializer
{
public:
	Serializer();
	virtual ~Serializer();

	std::vector<byte>& Serialize(DataContainer* dataContainer);

private:
	std::vector<byte> buffer;
	std::vector<byte> encodedBuffer;
};

}

#endif // End _SENLA_SERIALIZER_H_