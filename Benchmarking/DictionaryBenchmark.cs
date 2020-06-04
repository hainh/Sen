using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Benchmarking
{
    public class DictionaryBenchmark
    {
        public DictionaryBenchmark()
        {
            ArrIn = new RuntimeTypeHandle[1000];
            ArrOut = new RuntimeTypeHandle[1000];
            int i = 0;
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                Type[] types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (i < 1000)
                    {
                        ArrIn[i] = type.TypeHandle;
                    }
                    else
                    {
                        if (i < 2000)
                        {
                            ArrOut[i - 1000] = type.TypeHandle;
                        }
                        else
                        {
                            return;
                        }
                    }
                    i++;
                }
            }
        }

        Dictionary<RuntimeTypeHandle, int> dictionary;

        RuntimeTypeHandle[] ArrIn;
        RuntimeTypeHandle[] ArrOut;
        int[] Index;

        [Params(1, 2, 3, 4, 5)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            dictionary = new Dictionary<RuntimeTypeHandle, int>(N);
            Index = new int[N];
            var random = new Random();
            for (int i = 0; i < N; i++)
            {
                dictionary.Add(ArrIn[i], i);
                Index[i] = i;
                if (i > 1)
                {
                    var r = random.Next(i - 1);
                    Index[i] = Index[r];
                    Index[r] = i;
                }
            }
        }

        [Benchmark]
        public int GetValueIn()
        {
            dictionary.TryGetValue(ArrIn[Index[0]], out int val);
            return val;
        }


        [Benchmark]
        public int GetValueOut()
        {
            dictionary.TryGetValue(ArrOut[Index[0]], out int val);
            return val;
        }
    }

    public class IfElseBenchmark
    {
        KeyValuePair<RuntimeTypeHandle, int> keyValue1 = new KeyValuePair<RuntimeTypeHandle, int>(typeof(object).TypeHandle, 1);
        KeyValuePair<RuntimeTypeHandle, int> keyValue2 = new KeyValuePair<RuntimeTypeHandle, int>(typeof(string).TypeHandle, 2);
        KeyValuePair<RuntimeTypeHandle, int> keyValue3 = new KeyValuePair<RuntimeTypeHandle, int>(typeof(Type).TypeHandle, 3);
        KeyValuePair<RuntimeTypeHandle, int> keyValue4 = new KeyValuePair<RuntimeTypeHandle, int>(typeof(Tuple).TypeHandle, 4);
        KeyValuePair<RuntimeTypeHandle, int> keyValue5 = new KeyValuePair<RuntimeTypeHandle, int>(typeof(Version).TypeHandle, 5);
        KeyValuePair<RuntimeTypeHandle, int> keyValue6 = new KeyValuePair<RuntimeTypeHandle, int>(typeof(Activator).TypeHandle, 6);
        KeyValuePair<RuntimeTypeHandle, int> keyValue7 = new KeyValuePair<RuntimeTypeHandle, int>(typeof(AppDomain).TypeHandle, 7);

        RuntimeTypeHandle valueToFind;
        KeyValuePair<RuntimeTypeHandle, int>[] Arr;

        public IfElseBenchmark()
        {
            Arr = new KeyValuePair<RuntimeTypeHandle, int>[]
            {
                keyValue1,
                keyValue2,
                keyValue3,
                keyValue4,
                keyValue5,
                keyValue6,
                keyValue7,
            };
            /*int i = 0;
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                Type[] types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (i < 1000)
                    {
                        ArrIn[i] = type.TypeHandle;
                    }
                    else
                    {
                        if (i < 2000)
                        {
                            ArrOut[i - 1000] = type.TypeHandle;
                        }
                        else
                        {
                            return;
                        }
                    }
                    i++;
                }
            }*/
        }

        [Params(1, 2, 3, 4, 5, 6, 7)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            switch (N)
            {
                case 1:
                    valueToFind = keyValue1.Key;
                    break;
                case 2:
                    valueToFind = keyValue2.Key;
                    break;
                case 3:
                    valueToFind = keyValue3.Key;
                    break;
                case 4:
                    valueToFind = keyValue4.Key;
                    break;
                case 5:
                    valueToFind = keyValue5.Key;
                    break;
                case 6:
                    valueToFind = keyValue6.Key;
                    break;
                case 7:
                    valueToFind = keyValue7.Key;
                    break;
                default:
                    break;
            }
        }

        [Benchmark]
        public int GetValueInArray()
        {
            for (int i = 0; i < Arr.Length; i++)
            {
                if (valueToFind.Equals(Arr[i].Key))
                {
                    return Arr[i].Value;
                }
            }
            return 0;
        }

        public int GetValueIfElse()
        {
            if (valueToFind.Equals(keyValue1.Key))
            {
                return keyValue1.Value;
            }
            if (valueToFind.Equals(keyValue2.Key))
            {
                return keyValue2.Value;
            }
            if (valueToFind.Equals(keyValue3.Key))
            {
                return keyValue3.Value;
            }
            if (valueToFind.Equals(keyValue4.Key))
            {
                return keyValue4.Value;
            }
            if (valueToFind.Equals(keyValue5.Key))
            {
                return keyValue5.Value;
            }
            if (valueToFind.Equals(keyValue6.Key))
            {
                return keyValue6.Value;
            }
            if (valueToFind.Equals(keyValue7.Key))
            {
                return keyValue7.Value;
            }
            return 0;
        }
    }
}
