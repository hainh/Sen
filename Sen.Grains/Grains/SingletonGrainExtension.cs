using Orleans;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sen
{
    public static class SingletonGrainExtension
    {
        /// <summary>
        /// Create a singleton grain of type <typeparamref name="TGrain"/>
        /// </summary>
        public static TGrain GetGrain<TGrain>(this IGrainFactory grainFactory) where TGrain : ISingletonGrain, IGrainWithStringKey
        {
            return grainFactory.GetGrain<TGrain>(string.Empty);
        }
    }
}
