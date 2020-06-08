using System;
using System.Collections.Generic;
using System.Text;

namespace Trial
{
    public class DynamicBase
    {
        public int Method(Abc abc) => 2;

        public DynamicBase()
        {
        }

        public void Run()
        {
            IAbc abc = new Abc();
            Console.WriteLine(((dynamic)this).Method((dynamic)abc));
            IAbc aha = new Aha();
            Console.WriteLine(((dynamic)this).Method((dynamic)aha));
        }
    }

    public class DynamicSub : DynamicBase
    {
        public int Method(Aha a) => 19;

        public void Run1()
        {
            IAbc abc = new Abc();
            Console.WriteLine(((dynamic)this).Method((dynamic)abc));
            IAbc aha = new Aha();
            Console.WriteLine(((dynamic)this).Method((dynamic)aha));
        }
    }

    public class Abc : IAbc { }

    public class Aha : IAbc { }

    public interface IAbc { }
}
