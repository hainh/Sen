
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
        var client = new UnityClient("127.0.0.1", 6666);
        client.Connect(Protocol.Tcp);

        //int clientFrequency = 1000;
        //var timer = new System.Timers.Timer(1000.0 / clientFrequency);
        //timer.Elapsed += (args1, args2) =>
        //{
        //    client.Tick(1);
        //};

        //timer.AutoReset = true;
        //timer.Enabled = true;

        Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(1);
                client.Tick(1);
            }
        });

        Task.Run(async () =>
        {
            await Task.Delay(1000);
            client.Send(new Hello() { Message = "Unity client day!" }, new Sen.NetworkOptions());
        });

        Console.ReadKey();
        client.Disconnect();
        //timer.Stop();
        //timer.Dispose();
    }
}

public class UnityClient : SenClient<IDemoUnionData>
{
    public UnityClient(string ipAddress, int port) : base(ipAddress, port)
    {
    }

    public override void OnStateChange(ConnectionState state)
    {
        Console.WriteLine("State changed: " + state);
        switch (state)
        {
            case ConnectionState.Connecting:
                break;
            case ConnectionState.Connected:
                Send(new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes("demo/token1")), new Sen.NetworkOptions());
                break;
            case ConnectionState.Disconnected:
                break;
            default:
                break;
        }
    }
    public void HandleMessage(Hello data, NetworkOptions options)
    {

    }
}