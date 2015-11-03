/*
Copyright 2015 mabakay, based on FileCache (http://fc.codeplex.com)

This file is part of nFileCache (https://github.com/mabakay/nFileCache).

nFileCache is distributed under the Microsoft Public License (Ms-PL).
Consult "LICENSE.txt" included in this package for the complete Ms-PL license.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace nFileCache.UnitTests
{
    /// <summary>
    ///This is a test class for NFileCacheTest and is intended
    ///to contain all NFileCacheTest Unit Tests
    ///</summary>
    [TestClass()]
    public class NFileCacheTest
    {
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        [TestMethod]
        public void AbsoluteExpirationTest()
        {
            NFileCache target = new NFileCache();
            CacheItemPolicy policy = new CacheItemPolicy();

            // Add an item and have it expire yesterday
            policy.AbsoluteExpiration = DateTime.Now.AddDays(-1);
            target.Set("test", "test", policy);

            // Then try to access the item
            object result = target.Get("test");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void SlidingExpirationTest()
        {
            NFileCache target = new NFileCache();
            CacheItemPolicy policy = new CacheItemPolicy();

            // Add an item and have it expire 500 ms from now
            policy.SlidingExpiration = new TimeSpan(0, 0, 0, 0, 500);
            target.Set("test", "test", policy);

            // Sleep for 200
            Thread.Sleep(200);

            // Then try to access the item
            object result = target.Get("test");
            Assert.AreEqual("test", result);

            // Sleep for another 200
            Thread.Sleep(200);

            // Then try to access the item
            result = target.Get("test");
            Assert.AreEqual("test", result);

            // Then sleep for more than 500 ms. Should be gone
            Thread.Sleep(600);
            result = target.Get("test");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void PolicySaveTest()
        {
            NFileCache target = new NFileCache();
            CacheItemPolicy policy = new CacheItemPolicy();
            policy.SlidingExpiration = new TimeSpan(1, 0, 0, 0, 0);
            target.Set("sliding", "test", policy);

            CacheItemPolicy returnPolicy = target.GetPolicy("sliding");
            Assert.AreEqual(policy.SlidingExpiration, returnPolicy.SlidingExpiration);

            policy = new CacheItemPolicy();
            policy.AbsoluteExpiration = DateTimeOffset.Now.AddDays(1);
            target.Set("absolute", "test", policy.AbsoluteExpiration);

            returnPolicy = target.GetPolicy("absolute");
            Assert.AreEqual(policy.AbsoluteExpiration, returnPolicy.AbsoluteExpiration);
        }

        [TestMethod]
        public void CustomObjectSaveTest()
        {
            NFileCache target = new NFileCache();

            // Create custom object
            CustomObjB custom = new CustomObjB()
            {
                Num = 5,
                Obj = new CustomObjA()
                {
                    Name = "test"
                }
            };

            CacheItem item = new CacheItem("foo")
            {
                Value = custom,
                RegionName = "foobar"
            };

            // Set it
            target.Set(item, new CacheItemPolicy());

            // Now get it back
            CacheItem fromCache = target.GetCacheItem("foo", "foobar");

            // Pulling twice increases code coverage
            fromCache = target.GetCacheItem("foo", "foobar");
            custom = fromCache.Value as CustomObjB;

            Assert.IsNotNull(custom);
            Assert.IsNotNull(custom.Obj);
            Assert.AreEqual(custom.Num, 5);
            Assert.AreEqual(custom.Obj.Name, "test");
        }

        [TestMethod]
        public void CacheSizeTest()
        {
            NFileCache target = new NFileCache();

            // Flush cache to make sure we're starting fresh
            target.Flush();

            target["foo"] = "bar";
            target["foo"] = "foobar";

            long cacheSize = target.GetCacheSize("bar");
            Assert.AreEqual(0, cacheSize);

            cacheSize = target.GetCacheSize();
            Assert.IsTrue(cacheSize > 0);

            target.Remove("foo");
            cacheSize = target.GetCacheSize();
            Assert.AreEqual(0, cacheSize);
        }

        [TestMethod]
        public void MaxCacheSizeTest()
        {
            NFileCache target = new NFileCache(true);
            target.MaxCacheSize = 0;
            bool isEventCalled = false;

            target.MaxCacheSizeReached += (sender, args) => isEventCalled = true;
            target["foo"] = "bar";

            Assert.IsTrue(isEventCalled);
        }

        [TestMethod]
        public void FlushTest()
        {
            NFileCache target = new NFileCache();
            target["foo"] = "bar";

            // Attempt flush
            target.Flush(DateTime.Now.AddDays(1));

            // Check to see if size ends up at zero (expected result)
            Assert.AreEqual(0L, target.GetCacheSize());
        }

        [TestMethod]
        public void RemoveTest()
        {
            NFileCache target = new NFileCache();
            target.Set("test", "test", DateTimeOffset.Now.AddDays(3));
            object result = target.Get("test");
            Assert.AreEqual("test", result);

            // Check file system to be sure item was created
            string fileName = "test".GetHashCode().ToString("0000");
            string cachePath = Path.Combine(target.CacheDir, "_", fileName.Substring(0, 2), fileName.Substring(2));
            Assert.IsTrue(File.Exists(cachePath));

            // Now delete
            target.Remove("test");
            result = target["test"];
            Assert.IsNull(result);

            // Check file system to be sure item was removed
            Assert.IsFalse(File.Exists(cachePath));
        }

        [TestMethod]
        public void TestCount()
        {
            NFileCache target = new NFileCache("testCount");

            // Flush cache to make sure we're starting fresh
            target.Flush();

            target["test"] = "test";
            target["test"] = "bar";

            Assert.AreEqual(0, target.GetCount("bar"));

            object result = target.Get("test");
            Assert.AreEqual("bar", result);
            Assert.AreEqual(1, target.GetCount());
        }

        [TestMethod]
        public void DefaultRegionTest()
        {
            NFileCache cacheWithDefaultRegion = new NFileCache();
            cacheWithDefaultRegion.DefaultRegion = "foo";

            NFileCache defaultCache = new NFileCache();
            cacheWithDefaultRegion["foo"] = "bar";

            string pull = defaultCache.Get("foo", "foo") as string;
            Assert.AreEqual("bar", pull);
        }

        [TestMethod]
        [ExpectedException(typeof(IOException))]
        public void AccessTimeoutTest()
        {
            NFileCache target = new NFileCache();
            target.AccessTimeout = new TimeSpan(1);
            target["primer"] = 0;

            string fileName = "foo".GetHashCode().ToString("0000");
            string filePath = Path.Combine(target.CacheDir, "_", fileName.Substring(0, 2), fileName.Substring(2));

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            using (FileStream stream = File.Open(filePath, FileMode.Create))
            {
                object result = target["foo"];
            }
        }

        [TestMethod]
        public void DefaultPolicyTest()
        {
            NFileCache target = new NFileCache();
            CacheItemPolicy policy = new CacheItemPolicy();
            policy.SlidingExpiration = new TimeSpan(10);
            target.DefaultPolicy = policy;

            target["foo"] = "bar";

            Thread.Sleep(15);

            object result = target["foo"];
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetEnumeratorTest()
        {
            NFileCache target = new NFileCache();

            // Flush cache to make sure we're starting fresh
            target.Flush();

            target["foo"] = 1;
            target["bar"] = 2;

            foreach (KeyValuePair<string, object> kvp in target)
            {
                Assert.IsNotNull(kvp.Key);
                Assert.IsNotNull(kvp.Value);
                Assert.AreEqual(target[kvp.Key], kvp.Value);
            }
        }

        [TestMethod]
        public void GetKeysTest()
        {
            NFileCache target = new NFileCache();

            // Flush cache to make sure we're starting fresh
            target.Flush();

            target["foo"] = 1;
            target["bar"] = 2;

            CollectionAssert.AreEqual(new[] { "bar", "foo" }, target.GetKeys().OrderBy(key => key).ToArray());
        }

        [TestMethod]
        public void GetValuesTest()
        {
            NFileCache target = new NFileCache();

            // Flush cache to make sure we're starting fresh
            target.Flush();

            target["foo"] = 1;
            target["bar"] = 2;

            CollectionAssert.AreEqual(new[] { 1, 2 }, target.GetValues(new[] { "foo", "bar" }).Select(item => item.Value).ToArray());
        }

        [TestMethod]
        public void StreamItemTest()
        {
            NFileCache target = new NFileCache();

            // Flush cache to make sure we're starting fresh
            target.Flush();

            const string poem = "Biały koń marzeń";

            target["stream"] = new MemoryStream(Encoding.UTF8.GetBytes(poem));
            string textvalue = new StreamReader((Stream)target["stream"]).ReadToEnd();

            Assert.AreEqual(textvalue, poem);
        }
    }
}
