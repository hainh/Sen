using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Core.Utilities
{
    public static class Extensions
    {
        public static void Add(this Dictionary<byte, object> data, Enum key, object @value)
        {
            data.Add((byte)(object)key, @value);
        }

        public static bool ContainsKey(this Dictionary<byte, object> data, Enum key)
        {
            return data.ContainsKey((byte)(object)key);
        }
    }
}
