/*
Copyright 2015 mabakay, based on FileCache (http://fc.codeplex.com)

This file is part of nFileCache (https://github.com/mabakay/nFileCache).

nFileCache is distributed under the Microsoft Public License (Ms-PL).
Consult "LICENSE.txt" included in this package for the complete Ms-PL license.
*/

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;

namespace System.Runtime.Caching
{
    public sealed class NFileCache : ObjectCache
    {
        #region Fields

        private const string CacheFolderName = "cache";

        private static int _nameCounter = 1;
        private readonly string _name = "FileCache_" + _nameCounter;
        private readonly IFileSerializer _serializer;
        private CacheItemPolicy _defaultPolicy = new CacheItemPolicy();
        private TimeSpan _accessTimeout = TimeSpan.Zero;
        private long _maxCacheSize = long.MaxValue;

        #endregion

        #region Properties

        /// <summary>
        /// Used to store current directory path for cache.
        /// </summary>
        public string CacheDir { get; private set; }

        /// <summary>
        /// Used to store the default region when accessing the cache via index calls.
        /// </summary>
        public string DefaultRegion { get; set; }

        /// <summary>
        /// Used to store the default policy when setting cache values via index calls.
        /// </summary>
        public CacheItemPolicy DefaultPolicy
        {
            get
            {
                return _defaultPolicy;
            }
            set
            {
                ValidatePolicy(value);

                _defaultPolicy = value;
            }
        }

        /// <summary>
        /// Used to determine how long wait for a file to become available. Default (00:00:00) is indefinite.
        /// When the timeout is reached, an exception will be thrown.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public TimeSpan AccessTimeout
        {
            get
            {
                return _accessTimeout;
            }
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentException("Access timeout can not be negative.");
                }

                _accessTimeout = value;
            }
        }

        /// <summary>
        /// Used to specify the disk size, in bytes, that can be used by the cache.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public long MaxCacheSize
        {
            get
            {
                return _maxCacheSize;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("Max cache size can not be negative.");
                }

                _maxCacheSize = value;
            }
        }

        /// <summary>
        /// Returns the approximate size of the cache. If cache instance was initialized with param "calculateCacheSize" set to false,
        /// then returned size maps only items added and removed from time the cache was created.
        /// </summary>
        public long CurrentCacheSize { get; private set; }

        /// <summary>
        /// Event that will be called when <see cref="MaxCacheSize"/> is reached.
        /// </summary>
        public event EventHandler<FileCacheEventArgs> MaxCacheSizeReached;

        /// <summary>
        /// The default cache path used by cache.
        /// </summary>
        private string DefaultCachePath
        {
            get
            {
                return Path.Combine(Directory.GetCurrentDirectory(), CacheFolderName);
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a default instance of the cache.
        /// </summary>
        /// <param name="calculateCacheSize">If true, will calculate the cache's current size upon new object creation.
        /// Turned off by default as directory traversal is somewhat expensive and may not always be necessary based on use case.
        /// </param>
        public NFileCache(bool calculateCacheSize = false)
            : this(null, (SerializationBinder)null, calculateCacheSize)
        {

        }

        /// <summary>
        /// Creates an instance of the cache using the supplied path as the root save path.
        /// </summary>
        /// <param name="cacheRoot">The cache's root file path.</param>
        /// <param name="calculateCacheSize">If true, will calculate the cache's current size upon new object creation.
        /// Turned off by default as directory traversal is somewhat expensive and may not always be necessary based on use case.
        /// </param>
        public NFileCache(string cacheRoot, bool calculateCacheSize = false)
            : this(cacheRoot, (SerializationBinder)null, calculateCacheSize)
        {

        }

        /// <summary>
        /// Creates an instance of the cache.
        /// </summary>
        /// <param name="binder">The SerializationBinder used to deserialize cached objects by default serializer.</param>
        /// <param name="calculateCacheSize">If true, will calculate the cache's current size upon new object creation.
        /// Turned off by default as directory traversal is somewhat expensive and may not always be necessary based on use case.
        /// </param>
        public NFileCache(SerializationBinder binder, bool calculateCacheSize = false)
            : this(null, binder, calculateCacheSize)
        {

        }

        /// <summary>
        /// Creates an instance of the cache.
        /// </summary>
        /// <param name="serializer">Serializer instance used to serialize and deserializer cache items.</param>
        /// <param name="calculateCacheSize">If true, will calculate the cache's current size upon new object creation.
        /// Turned off by default as directory traversal is somewhat expensive and may not always be necessary based on use case.
        /// </param>
        public NFileCache(IFileSerializer serializer, bool calculateCacheSize = false)
            : this(null, serializer, calculateCacheSize)
        {

        }

        /// <summary>
        /// Creates an instance of the cache.
        /// </summary>
        /// <param name="cacheRoot">The cache's root file path.</param>
        /// <param name="binder">The SerializationBinder used to deserialize cached objects by default serializer.</param>
        /// <param name="calculateCacheSize">If true, will calculate the cache's current size upon new object creation.
        /// Turned off by default as directory traversal is somewhat expensive and may not always be necessary based on use case.
        /// </param>
        public NFileCache(string cacheRoot, SerializationBinder binder, bool calculateCacheSize = false) :
            this(cacheRoot, new FileSerializer(binder), calculateCacheSize)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacheRoot">The cache's root file path.</param>
        /// <param name="serializer">Serializer instance used to serialize and deserializer cache items.</param>
        /// <param name="calculateCacheSize">If true, will calculate the cache's current size upon new object creation.
        /// Turned off by default as directory traversal is somewhat expensive and may not always be necessary based on use case.
        /// </param>
        public NFileCache(string cacheRoot, IFileSerializer serializer, bool calculateCacheSize = false)
        {
            Interlocked.Increment(ref _nameCounter);
            _serializer = serializer;

            CacheDir = cacheRoot ?? DefaultCachePath;
            DefaultRegion = null;

            if (calculateCacheSize)
            {
                CurrentCacheSize = GetCacheSize();
            }
        }

        #endregion

        #region Custom methods

        /// <summary>
        /// Calculates the size, in bytes of the cache.
        /// </summary>
        /// <param name="regionName">The region to calculate. If NULL, will return total cache size.</param>
        public long GetCacheSize(string regionName = null)
        {
            string cachePath = regionName == null ? CacheDir : GetCachePath(regionName);

            var root = new DirectoryInfo(cachePath);
            if (!root.Exists)
            {
                return 0;
            }

            return root.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
        }

        /// <summary>
        /// Flushes the cache using DateTime.Now as the minimum date.
        /// </summary>
        /// <param name="regionName">The region to flush. If NULL, will flush all regions.</param>
        public void Flush(string regionName = null)
        {
            Flush(DateTime.Now, regionName);
        }

        /// <summary>
        /// Flushes the cache based on last access date, filtered by optional region.
        /// </summary>
        /// <param name="minDate">Minimum date of cache item last access date.</param>
        /// <param name="regionName">The region to flush. If NULL, will flush all regions.</param>
        public void Flush(DateTime minDate, string regionName = null)
        {
            string cachePath = regionName == null ? CacheDir : GetCachePath(regionName);

            var root = new DirectoryInfo(cachePath);
            if (!root.Exists)
            {
                return;
            }

            foreach (FileInfo fi in root.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                if (minDate > fi.LastAccessTime)
                {
                    CurrentCacheSize -= fi.Length;

                    fi.Delete();
                }
            }
        }

        /// <summary>
        /// Returns the policy attached to a given cache item.  
        /// </summary>
        /// <param name="key">The key of the item.</param>
        /// <param name="regionName">The region in which the key exists.</param>
        public CacheItemPolicy GetPolicy(string key, string regionName = null)
        {
            string cacheItemPath = GetItemPath(key, regionName);

            FileCacheItem item = ReadFile(cacheItemPath);

            return item.Policy;
        }

        /// <summary>
        /// Returns a list of keys for a given region.  
        /// </summary>
        /// <param name="regionName">The region to enumerate. If NULL, will enumarate all regions.</param>
        public IEnumerable<string> GetKeys(string regionName = null)
        {
            string cachePath = regionName == null ? CacheDir : GetCachePath(regionName);

            if (!Directory.Exists(cachePath))
            {
                return Enumerable.Empty<string>();
            }

            return Directory.EnumerateFiles(cachePath, "*", SearchOption.AllDirectories).Select(item => ReadFile(item).Key);
        }

        #endregion

        #region Helper methods

        /// <summary>
        /// This function servies to centralize file stream access within this class.
        /// </summary>
        private FileStream GetStream(string path, FileMode mode, FileAccess access, FileShare share)
        {
            FileStream stream = null;
            TimeSpan interval = TimeSpan.FromMilliseconds(50);
            TimeSpan totalTime = TimeSpan.Zero;
            while (stream == null)
            {
                try
                {
                    stream = new FileStream(path, mode, access, share);
                }
                catch (IOException)
                {
                    Thread.Sleep(interval);
                    totalTime += interval;

                    //if we've waited too long, throw the original exception.
                    if (AccessTimeout.Ticks != 0)
                    {
                        if (totalTime > AccessTimeout)
                        {
                            throw;
                        }
                    }
                }
            }

            return stream;
        }

        /// <summary>
        /// This function serves to centralize file reads within this class.
        /// </summary>
        private FileCacheItem ReadFile(string filePath)
        {
            FileCacheItem item = null;

            if (File.Exists(filePath))
            {
                using (FileStream stream = GetStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    item = _serializer.Deserialize(stream);
                }
            }

            return item;
        }

        /// <summary>
        /// This function serves to centralize file writes within this class
        /// </summary>
        private void WriteFile(string filePath, FileCacheItem data)
        {
            // Remove current item from cache size calculations
            if (File.Exists(filePath))
            {
                CurrentCacheSize -= new FileInfo(filePath).Length;
            }

            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write the object payload
            using (FileStream stream = GetStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                _serializer.Serialize(stream, data);
            }

            // Adjust cache size
            CurrentCacheSize += new FileInfo(filePath).Length;

            // Check to see if limit was reached
            if (CurrentCacheSize > MaxCacheSize)
            {
                if (MaxCacheSizeReached != null)
                    MaxCacheSizeReached(this, new FileCacheEventArgs(CurrentCacheSize, MaxCacheSize));
            }
        }

        /// <summary>
        /// Builds cache root folder path based on given region name.
        /// </summary>
        private string GetCachePath(string regionName = null)
        {
            regionName = string.Format("{0}{1}", "_", regionName == null ? string.Empty : regionName.GetHashCode().ToString());

            return Path.Combine(CacheDir, regionName);
        }

        /// <summary>
        /// Builds cache file phisical disk file paths.
        /// </summary>
        private string GetItemPath(string key, string regionName = null)
        {
            string directory = GetCachePath(regionName);
            string fileName = key.GetHashCode().ToString("0000");

            string filePath = Path.Combine(directory, fileName.Substring(0, 2), fileName.Substring(2));

            return filePath;
        }

        /// <summary>
        /// Validates cache item policy for unsupported capabilities.
        /// </summary>
        private void ValidatePolicy(CacheItemPolicy policy)
        {
            if (policy.AbsoluteExpiration != ObjectCache.InfiniteAbsoluteExpiration && policy.SlidingExpiration != ObjectCache.NoSlidingExpiration)
                throw new ArgumentException("Policy can not have absolute and sliding expiration set at the same time.", "policy");

            if (policy.SlidingExpiration < ObjectCache.NoSlidingExpiration)
                throw new ArgumentOutOfRangeException("policy", policy.SlidingExpiration, "Sliding expiration can not be negative.");

            if (policy.ChangeMonitors.Any())
                throw new NotSupportedException("Change monitors are not supported.");

            if (policy.RemovedCallback != null || policy.UpdateCallback != null)
                throw new NotSupportedException("Remove and update callbacks are not supported.");

            if (policy.Priority != CacheItemPriority.Default)
                throw new ArgumentOutOfRangeException("policy", policy.Priority, "Given policy prority is not supported.");
        }

        #endregion

        #region ObjectCache overrides

        public override object AddOrGetExisting(string key, object value, CacheItemPolicy policy, string regionName = null)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            if (value == null)
                throw new ArgumentNullException("value");

            ValidatePolicy(policy);

            object oldData = null;
            string cacheItemPath = GetItemPath(key, regionName);

            // Pull old value if it exists
            if (File.Exists(cacheItemPath))
            {
                try
                {
                    oldData = Get(key, regionName);
                }
                catch
                {
                    oldData = null;
                }
            }

            FileCacheItem newItem = new FileCacheItem(key, policy, value);

            WriteFile(cacheItemPath, newItem);

            return oldData;
        }

        public override CacheItem AddOrGetExisting(CacheItem value, CacheItemPolicy policy)
        {
            object oldData = AddOrGetExisting(value.Key, value.Value, policy, value.RegionName);

            CacheItem returnItem = null;
            if (oldData != null)
            {
                returnItem = new CacheItem(value.Key, oldData, value.RegionName);
            }

            return returnItem;
        }

        public override object AddOrGetExisting(string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)
        {
            CacheItemPolicy policy = new CacheItemPolicy { AbsoluteExpiration = absoluteExpiration };

            return AddOrGetExisting(key, value, policy, regionName);
        }

        public override bool Contains(string key, string regionName = null)
        {
            string cacheItemPath = GetItemPath(key, regionName);

            return File.Exists(cacheItemPath);
        }

        public override CacheEntryChangeMonitor CreateCacheEntryChangeMonitor(IEnumerable<string> keys, string regionName = null)
        {
            throw new NotSupportedException();
        }

        public override DefaultCacheCapabilities DefaultCacheCapabilities
        {
            get
            {
                return DefaultCacheCapabilities.CacheRegions | DefaultCacheCapabilities.AbsoluteExpirations | DefaultCacheCapabilities.SlidingExpirations;
            }
        }

        public override object Get(string key, string regionName = null)
        {
            string cacheItemPath = GetItemPath(key, regionName);

            FileCacheItem item = ReadFile(cacheItemPath);

            if (item == null)
            {
                return null;
            }

            // Did the item expire?
            if (item.Policy.AbsoluteExpiration < DateTime.Now)
            {
                // Set the item to null
                item.Payload = null;

                CurrentCacheSize -= new FileInfo(cacheItemPath).Length;

                // Delete the file from the cache
                File.Delete(cacheItemPath);
            }
            else
            {
                // Does the item have a sliding expiration?
                if (item.Policy.SlidingExpiration > ObjectCache.NoSlidingExpiration)
                {
                    DateTimeOffset absoluteExpiration = DateTime.Now.Add(item.Policy.SlidingExpiration);

                    // Minimize disk access for performance optimalization
                    if (absoluteExpiration - item.Policy.AbsoluteExpiration >= TimeSpan.FromSeconds(1) || absoluteExpiration < item.Policy.AbsoluteExpiration)
                    {
                        item.Policy.AbsoluteExpiration = absoluteExpiration;

                        WriteFile(cacheItemPath, item);
                    }
                }
            }

            return item.Payload;
        }

        public override CacheItem GetCacheItem(string key, string regionName = null)
        {
            object value = Get(key, regionName);

            CacheItem item = new CacheItem(key);
            item.Value = value;
            item.RegionName = regionName;

            return item;
        }

        public override long GetCount(string regionName = null)
        {
            string cachePath = regionName == null ? CacheDir : GetCachePath(regionName);

            if (!Directory.Exists(cachePath))
            {
                return 0;
            }

            return Directory.EnumerateFiles(cachePath, "*", SearchOption.AllDirectories).Count();
        }

        /// <summary>
        /// Returns an enumerator for the specified region.
        /// </summary>
        /// <param name="regionName">The region to enumerate. If NULL, will enumarate all regions.</param>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator(string regionName = null)
        {
            string cachePath = regionName == null ? CacheDir : GetCachePath(regionName);

            if (!Directory.Exists(cachePath))
            {
                return Enumerable.Empty<KeyValuePair<string, object>>().GetEnumerator();
            }

            var items = from filePath in Directory.EnumerateFiles(cachePath, "*", SearchOption.AllDirectories)
                        let fileCacheItem = ReadFile(filePath)
                        select new KeyValuePair<string, object>(fileCacheItem.Key, fileCacheItem.Payload);

            return items.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator with all cache items.
        /// </summary>
        protected override IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return GetEnumerator();
        }

        public override IDictionary<string, object> GetValues(IEnumerable<string> keys, string regionName = null)
        {
            return keys.ToDictionary(key => key, value => Get(value, regionName));
        }

        public override string Name
        {
            get { return _name; }
        }

        public override object Remove(string key, string regionName = null)
        {
            object valueToDelete = null;

            if (Contains(key, regionName))
            {
                string cacheItemPath = GetItemPath(key, regionName);

                //remove cache entry
                valueToDelete = Get(key, regionName);

                if (valueToDelete != null)
                {
                    FileInfo fi = new FileInfo(cacheItemPath);

                    CurrentCacheSize -= fi.Length;

                    fi.Delete();
                }
            }

            return valueToDelete;
        }

        public override void Set(string key, object value, CacheItemPolicy policy, string regionName = null)
        {
            Add(key, value, policy, regionName);
        }

        public override void Set(CacheItem item, CacheItemPolicy policy)
        {
            Add(item, policy);
        }

        public override void Set(string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)
        {
            Add(key, value, absoluteExpiration, regionName);
        }

        public override object this[string key]
        {
            get
            {
                return Get(key, DefaultRegion);
            }
            set
            {
                Set(key, value, DefaultPolicy, DefaultRegion);
            }
        }

        #endregion
    }
}
