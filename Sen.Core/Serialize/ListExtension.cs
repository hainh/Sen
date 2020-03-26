using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Core.Serialize
{
    public static class ListExtension
    {
        public static void Append<T>(this List<T> stream, T value)
        {
            stream.Add(value);
        }

        public static void Append<T>(this List<T> stream, IEnumerable<T> buffer)
        {
            stream.AddRange(buffer);
        }

        public static void Prepend<T>(this List<T> stream, T value)
        {
            stream.Insert(0, value);
        }

        public static void Prepend<T>(this List<T> stream, IEnumerable<T> buffer)
        {
            stream.InsertRange(0, buffer);
        }
    }
}
