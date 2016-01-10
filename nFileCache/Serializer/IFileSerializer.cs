/*
Copyright © mabakay 2015-2016, based on FileCache (http://fc.codeplex.com)

This file is part of nFileCache (https://github.com/mabakay/nFileCache).

nFileCache is distributed under the Microsoft Public License (Ms-PL).
Consult "LICENSE.txt" included in this package for the complete Ms-PL license.
*/

using System.IO;

namespace System.Runtime.Caching
{
    public interface IFileSerializer
    {
        FileCacheItem Deserialize(Stream stream);
        void Serialize(Stream stream, FileCacheItem cacheItem);
    }
}