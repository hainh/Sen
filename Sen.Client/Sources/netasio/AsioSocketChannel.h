#ifndef _SENLA_ASIO_SOCKET_CHANNEL_H_
#define _SENLA_ASIO_SOCKET_CHANNEL_H_

#include <regex>
#include <mutex>
#include <queue>

#include "asio.hpp"
#include "AbstractSocketChannel.h"
#include "ConnectionStatus.h"
#include "Connector.h"
#include "MessageBuffer.h"

namespace Senla {


class AsioSocketChannel : public AbstractSocketChannel
{
public:
	AsioSocketChannel()
	{
	}
	virtual ~AsioSocketChannel()
	{
	}

	virtual void writeAsync(MessageObj* message) = 0;

	virtual void connect(const std::string& endpoint, unsigned short port) = 0;

	virtual void disconnect() = 0;

	bool isDisconnected()
	{
		return _disconnected;
	}

	bool connectionAccepted()
	{
		return !_disconnected && _connectionAccepted;
	}
protected:
	void onAsioError(const asio::error_code & error)
	{
		//if (_disconnected)
		//{
		//	return;
		//}

		auto code = error.value();

		switch (code)
		{
		case asio::error::operation_aborted:
			break;
		case asio::error::access_denied:// basic errors
		case asio::error::address_family_not_supported:
		case asio::error::address_in_use:
		case asio::error::already_connected:
		case asio::error::already_started:
		case asio::error::broken_pipe:
		case asio::error::bad_descriptor:
		case asio::error::fault: // bad address
		case asio::error::host_unreachable:
		case asio::error::in_progress:
		case asio::error::interrupted:
		case asio::error::invalid_argument:
		case asio::error::message_size:
		case asio::error::name_too_long:
		case asio::error::no_buffer_space:
		case asio::error::no_descriptors:
		case asio::error::no_memory:
		case asio::error::no_permission:
		case asio::error::no_such_device:
		case asio::error::no_protocol_option:
		case asio::error::not_connected:
		case asio::error::not_socket:
		case asio::error::operation_not_supported:
		case asio::error::shut_down:
		case asio::error::try_again:
		case asio::error::would_block://end basic errors
		case asio::error::already_open://misc errors
		case asio::error::not_found:
		case asio::error::fd_set_failure:
			//pushDisconnectMessages(ConnectionStatus::INTERNAL_ERROR);
			printf("Internal error %d, '%s'\n", code, error.message().c_str());
			onError((int)ConnectionStatus::INTERNAL_ERROR, error.message().c_str());
			break;
		case asio::error::connection_aborted:
		case asio::error::connection_reset:
		case asio::error::eof:
		case asio::error::timed_out:
			//pushDisconnectMessages(ConnectionStatus::DISCONNECTED_CONNECTION_LOST);
			printf("Connection lost %d, '%s'\n", code, error.message().c_str());
			onError((int)ConnectionStatus::DISCONNECTED_CONNECTION_LOST, error.message().c_str());
			break;
		case asio::error::connection_refused:
			//pushDisconnectMessages(ConnectionStatus::DISCONNECTED_SERVER_ABORTED);
			printf("Connection refused by server, code %d, '%s'\n", code, error.message().c_str());
			onError((int)ConnectionStatus::DISCONNECTED_SERVER_ABORTED, error.message().c_str());
			break;
		case asio::error::network_down:
		case asio::error::network_reset:
		case asio::error::network_unreachable:
			//pushDisconnectMessages(ConnectionStatus::DISCONNECTED_NETWORK_FAILED);
			printf("Network failed %d, '%s'\n", code, error.message().c_str());
			onError((int)ConnectionStatus::DISCONNECTED_NETWORK_FAILED, error.message().c_str());
			break;
		case asio::error::host_not_found:
		case asio::error::host_not_found_try_again:
		case asio::error::no_data:
		case asio::error::no_recovery:
			//pushDisconnectMessages(ConnectionStatus::RESOLVER_FAILED);
			printf("DNS failed %d, '%s'\n", code, error.message().c_str());
			onError((int)ConnectionStatus::RESOLVER_FAILED, error.message().c_str());
			break;

		case asio::error::service_not_found:
		case asio::error::socket_type_not_supported:
			//pushDisconnectMessages(ConnectionStatus::SOCKET_ADDRESS_ERROR);
			printf("Socket address failed %d, '%s'\n", code, error.message().c_str());
			onError((int)ConnectionStatus::SOCKET_ADDRESS_ERROR, error.message().c_str());
			break;
		default:
			printf("Error not handled: %d, '%s'\n", code, error.message().c_str());
			onError((int)ConnectionStatus::DISCONNECTED_UNKOWN_REASON, error.message().c_str());
			break;
		}

		disconnect();
	}

	virtual void onError(int errorCode, const char* const message) = 0;

protected:
	bool _disconnected;
	bool _connectionAccepted;

private:

};

class TcpSocketChannel : public AsioSocketChannel
{
public:
	TcpSocketChannel()
	{
		_pipeline = new DefaultChannelPipeline(this);
	}

	virtual ~TcpSocketChannel()
	{
		if (_pipeline)
		{
			delete _pipeline;
			_pipeline = nullptr;
		}

		if (_socket)
		{
			delete _socket;
			_socket = nullptr;
		}

		if (s_countInstances == 0)
		{
			Connector::destroyInstance();
		}
	}

	virtual void writeAsync(MessageObj* message) override
	{
		((DefaultChannelPipeline*)_pipeline)->writeAsyncBegin(message);
	}

	virtual void connect(const std::string& ip, unsigned short port) override
	{
		asio::ip::tcp::resolver::iterator endpoint_iter;

		Connector::getInstance()->startService();
		_socket = new asio::ip::tcp::socket(Connector::getInstance()->getIoService());

		_disconnected = false;
		_connectionAccepted = false;

		// IP parse here
		if (std::regex_match(ip, std::regex("^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$")))
		{
			auto byteStr = split(ip, '.');
			char bytes[6];
			bytes[0] = atoi(byteStr[0].c_str());
			bytes[1] = atoi(byteStr[1].c_str());
			bytes[2] = atoi(byteStr[2].c_str());
			bytes[3] = atoi(byteStr[3].c_str());
			auto address_v4 = asio::ip::address_v4((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
			auto address = asio::ip::address(address_v4);
			auto address_endpoint = asio::ip::tcp::endpoint(address, port);

			snprintf(bytes, 6, "%d", port);
			endpoint_iter = asio::ip::tcp::resolver::iterator::create(address_endpoint, ip, bytes);

			// Do connect
			asio::async_connect(*_socket, endpoint_iter, std::bind(&TcpSocketChannel::onConnected, this, std::placeholders::_1, std::placeholders::_2));
		}
		// Resolve Host name to IP addresses
		else
		{
			asio::ip::tcp::resolver resolver(_socket->get_io_service());
			asio::ip::tcp::resolver::query query(ip, "");
			resolver.async_resolve(query, [port, this](const asio::error_code& error, asio::ip::tcp::resolver::iterator iterator) {
				if (!!error)
				{
					//this->_messageQueue.enqueue(new ConnectionStatusData(ConnectionStatus::DISCONNECTED_RESOLVER_FAILED));
					//this->_messageQueue.enqueue(new ConnectionStatusData(ConnectionStatus::DISCONNECTED));
					onAsioError(error);
				}
				else
				{
					// Do connect
					asio::async_connect(*_socket, iterator, std::bind(&TcpSocketChannel::onConnected, this, std::placeholders::_1, std::placeholders::_2));
				}
			});

		}
	}

	void onConnected(const asio::error_code & error, asio::ip::tcp::resolver::iterator it)
	{
		if (!!error)
		{
			onAsioError(error);
		}
		else
		{
			_connectionAccepted = true;
			((DefaultChannelPipeline*)_pipeline)->fireChannelActiveEvent();
			//this->_messageQueue.enqueue(new ConnectionStatusData(ConnectionStatus::CONNECTED));
			_socket->async_receive(asio::buffer(_buffer), std::bind(&TcpSocketChannel::onReceived, this, std::placeholders::_1, std::placeholders::_2));
		}
	}

	void onReceived(const asio::error_code & error, std::size_t bytes_transferred)
	{
		if (!!error)
		{
			onAsioError(error);
		}
		else
		{
			// Hooray, we've just receive from server, let deserialize it
			//_receivedBuffer.enqueue(_buffer, (int)bytes_transferred);
			auto message = new MessageBuffer(_buffer, (uint32_t)bytes_transferred);

			((DefaultChannelPipeline*)_pipeline)->fireChannelReadEvent(message);

			delete message;

			if (!_disconnected)
			{
				// Continue to receive more data
				_socket->async_receive(asio::buffer(_buffer), std::bind(&TcpSocketChannel::onReceived, this, std::placeholders::_1, std::placeholders::_2));
			}
		}
	}

	virtual void disconnect() override
	{
		if (_disconnected || !_connectionAccepted)
		{
			return;
		}
		_disconnected = true;
		_connectionAccepted = false;
		if (_socket->is_open())
		{
			try
			{
				std::lock_guard<std::mutex> lock(_sendingDataQueueMutex);
				while (!_sendingDataQueue.empty())
						_sendingDataQueue.pop();
				//_socket->cancel();
				_socket->shutdown(asio::socket_base::shutdown_type::shutdown_both);
				_socket->close();
			}
			catch (const std::exception& e)
			{
				printf("disconnect error %s\n", e.what());
			}
		}

		((DefaultChannelPipeline*)_pipeline)->fireChannelInactiveEvent();
	}

	virtual void onError(int errorCode, const char* const message) override
	{
		((DefaultChannelPipeline*)_pipeline)->fireChannelErrorEvent(errorCode, message);
		disconnect();
	}

protected:

	void writeMessageOnWire(MessageObj* message)
	{
		auto data = dynamic_cast<MessageBuffer*>(message);
		Assert(data, "Message must be a MessageBuffer");

		do
		{
			std::lock_guard<std::mutex> lock(_sendingDataQueueMutex);
			if (_sendingDataQueue.size() <= 1)
			{
				_sendingDataQueue.push(std::vector<unsigned char>(data->data(), data->data() + data->size()));
			}
			else
			{
				auto& lastChunk = _sendingDataQueue.back();
				lastChunk.insert(lastChunk.end(), data->data(), data->data() + data->size());
			}
		} while (false);

		write_async();
	}

private:

	void write_async()
	{
		std::lock_guard<std::mutex> lock(_sendingDataQueueMutex);
		if (_sendingDataQueue.empty() || _disconnected)
		{
			while (_disconnected && !_sendingDataQueue.empty())
				_sendingDataQueue.pop();
			return;
		}

		if (_sendingData != nullptr)
		{
			delete[] _sendingData;
			_sendingData = nullptr;
		}
		auto& toSendArray = _sendingDataQueue.front();
		_sendingData = new unsigned char[toSendArray.size()];
		memcpy(_sendingData, toSendArray.data(), toSendArray.size());

		asio::async_write(*_socket, asio::buffer(_sendingData, toSendArray.size()), [this](const asio::error_code & error, std::size_t bytes_transferred) {
			if (!!error)
			{
				onAsioError(error);
				printf("\nError send operation: %s\n", error.message().c_str());
			}
			else
			{
				do
				{
					std::lock_guard<std::mutex> lock(_sendingDataQueueMutex);
					if (!_sendingDataQueue.empty())
					{
						_sendingDataQueue.pop();
					}
				} while (false);

				write_async();
			}
		});
	}

	static std::vector<std::string> split(const std::string &s, char delim)
	{
		std::vector<std::string> elems;
		std::stringstream ss(s);
		std::string item;
		while (std::getline(ss, item, delim))
		{
			elems.push_back(item);
		}
		return elems;
	}

private:

	/*
	buffer to accept data from socket stream
	*/
	uint8_t _buffer[512];

	std::queue<std::vector<unsigned char>> _sendingDataQueue;
	unsigned char* _sendingData;
	std::mutex _sendingDataQueueMutex;

	asio::ip::tcp::socket* _socket;

};

}

#endif //_SENLA_ASIO_SOCKET_CHANNEL_H_