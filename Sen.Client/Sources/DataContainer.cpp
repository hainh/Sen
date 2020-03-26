#include <sstream>
#include "DataContainer.h"

static std::string ServiceTypes[] =
{
	"EventData",
	"OperationData",
	"PingData",
	"EncryptData",
};

namespace Senla{

DataContainer::DataContainer()
	: _code(0)
	, _parameters()
{
}

DataContainer::DataContainer(byte code)
	: _code(code)
	, _parameters()
{
}

DataContainer::DataContainer(byte code, Value & parameters)
	: _code(code)
	, _parameters(parameters)
{
}

std::string DataContainer::toString()
{
	std::ostringstream os;

	os << ServiceTypes[getServiceType()] << " code " << (int)Code() << Parameters().toString();

	return os.str();
}

EventData::EventData()
{
}

EventData::EventData(byte code)
	: DataContainer(code)
{
}

EventData::EventData(byte code, Value & parameters)
	: DataContainer(code, parameters)
{
}

byte EventData::getServiceType()
{
	return byte(ServiceType::EventData);
}

OperationData::OperationData()
{
}

OperationData::OperationData(byte code)
	: DataContainer(code)
{
}

OperationData::OperationData(byte code, Value & parameters)
	: DataContainer(code, parameters)
{
}

byte OperationData::getServiceType()
{
	return byte(ServiceType::OperationData);
}

//EncryptedData::EncryptedData()
//	: DataContainer(code, para)
//{
//}
//
//byte EncryptedData::getServiceType()
//{
//	return byte(ServiceType::EncryptData);
//}

inline Ping::Ping()
{
}

Ping::Ping(byte code)
	: DataContainer(code)
{
}

Ping::Ping(byte code, Value & parameters)
	: DataContainer(code, parameters)
{
}

}