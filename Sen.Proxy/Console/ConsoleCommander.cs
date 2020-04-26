using DotNetty.Buffers;
using DotNetty.Codecs.Http.WebSockets;
using DotNetty.Transport.Channels;
using Orleans.Concurrency;
using Sen.OrleansInterfaces;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sen.Proxy.Benchmark
{
    class ConsoleCommander
    {
        public static void ForwardDataToServer(IProxyConnection proxyConnection, WebSocketFrame frame, Action<Task> onComplete)
        {
            var buffer = new byte[frame.Content.ReadableBytes];
            frame.Content.MarkReaderIndex();
            frame.Content.ReadBytes(buffer);
            frame.Content.ResetReaderIndex();
            proxyConnection.Read(buffer.AsImmutable()).ContinueWith(onComplete);
        }

        public static void ForwardDataToServerAlwaysArrayPool(IProxyConnection proxyConnection, WebSocketFrame frame, Action<Task> onComplete)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(frame.Content.ReadableBytes);
            frame.Content.MarkReaderIndex();
            frame.Content.ReadBytes(buffer, 0, frame.Content.ReadableBytes);
            frame.Content.ResetReaderIndex();
            proxyConnection.Read(buffer.AsImmutable()).ContinueWith(onComplete);
            ArrayPool<byte>.Shared.Return(buffer);
        }

        public static void ForwardDataToServerArrayPoolImmutable(IProxyConnection proxyConnection, WebSocketFrame frame, Action<Task> onComplete)
        {
            //bool usePool = frame.Content.ReadableBytes > 400;
            //var buffer = usePool ? ArrayPool<byte>.Shared.Rent(frame.Content.ReadableBytes) : new byte[frame.Content.ReadableBytes];
            //frame.Content.MarkReaderIndex();
            //frame.Content.ReadBytes(buffer, 0, frame.Content.ReadableBytes);
            //frame.Content.ResetReaderIndex();
            //proxyConnection.Read(new Immutable<byte[]>(buffer)).ContinueWith(onComplete);
            //if (usePool)
            //{
            //    ArrayPool<byte>.Shared.Return(buffer);
            //}
        }

        public static void ForwardDataToServerSegmentArraySerializable(IProxyConnection proxyConnection, WebSocketFrame frame, Action<Task> onComplete)
        {
            //var buffer = frame.Content.GetIoBuffer();
            //proxyConnection.Read(new ByteArraySegment(buffer)).ContinueWith(onComplete);
        }

        delegate void SendWithDataType(IProxyConnection proxyConnection, WebSocketFrame frame, Action<Task> onComplete);

        static readonly SendWithDataType[] ForwardDataFunc = new SendWithDataType[]
        {
            ForwardDataToServer,
            ForwardDataToServerAlwaysArrayPool,
            ForwardDataToServerArrayPoolImmutable,
            ForwardDataToServerSegmentArraySerializable
        };

        static readonly Dictionary<SendWithDataType, string> OptimizeLevelDesciption = new Dictionary<SendWithDataType, string>()
        {
            { ForwardDataToServer,                        "Optimization Lv1: Sending byte array" },
            { ForwardDataToServerAlwaysArrayPool,         "Optimization Lv2: Sending byte array create by array pool" },
            { ForwardDataToServerArrayPoolImmutable,      "Optimization Lv3: Sending byte array create by array pool if length < 400 and use Immutable" },
            { ForwardDataToServerSegmentArraySerializable,"Optimization Lv4: Sending byte array not by copying but using array segment and customize serializer" },
        };

        public static async Task ForwardMessage(IProxyConnection proxyConnection, WebSocketFrame frame, int round, int packages)
        {
            if (round < 1)
            {
                return;
            }
            var funcIndex = frame.Content.ReadableBytes % 4;
            SendWithDataType func = ForwardDataFunc[funcIndex];

            TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>();
            int countPkg = 0;

            for (int i = 0; i < packages; i++)
            {
                func(proxyConnection, frame, OnCompleteSend);
            }

            await taskCompletionSource.Task;

            await ForwardMessage(proxyConnection, frame, round - 1, packages);

            void OnCompleteSend(Task task)
            {
                Interlocked.Increment(ref countPkg);
                if (packages == countPkg)
                {
                    taskCompletionSource.SetResult(1);
                }
            }
        }

        public static async Task<bool> Benchmark(string[] args, IProxyConnection proxyConnection, int roundA)
        {
            if (roundA <= 0)
            {
                return true;
            }
            if (args[0] == "bye")
            {
                return false;
            }

            if (args.Length != 3 || args.Any(s => !int.TryParse(s, out int _)))
            {
                return true;
            }

            int frameLength = int.Parse(args[0]);
            int package = int.Parse(args[1]);
            int round = int.Parse(args[2]);
            byte[] realData = new byte[frameLength];
            new Random().NextBytes(realData);
            IByteBuffer buffer = Unpooled.Buffer(16 * (1 << 20));
            buffer.WriteBytes(realData);
            var frame = new BinaryWebSocketFrame(buffer);

            Console.WriteLine("+ Start with " + OptimizeLevelDesciption[ForwardDataFunc[frameLength % 4]]);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            await ForwardMessage(proxyConnection, frame, round, package);
            stopwatch.Stop();
            Console.WriteLine(
                string.Format("  Complete recieving {0:000,000,000} pkgs of {1} bytes in {2:000,000} ms at {3:00,000.} msg/s with transfer rate {4:0##.##} MB/s"
                , package * round
                , frameLength
                , stopwatch.ElapsedMilliseconds
                , package * round * Stopwatch.Frequency / (double)stopwatch.ElapsedTicks
                , package * round * Stopwatch.Frequency / (double)stopwatch.ElapsedTicks * frameLength / (1 << 20)));

            frameLength++;
            args[0] = frameLength.ToString();
            return await Benchmark(args, proxyConnection, roundA - 1);
        }

        public static async Task Start(IProxyConnection proxyConnection)
        {
            string args;
            do
            {
                args = Console.ReadLine();
            } while (await Benchmark(args.Split(' ', StringSplitOptions.RemoveEmptyEntries), proxyConnection, 4));
        }
    }
}
