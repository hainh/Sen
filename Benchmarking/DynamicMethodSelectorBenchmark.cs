using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Benchmarking
{
    delegate IMyInterface MDelegate(C18 c);

    public class MultiOverload
    {
        dynamic c18 = new C18();
        dynamic c19 = new C19();
        dynamic obj = 1;
        MDelegate MC18;

        [GlobalSetup]
        public void Setup()
        {
            MC18 = M;
        }


        [Benchmark]
        public void DynamicCall()
        {
            ((dynamic)this).M(c19);
        }

        [Benchmark]
        public void DynamicCallWithFallback()
        {
            ((dynamic)this).M(obj);
        }

        [Benchmark]
        public void DirectDelegateCall()
        {
            MC18(c18);
        }

        protected IMyInterface M(C18 c) { return c; }
    }

    public class DynamicMethodSelectorBenchmark : MultiOverload
    {
        public IMyInterface M(object c)  { return null; }
        public IMyInterface M(C0 c)  { return c; }
        public IMyInterface M(C1 c)  { return c; }
        public IMyInterface M(C2 c)  { return c; }
        public IMyInterface M(C3 c)  { return c; }
        public IMyInterface M(C4 c)  { return c; }
        public IMyInterface M(C5 c)  { return c; }
        public IMyInterface M(C6 c)  { return c; }
        public IMyInterface M(C7 c)  { return c; }
        public IMyInterface M(C8 c)  { return c; }
        public IMyInterface M(C9 c)  { return c; }
        public IMyInterface M(C10 c) { return c; }
        public IMyInterface M(C11 c) { return c; }
        public IMyInterface M(C12 c) { return c; }
        public IMyInterface M(C13 c) { return c; }
        public IMyInterface M(C14 c) { return c; }
        public IMyInterface M(C15 c) { return c; }
        public IMyInterface M(C16 c) { return c; }
        public IMyInterface M(C17 c) { return c; }
        public IMyInterface M(C19 c) { return c; }
    }

    public interface IMyInterface { }
    public class C0 : IMyInterface { }
    public class C1 : IMyInterface { }
    public class C2 : IMyInterface { }
    public class C3 : IMyInterface { }
    public class C4 : IMyInterface { }
    public class C5 : IMyInterface { }
    public class C6 : IMyInterface { }
    public class C7 : IMyInterface { }
    public class C8 : IMyInterface { }
    public class C9 : IMyInterface { }
    public class C10 : IMyInterface { }
    public class C11 : IMyInterface { }
    public class C12 : IMyInterface { }
    public class C13 : IMyInterface { }
    public class C14 : IMyInterface { }
    public class C15 : IMyInterface { }
    public class C16 : IMyInterface { }
    public class C17 : IMyInterface { }
    public class C18 : IMyInterface { }
    public class C19 : IMyInterface { }
}
