/*
Copyright © mabakay 2015-2016, based on FileCache (http://fc.codeplex.com)

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
            var surrogateSelector = new SurrogateSelector();
            surrogateSelector.AddSurrogate(typeof(CacheItemPolicy), new StreamingContext(StreamingContextStates.All), new CacheItemPolicySurrogate());

            BinaryFormatter formatter = new BinaryFormatter();
            formatter.SurrogateSelector = surrogateSelector;
            formatter.Binder = _binder;

            FileCacheItem item = null;

            try
            {
                string key = (string)formatter.Deserialize(stream);
                CacheItemPolicy policy = (CacheItemPolicy)formatter.Deserialize(stream);
                object payload = formatter.Deserialize(stream);

                item = new FileCacheItem(key, policy, payload);
            }
            catch (SerializationException)
            {

            }

            return item;
        }

        public void Serialize(Stream stream, FileCacheItem cacheItem)
        {
            var surrogateSelector = new SurrogateSelector();
            surrogateSelector.AddSurrogate(typeof(CacheItemPolicy), new StreamingContext(StreamingContextStates.All), new CacheItemPolicySurrogate());

            BinaryFormatter formatter = new BinaryFormatter();
            formatter.SurrogateSelector = surrogateSelector;

            formatter.Serialize(stream, cacheItem.Key);
            formatter.Serialize(stream, cacheItem.Policy);
            formatter.Serialize(stream, cacheItem.Payload);
        }

        #endregion
    }
}