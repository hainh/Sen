// Senla1.cpp : Defines the entry point for the console application.
//

#include <thread>
#include <regex>
#include "BasePeer.h"

using namespace Senla;

int disconnected = 0;

class MyPeer : public BasePeer
{
public:
	MyPeer();
	virtual ~MyPeer();

	// Inherited via BasePeer_Old
	virtual void onStatusChanged(ConnectionStatus status) override
	{
		//printf("status changed to %d, peer [%d]\n", (int)status, number);
		Value parameters;
		//parameters[(byte)14] = (byte)3;
		//parameters[(byte)15] = 3i16;
		//parameters[(byte)16] = 30ui16;
		//parameters[(byte)17] = 31i32;
		//parameters[(byte)18] = 32ui32;
		parameters[(byte)0] = 35i64;
		//parameters[(byte)20] = 36ui64;
		//parameters[(byte)21] = 312.0;
		//parameters[(byte)22] = 3123.0f;
		//parameters[(byte)140] = false;
		//parameters[(byte)141] = std::string("xin chao server");
		//parameters[(byte)20] = std::vector<Value>(2, std::string("xin chao server, day la array"));
		//parameters[(byte)201] = std::vector<Value>(2, 46ui8);
		//parameters[(byte)202] = std::vector<Value>(2, 721i16);
		//parameters[(byte)203] = std::vector<Value>(2, 722ui16);
		//parameters[(byte)204] = std::vector<Value>(2, 723i32);
		//parameters[(byte)205] = std::vector<Value>(2, 724ui32);
		//parameters[(byte)206] = std::vector<Value>(2, 725i64);
		//parameters[(byte)207] = std::vector<Value>(2, 726ui64);
		//parameters[(byte)208] = std::vector<Value>(2, 727.0f);
		//parameters[(byte)209] = std::vector<Value>(2, 728.0);
		//parameters[(byte)210] = std::vector<Value>(9, true);
		OperationData data((byte)1, parameters);

		switch (status)
		{
		case Senla::ConnectionStatus::CONNECTED:
			this->sendOperationRequest(data);
			break;
		case Senla::ConnectionStatus::DISCONNECTED:
			printf("Disconnected\n");
			break;
		default:
			printf("On Disconnected code %d\n", (int)status);
			disconnected++;
			break;
		}
	}

	virtual void onOperationResponse(OperationData & opData, SendParameters sendParameters) override
	{
		//sendOperationRequest(opData);
	}

	virtual void onEvent(EventData & eventData, SendParameters sendParameters) override
	{
	}

	int number;
};

int _num = 0;

MyPeer::MyPeer()
	: BasePeer(Protocol::TCP)
{
	number = _num++;
}

MyPeer::~MyPeer()
{
}
void test();
void stretchTest16k();
//void test1();
void test2();

int main()
{
	stretchTest16k();
	system("pause");

	return 0;
}

void stretchTest16k()
{
	printf("Start\n");
	const int numPeers = 1;
	std::vector<MyPeer*> peers;
	for (int i = 0; i < numPeers; ++i)
	{
		try
		{
			auto peer = new MyPeer();
			peer->connect("127.0.0.1", 7714, "");
			peers.push_back(peer);
		}
		catch (std::exception e)
		{
			printf("\n%s", e.what());
		}
	}
	bool stop = false;
	long timeline = 0;
	while (!stop)
	{
		std::this_thread::sleep_for(std::chrono::milliseconds(30));

		for (int i = 0; i < peers.size(); ++i)
		{
			auto peer = peers[i];
			auto connected = !peer->isDisconnected();
			//if (connected)
			{
				peer->update(.03f);
			}
			//stop = stop || connected;
		}
		//stop = !stop;
		if (stop)
		{
			/*for (int i = 0; i < peers.size(); ++i)
			{
				delete peers[i];
			}
			peers.clear();*/
			//continue;
		}

		timeline += 30;

		if (timeline % 3000 == 0)
		{
			/*for (int i = 0; i < peers.size(); ++i)
			{
				if (!peers[i]->isDisconnected())
					peers[i]->disconnect();
			}*/
			printf("3s passed\n");
		}

		if (disconnected > 0)
		{
			printf("disconnected %d peers\n", disconnected);
			disconnected = 0;
		}
	}

	printf("End\n");
}

void test()
{
	printf("Start\n");
	//test2();
	const int numPeers = 3000;
	std::vector<MyPeer*> peers;
	for (int i = 0; i < numPeers; ++i)
	{
		try
		{
			auto peer = new MyPeer();
			peer->connect("127.0.0.1", 7714, "");
			peers.push_back(peer);
		}
		catch (std::exception e)
		{
			printf("\n%s", e.what());
		}
	}
	bool stop = false;
	long timeline = 0;
	while (!stop)
	{
		for (int i = 0; i < peers.size(); ++i)
		{
			auto peer = peers[i];
			auto connected = !peer->isDisconnected();
			if (connected)
			{
				peer->update(.03f);
			}
			stop = stop || connected;
		}
		stop = !stop;
		if (stop)
			printf("Stopped\n");
		std::this_thread::sleep_for(std::chrono::milliseconds(30));

		timeline += 30;

		if (timeline % 3000 == 0)
		{
			auto r = 1000;
			for (int i = 0; i < r && !peers.empty(); i++)
			{
				peers.back()->disconnect();
				peers.pop_back();
			}
			printf("Peers remain after pop %d", (int)peers.size());
		}

		if ((timeline + 1500) % 3000 == 0)
		{
			auto r = 1000;
			for (size_t i = 0; i < r; i++)
			{
				auto peer = new MyPeer();
				peer->connect("127.0.0.1", 7714, "");
				peers.push_back(peer);
			}

			for (int i = 0;i < peers.size(); i++)
			{
				if (peers[i]->isDisconnected())
				{
					delete peers[i];
					peers.erase(peers.begin() + i);
					--i;
					continue;
				}

				if (!peers[i]->connectionAccepted())
					continue;

				Value params;
				params[(byte)0] = std::vector<Value>(100, 2);
				peers[i]->sendOperationRequest(Senla::OperationData(1, params));
			}
			printf("Peers remain after push %d\n", peers.size());
		}

		if (disconnected > 0)
		{
			printf("disconnected %d peers\n", disconnected);
			disconnected = 0;
		}
	}

	printf("End\n");

	system("pause");

}

void test1()
{

	gamesen::CircularBuffer buffer;
	Deserializer des;
	Serializer ser;

	Value parameters;
	parameters[(byte)14] = (byte)3;
	parameters[(byte)15] = 3i16;
	parameters[(byte)16] = 3090ui16;
	parameters[(byte)17] = 10293831i32;
	parameters[(byte)18] = 252ui32;
	parameters[(byte)19] = 65530i64;
	parameters[(byte)20] = 3986ui64;
	parameters[(byte)21] = 312.0;
	parameters[(byte)22] = 3123.0f;
	parameters[(byte)140] = false;
	parameters[(byte)141] = std::string("xin chao server");
	parameters[(byte)200] = std::vector<Value>(2, std::string("xin chao server"));
	parameters[(byte)201] = std::vector<Value>(2, 46ui8);
	parameters[(byte)202] = std::vector<Value>(2, 721i16);
	parameters[(byte)203] = std::vector<Value>(2, 3722ui16);
	parameters[(byte)204] = std::vector<Value>(2, 4029723i32);
	parameters[(byte)205] = std::vector<Value>(2, 72934ui32);
	parameters[(byte)206] = std::vector<Value>(2, -7223445i64);
	parameters[(byte)207] = std::vector<Value>(2, 728982346ui64);
	parameters[(byte)208] = std::vector<Value>(2, 727.0f);
	parameters[(byte)209] = std::vector<Value>(2, 7284378.0);
	parameters[(byte)210] = std::vector<Value>(9, true);
	OperationData data((byte)4, parameters);

	auto& result = ser.Serialize(&data);
	std::ostringstream oss;
	for (size_t i = 0; i < result.size(); i++)
	{
		oss << (int)result[i] << ", ";
	}
	printf("%s", oss.str().c_str());

	buffer.enqueue(result.data(), result.size());
	auto adata = des.DeserializeData(buffer);
	auto outputString = adata->toString();
	printf("\n%s", outputString.c_str());

	byte raw[] = { 217, 11, 4, 14, 97, 15, 194, 1, 16, 197, 132, 6, 17, 195, 163, 146, 186, 2, 18, 134, 63, 19, 4, 244, 255, 7, 20, 7, 146, 31, 21, 9, 0, 0, 0, 0, 0, 128, 115, 64, 22, 8, 0, 48, 67, 69, 140, 0, 141, 234, 3, 120, 105, 110, 32, 99, 104, 97, 111, 32, 115, 101, 114, 118, 101, 114, 200, 90, 15, 120, 105, 110, 32, 99, 104, 97, 111, 32, 115, 101, 114, 118, 101, 114, 15, 120, 105, 110, 32, 99, 104, 97, 111, 32, 115, 101, 114, 118, 101, 114, 201, 81, 46, 46, 202, 82, 162, 11, 162, 11, 203, 85, 138, 29, 138, 29, 204, 83, 182, 244, 235, 3, 182, 244, 235, 3, 205, 86, 230, 185, 4, 230, 185, 4, 206, 84, 171, 226, 241, 6, 171, 226, 241, 6, 207, 87, 202, 198, 205, 219, 2, 202, 198, 205, 219, 2, 208, 88, 0, 192, 53, 68, 0, 192, 53, 68, 209, 89, 0, 0, 0, 128, 166, 201, 91, 65, 0, 0, 0, 128, 166, 201, 91, 65, 210, 176, 2, 255, 1 };
	buffer.clear();
	buffer.enqueue(raw, sizeof(raw) / sizeof(byte));

	auto adata1 = des.DeserializeData(buffer);
	auto outputString1 = adata1->toString();
	printf("\n%s", outputString1.c_str());
}

void test2()
{
	gamesen::CircularBuffer buffer;
	Deserializer des;
	Serializer ser;

	Value parameters;
	parameters[(byte)0] = 9287372ui64;
	parameters[(byte)1] = 2837498ui64;
	Ping data((byte)4, parameters);

	auto& result = ser.Serialize(&data);
	std::ostringstream oss;
	for (size_t i = 0; i < result.size(); i++)
	{
		oss << (int)result[i] << ", ";
	}
	printf("%s", oss.str().c_str());

	buffer.enqueue(result.data(), result.size());
	auto adata = des.DeserializeData(buffer);
	auto outputString = adata->toString();
	printf("\n%s", outputString.c_str());
	 
	byte raw[] = { 106, 4, 0, 7, 204, 237, 182, 4, 1, 7, 250, 151, 173, 1 };
	buffer.clear();
	buffer.enqueue(raw, sizeof(raw) / sizeof(byte));

	auto adata1 = des.DeserializeData(buffer);
	auto outputString1 = adata1->toString();
	printf("\n%s", outputString1.c_str());
}