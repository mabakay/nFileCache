/*
Copyright 2014 mabakay

This file is part of NFileCache (http://nfilecache.codeplex.com).

NFileCache is distributed under the Microsoft Public License (Ms-PL).
Consult "LICENSE.txt" included in this package for the complete Ms-PL license.
*/

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace System.Runtime.Caching
{
    internal class NFileSerializer : INFileSerializer
    {
        #region Fields

        private readonly SerializationBinder _binder; 

        #endregion

        #region Constructors

        public NFileSerializer(SerializationBinder binder)
        {
            _binder = binder;
        } 

        #endregion

        #region INFileSerializer

        public NFileCacheItem Deserialize(Stream stream)
        {
            NFileCacheItem item = null;

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

                item = new NFileCacheItem(key, policy, payload);
            }
            catch (SerializationException)
            {

            }

            return item;
        }

        public void Serialize(Stream stream, NFileCacheItem cacheItem)
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