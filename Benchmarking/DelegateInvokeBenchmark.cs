using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Benchmarking
{
    delegate void Call();

    public class DelegateInvokeBenchmark
    {
        public DelegateInvokeBenchmark()
        {
            Call = call;
        }

        Call Call;

        public long N;
        void call()
        {
            N++;
        }

        [Benchmark]
        public void CallDirect()
        {
            call();
        }

        [Benchmark]
        public void CallDelegate()
        {
            Call();
        }
    }
}
