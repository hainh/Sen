﻿using Sen.Server;
using System.Threading.Tasks;

namespace DemoSilo
{
    class Program
    {
        public static async Task<int> Main()
        {
            return await SenServer.Run();
        }
    }
}
