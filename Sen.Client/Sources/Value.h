#ifndef _VALUE_H_
#define _VALUE_H_

#include <stdint.h>
#include <string>
#include <vector>
#include <map>

namespace Senla
{
	typedef uint8_t byte;
	typedef uint16_t uint16;
	typedef uint32_t uint32;
	typedef uint64_t uint64;
	typedef int16_t int16;
	typedef int32_t int32;
	typedef int64_t int64;

	class Value
	{
	public:
		enum class Type
		{
			NULL_TYPE = -1,
			BOOL,
			BYTE,
			INT16,
			INT32,
			INT64,
			UINT16,
			UINT32,
			UINT64,
			FLOAT,
			DOUBLE,
			STRING,
			DICTIONARY,

			ARRAY = 16,
			ARRAY_BOOL = 16,
			ARRAY_BYTE,
			ARRAY_INT16,
			ARRAY_INT32,
			ARRAY_INT64,
			ARRAY_UINT16,
			ARRAY_UINT32,
			ARRAY_UINT64,
			ARRAY_FLOAT,
			ARRAY_DOUBLE,
			ARRAY_STRING,
		};

	private:
		union ValueHolder
		{
			bool bool_;
			byte byte_;
			int16 int16_;
			int32 int32_;
			int64 int64_;
			uint16 uint16_;
			uint32 uint32_;
			uint64 uint64_;
			float float_;
			double double_;

			std::string* string_;
			std::wstring* wstring_;

			std::vector<Value>* array_;
			std::map<byte, Value>* dict_;
		};

	protected:

	private:
		ValueHolder value_;
		Type type_;

	public:
		Value(bool   value);
		Value(byte   value);
		Value(int16  value);
		Value(int32  value);
		Value(int64  value);
		Value(uint16 value);
		Value(uint32 value);
		Value(uint64 value);
		Value(float  value);
		Value(double value);
		Value(const std::string& value);
		/*
		If vector is empty, value will be Null type
		*/
		Value(const std::vector<Value>& values);
		/*
		If dict is empty, value will be Null type
		*/
		Value(const std::map<byte, Value>& values);
		Value(const Value& other);

		/*
		Construct a Null value, Null value can be a Dictionary value later by ``Value& operator[](byte index)``
		*/
		Value();

		bool isNull() { return type_ == Type::NULL_TYPE; }
		bool isBool() { return type_ == Type::BOOL; }
		bool isByte() { return type_ == Type::BYTE; }
		bool isInt16() { return type_ == Type::INT16; }
		bool isInt32() { return type_ == Type::INT32; }
		bool isInt64() { return type_ == Type::INT64; }
		bool isUint16() { return type_ == Type::UINT16; }
		bool isUint32() { return type_ == Type::UINT32; }
		bool isUint64() { return type_ == Type::UINT64; }
		bool isFloat() { return type_ == Type::FLOAT; }
		bool isDouble() { return type_ == Type::DOUBLE; }
		bool isString() { return type_ == Type::STRING; }
		bool isArray() { return type_ == Type::ARRAY; }
		bool isDict() { return type_ == Type::DICTIONARY; }

		bool asBool();
		byte asByte();
		int16 asInt16();
		int32 asInt32();
		int64 asInt64();
		uint16 asUint16();
		uint32 asUint32();
		uint64 asUint64();
		float asFloat();
		double asDouble();
		std::string& asString();
		template<typename Ty> std::vector<Ty> asArray();

		Value& operator[](size_t index);
		const Value& operator[](size_t index) const;
		Value& operator[](byte index);
		const Value& operator[](byte index) const;

		const int length() const
		{
			if (type_ == Type::ARRAY) 
				return (int)value_.array_->size();
			return 0;
		}

		explicit operator bool() { return asBool(); }
		explicit operator byte() { return asByte(); }
		explicit operator int16() { return asInt16(); }
		explicit operator int32() { return asInt32(); }
		explicit operator int64() { return asInt64(); }
		explicit operator uint16() { return asUint16(); }
		explicit operator uint32() { return asUint32(); }
		explicit operator uint64() { return asUint64(); }
		explicit operator float() { return asFloat(); }
		explicit operator double() { return asDouble(); }
		explicit operator std::string&() { return asString(); }
		template<typename Ty> explicit operator std::vector<Ty>() { return asArray<Ty>(); }
		Value& operator =(Value other);

		~Value();

		void swap(Value& other);

		bool hasEntry(byte key);

		std::vector<Value>& asArrayValue();

		std::map<byte, Value>& asDict();

		std::string toString();
	protected:
		bool dict();
		void assertType(const Type& type) const;
	};

	template<typename Ty>
	inline std::vector<Ty> Value::asArray()
	{
		std::vector<Ty> res;
		size_t length = value_.array_->size();
		for (size_t i = 0; i < length; i++)
		{
			res.push_back((Ty)(*this)[i]);
		}

		return res;
	}


}

#endif