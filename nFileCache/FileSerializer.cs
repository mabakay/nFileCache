/*
Copyright 2015 mabakay, based on FileCache (http://fc.codeplex.com)

This file is part of nFileCache (https://github.com/mabakay/nFileCache).

nFileCache is distributed under the Microsoft Public License (Ms-PL).
Consult "LICENSE.txt" included in this package for the complete Ms-PL license.
*/

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace System.Runtime.Caching
{
    internal class FileSerializer : IFileSerializer
    {
        #region Fields

        private readonly SerializationBinder _binder; 

        #endregion

        #region Constructors

        public FileSerializer(SerializationBinder binder)
        {
            _binder = binder;
        } 

        #endregion

        #region IFileSerializer

        public FileCacheItem Deserialize(Stream stream)
        {
            FileCacheItem item = null;

            BinaryFormatter formatter = new BinaryFormatter { Binder = _binder };

            try
            {
                string key = (string)formatter.Deserialize(stream);
                CacheItemPolicy policy = ((SerializableCacheItemPolicy)formatter.Deserialize(stream)).GetCacheItemPolicy();
                object payload = formatter.Deserialize(stream);

                if (payload is SerializableStream)
                {
                    payload = new MemoryStream(((SerializableStream)payload).Data);
                }

                item = new FileCacheItem(key, policy, payload);
            }
            catch (SerializationException)
            {

            }

            return item;
        }

        public void Serialize(Stream stream, FileCacheItem cacheItem)
        {
            BinaryFormatter formatter = new BinaryFormatter();

            string key = cacheItem.Key;
            SerializableCacheItemPolicy policy = new SerializableCacheItemPolicy(cacheItem.Policy);
            object payload = cacheItem.Payload;

            Stream streamValue = cacheItem.Payload as Stream;
            if (streamValue != null)
            {
                payload = new SerializableStream(streamValue);
            }

            formatter.Serialize(stream, key);
            formatter.Serialize(stream, policy);
            formatter.Serialize(stream, payload);
        } 

        #endregion
    }
}