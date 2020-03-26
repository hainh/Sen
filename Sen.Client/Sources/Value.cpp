#include <cassert>
#include <sstream>
#include "Value.h"

std::string Types[];

namespace Senla
{
	Value::~Value()
	{
		switch (type_)
		{
		case Senla::Value::Type::STRING:
			if (value_.string_ != nullptr)
			{
				delete value_.string_;
				value_.string_ = nullptr;
			}
			break;
		case Senla::Value::Type::DICTIONARY:
			if (value_.dict_ != nullptr)
			{
				delete value_.dict_;
				value_.dict_ = nullptr;
			}
			break;
		case Senla::Value::Type::ARRAY:
			if (value_.array_ != nullptr)
			{
				delete value_.array_;
				value_.array_ = nullptr;
			}
			break;
		default:
			break;
		}
	}

	void Value::swap(Value & other)
	{
		Type temp = type_;
		type_ = other.type_;
		other.type_ = temp;

		ValueHolder val = value_;
		value_ = other.value_;
		other.value_ = val;
	}

	bool Value::hasEntry(byte key)
	{
		assert(type_ == Type::DICTIONARY);
		return value_.dict_->find(key) != value_.dict_->end();
	}

	std::vector<Value>& Value::asArrayValue()
	{
		assert(type_ == Type::ARRAY);

		return *value_.array_;
	}

	std::map<byte, Value>& Value::asDict()
	{
		assert(type_ == Type::DICTIONARY);
		return *value_.dict_;
	}

	std::string Value::toString()
	{
		std::ostringstream oss;

		if ((int)type_ < (int)Senla::Value::Type::DICTIONARY)
		{
			oss << "[" << Types[(int)type_ + 1] << "] ";
		}

		switch (type_)
		{
		case Senla::Value::Type::NULL_TYPE:
			break;
		case Senla::Value::Type::BOOL:
			oss << (value_.bool_ ? "true" : "false");
			break;
		case Senla::Value::Type::BYTE:
			oss << (int)asByte();
			break;
		case Senla::Value::Type::INT16:
			oss << (int)asInt16();
			break;
		case Senla::Value::Type::INT32:
			oss << asInt32();
			break;
		case Senla::Value::Type::INT64:
			oss << asInt64();
			break;
		case Senla::Value::Type::UINT16:
			oss << (int)asUint16();
			break;
		case Senla::Value::Type::UINT32:
			oss << asUint32();
			break;
		case Senla::Value::Type::UINT64:
			oss << asUint64();
			break;
		case Senla::Value::Type::FLOAT:
			oss << asFloat();
			break;
		case Senla::Value::Type::DOUBLE:
			oss << asDouble();
			break;
		case Senla::Value::Type::STRING:
			oss << asString();
			break;
		case Senla::Value::Type::DICTIONARY:
		{
			oss << "{\n";
			auto& dic = asDict();
			auto size = dic.size();

			for (auto &item : dic)
			{
				oss << "  " << (int)item.first << ": " << item.second.toString();
				if (--size > 0)
				{
					oss << '\n';
				}
			}

			oss << "\n}";
		}
			break;
		case Senla::Value::Type::ARRAY_BOOL:
		case Senla::Value::Type::ARRAY_BYTE:
		case Senla::Value::Type::ARRAY_INT16:
		case Senla::Value::Type::ARRAY_INT32:
		case Senla::Value::Type::ARRAY_INT64:
		case Senla::Value::Type::ARRAY_UINT16:
		case Senla::Value::Type::ARRAY_UINT32:
		case Senla::Value::Type::ARRAY_UINT64:
		case Senla::Value::Type::ARRAY_FLOAT:
		case Senla::Value::Type::ARRAY_DOUBLE:
		case Senla::Value::Type::ARRAY_STRING:
		{
			auto &items = asArrayValue();
			oss << "[";
			for (size_t i = 0; i < items.size(); i++)
			{
				oss << items[i].toString();
				if (i < items.size() - 1)
				{
					oss << ", ";
				}
			}
			oss << "]";
		}
			break;
		default:
			break;
		}

		return oss.str();
	}

	bool Value::dict()
	{
		if (type_ != Type::NULL_TYPE)
		{
			return false;
		}

		type_ = Type::DICTIONARY;
		value_.dict_ = new std::map<byte, Value>();

		return true;
	}

	void Value::assertType(const Type & type) const
	{
		assert(type_ == type);
	}

	/*Value & Value::at(size_t index)
	{
		if (type_ == Value::Type::DICTIONARY)
		{
			std::map<byte, Value>::iterator it = value_.dict_->find(index);
			if (it == value_.dict_->end())
			{
				assert(false);
			}

			return it->second;
		}
		else if (type_ == Type::ARRAY)
		{
			return (*value_.array_)[index];
		}
	}*/

	Value::Value(bool value)
		: type_(Type::BOOL)
	{
		value_.bool_ = value;
	}

	Value::Value(byte value)
		: type_(Type::BYTE)
	{
		value_.byte_ = value;
	}

	Value::Value(int16 value)
		: type_(Type::INT16)
	{
		value_.int16_ = value;
	}

	Value::Value(int32 value)
		: type_(Type::INT32)
	{
		value_.int32_ = value;
	}

	Value::Value(int64 value)
		: type_(Type::INT64)
	{
		value_.int64_ = value;
	}

	Value::Value(uint16 value)
		: type_(Type::UINT16)
	{
		value_.uint16_ = value;
	}

	Value::Value(uint32 value)
		: type_(Type::UINT32)
	{
		value_.uint32_ = value;
	}
	Value::Value(uint64 value)
		: type_(Type::UINT64)
	{
		value_.uint64_ = value;
	}

	Value::Value(float value)
		: type_(Type::FLOAT)
	{
		value_.float_ = value;
	}

	Value::Value(double value)
		: type_(Type::DOUBLE)
	{
		value_.double_ = value;
	}

	Value::Value(const std::string& value)
		: type_(Type::STRING)
	{
		value_.string_ = new std::string(value);
	}

	Value::Value(const std::vector<Value>& values)
		:type_(Type::ARRAY)
	{
		if (values.empty())
		{
			type_ = Type::NULL_TYPE;
			return;
		}
		value_.array_ = new std::vector<Value>(values);
	}

	Value::Value(const std::map<byte, Value>& values)
		: type_(Type::DICTIONARY)
	{
		if (values.empty())
		{
			type_ = Type::NULL_TYPE;
			return;
		}
		value_.dict_ = new std::map<byte, Value>(values);
	}

	Value::Value(const Value & other)
		: type_(other.type_)
	{
		switch (type_)
		{
		case Senla::Value::Type::BOOL:
		case Senla::Value::Type::BYTE:
		case Senla::Value::Type::INT16:
		case Senla::Value::Type::INT32:
		case Senla::Value::Type::INT64:
		case Senla::Value::Type::UINT16:
		case Senla::Value::Type::UINT32:
		case Senla::Value::Type::UINT64:
		case Senla::Value::Type::FLOAT:
		case Senla::Value::Type::DOUBLE:
			value_ = other.value_;
			break;
		case Senla::Value::Type::STRING:
			value_.string_ = new std::string(*other.value_.string_);
			break;
		case Senla::Value::Type::DICTIONARY:
			value_.dict_ = new std::map<byte, Value>(other.value_.dict_->begin(), other.value_.dict_->end());
			break;
		case Senla::Value::Type::ARRAY:
			value_.array_ = new std::vector<Value>(other.value_.array_->begin(), other.value_.array_->end());
			break;
		default:
			break;
		}
	}

	Value::Value()
		: type_(Value::Type::NULL_TYPE)
	{
	}


	bool Value::asBool()
	{
		assertType(Type::BOOL);
		return value_.bool_;
	}

	byte Value::asByte()
	{
		assertType(Type::BYTE);
		return value_.byte_;
	}

	int16 Value::asInt16()
	{
		assertType(Type::INT16);
		return value_.int16_;
	}

	int32 Value::asInt32()
	{
		assertType(Type::INT32);
		return value_.int32_;
	}

	int64 Value::asInt64()
	{
		assertType(Type::INT64);
		return value_.int64_;
	}

	uint16 Value::asUint16()
	{
		assertType(Type::UINT16);
		return value_.uint16_;
	}

	uint32 Value::asUint32()
	{
		assertType(Type::UINT32);
		return value_.uint32_;
	}

	uint64 Value::asUint64()
	{
		assertType(Type::UINT64);
		return value_.uint64_;
	}

	float Value::asFloat()
	{
		assertType(Type::FLOAT);
		return value_.float_;
	}

	double Value::asDouble()
	{
		assertType(Type::DOUBLE);
		return value_.double_;
	}

	std::string & Value::asString()
	{
		assertType(Type::STRING);
		return *value_.string_;
	}

	Value & Value::operator[](size_t index)
	{
		assertType(Type::ARRAY);
		return (*value_.array_)[index];
	}

	const Value & Value::operator[](size_t index) const
	{
		assertType(Type::ARRAY);
		return (*value_.array_)[index];
	}

	Value & Value::operator[](byte index)
	{
		dict();

		assertType(Type::DICTIONARY);

		std::map<byte, Value>::iterator it = value_.dict_->find(index);
		if (it == value_.dict_->end())
		{
			value_.dict_->insert(std::pair<byte, Value>(index, Value()));
			it = value_.dict_->find(index);
		}

		return it->second;
	}

	const Value & Value::operator[](byte index) const
	{
		assert(type_ == Type::DICTIONARY);

		std::map<byte, Value>::iterator it = value_.dict_->find(index);
		if (it == value_.dict_->end())
		{
			value_.dict_->insert(std::pair<byte, Value>(index, Value()));
			it = value_.dict_->find(index);
		}

		return it->second;
	}

	Value & Value::operator=(Value other)
	{
		swap(other);

		return (*this);
	}
}



std::string Types[]
{
	"NULL",
	"BOOL",
	"BYTE",
	"INT16",
	"INT32",
	"INT64",
	"UINT16",
	"UINT32",
	"UINT64",
	"FLOAT",
	"DOUBLE",
	"STRING",
	"", //DICTIONARY
	"",
	"",
	"",
	"",

	"ARRAY_BOOL", //16
	"ARRAY_BYTE",
	"ARRAY_INT16",
	"ARRAY_INT32",
	"ARRAY_INT64",
	"ARRAY_UINT16",
	"ARRAY_UINT32",
	"ARRAY_UINT64",
	"ARRAY_FLOAT",
	"ARRAY_DOUBLE",
	"ARRAY_STRING",
};