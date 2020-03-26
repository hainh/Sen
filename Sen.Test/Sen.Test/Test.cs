using Senla.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Test
{
    class Comparer : IEqualityComparer<KeyValuePair<byte, object>>
    {
        public bool Equals(KeyValuePair<byte, object> x, KeyValuePair<byte, object> y)
        {
            if (x.Key == y.Key && x.Value is Array && x.Value.GetType().FullName == y.Value.GetType().FullName)
            {
                var e1 = (x.Value as Array).GetEnumerator();
                var e2 = (y.Value as Array).GetEnumerator();
                bool equal = true;
                while (equal && e1.MoveNext() && e2.MoveNext())
                {
                    if (e1.Current is string || e2.Current is string)
                    {
                        equal = (string.IsNullOrEmpty(e1.Current as string) && string.IsNullOrEmpty(e2.Current as string)) || string.Equals(e1.Current as string, e2.Current as string);
                    }
                    else
                    {
                        equal = e1.Current.Equals(e2.Current);
                    }
                }

                equal = equal && !e1.MoveNext() && !e2.MoveNext();
                return equal;
            }
            return x.Key == y.Key && x.Value.GetType().FullName == y.Value.GetType().FullName && x.Value.Equals(y.Value);
        }

        public int GetHashCode(KeyValuePair<byte, object> obj)
        {
            return obj.Key.GetHashCode() ^ obj.Value.GetHashCode();
        }
    }

    class Peer : PeerBase
    {
        public Peer(ISocketPeer socketPeer) : base(socketPeer)
        {
            //SendEvent(new EventData(), new SendParameters());
        }

        public override void OnDisconnect(DisconnectReason reason)
        {
            //Utils.WriteLineT("Lost connection {0}: {1}", ConnectionId, reason.ToString());
        }

        public override void OnOperationRequest(OperationData operationData, SendParameters sendParameters)
        {
            SendOperationResponse(operationData, sendParameters);
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

    public class Test : IApplication
    {
        public string AppName { get; set; }

        public PeerBase CreatePeer(ISocketPeer socketPeer)
        {
            return new Peer(socketPeer);
        }

        public void Setup()
        {
            var mongoClient = new MongoDB.Driver.MongoClient();
            var db = mongoClient.GetDatabase("gambledata");
        }

        public void Shutdown()
        {
        }
    }
}
