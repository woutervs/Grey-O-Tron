using System;
using System.Runtime.Caching;

namespace GreyOTron.Library.Helpers
{
    public class Cache
    {
        public T GetFromCache<T>(string name, TimeSpan? slidingExpiration, Func<T> create)
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

        public void RemoveFromCache(string name)
        {
            var cache = MemoryCache.Default;
            cache.Remove(name, CacheEntryRemovedReason.Removed);
        }
    }
}
