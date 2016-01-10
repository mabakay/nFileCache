/*
Copyright © mabakay 2015-2016, based on FileCache (http://fc.codeplex.com)

This file is part of nFileCache (https://github.com/mabakay/nFileCache).

nFileCache is distributed under the Microsoft Public License (Ms-PL).
Consult "LICENSE.txt" included in this package for the complete Ms-PL license.
*/

using System.Runtime.Serialization;

namespace System.Runtime.Caching
{
    internal class CacheItemPolicySurrogate : ISerializationSurrogate
    {
        /// <summary>
        /// Manually add objects to the <see cref="SerializationInfo"/> store.
        /// </summary>
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            CacheItemPolicy policy = (CacheItemPolicy)obj;

            info.AddValue("AbsoluteExpiration", policy.AbsoluteExpiration);
            info.AddValue("SlidingExpiration", policy.SlidingExpiration);
        }

        /// <summary>
        /// Retrieves objects from the <see cref="SerializationInfo"/> store.
        /// </summary>
        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            CacheItemPolicy policy = (CacheItemPolicy)obj;

            policy.AbsoluteExpiration = (DateTimeOffset)info.GetValue("AbsoluteExpiration", typeof(DateTimeOffset));
            policy.SlidingExpiration = (TimeSpan)info.GetValue("SlidingExpiration", typeof(TimeSpan));

            return policy;
        }
    }
}