using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;
#if !UNITY
using Microsoft.Extensions.ObjectPool1;
#endif

namespace Sen
{
    /// <summary>
    /// Network options of receiving data, don't reuse a <see cref="NetworkOptions"/> object yourself
    /// if you didn't create it because it will be reused by framework.
    /// <list type="bullet"></list>
    /// Sending a response from <see cref="IPlayer.HandleMessage(IUnionData, NetworkOptions)"/> the <see cref="NetworkOptions"/> will
    /// be reused.
    /// </summary>
    public class NetworkOptions
    {
#if UNITY
        public
#else
        internal
#endif
        NetworkOptions()
        {
            Default();
        }

        #region Data
        /// <summary>
        /// Option to encrypt the message
        /// </summary>
        public bool Secure;

        public Reliability Reliability;

        /// <summary>
        /// Use internally
        /// </summary>
        internal MessageType MessageType;
        #endregion

        public ushort ToServiceCode()
        {
            return (ushort)((Secure ? 1 : 0) | ((byte)Reliability << 1) | ((byte)MessageType << 5));
        }

        public void Default()
        {
            Secure = false;
            Reliability = Reliability.ReliableOrderedSequenced;
            MessageType = MessageType.Normal;
        }

        public void SetValues(ushort serviceCode)
        {
            Secure = (serviceCode & 1) != 0;
            Reliability = (Reliability)((serviceCode & 0b_0001_1110) >> 1);
            MessageType = (MessageType)((serviceCode & 0b_1110_0000) >> 5);
        }

#if !UNITY
        private static readonly DefaultObjectPool<NetworkOptions> _pool = new(new NetworkOptionPoolObjectPolicy(), Environment.ProcessorCount * 10);
        /// <summary>
        /// Create a object from pool
        /// </summary>
        public static NetworkOptions Create() => _pool.Get();
        /// <summary>
        /// Create a object from pool
        /// </summary>
        /// <param name="serviceCode">A service code to initialize the result object</param>
        /// <returns></returns>
        public static NetworkOptions Create(ushort serviceCode)
        {
            var value = _pool.Get();
            value.SetValues(serviceCode);
            return value;
        }
        /// <summary>
        /// Return the <paramref name="networkOptions"/> to the pool
        /// </summary>
        /// <param name="networkOptions">Object to be returned</param>
        public static void Return(NetworkOptions networkOptions) => _pool.Return(networkOptions);
#endif
    }

#if !UNITY
    internal class NetworkOptionPoolObjectPolicy : PooledObjectPolicy<NetworkOptions>
    {
        public override NetworkOptions Create()
        {
            return new NetworkOptions();
        }

        public override bool Return(NetworkOptions obj)
        {
            return true;
        }
    }
#endif
}
