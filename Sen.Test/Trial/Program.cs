using System;

namespace Trial
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("This has color?");
            new DynamicSub().Run();
            MessagePack.Attemp();
        }
    }
}
