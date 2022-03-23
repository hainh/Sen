﻿
using Demo.Interfaces;
using Demo.Interfaces.Message;
using Sen;
using Sen.Client.Unity;
using Sen.Client.Unity.Abstract;

namespace Demo.Client.Unity;

public class Program
{
    public static void Main(string[] _)
    {
        new MessageHandler();
        //timer.Stop();
        //timer.Dispose();
    }
}

public class MessageHandler : IMessageHandler
{
    private SenClient<IDemoUnionData> client;

    public MessageHandler()
    {
        client = new SenClient<IDemoUnionData>("127.0.0.1", 6666, this);
        client.Connect(Protocol.Tcp, "demo", "?????");
        Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(1);
                client.Tick(1);
            }
        });
        Console.ReadKey();
        client.Disconnect();
    }

    public void HandleMessage(Hello data, NetworkOptions options)
    {
        Console.WriteLine("Hello from server: " + data.Message);
        client.Send(new JoinRoom() { RoomName = "World" }, options);
    }

    int count = 0;
    public void HandleMessage(JoinRoom joinRoom, NetworkOptions options)
    {
        Console.WriteLine("Server: " + joinRoom.RoomName);
        client.Send(new Hello() { Message = "Count " + (count++) }, options);
    }

    public void HandleMessage(IDemoUnionData message, NetworkOptions options)
    {
        Console.WriteLine("No message handler for " + message.GetType().FullName);
    }

    public void HandleMessage(IUnionData message, NetworkOptions networkOptions)
    {
        ((dynamic)this).HandleMessage((Hello)message, networkOptions);
    }

    public void OnStateChange(ConnectionState state)
    {
        Console.WriteLine(state.ToString());
        switch (state)
        {
            case ConnectionState.Connecting:
                break;
            case ConnectionState.Connected:
                break;
            case ConnectionState.Authorized:
                client.Send(new Hello() { Message = "Unity client day!" }, new Sen.NetworkOptions());
                break;
            case ConnectionState.Disconnected:
                break;
            default:
                break;
        }
    }
}