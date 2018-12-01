using System;
using System.Runtime.Caching;

namespace GreyOTron
{
    public static class Cache
    {
        public static T GetFromCache<T>(string name, TimeSpan? slidingExpiration, Func<T> create)
        {
            var cache = MemoryCache.Default;
            var obj = (T) cache[name];
            if (obj == null)
            {
                var policy = new CacheItemPolicy();
                if (slidingExpiration.HasValue)
                {
                    policy.SlidingExpiration = slidingExpiration.Value;
                }

                obj = create();
                cache.Set(name, obj, policy);
            }

            return obj;
        }
    }
}
