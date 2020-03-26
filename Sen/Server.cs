using Senla.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Senla.Server.Configuration;
using System.Configuration;
using System.Reflection;
using System.IO;

namespace Senla.Server
{
    class Server
    {
        static string BaseDirectory
        {
            get
            {
                return Environment.CurrentDirectory + "\\";
            }
        }

        static string AppsDirectory
        {
            get
            {
                return Path.GetFullPath(BaseDirectory + "..\\");
            }
        }

        static List<IApplication> GetApps(List<Application> configs)
        {
            var apps = new List<IApplication>(configs.Count);

            foreach (var appConfig in configs)
            {
                var appDll = Assembly.LoadFrom(AppsDirectory + appConfig.BaseDir + "\\" + Path.GetFileName(appConfig.Assembly) + ".dll");
                var typeOfApp = appDll.GetType(appConfig.Type);
                var app = Activator.CreateInstance(typeOfApp) as IApplication;

                if (app != null)
                {
                    app.AppName = appConfig.Name;
                    apps.Add(app);
                }
                else
                {
                    throw new ApplicationException(string.Format("App not found: {0}, {1}", appConfig.Type, appConfig.Assembly));
                }
            }
            return apps;
        }

        static List<Application> ParseAppConfigs()
        {
            var configuration = ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None);

            var senlaSection = (SenlaSection)configuration.GetSection("Senla");

            var apps = new List<Application>(senlaSection.Applications.Count);
            for (int i = 0; i < senlaSection.Applications.Count; i++)
            {
                apps.Add(senlaSection.Applications[i]);
            }

            return apps;
        }

        public static void Main(string[] args)
        {
            List<Application> configs = ParseAppConfigs();
            List<IApplication> apps = GetApps(configs);

            if (Environment.UserInteractive)
            {
                var console = new Bootstrap.ConsoleBootstrap();
                console.Start(apps, configs);
                console = null;
                configs = null;
                apps = null;

                GC.Collect(2, GCCollectionMode.Forced);
                Console.Write("Press any key to exit");
                Console.ReadKey();
                Console.WriteLine();
            }
            else
            {
                new Bootstrap.ServiceBootstrap(apps, configs).Start();
            }
        }
    }

    public static class Utils
    {
        public static void WriteLineT(string format, params object[] p)
        {
            Console.Write(DateTime.Now.ToLongTimeString());
            Console.Write(" ");
            Console.WriteLine(format, p);
        }
    }
}
