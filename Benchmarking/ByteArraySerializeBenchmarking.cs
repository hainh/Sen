using BenchmarkDotNet.Attributes;
using DotNetty.Buffers;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Benchmarking
{
    [MemoryDiagnoser]
    class ByteArraySerializeBenchmarking
    {
        private void InitializeSerializer()
        {
            var client = new ClientBuilder()
                .UseLocalhostClustering()
                .Build();
            this.serializationManager = client.ServiceProvider.GetService(typeof(SerializationManager)) as SerializationManager;
        }

        private SerializationManager serializationManager;

        [GlobalSetup]
        public void BenchmarkSetup()
        {
            InitializeSerializer();
            Random random = new Random();
            IByteBuffer buffer = Unpooled.Buffer(16 * (1 << 20), 16 * (1 << 20));
            byte[] data = new byte[23];
            Array.Fill(data, (byte)5);
            buffer.WriteBytes(data);
        }

        [Benchmark]
        public void SerializeBenchmark()
        {

        }

        [Benchmark]
        public void DeserializeBenchmark()
        {

        }
    }
}
