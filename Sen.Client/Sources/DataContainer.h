#ifndef _DATA_CONTAINER_H_
#define _DATA_CONTAINER_H_
#include "Value.h"

namespace Senla
{
	enum class ServiceType
	{
		EventData     = 0,
		OperationData = 1,
		PingData      = 2,
		EncryptData   = 3,
		ConfigData    = 4,

		NON_TRANSFER_TYPES_START = 8,
		ConnectionStatus,
	};

	class DataContainer
	{
	public:
		DataContainer();
		DataContainer(byte code);
		DataContainer(byte code, Value& parameters);
		virtual ~DataContainer() { }

		virtual byte getServiceType() = 0;

		byte Code()
		{
			return _code;
		}

		Value& Parameters()
		{
			return _parameters;
		}

		void Code(byte code)
		{
			_code = code;
		}

		void Parameters(Value& parameters)
		{
			_parameters = parameters;
		}

		std::string toString();

	protected:
		byte _code;
		Value _parameters;
	};

	class EventData : public DataContainer
	{
	public:
		EventData();
		EventData(byte code);
		EventData(byte code, Value& parameters);
		virtual ~EventData() { }

		// Inherited via DataContainer
		virtual byte getServiceType() override;
	};

	class OperationData : public DataContainer
	{
	public:
		OperationData();
		OperationData(byte code);
		OperationData(byte code, Value& parameters);
		virtual ~OperationData() { }

		// Inherited via DataContainer
		virtual byte getServiceType() override;
	};

	class Ping : public DataContainer
	{
	public:
		Ping();
		Ping(byte code);
		Ping(byte code, Value& parameters);
		virtual ~Ping() { }

		// Inherited via DataContainer
		virtual byte getServiceType() override
		{
			return byte(ServiceType::PingData);
		}

	};

	//class EncryptedData : public DataContainer
	//{
	//public:
	//	EncryptedData();
	//	virtual ~EncryptedData() { }

	//	// Inherited via DataContainer
	//	virtual byte getServiceType() override;

	//	void Code(byte code) { _code = code; }

	//	void Parameters(Value& parameters) { _parameters = parameters; }
	//};

	class ConfigData : public DataContainer
	{
	public:
		ConfigData()
		{
		}

		ConfigData(byte code, Value& parameters)
			: DataContainer(code, parameters)
		{ }

		~ConfigData()
		{
		}

		// Inherited via DataContainer
		virtual byte getServiceType() override
		{
			return byte(ServiceType::ConfigData);
		}

	};

	}
#endif