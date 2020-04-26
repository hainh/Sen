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

        [DefaultValue(UseExternalProxy.None)]
        public UseExternalProxy UseExternalProxy { get; private set; }

        [DefaultValue(typeof(TcpListener[]))]
        public TcpListener[] TcpListeners { get; private set; }

        [DefaultValue(typeof(UdpListener[]))]
        public UdpListener[] UdpListeners { get; private set; }

        [DefaultValue(typeof(WebSocketListener[]))]
        public WebSocketListener[] WebsocketListeners { get; private set; }

        [JsonIgnore]
        public IEnumerable<Listener> Listeners => TcpListeners.Concat<Listener>(WebsocketListeners).Concat(UdpListeners);
    }

    public class Listener : JsonConfig<Listener>
    {
        public int Port { get; set; }
    }

    public class UdpListener : Listener
    {

    }

    public class TcpListener: Listener
    {
    }

    public class WebSocketListener : TcpListener
    {
        public bool UseTLS => !string.IsNullOrWhiteSpace(CertificateName);

        [DefaultValue(StoreLocation.CurrentUser)]
        public StoreLocation StoreLocation { get; set; }

        [DefaultValue("")]
        public string CertificateName { get; set; }
    }
}
