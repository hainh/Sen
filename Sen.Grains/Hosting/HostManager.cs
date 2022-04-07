using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sen
{
    public class HostManager
    {
        public static IHost Host { get; internal set; } = null!;
    }
}
