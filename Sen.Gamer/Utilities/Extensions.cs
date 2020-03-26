using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senla.Gamer.Utilities
{
    public static class ULongExtension
    {
        public static bool BitAt(this ulong[] data, int index)
        {
            int atByte = index / 64;
            int atBit = index % 64;
            ulong maskBits = 1UL << atBit;
            return (data[atByte] & maskBits) > 0;
        }

        public static void SetBit(this ulong[] data, int index)
        {
            int atByte = index / 64;
            int atBit = index % 64;
            ulong maskBits = 1UL << atBit;
            data[atByte] = data[atByte] | maskBits;
        }

        public static void UnsetBit(this ulong[] data, int index)
        {
            int atByte = index / 64;
            int atBit = index % 64;
            ulong maskBits = ~(1UL << atBit);
            data[atByte] = data[atByte] & maskBits;
        }

    }

    public static class StringExtension
    {
        public static char[] hexa = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
        
        public static String GetMD5(this string data)
        {
            var checksum = System.Security.Cryptography.MD5.Create().ComputeHash((data).Select(c => (byte)c).ToArray());
            var strChecksum = new StringBuilder(32);

            for (int i = 0; i < hexa.Length; ++i)
            {
                strChecksum.Append(hexa[checksum[i] >> 4]);
                strChecksum.Append(hexa[checksum[i] & 0x0f]);
            }

            return strChecksum.ToString();
        }
    }

    /// <summary>
    /// Thread safe Random
    /// </summary>
    public static class RandomTS
    {
        /// <summary>
        /// Returns nonnegative random integer
        /// </summary>
        public static int Next()
        {
            return Akka.Util.ThreadLocalRandom.Current.Next();
        }

        /// <summary>
        /// Return a nonnegative integer less than the specified maximum value
        /// </summary>
        /// <param name="maxValue">Exclusive upper bound of the random number to be generated. Must be nonnegative</param>
        /// <returns></returns>
        public static int Next(int maxValue)
        {
            return Akka.Util.ThreadLocalRandom.Current.Next(maxValue);
        }

        public static int Next(int minValue, int maxValue)
        {
            return Akka.Util.ThreadLocalRandom.Current.Next(minValue, maxValue);
        }

        public static void NextBytes(byte[] buffer)
        {
            Akka.Util.ThreadLocalRandom.Current.NextBytes(buffer);
        }

        public static double NextDouble()
        {
            return Akka.Util.ThreadLocalRandom.Current.NextDouble();
        }
    }
}
