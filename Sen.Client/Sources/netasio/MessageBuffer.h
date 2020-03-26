#ifndef _SENLA_ABSTRACT_SOCKET_H
#define _SENLA_ABSTRACT_SOCKET_H

#include <string>
#include "MessageObj.h"

namespace Senla {

class MessageBuffer : public MessageObj
{
public:
	MessageBuffer(uint8_t* buffer, uint32_t size)
	{
		copy(buffer, size);
	}

	MessageBuffer(MessageBuffer& other)
	{
		copy(other._buffer, other._size);
	}

	virtual ~MessageBuffer()
	{
		if (_buffer)
		{
			delete[] _buffer;
			_buffer = nullptr;
		}
	}

	uint8_t* data() { return _buffer; }

	uint32_t size() { return _size; }

private:
	void copy(uint8_t* buffer, uint32_t size)
	{
		_buffer = new uint8_t[size];
		_size = size;
		memcpy(_buffer, buffer, sizeof(uint8_t) * size);
	}

private:

	uint8_t* _buffer;
	uint32_t _size;
};

}

#endif //