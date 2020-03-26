
#ifndef _SENLA_CLIENT_H_
#define _SENLA_CLIENT_H_

#include <thread>
#include "asio.hpp"

namespace Senla {

class Connector
{
public:
	void startService()
	{
		// Service is running
		if (_service_thread.joinable())
		{
			return;
		}

		_service_thread = std::move(std::thread([this]() {
			_io_service.run();
		}));
	}

	asio::io_service& getIoService()
	{
		return _io_service;
	}

	static Connector* getInstance()
	{
		if (s_instance == nullptr)
		{
			s_instance = new Connector();
		}
		return s_instance;
	}

	static void destroyInstance()
	{
		if (s_instance != nullptr)
		{
			delete s_instance;
			s_instance = nullptr;
		}
	}

	Connector()
		: _io_service()
		, _work(new asio::io_service::work(_io_service))
	{
	}
	~Connector()
	{
		if (_service_thread.joinable())
		{
			_io_service.stop();
			_work.reset();
			_service_thread.join();
		}
	}

	friend class BasePeer_Old;

private:
	asio::io_service _io_service;
	std::auto_ptr<asio::io_service::work> _work;
	std::thread _service_thread;

	static Connector* s_instance;

	Connector(const Connector& c)
	{
	}
	void operator=(const Connector& c)
	{
	}
};
}

#endif
