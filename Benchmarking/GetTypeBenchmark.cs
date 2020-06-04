using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Benchmarking
{
    public class GetTypeBenchmark
    {
        public GetTypeBenchmark()
        {

        }

        public Type[] array;
        public RuntimeTypeHandle[] brray;

        private A[] aa;
        [GlobalSetup]
        public void BenchmarkSetup()
        {
            aa = new A[4];
            aa[0] = new A();
            aa[1] = new B();
            aa[2] = new C();
            aa[3] = new D();

            array = new Type[4];
            brray = new RuntimeTypeHandle[4];
        }

        [Benchmark]
        public void SystemType()
        {
            array[0] = aa[0].GetType();
            array[1] = aa[1].GetType();
            array[2] = aa[2].GetType();
            array[3] = aa[3].GetType();
        }

        [Benchmark]
        public void SystemRuntimeTypeHandle()
        {
            brray[0] = Type.GetTypeHandle(aa[0]);
            brray[1] = Type.GetTypeHandle(aa[1]);
            brray[2] = Type.GetTypeHandle(aa[2]);
            brray[3] = Type.GetTypeHandle(aa[3]);
        }
    }

    class A { }

    class B : A { }

    class C : B { }

    class D : A { }
}
