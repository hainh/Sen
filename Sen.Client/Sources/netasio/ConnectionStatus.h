#ifndef _SENLA_CONNECTION_STATUS_H_
#define _SENLA_CONNECTION_STATUS_H_

namespace Senla {

enum class ConnectionStatus
{
	CONNECTED,

	// After all, this status indicates connection was disconnected
	DISCONNECTED,

	// Client disconnects
	DISCONNECTED_BY_CLIENT,

	// No network
	DISCONNECTED_NETWORK_FAILED,

	// Timeout or connection disrupted
	DISCONNECTED_CONNECTION_LOST,

	// Server refused to connect
	DISCONNECTED_SERVER_ABORTED,

	// Internal socket error
	INTERNAL_ERROR,

	// Socket address error
	SOCKET_ADDRESS_ERROR,

	// Couldn't resolve address supplied
	RESOLVER_FAILED,

	DISCONNECTED_UNKOWN_REASON = 2048,
};

}

#endif //_SENLA_CONNECTION_STATUS_H_