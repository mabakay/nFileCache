/*
Copyright 2014 mabakay, based on FileCache (http://fc.codeplex.com)

This file is part of NFileCache (http://nfilecache.codeplex.com).

NFileCache is distributed under the Microsoft Public License (Ms-PL).
Consult "LICENSE.txt" included in this package for the complete Ms-PL license.
*/

namespace System.Runtime.Caching
{
    [Serializable]
    internal sealed class SerializableCacheItemPolicy
    {
        #region Fields

        private TimeSpan _slidingExpiration; 

        #endregion

        #region Properties

        public DateTimeOffset AbsoluteExpiration { get; set; }

        public TimeSpan SlidingExpiration
        {
            get
            {
                return _slidingExpiration;
            }
            set
            {
                _slidingExpiration = value;

                if (_slidingExpiration > ObjectCache.NoSlidingExpiration)
                {
                    AbsoluteExpiration = DateTimeOffset.Now.Add(_slidingExpiration);
                }
            }
        } 

        #endregion

        #region Constructors

        public SerializableCacheItemPolicy(CacheItemPolicy policy)
        {
            AbsoluteExpiration = policy.AbsoluteExpiration;
            SlidingExpiration = policy.SlidingExpiration;
        }

        #endregion

        #region Methods

        public CacheItemPolicy GetCacheItemPolicy()
        {
            CacheItemPolicy policy = new CacheItemPolicy
            {
                AbsoluteExpiration = AbsoluteExpiration,
                SlidingExpiration = SlidingExpiration
            };

            return policy;
        }  

        #endregion
    }
}
