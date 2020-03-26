#include "Deserializer.h"

namespace Senla {

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

static bool TryReadVarInt(CircularBuffer &data, uint64 &varInt, int &offset)
{
	varInt = 0;
	if (data.size() == 0)
	{
		return false;
	}

	int i = 0;
	int shift;
	while (true)
	{
		byte b = data[offset];
		shift = 7 * i;
		varInt = varInt | ((uint64)(b & 0x7F) << shift);
		++offset;
		++i;
		if (shift > 63)
		{
			throw 2;
		}
		if (b < 0x80)
		{
			return true;
		}

		if (offset >= data.size())
		{
			offset -= i; // reset offset
			return false;
		}
	}

}

static byte ReadByte(CircularBuffer &data, int &offset)
{
	return data[offset++];
}

static void Discard(CircularBuffer &data, int count)
{
	for (int i = 0; i < count; i++)
	{
		data.dequeue();
	}
}

static int64 ToSignedInt(uint64 num)
{
	bool isNeg = (num & 1) == 1;
	if (isNeg)
	{
		return -(int64)(num >> 1);
	}

	return (int64)(num >> 1);
}

static byte bits[12];

static float ReadFloat(CircularBuffer &data, int &offset)
{
	float *value;
	if (isLittleEndian())
	{
		bits[0] = data[offset++];
		bits[1] = data[offset++];
		bits[2] = data[offset++];
		bits[3] = data[offset++];
	}
	else
	{
		bits[3] = data[offset++];
		bits[2] = data[offset++];
		bits[1] = data[offset++];
		bits[0] = data[offset++];
	}

	value = reinterpret_cast<float*>(bits);
	return *value;
}

static double ReadDouble(CircularBuffer &data, int &offset)
{
	double *value;
	if (isLittleEndian())
	{
		bits[0] = data[offset++];
		bits[1] = data[offset++];
		bits[2] = data[offset++];
		bits[3] = data[offset++];
		bits[4] = data[offset++];
		bits[5] = data[offset++];
		bits[6] = data[offset++];
		bits[7] = data[offset++];
	}
	else
	{
		bits[7] = data[offset++];
		bits[6] = data[offset++];
		bits[5] = data[offset++];
		bits[4] = data[offset++];
		bits[3] = data[offset++];
		bits[2] = data[offset++];
		bits[1] = data[offset++];
		bits[0] = data[offset++];
	}

	value = reinterpret_cast<double*>(bits);
	return *value;
}

static Value ReadString(CircularBuffer &data, int &offset, int length)
{
	if (length == 0)
	{
		return Value(std::string());
	}

	if (length <= sizeof(bits))
	{
		offset += data.copyTo(bits, sizeof(bits) / sizeof(byte), 0, offset, length);
		return Value(std::string((char*)bits, length));
	}

	auto bytes = new byte[length];
	offset += data.copyTo(bytes, length * sizeof(byte), 0, offset, length);
	std::string str((char*)bytes, length);
	delete[] bytes;

	return Value(str);
}

static Value ReadBoolArray(CircularBuffer &data, int &offset, int length)
{
	// Special case that carries a null
	if (length == 0)
	{
		return Value();
	}

	int bytesNeeded = length / 8 + (length % 8 > 0 ? 1 : 0);
	auto bools = std::vector<Value>();

	for (int i = 0; i < length; i++)
	{
		bools.push_back(Value((data[offset] & (1 << (i % 8))) > 0));
		if ((i + 1) % 8 == 0)
		{
			++offset;
		}
		else if (i == length - 1)
		{
			++offset;
		}
	}

	return Value(bools);
}

static Value ReadByteArray(CircularBuffer &data, int &offset, int length)
{
	auto bytes = new byte[length];
	int copied = data.copyTo(bytes, length, 0, offset, length);
	offset += copied;
	std::vector<Value> vb;
	for (int i = 0; i < length; i++)
	{
		vb.push_back(Value(bytes[i]));
	}

	return Value(vb);
}

static Value ReadInt16Array(CircularBuffer &data, int &offset, int length)
{
	std::vector<Value> ss;
	uint64 value;
	for (int i = 0; i < length; i++)
	{
		TryReadVarInt(data, value, offset);
		ss.push_back(Value((int16)ToSignedInt(value)));
	}

	return Value(ss);
}

static Value ReadInt32Array(CircularBuffer &data, int &offset, int length)
{
	std::vector<Value> ss;
	uint64 value;
	for (int i = 0; i < length; i++)
	{
		TryReadVarInt(data, value, offset);
		ss.push_back(Value((int32)ToSignedInt(value)));
	}

	return Value(ss);
}

static Value ReadInt64Array(CircularBuffer &data, int &offset, int length)
{
	std::vector<Value> ss;
	uint64 value;
	for (int i = 0; i < length; i++)
	{
		TryReadVarInt(data, value, offset);
		ss.push_back(Value(ToSignedInt(value)));
	}

	return Value(ss);
}

static Value ReadUint16Array(CircularBuffer &data, int &offset, int length)
{
	std::vector<Value> ss;
	uint64 value;
	for (int i = 0; i < length; i++)
	{
		TryReadVarInt(data, value, offset);
		ss.push_back(Value((uint16)value));
	}

	return Value(ss);
}

static Value ReadUint32Array(CircularBuffer &data, int &offset, int length)
{
	std::vector<Value> ss;
	uint64 value;
	for (int i = 0; i < length; i++)
	{
		TryReadVarInt(data, value, offset);
		ss.push_back(Value((uint32)value));
	}

	return Value(ss);
}

static Value ReadUint64Array(CircularBuffer &data, int &offset, int length)
{
	std::vector<Value> ss;
	uint64 value;
	for (int i = 0; i < length; i++)
	{
		TryReadVarInt(data, value, offset);
		ss.push_back(Value(value));
	}

	return Value(ss);
}

static Value ReadFloatArray(CircularBuffer &data, int &offset, int length)
{
	std::vector<Value> ss;
	for (int i = 0; i < length; i++)
	{
		ss.push_back(Value(ReadFloat(data, offset)));
	}

	return Value(ss);
}

static Value ReadDoubleArray(CircularBuffer &data, int &offset, int length)
{
	std::vector<Value> ss;
	for (int i = 0; i < length; i++)
	{
		ss.push_back(Value(ReadDouble(data, offset)));
	}

	return Value(ss);
}

static Value ReadStringArray(CircularBuffer &data, int &offset, int length)
{
	std::vector<Value> ss;
	uint64 slen;
	for (int i = 0; i < length; i++)
	{
		TryReadVarInt(data, slen, offset);
		ss.push_back(ReadString(data, offset, (int)slen));
	}

	return Value(ss);
}

static Value ReadParameters(CircularBuffer &data, int &offset, int maxOffset)
{
	Value parameters;
	uint64 varInt;
	byte code;
	Value::Type type;
	uint64 value;

	if (data.size() == 12 && data.getData()[0] == 1)
	{
		data[16] = 1;
	}

	while (offset < maxOffset)
	{
		code = ReadByte(data, offset);
		TryReadVarInt(data, varInt, offset);

		type = (Value::Type)(varInt & 0x1F); // 5 bits type
		value = varInt >> 5;
		int length = (int)value;

		switch (type)
		{
		case Value::Type::BOOL:
			parameters[code] = value > 0;
			break;
		case Value::Type::BYTE:
			parameters[code] = (byte)value;
			break;
		case Value::Type::INT16:
			parameters[code] = (short)ToSignedInt(value);
			break;
		case Value::Type::INT32:
			parameters[code] = (int)ToSignedInt(value);
			break;
		case Value::Type::INT64:
			TryReadVarInt(data, value, offset);
			parameters[code] = ToSignedInt(value);
			break;
		case Value::Type::UINT16:
			parameters[code] = (uint16)value;
			break;
		case Value::Type::UINT32:
			parameters[code] = (uint32)value;
			break;
		case Value::Type::UINT64:
			TryReadVarInt(data, value, offset);
			parameters[code] = value;
			break;
		case Value::Type::FLOAT:
			parameters[code] = ReadFloat(data, offset);
			break;
		case Value::Type::DOUBLE:
			parameters[code] = ReadDouble(data, offset);
			break;
		case Value::Type::STRING:
			parameters[code] = ReadString(data, offset, (int)value);
			break;
		case Value::Type::ARRAY_BOOL: // Special case that 'bools = null'
			parameters[code] = ReadBoolArray(data, offset, length);
			break;
		case Value::Type::ARRAY_BYTE:
			parameters[code] = ReadByteArray(data, offset, length);
			break;
		case Value::Type::ARRAY_INT16:
			parameters[code] = ReadInt16Array(data, offset, length);
			break;
		case Value::Type::ARRAY_INT32:
			parameters[code] = ReadInt32Array(data, offset, length);
			break;
		case Value::Type::ARRAY_INT64:
			parameters[code] = ReadInt64Array(data, offset, length);
			break;
		case Value::Type::ARRAY_UINT16:
			parameters[code] = ReadUint16Array(data, offset, length);
			break;
		case Value::Type::ARRAY_UINT32:
			parameters[code] = ReadUint32Array(data, offset, length);
			break;
		case Value::Type::ARRAY_UINT64:
			parameters[code] = ReadUint64Array(data, offset, length);
			break;
		case Value::Type::ARRAY_FLOAT:
			parameters[code] = ReadFloatArray(data, offset, length);
			break;
		case Value::Type::ARRAY_DOUBLE:
			parameters[code] = ReadDoubleArray(data, offset, length);
			break;
		case Value::Type::ARRAY_STRING:
			parameters[code] = ReadStringArray(data, offset, length);
			break;
		default:
			break;
		}
	}

	if (offset != maxOffset)
	{
		throw 3;
	}

	return parameters;
}

DataContainer * Deserializer::DeserializeData(CircularBuffer& rawData)
{
	uint64 varInt;
	int offset = 0;

	if (!TryReadVarInt(rawData, varInt, offset))
	{
		return nullptr;
	}

	ServiceType type = (ServiceType)((byte)(varInt & 0x07));
	int frameSize = (int)(varInt >> 3);

	int maxSize = 65535; // 64kB
	if (frameSize > maxSize)
	{
		throw 1;
	}

	// Not enough data for a frame
	if (rawData.size() - offset < frameSize)
	{
		return nullptr;
	}

	DataContainer* dataContainer = nullptr;
	byte code = ReadByte(rawData, offset);
	Value parameters = ReadParameters(rawData, offset, frameSize + offset - 1);

	switch (type)
	{
	case ServiceType::EventData:
		dataContainer = new EventData(code, parameters);
		break;
	case ServiceType::OperationData:
		dataContainer = new OperationData(code, parameters);
		break;
	case ServiceType::PingData:
		dataContainer = new Ping(code, parameters);
		break;
	case ServiceType::EncryptData:
		break;
	case ServiceType::ConfigData:
		dataContainer = new ConfigData(code, parameters);
		break;
	default:
		break;
	}

	Discard(rawData, offset);

	return dataContainer;
}

Deserializer::Deserializer()
{
}


Deserializer::~Deserializer()
{
}

}