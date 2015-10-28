/*
Copyright 2014 mabakay

This file is part of NFileCache (http://nfilecache.codeplex.com).

NFileCache is distributed under the Microsoft Public License (Ms-PL).
Consult "LICENSE.txt" included in this package for the complete Ms-PL license.
*/

using System.IO;

namespace System.Runtime.Caching
{
    public interface INFileSerializer
    {
        NFileCacheItem Deserialize(Stream stream);
        void Serialize(Stream stream, NFileCacheItem cacheItem);
    }
}