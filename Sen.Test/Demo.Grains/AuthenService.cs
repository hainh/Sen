using Demo.Interfaces;
using Orleans;
using Sen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Grains
{
    public class AuthenService : Grain, IAuthService
    {
        public ValueTask<bool> Login(string username, string password)
        {
            return ValueTask.FromResult(true);
        }
    }
}
