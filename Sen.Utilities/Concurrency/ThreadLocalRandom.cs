
using System;
using System.Threading;

namespace Sen.Utilities
{
    public static class ThreadSafeRandom
    {
        static ThreadSafeRandom()
        {
            var buf = new byte[4];
            new System.Security.Cryptography.RNGCryptoServiceProvider().GetBytes(buf);
            _seed = BitConverter.ToInt32(buf, 0);
        }

        private static int _seed;

        private static readonly ThreadLocal<Random> _rng = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref _seed)));

        /// <summary>
        /// The current <see cref="Random"/> object available to this thread
        /// </summary>
        public static Random Current
        {
            get
            {
                return _rng.Value;
            }
        }
    }
}

