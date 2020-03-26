using Senla.Core;
using Senla.Server.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Server.Bootstrap
{
    /// <summary>
    /// Uses to run Senla server in Console mode
    /// </summary>
    public class ConsoleBootstrap
    {
        public void Start(IEnumerable<IApplication> apps, IEnumerable<Application> configs)
        {
            var bootstrap = new SocketServer.Bootstrap();
            var t = bootstrap.RunServer(apps, configs);

            string terminate = "u4B#00*n!aPr5[d>kOs,/|Gj3;(c}@zB+~eK";

            string command;
            do
            {
                command = Console.ReadLine();
                switch (command)
                {
                    case "exit":
                        Console.Write("Are you sure?(y/n): ");
                        command = Console.ReadKey().KeyChar.ToString().ToLower();
                        if (command == "y")
                            command = terminate;
                        break;
                    default:
                        break;
                }
            } while (command != terminate);

            bootstrap.ShutdownServer();

            bootstrap = null;
        }
    }
}
