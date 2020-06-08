using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Benchmarking
{
    public class LessKeyReadOnlyDictionaryBenchmark
    {
        /*public LessKeyReadOnlyDictionary<RuntimeTypeHandle, int> LK15;
        public LessKeyReadOnlyDictionary<RuntimeTypeHandle, int> LK35;
        public Dictionary<RuntimeTypeHandle, int> Dict;

        RuntimeTypeHandle param15;

        [Params(3, 6, 9, 14)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            Dict = new Dictionary<RuntimeTypeHandle, int>(35);
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            getTypes();
            LK15 = new LessKeyReadOnlyDictionary<RuntimeTypeHandle, int>(new Dictionary<RuntimeTypeHandle, int>(Dict.Take(15)));
            LK35 = new LessKeyReadOnlyDictionary<RuntimeTypeHandle, int>(Dict);
            param15 = Dict.ElementAt(N).Key;
            void getTypes()
            {
                int i = 0;
                foreach (Assembly assembly in assemblies)
                {
                    Type[] types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (i < 35)
                        {
                            Dict.Add(type.TypeHandle, i);
                        }
                        else
                        {
                            return;
                        }
                        i++;
                    }
                }
            }
        }

        [Benchmark]
        public int GetLK15()
        {
            LK15.TryGetValue(param15, out int v);
            return v;
        }

        public int GetLk35()
        {
            LK35.TryGetValue(param15, out int v);
            return v;
        }

        public int GetDict()
        {
            Dict.TryGetValue(param15, out int v);
            return v;
        }*/
    }
}
