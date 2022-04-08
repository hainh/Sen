using Sen.Utilities.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;

namespace Sen.Proxy
{
    public class ProxyConfig : JsonFileConfig<ProxyConfig>
    {
        public ProxyConfig(string fileName) : base(fileName)
        {
        }

        protected override bool WatchForFileChange => false;

        [DefaultValue(UseExternalProxy.None)]
        public UseExternalProxy UseExternalProxy { get; private set; }

        [DefaultValue(typeof(TcpListener[]))]
        public TcpListener[] ServerToServerListeners { get; private set; }

        [DefaultValue(typeof(TcpListener[]))]
        public TcpListener[] TcpListeners { get; private set; }

        [DefaultValue(typeof(UdpListener[]))]
        public UdpListener[] UdpListeners { get; private set; }

        [DefaultValue(typeof(WebSocketListener[]))]
        public WebSocketListener[] WebsocketListeners { get; private set; }

        [JsonIgnore]
        public IEnumerable<Listener> Listeners => TcpListeners.Concat<Listener>(WebsocketListeners).Concat(UdpListeners).Concat(ServerToServerListeners);

        protected override void ManualValidate()
        {
            if (ServerToServerListeners.Length > 1) throw new Exception($"{nameof(ServerToServerListeners)} must have at most 01 listener.");
            if (ServerToServerListeners.Length == 1 && TcpListeners.Concat<Listener>(WebsocketListeners).Concat(UdpListeners).Any(listener => listener.Port == ServerToServerListeners[0].Port))
            {
                throw new Exception($"{nameof(ServerToServerListeners)} Port number cannot use same as other ports");
            }
        }
    }

    public class Listener : JsonConfig<Listener>
    {
        public int Port { get; set; }

        public bool UseTLS => !string.IsNullOrWhiteSpace(CertificateName);

        [DefaultValue(StoreLocation.CurrentUser)]
        public StoreLocation StoreLocation { get; set; }

        [DefaultValue("")]
        public string CertificateName { get; set; }
    }

    public class UdpListener : Listener
    {

    }

    public class TcpListener: Listener
    {
    }

    public class WebSocketListener : TcpListener
    {
    }
}
