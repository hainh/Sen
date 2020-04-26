using BenchmarkDotNet.Running;
using System;

namespace Benchmarking
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start Benchmarking!");
            var summary = BenchmarkRunner.Run<ArrayPoolBenchmark>();
            Console.WriteLine(summary);
        }
    }
}
