using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Benchmarking
{
    public class TryCatchImpactBenchmark
    {
        public long TC;
        public long NoTC;

        [Benchmark]
        public void UseTryCatch()
        {
            try
            {
                TC++;
            }
            catch (Exception)
            {
                throw;
            }
        }

        [Benchmark]
        public void NoTryCatch()
        {
            NoTC++;
        }
    }
}
