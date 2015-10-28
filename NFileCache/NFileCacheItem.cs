/*
Copyright 2014 mabakay, based on FileCache (http://fc.codeplex.com)

This file is part of NFileCache (http://nfilecache.codeplex.com).

NFileCache is distributed under the Microsoft Public License (Ms-PL).
Consult "LICENSE.txt" included in this package for the complete Ms-PL license.
*/

namespace System.Runtime.Caching
{
    public sealed class NFileCacheItem
    {
        #region Properties
        
        public string Key { get; set; }
        public object Payload { get; set; }
        public CacheItemPolicy Policy { get; set; } 

        #endregion

        #region Constructors

        public NFileCacheItem(string key, CacheItemPolicy policy = null, object payload = null)
        {
            Key = key;
            Policy = policy;
            Payload = payload;
        } 

        #endregion
    }
}
