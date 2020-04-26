using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Benchmarking
{

    public class ArrayPoolBenchmark
    {
        public ArrayPoolBenchmark()
        {
        }

        [Params(15, 50, 100, 150, 200, 300, 500, 1000)]
        public int N;

        public byte[] A;
        public byte[] B;

        [Benchmark]
        public void NewArray()
        {
            A = new byte[N];
        }

        [Benchmark]
        public void ArrayPool()
        {
            B = ArrayPool<byte>.Shared.Rent(N);
            ArrayPool<byte>.Shared.Return(B);
        }
    }
}
