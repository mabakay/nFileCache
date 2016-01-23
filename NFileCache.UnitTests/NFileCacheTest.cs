/*
Copyright © mabakay 2015-2016, based on FileCache (http://fc.codeplex.com)

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
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace nFileCache.UnitTests
{
    /// <summary>
    ///This is a test class for NFileCacheTest and is intended
    ///to contain all NFileCacheTest Unit Tests
    ///</summary>
    [TestClass]
    public class NFileCacheTest
    {
        #region Additional test attributes

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}

        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}

        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}

        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}

        #endregion

        [TestMethod]
        public void AbsoluteExpirationTest()
        {
            NFileCache target = new NFileCache("AbsoluteExpirationTest");
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
            NFileCache target = new NFileCache("SlidingExpirationTest");
            CacheItemPolicy policy = new CacheItemPolicy();

            // Add an item and have it expire 500 ms from now
            policy.SlidingExpiration = new TimeSpan(0, 0, 0, 0, 500);
            target.Set("test", "test", policy);

            // Sleep for 200
            Thread.Sleep(200);

            // Then try to access the item
            object result = target.Get("test");
            Assert.AreEqual("test", result);

            // Sleep for another 350
            Thread.Sleep(350);

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
            NFileCache target = new NFileCache("PolicySaveTest");
            CacheItemPolicy policy = new CacheItemPolicy();
            policy.SlidingExpiration = new TimeSpan(1, 0, 0, 0, 0);
            target.Set("sliding", "test", policy);

            CacheItemPolicy returnPolicy = target.GetPolicy("sliding");
            Assert.AreEqual(policy.SlidingExpiration, returnPolicy.SlidingExpiration);

            policy = new CacheItemPolicy();
            policy.AbsoluteExpiration = DateTime.Now.AddDays(1);
            target.Set("absolute", "test", policy.AbsoluteExpiration);

            returnPolicy = target.GetPolicy("absolute");
            Assert.AreEqual(policy.AbsoluteExpiration, returnPolicy.AbsoluteExpiration);
        }

        [TestMethod]
        public void CustomObjectSaveTest()
        {
            NFileCache target = new NFileCache("CustomObjectSaveTest");

            // Create custom object
            CustomObjB custom = new CustomObjB
            {
                Num = 5,
                Obj = new CustomObjA
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
            NFileCache target = new NFileCache("CacheSizeTest");

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
            NFileCache target = new NFileCache("MaxCacheSizeTest", true);
            target.MaxCacheSize = 0;
            bool isEventCalled = false;

            target.MaxCacheSizeReached += (sender, args) => isEventCalled = true;
            target["foo"] = "bar";

            Assert.IsTrue(isEventCalled);
        }

        [TestMethod]
        public void FlushTest()
        {
            NFileCache target = new NFileCache("FlushTest");

            target.Add("foo", 1, DateTime.Now); // expires immediately
            target.Add("bar", 2, DateTime.Now.AddDays(1)); // set to expire tomorrow

            // Attempt flush
            target.Flush();

            Assert.IsNull(target["foo"]);
            Assert.IsNotNull(target["bar"]);
        }

        [TestMethod]
        public void RemoveTest()
        {
            NFileCache target = new NFileCache("RemoveTest");
            target.Set("test", "test", DateTime.Now.AddDays(3));
            object result = target.Get("test");
            Assert.AreEqual("test", result);

            // Check file system to be sure item was created
            string itemPath = target.GetItemPath("test");
            Assert.IsTrue(File.Exists(itemPath));

            // Now delete
            target.Remove("test");
            result = target["test"];
            Assert.IsNull(result);

            // Check file system to be sure item was removed
            Assert.IsFalse(File.Exists(itemPath));
        }

        [TestMethod]
        public void ShrinkCacheTest()
        {
            NFileCache target = new NFileCache("ShrinkCacheTest");

            // Test empty case
            Assert.AreEqual(0, target.Trim(0));

            // Insert 4 items, and keep track of their size
            target.Add("item1", "test1", DateTime.Now.AddDays(1));
            long size1 = target.GetCacheSize();

            target.Add("item2", "test22", DateTime.Now);
            long size2 = target.GetCacheSize() - size1;

            target.Add("item3", "test333", DateTime.Now.AddSeconds(10));
            long size3 = target.GetCacheSize() - size1 - size2;

            target.Add("item4", "test4444", DateTime.Now.AddDays(-1));
            long size4 = target.GetCacheSize() - size1 - size2 - size3;

            // Shrink to the size of the first 3 items (should remove item4 because it's the oldest, keeping the other 3)
            long newSize = target.Trim(size1 + size2 + size3);
            Assert.AreEqual(size1 + size2 + size3, newSize);

            // Shrink to just smaller than two items (should keep just item1, delete item2 and item3)
            newSize = target.Trim(size1 + size3 - 1);
            Assert.AreEqual(size1, newSize);

            // Shrink to size 1 (should delete everything)
            newSize = target.Trim(1);
            Assert.AreEqual(0, newSize);
        }

        [TestMethod]
        public void AutoShrinkTest()
        {
            NFileCache target = new NFileCache("AutoShrinkTest");
            target.DefaultPolicy = new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(1D) };

            target.MaxCacheSize = 20000;
            target.CacheResized += (object sender, FileCacheEventArgs args) =>
            {
                Assert.IsNotNull(target["foo10"]);
                Assert.IsNotNull(target["foo40"]);
            };

            for (int i = 0; i < 100; i++)
            {
                target["foo" + i] = "bar";

                // Test to make sure it leaves items that have been recently accessed.
                if (i % 5 == 0 && i != 0)
                {
                    var foo10 = target.Get("foo10");
                    var foo40 = target.Get("foo40");
                }
            }
        }

        [TestMethod]
        public void TestCount()
        {
            NFileCache target = new NFileCache("TestCount");

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
            NFileCache cacheWithDefaultRegion = new NFileCache("DefaultRegionTest");
            cacheWithDefaultRegion.DefaultRegion = "foo";

            NFileCache defaultCache = new NFileCache("DefaultRegionTest");
            cacheWithDefaultRegion["foo"] = "bar";

            string pull = defaultCache.Get("foo", "foo") as string;
            Assert.AreEqual("bar", pull);
        }

        [TestMethod]
        [ExpectedException(typeof(IOException))]
        public void AccessTimeoutTest()
        {
            NFileCache target = new NFileCache("AccessTimeoutTest");
            target.AccessTimeout = new TimeSpan(1);
            target["foo"] = 0;

            // Lock actual file system record
            string itemPath = target.GetItemPath("foo");
            using (FileStream stream = File.Open(itemPath, FileMode.Create))
            {
                object result = target["foo"];
            }
        }

        [TestMethod]
        public void DefaultPolicyTest()
        {
            NFileCache target = new NFileCache("DefaultPolicyTest");
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
            NFileCache target = new NFileCache("GetEnumeratorTest");

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
            NFileCache target = new NFileCache("GetKeysTest");

            target["foo"] = 1;
            target["bar"] = 2;

            CollectionAssert.AreEqual(new[] { "bar", "foo" }, target.GetKeys().OrderBy(key => key).ToArray());
        }

        [TestMethod]
        public void GetValuesTest()
        {
            NFileCache target = new NFileCache("GetValuesTest");

            target["foo"] = 1;
            target["bar"] = 2;

            CollectionAssert.AreEqual(new[] { 1, 2 }, target.GetValues(new[] { "foo", "bar" }).Select(item => item.Value).ToArray());
        }

        [TestMethod]
        public void StreamItemTest()
        {
            NFileCache target = new NFileCache("StreamItemTest");

            const string poem = "Biały koń marzeń";

            target["stream"] = new MemoryStream(Encoding.UTF8.GetBytes(poem));
            string textvalue = new StreamReader((Stream)target["stream"]).ReadToEnd();

            Assert.AreEqual(textvalue, poem);
        }

        [TestMethod]
        public void AnonymousTypeItemTest()
        {
            NFileCache target = new NFileCache("AnonymousTypeItemTest");

            const string poem = "Biały koń marzeń";

            target["anonymousItem"] = new { Id = 11, Poem = poem };
            var anonymousItem = (dynamic)target["anonymousItem"];

            Assert.AreEqual(anonymousItem.Id, 11);
            Assert.AreEqual(anonymousItem.Poem, poem);
        }

        [TestMethod]
        public void MultiThreadTest()
        {
            NFileCache target = new NFileCache("MultiThreadTest");

            const int threadCount = 4;
            var rnd = new Random();

            Parallel.For(0, threadCount, i =>
            {
                for (int j = 0; j < 500; j++)
                {
                    target["foo" + rnd.Next(10)] = "bar" + i;
                }
            });
        }
    }
}
