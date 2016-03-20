#region License
// 
// Copyright (c) 2013, Kooboo team
// 
// Licensed under the BSD License
// See the file LICENSE.txt for details.
// 
#endregion

using System;
using Kooboo.CMS.Common.Persistence.Non_Relational;
using Kooboo.Extensions.Common;

namespace Kooboo.CMS.Caching
{
    /// <summary>
    /// 
    /// </summary>
    public static class CacheManagerFactory
    {
        #region fields
        private static CacheManager cacheManager;
        #endregion

        #region properties

        public static CacheManager DefaultCacheManager
        {
            get
            {
                return cacheManager;
            }
            set
            {
                cacheManager = value;
            }
        }

        #endregion

        #region .ctor
        static CacheManagerFactory()
        {
            DefaultCacheManager = new MemoryCacheManager();
        }
        #endregion        

        #region Methods
        public static void ClearWithNotify(string cacheName)
        {
            DefaultCacheManager.Clear(cacheName);
            CacheExpiredNotification.Notify(cacheName, null);
        }
        #endregion

        public static T GetActual<T>(string key, string type, T value) where T : class, IPersistable
        {
            var cacheKey = type + ":" + key;
            var cache = DefaultCacheManager.GlobalObjectCache();
            var actualFolder = cache.Get(cacheKey) as T;
            if (actualFolder == null)
            {
                var actual = value.AsActual();
                DefaultCacheManager.GlobalObjectCache().Set(cacheKey, actual, new DateTimeOffset(DateTime.Now.AddSeconds(CacheSettings.StandartExpirationIntervalSecond)));
                return actual;
            }
            return actualFolder;
        }
    }
}
