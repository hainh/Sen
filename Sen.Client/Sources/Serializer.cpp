#include "Serializer.h"

namespace Senla {

#define AddRange(buffer, encodeExp) encodeExp; buffer.insert(buffer.end(), encodedBuffer.begin(), encodedBuffer.end())

static inline byte hasMoreByte(uint64 value)
{
	return (byte)(value | 128);
}
/// <summary>
/// Encode numberic type that equal or more than 2 bytes length.
/// </summary>
/// <param name="value"></param>
/// <param name="output"></param>
/// <returns></returns>
static std::vector<byte>& encodeVarInt(uint64 value, std::vector<byte>& output)
{
	output.clear();
	if (value < (1 << 7))
	{
		output.push_back((byte)value);
	}
	else if (value < (1UL << 14))
	{
		output.push_back(hasMoreByte(value & 0x7F));
		output.push_back((byte)(value >> 7));
	}
	else if (value < (1UL << 21))
	{
		output.push_back(hasMoreByte(value & 0x7F));
		output.push_back(hasMoreByte((value >> 7) & 0x7F));
		output.push_back((byte)(value >> 14));
	}
	else if (value < (1UL << 28))
	{
		output.push_back(hasMoreByte(value & 0x7F));
		output.push_back(hasMoreByte((value >> 7) & 0x7F));
		output.push_back(hasMoreByte((value >> 14) & 0x7F));
		output.push_back((byte)(value >> 21));
	}
	else if (value < ((uint64)1UL << 35))
	{
		output.push_back(hasMoreByte(value & 0x7F));
		output.push_back(hasMoreByte((value >> 7) & 0x7F));
		output.push_back(hasMoreByte((value >> 14) & 0x7F));
		output.push_back(hasMoreByte((value >> 21) & 0x7F));
		output.push_back((byte)(value >> 28));
	}
	else if (value < ((uint64)1UL << 42))
	{
		output.push_back(hasMoreByte(value & 0x7F));
		output.push_back(hasMoreByte((value >> 7) & 0x7F));
		output.push_back(hasMoreByte((value >> 14) & 0x7F));
		output.push_back(hasMoreByte((value >> 21) & 0x7F));
		output.push_back(hasMoreByte((value >> 28) & 0x7F));
		output.push_back((byte)(value >> 35));
	}
	else if (value < ((uint64)1UL << 49))
	{
		output.push_back(hasMoreByte(value & 0x7F));
		output.push_back(hasMoreByte((value >> 7) & 0x7F));
		output.push_back(hasMoreByte((value >> 14) & 0x7F));
		output.push_back(hasMoreByte((value >> 21) & 0x7F));
		output.push_back(hasMoreByte((value >> 28) & 0x7F));
		output.push_back(hasMoreByte((value >> 35) & 0x7F));
		output.push_back((byte)(value >> 42));
	}
	else if (value < ((uint64)1UL << 56))
	{
		output.push_back(hasMoreByte(value & 0x7F));
		output.push_back(hasMoreByte((value >> 7) & 0x7F));
		output.push_back(hasMoreByte((value >> 14) & 0x7F));
		output.push_back(hasMoreByte((value >> 21) & 0x7F));
		output.push_back(hasMoreByte((value >> 28) & 0x7F));
		output.push_back(hasMoreByte((value >> 35) & 0x7F));
		output.push_back(hasMoreByte((value >> 42) & 0x7F));
		output.push_back((byte)(value >> 49));
	}
	else if (value < ((uint64)1UL << 63))
	{
		output.push_back(hasMoreByte(value & 0x7F));
		output.push_back(hasMoreByte((value >> 7) & 0x7F));
		output.push_back(hasMoreByte((value >> 14) & 0x7F));
		output.push_back(hasMoreByte((value >> 21) & 0x7F));
		output.push_back(hasMoreByte((value >> 28) & 0x7F));
		output.push_back(hasMoreByte((value >> 35) & 0x7F));
		output.push_back(hasMoreByte((value >> 42) & 0x7F));
		output.push_back(hasMoreByte((value >> 49) & 0x7F));
		output.push_back((byte)(value >> 56));
	}
	else
	{
		output.push_back(hasMoreByte(value & 0x7F));
		output.push_back(hasMoreByte((value >> 7) & 0x7F));
		output.push_back(hasMoreByte((value >> 14) & 0x7F));
		output.push_back(hasMoreByte((value >> 21) & 0x7F));
		output.push_back(hasMoreByte((value >> 28) & 0x7F));
		output.push_back(hasMoreByte((value >> 35) & 0x7F));
		output.push_back(hasMoreByte((value >> 42) & 0x7F));
		output.push_back(hasMoreByte((value >> 49) & 0x7F));
		output.push_back(hasMoreByte((value >> 56) & 0x7F));
		output.push_back((byte)(value >> 63));
	}

	return output;
}

static uint64 getJaggedValue(int64 originValue)
{
	uint64 value = originValue < 0 ? (((uint64)(-originValue)) << 1) | 0x01 : (uint64)originValue << 1;
	return value;
}

/// <summary>
/// Encode numberic type that equal or more than 2 bytes length as a "jagged" value
/// </summary>
/// <param name="originValue"></param>
/// <param name="output"></param>
/// <returns></returns>
static std::vector<byte>& encodeVarInt(int64 originValue, std::vector<byte>& output)
{
	// To "jagged" value
	uint64 value = getJaggedValue(originValue);
	return encodeVarInt(value, output);
}

static bool isLittleEndian()
{
	static int endian = -1;
	if (endian == -1)
	{
		int checker = 1;

		char* c = reinterpret_cast<char*>(&checker);
		endian = *c;
	}

	return endian == 1;
}

static std::vector<byte>& getBytes(float value, std::vector<byte>& output)
{
	output.clear();
	if (isLittleEndian())
	{
		byte* v = reinterpret_cast<byte*>(&value);
		output.push_back(*v);
		output.push_back(*(v + 1));
		output.push_back(*(v + 2));
		output.push_back(*(v + 3));
	}
	else
	{
		byte* v = reinterpret_cast<byte*>(&value);
		output.push_back(*(v + 3));
		output.push_back(*(v + 2));
		output.push_back(*(v + 1));
		output.push_back(*v);
	}

	return output;
}

static std::vector<byte>& getBytes(double value, std::vector<byte>& output)
{
	output.clear();
	if (isLittleEndian())
	{
		byte* v = reinterpret_cast<byte*>(&value);
		output.push_back(*v);
		output.push_back(*(v + 1));
		output.push_back(*(v + 2));
		output.push_back(*(v + 3));
		output.push_back(*(v + 4));
		output.push_back(*(v + 5));
		output.push_back(*(v + 6));
		output.push_back(*(v + 7));
	}
	else
	{
		byte* v = reinterpret_cast<byte*>(&value);
		output.push_back(*(v + 7));
		output.push_back(*(v + 6));
		output.push_back(*(v + 5));
		output.push_back(*(v + 4));
		output.push_back(*(v + 3));
		output.push_back(*(v + 2));
		output.push_back(*(v + 1));
		output.push_back(*v);
	}

	return output;
}

static std::vector<byte>& getBytes(std::string& value, std::vector<byte>& output)
{
	output.clear();
	output.insert(output.end(), value.begin(), value.end());

	return output;
}

Serializer::Serializer()
{
}

Serializer::~Serializer()
{
}

std::vector<byte>& Senla::Serializer::Serialize(DataContainer * data)
{
	buffer.clear();

	// Put data code
	buffer.push_back(data->Code());
	uint64 param = 0;

	Value& parameters = data->Parameters();

	if (parameters.isNull())
	{
		buffer.insert(buffer.begin(), data->getServiceType() | (1 << 3));
		return buffer;
	}

	auto& dict = parameters.asDict();

	for (auto& item : dict)
	{
		// Put parameter code
		buffer.push_back(item.first);
		
		if (item.second.isByte())
		{
			param = (byte)Value::Type::BYTE | (uint64)((byte)(item.second) << 5);
			AddRange(buffer, encodeVarInt(param, encodedBuffer));
		}
		else if (item.second.isBool())
		{
			param = (byte)Value::Type::BOOL | ((bool)item.second ? 1UL << 5 : 0);
			AddRange(buffer, encodeVarInt(param, encodedBuffer));
		}
		else if (item.second.isInt16())
		{
			param = (byte)Value::Type::INT16 | (getJaggedValue((short)item.second) << 5);
			AddRange(buffer, encodeVarInt(param, encodedBuffer));
		}
		else if (item.second.isInt32())
		{
			param = (byte)Value::Type::INT32 | (getJaggedValue((int)item.second) << 5);
			AddRange(buffer, encodeVarInt(param, encodedBuffer));
		}
		else if (item.second.isInt64())
		{
			buffer.push_back((byte)Value::Type::INT64);
			param = getJaggedValue((int64)item.second);
			AddRange(buffer, encodeVarInt(param, encodedBuffer));
		}
		else if (item.second.isUint16())
		{
			param = (byte)Value::Type::UINT16 | (uint64)(((uint16)item.second) << 5);
			AddRange(buffer, encodeVarInt(param, encodedBuffer));
		}
		else if (item.second.isUint32())
		{
			param = (byte)Value::Type::UINT32 | (((uint32)item.second) << 5);
			AddRange(buffer, encodeVarInt(param, encodedBuffer));
		}
		else if (item.second.isUint64())
		{
			buffer.push_back((byte)Value::Type::UINT64);
			param = (uint64)item.second;
			AddRange(buffer, encodeVarInt(param, encodedBuffer));
		}
		else if (item.second.isFloat())
		{
			buffer.push_back((byte)Value::Type::FLOAT);
			AddRange(buffer, getBytes((float)item.second, encodedBuffer));
		}
		else if (item.second.isDouble())
		{
			buffer.push_back((byte)Value::Type::DOUBLE);
			AddRange(buffer, getBytes((double)item.second, encodedBuffer));
		}
		else if (item.second.isString())
		{
			std::string str = item.second.asString();
			param = (byte)Value::Type::STRING | (((uint32)str.size() << 5));
			AddRange(buffer, encodeVarInt(param, encodedBuffer));
			AddRange(buffer, getBytes(str, encodedBuffer));
		}
		else if (item.second.isNull())
		{
			buffer.push_back((byte)Value::Type::ARRAY);
		}
		else if (item.second.isArray())
		{
			const uint32 length = (uint32)item.second.length();
			Value& type = item.second[(size_t)0]; // first item knows type
			if (type.isBool())
			{
				Value& bools = item.second;
				param = (byte)Value::Type::ARRAY_BOOL | ((uint32)(length) << 5);
				AddRange(buffer, encodeVarInt(param, encodedBuffer));

				int bytesNeeded = length / 8 + (length % 8 > 0 ? 1 : 0);
				byte b = 0;
				for (size_t i = 0; i < length; i++)
				{
					if (bools[i])
					{
						b = (byte)(b | (1 << (i % 8)));
					}

					if ((i + 1) % 8 == 0)
					{
						buffer.push_back(b);
						b = 0;
					}
					else if (i == length - 1)
					{
						buffer.push_back(b);
					}
				}
			}
			else if (type.isByte())
			{
				param = (byte)Value::Type::ARRAY_BYTE | ((length) << 5);
				AddRange(buffer, encodeVarInt(param, encodedBuffer));
				std::vector<byte> data = item.second.asArray<byte>();
				buffer.insert(buffer.end(), data.begin(), data.end());
			}
			else if (type.isInt16())
			{
				param = (byte)Value::Type::ARRAY_INT16 | (length << 5);
				AddRange(buffer, encodeVarInt(param, encodedBuffer));
				for (size_t i = 0; i < length; i++)
				{
					AddRange(buffer, encodeVarInt((int64)(int16)item.second[i], encodedBuffer));
				}
			}
			else if (type.isInt32())
			{
				param = (byte)Value::Type::ARRAY_INT32 | (length << 5);
				AddRange(buffer, encodeVarInt(param, encodedBuffer));
				for (size_t i = 0; i < length; i++)
				{
					AddRange(buffer, encodeVarInt((int64)(int32)item.second[i], encodedBuffer));
				}
			}
			else if (type.isInt64())
			{
				param = (byte)Value::Type::ARRAY_INT64 | (length << 5);
				AddRange(buffer, encodeVarInt(param, encodedBuffer));
				for (size_t i = 0; i < length; i++)
				{
					AddRange(buffer, encodeVarInt((int64)item.second[i], encodedBuffer));
				}
			}
			else if (type.isUint16())
			{
				param = (byte)Value::Type::ARRAY_UINT16 | (length << 5);
				AddRange(buffer, encodeVarInt(param, encodedBuffer));
				for (size_t i = 0; i < length; i++)
				{
					AddRange(buffer, encodeVarInt((uint64)(uint16)item.second[i], encodedBuffer));
				}
			}
			else if (type.isUint32())
			{
				param = (byte)Value::Type::ARRAY_UINT32 | (length << 5);
				AddRange(buffer, encodeVarInt(param, encodedBuffer));
				for (size_t i = 0; i < length; i++)
				{
					AddRange(buffer, encodeVarInt((uint64)(uint32)item.second[i], encodedBuffer));
				}
			}
			else if (type.isUint64())
			{
				param = (byte)Value::Type::ARRAY_UINT64 | (length << 5);
				AddRange(buffer, encodeVarInt(param, encodedBuffer));
				for (size_t i = 0; i < length; i++)
				{
					AddRange(buffer, encodeVarInt((uint64)item.second[i], encodedBuffer));
				}
			}
			else if (type.isFloat())
			{
				param = (byte)Value::Type::ARRAY_FLOAT | (length << 5);
				AddRange(buffer, encodeVarInt(param, encodedBuffer));
				for (size_t i = 0; i < length; i++)
				{
					AddRange(buffer, getBytes((float)item.second[i], encodedBuffer));
				}
			}
			else if (type.isDouble())
			{
				param = (byte)Value::Type::ARRAY_DOUBLE | (length << 5);
				AddRange(buffer, encodeVarInt(param, encodedBuffer));
				for (size_t i = 0; i < length; i++)
				{
					AddRange(buffer, getBytes((double)item.second[i], encodedBuffer));
				}
			}
			else if (type.isString())
			{
				param = (byte)Value::Type::ARRAY_STRING | (length << 5);
				AddRange(buffer, encodeVarInt(param, encodedBuffer));
				for (size_t i = 0; i < length; i++)
				{
					std::string str = item.second[i].asString();
					param = (uint32)str.size();
					AddRange(buffer, encodeVarInt(param, encodedBuffer));
					AddRange(buffer, getBytes(str, encodedBuffer));
				}
			}
		}
	}

	uint64 size = (uint64)buffer.size();

	param = (size << 3) | data->getServiceType();
	encodeVarInt(param, encodedBuffer);
	buffer.insert(buffer.begin(), encodedBuffer.begin(), encodedBuffer.end());

	// return the buffer array
	return buffer;
}

}