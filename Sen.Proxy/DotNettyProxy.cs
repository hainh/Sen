﻿using DotNetty.Codecs.Http;
using DotNetty.Common;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Libuv;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Orleans;
using Sen.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Sen.Proxy
{
    public class DotNettyProxy : ISenProxy
    {
        public ProxyConfig ProxyConfig { get; }
        readonly Dictionary<string, X509Certificate2> Certificates = new Dictionary<string, X509Certificate2>();

        readonly ILogger<DotNettyProxy> logger = LoggerFactory.CreateLogger<DotNettyProxy>();

        private IEventLoopGroup bossGroup;
        private IEventLoopGroup workGroup;
        private List<IChannel> channels;

        public static ILoggerFactory LoggerFactory { get; } = new NLogLoggerFactory();

        public IServiceProvider Services => throw new NotImplementedException();

        private GrainFactory grainFactory;

        public DotNettyProxy()
        {
            Utilities.InternalLogger.LoggerFactory = LoggerFactory;
            DotNetty.Common.Internal.Logging.InternalLoggerFactory.DefaultFactory = LoggerFactory;

            ProxyConfig = new ProxyConfig("ProxyConfig.json").Load();

            Console.Title = "Sen Proxy - Client";
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.LogError(e.ExceptionObject as Exception, "Unhandled exception caught");
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            ResourceLeakDetector.Level = ResourceLeakDetector.DetectionLevel.Disabled;
            Console.WriteLine($"Resource Leak Detector Level : {ResourceLeakDetector.Level}");
            Console.WriteLine($"Server garbage collection : {(GCSettings.IsServerGC ? "Enabled" : "Disabled")}");
            Console.WriteLine($"Current latency mode for garbage collection: {GCSettings.LatencyMode}");

            PrepareAllCertificates(ProxyConfig.Listeners);

            bossGroup = new DispatcherEventLoopGroup();
            workGroup = new WorkerEventLoopGroup((DispatcherEventLoopGroup)bossGroup);
            channels = new List<IChannel>();

            var bootstrap = new ServerBootstrap();
            bootstrap
                    .Group(bossGroup, workGroup)
                    .Channel<TcpServerChannel>()
                    .Option(ChannelOption.SoBacklog, 8192)
                    .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        IPEndPoint localEndPoint = channel.LocalAddress as IPEndPoint;
                        Listener listener = GetListener(localEndPoint.Port);
                        if (listener is WebSocketListener webSocketListener)
                        {
                            if (webSocketListener.UseTLS)
                            {
                                if (!Certificates.TryGetValue(webSocketListener.CertificateName, out X509Certificate2 x509Certificate2))
                                {
                                    x509Certificate2 = GetCertificateFromStore(webSocketListener.StoreLocation, webSocketListener.CertificateName);
                                    Certificates.Add(webSocketListener.CertificateName, x509Certificate2);
                                }

                                pipeline.AddLast(TlsHandler.Server(x509Certificate2));
                            }
                            pipeline.AddLast(new HttpServerCodec(512, 1024, 8192));
                            pipeline.AddLast(new HttpObjectAggregator(100, true));
                            pipeline.AddLast(new WebSocketServerHandler(grainFactory, ProxyConfig.UseExternalProxy));
                        }
                        else if (listener is TcpListener tcpListener)
                        {

                        }
                        else if (listener is UdpListener udpListener)
                        {

                        }
                    }));
            if (IsUnixLike())
            {
                bootstrap
                        .Option(ChannelOption.SoReuseport, true)
                        .ChildOption(ChannelOption.SoReuseaddr, true);
            }

            await Bind();
            IChannel bootstrapChannel = await bootstrap.BindAsync(IPAddress.Loopback, 9090);
            IChannel nextChannel = await bootstrap.BindAsync(IPAddress.Loopback, 9091);
            Console.WriteLine("Websocket listening");
            
            async Task Bind()
            {
                foreach (Listener listener in ProxyConfig.Listeners)
                {
                    if (channels.All(c => (c.LocalAddress as IPEndPoint).Port != listener.Port))
                    {
                        try
                        {
                            IChannel channel = await bootstrap.BindAsync(listener.Port);
                            channels.Add(channel);
                            string msg = "Listening on port " + listener.Port;
                            logger.LogInformation(msg);
                            Console.WriteLine(msg);
                        }
                        catch (Exception e)
                        {
                            logger.LogError(e, "Cannot bind port {0}", listener.Port);
                        }
                    }
                }
            }
        }

        private bool IsUnixLike()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        }

        private void PrepareAllCertificates(IEnumerable<Listener> listeners)
        {
            IEnumerable<Listener> wss = listeners.Where(l => l is WebSocketListener webSocketListener && webSocketListener.UseTLS);
            foreach (WebSocketListener listener in wss)
            {
                var certificate = GetCertificateFromStore(listener.StoreLocation, listener.CertificateName);
                if (certificate == null)
                {
                    throw new Exception($"Cannot load certificate '{listener.CertificateName}' from store location '{listener.StoreLocation}'");
                }
                Certificates.Add(listener.CertificateName, certificate);
            }
        }

        private X509Certificate2 GetCertificateFromStore(StoreLocation storeLocation, string certName)
        {
            // Get the certificate store for the current user.
            X509Store store = new X509Store(storeLocation);
            try
            {
                store.Open(OpenFlags.ReadOnly);

                X509Certificate2Collection certCollection = store.Certificates;
                // If using a certificate with a trusted root you do not need to FindByTimeValid, instead:
                // currentCerts.Find(X509FindType.FindBySubjectDistinguishedName, certName, true);
                X509Certificate2Collection currentCerts = certCollection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
                X509Certificate2Collection signingCert = currentCerts.Find(X509FindType.FindBySubjectDistinguishedName, certName, false);
                if (signingCert.Count == 0)
                    return null;
                // Return the first certificate in the collection, has the right name and is current.
                return signingCert[0];
            }
            finally
            {
                store.Close();
            }
        }

        private Listener GetListener(int port)
        {
            return ProxyConfig.Listeners.FirstOrDefault(l => l.Port == port);
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (channels.Count > 0)
            {
                await Task.WhenAll(channels.Select(c => c.CloseAsync()));
            }

            await workGroup.ShutdownGracefullyAsync();
            await bossGroup.ShutdownGracefullyAsync();
        }

        public void Dispose()
        {
            if (!workGroup.IsShuttingDown)
            {
                StopAsync().Wait();
            }
        }

        public void SetGrainFactory(GrainFactory grainFactory)
        {
            this.grainFactory = grainFactory;
        }
    }
}
