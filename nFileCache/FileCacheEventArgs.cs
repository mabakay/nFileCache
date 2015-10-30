/*
Copyright 2014 mabakay, based on FileCache (http://fc.codeplex.com)

This file is part of NFileCache (http://nfilecache.codeplex.com).

NFileCache is distributed under the Microsoft Public License (Ms-PL).
Consult "LICENSE.txt" included in this package for the complete Ms-PL license.
*/

namespace System.Runtime.Caching
{
    public sealed class FileCacheEventArgs : EventArgs
    {
        #region Properties

        public long CurrentCacheSize { get; private set; }
        public long MaxCacheSize { get; private set; } 

        #endregion

        #region Constructors

        public FileCacheEventArgs(long currentSize, long maxSize)
        {
            CurrentCacheSize = currentSize;
            MaxCacheSize = maxSize;
        } 

        #endregion
    }
}
