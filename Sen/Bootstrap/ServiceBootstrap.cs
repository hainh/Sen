using Senla.Core;
using Senla.Server.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Server.Bootstrap
{
    /// <summary>
    /// Uses to run Senla server as a service
    /// </summary>
    public class ServiceBootstrap : ServiceBase
    {
        private IEnumerable<Application> _appConfigs;
        private IEnumerable<IApplication> _applications;

        SocketServer.Bootstrap _bootstrap;

        public ServiceBootstrap(IEnumerable<IApplication> apps, IEnumerable<Application> configs)
        {
            _applications = apps;
            _appConfigs = configs;
        }

        public void Start()
        {
            Run(this);
        }

        protected override void OnStart(string[] args)
        {
            _bootstrap = new SocketServer.Bootstrap();
            _bootstrap.RunServer(_applications, _appConfigs).Wait();
        }

        protected override void OnStop()
        {
            _bootstrap.ShutdownServer();
        }

        protected override void OnShutdown()
        {
            OnStop();
        }
    }
}
