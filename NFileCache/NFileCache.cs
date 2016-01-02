/*
Copyright 2015 mabakay, based on FileCache (http://fc.codeplex.com)

This file is part of nFileCache (https://github.com/mabakay/nFileCache).

nFileCache is distributed under the Microsoft Public License (Ms-PL).
Consult "LICENSE.txt" included in this package for the complete Ms-PL license.
*/

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;

[assembly: InternalsVisibleTo("NFileCache.UnitTests")]

namespace System.Runtime.Caching
{
    public sealed class NFileCache : ObjectCache
    {
        #region Consts

        private const string DefaultCacheFolderName = "cache";

        #endregion

        #region Fields

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
        /// Event that will be called when cache size was shrinked.
        /// </summary>
        public event EventHandler<FileCacheEventArgs> CacheResized;

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

            if (cacheRoot == null || !Path.IsPathRooted(cacheRoot))
            {
                CacheDir = Path.Combine(Directory.GetCurrentDirectory(), cacheRoot ?? DefaultCacheFolderName);
            }
            else
            {
                CacheDir = cacheRoot;
            }

            DefaultRegion = null;

            if (calculateCacheSize)
            {
                CurrentCacheSize = GetCacheSize();
            }

            MaxCacheSizeReached += NFileCache_MaxCacheSizeReached;
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
        /// Flushes the cache based on expiration date, filtered by optional region.
        /// </summary>
        /// <param name="minDate">Minimum date of cache item last access date.</param>
        /// <param name="regionName">The region to flush. If NULL, will flush all regions.</param>
        /// <returns>The amount removed (in bytes).</returns>
        public long Flush(DateTime minDate, string regionName = null)
        {
            long removed = 0;

            foreach (string key in GetKeys(regionName))
            {
                CacheItemPolicy policy = GetPolicy(key, regionName);

                // Did the item expire?
                if (policy.AbsoluteExpiration < minDate)
                {
                    string cacheItemPath = GetItemPath(key, regionName);

                    FileInfo fi = new FileInfo(cacheItemPath);

                    if (fi.Exists)
                    {
                        CurrentCacheSize -= fi.Length;
                        removed += fi.Length;

                        // Remove cache entry
                        fi.Delete();
                    }
                }
            }

            return removed;
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

        /// <summary>
        /// Shrinks the cache until the cache size is less than or equal to the size specified (in bytes).
        /// This is a rather expensive operation, so use with discretion.
        /// </summary>
        /// <param name="newSize">New maximum cache size.</param>
        /// <param name="regionName">The region to shrink. If NULL, will shrink all regions.</param>
        /// <returns>The new size of the cache.</returns>
        public long ShrinkCacheToSize(long newSize, string regionName = null)
        {
            long originalSize = 0, amount = 0, removed = 0;

            // if we're shrinking the whole cache, we can use the stored
            // size if it's available. If it's not available we calculate it and store
            // it for next time.
            if (regionName == null)
            {
                if (CurrentCacheSize == 0)
                {
                    CurrentCacheSize = GetCacheSize();
                }

                originalSize = CurrentCacheSize;
            }
            else
            {
                originalSize = GetCacheSize(regionName);
            }

            // Find out how much we need to get rid of
            amount = originalSize - newSize;

            // This will update CurrentCacheSize
            removed = DeleteOldestFiles(amount, regionName);

            // trigger the event
            if (CacheResized != null)
                CacheResized(this, new FileCacheEventArgs(originalSize - removed, MaxCacheSize));

            // return the final size of the cache (or region)
            return originalSize - removed;
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
                    if (AccessTimeout == TimeSpan.Zero || AccessTimeout > interval)
                    {
                        Thread.Sleep(interval);
                    }

                    // If we've waited too long, throw the original exception
                    if (AccessTimeout > TimeSpan.Zero)
                    {
                        totalTime += interval;

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
            else
            {
                var directoryPath = Path.GetDirectoryName(filePath);
                var di = new DirectoryInfo(directoryPath);

                if (!di.Exists)
                {
                    di.Create();
                }
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
        internal string GetCachePath(string regionName = null)
        {
            regionName = string.Format("{0}{1}", "_", regionName == null ? string.Empty : regionName.GetHashCode().ToString());

            return Path.Combine(CacheDir, regionName);
        }

        /// <summary>
        /// Builds cache file phisical disk file paths.
        /// </summary>
        internal string GetItemPath(string key, string regionName = null)
        {
            string directory = GetCachePath(regionName);
            string fileName = key.GetHashCode().ToString("0000");

            return Path.Combine(directory, fileName.Substring(0, 2), fileName.Substring(2));
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

        /// <summary>
        /// Delete the oldest items in the cache to shrink the cache by the specified amount (in bytes).
        /// </summary>
        /// <returns>The amount of data that was actually removed.</returns>
        private long DeleteOldestFiles(long amount, string regionName = null)
        {
            // Verify that we actually need to shrink
            if (amount <= 0)
            {
                return 0;
            }

            // Heap of all items
            var cacheReferences = new SortedSet<Tuple<DateTime, string>>(new CacheItemExpirationDateComparer());

            // Build a heap of all files in cache region
            foreach (string key in GetKeys(regionName))
            {
                // Build item reference
                string cacheItemPath = GetItemPath(key, regionName);
                CacheItemPolicy policy = GetPolicy(key, regionName);

                cacheReferences.Add(new Tuple<DateTime, string>(policy.AbsoluteExpiration.DateTime, cacheItemPath));
            }

            // Remove cache items until size requirement is met
            long removedBytes = 0;

            var enumerator = cacheReferences.GetEnumerator();
            while (removedBytes < amount && enumerator.MoveNext())
            {
                string cacheItemPath = enumerator.Current.Item2;

                // Remove oldest item
                FileInfo fi = new FileInfo(cacheItemPath);

                if (fi.Exists)
                {
                    CurrentCacheSize -= fi.Length;
                    removedBytes += fi.Length;

                    // Remove cache entry
                    fi.Delete();
                }
            }

            return removedBytes;
        }

        private void NFileCache_MaxCacheSizeReached(object sender, FileCacheEventArgs e)
        {
            // Shrink the cache to 75% of the max size
            // that way there's room for it to grow a bit
            // before we have to do this again.
            ShrinkCacheToSize((long)(MaxCacheSize * 0.75));
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

                FileInfo fi = new FileInfo(cacheItemPath);

                CurrentCacheSize -= fi.Length;

                // Delete the file from the cache
                fi.Delete();
            }
            else
            {
                // Does the item have a sliding expiration?
                if (item.Policy.SlidingExpiration > ObjectCache.NoSlidingExpiration)
                {
                    DateTimeOffset absoluteExpiration = DateTime.Now.Add(item.Policy.SlidingExpiration);
                    item.Policy.AbsoluteExpiration = absoluteExpiration;

                    WriteFile(cacheItemPath, item);
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

            return Directory.EnumerateFiles(cachePath, "*", SearchOption.AllDirectories).LongCount();
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

            string cacheItemPath = GetItemPath(key, regionName);

            FileInfo fi = new FileInfo(cacheItemPath);

            if (fi.Exists)
            {
                valueToDelete = Get(key, regionName);

                CurrentCacheSize -= fi.Length;

                // Remove cache entry
                fi.Delete();
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
