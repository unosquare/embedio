using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EmbedIO.Tests.TestObjects;
using EmbedIO.Utilities;
using NUnit.Framework;

namespace EmbedIO.Tests.Utilities
{
    [TestFixture]
    public class ComponentCollectionTest
    {
        [Test]
        public void AfterConstruction_IsEmpty()
        {
            var collection = new ComponentCollection<int>();
            Assert.AreEqual(0, collection.Count);
        }

        [Test]
        public void AddWithName_IncreasesCount()
        {
            var collection = new ComponentCollection<int> {
                { "one", 1 },
            };

            Assert.AreEqual(1, collection.Count);
        }

        [Test]
        public void AddWithoutName_IncreasesCount()
        {
            var collection = new ComponentCollection<int> {
                { "one", 1 },
            };

            Assert.AreEqual(1, collection.Count);
        }

        [Test]
        public void AddWithoutNameViaExtension_IncreasesCount()
        {
            var collection = new ComponentCollection<int> {
                1,
            };

            Assert.AreEqual(1, collection.Count);
        }

        [Test]
        public void AddWithEmptyName_ThrowsArgumentException()
        {
            var collection = new ComponentCollection<int>();
            Assert.Throws<ArgumentException>(() => collection.Add(string.Empty, 1));
        }

        [Test]
        public void AddWithDuplicateName_ThrowsArgumentException()
        {
            var collection = new ComponentCollection<int> {
                { "one", 1 },
            };

            Assert.Throws<ArgumentException>(() => collection.Add("one", 2));
        }

        [Test]
        public void AddNullComponent_ThrowsArgumentNullException()
        {
            var collection = new ComponentCollection<string>();
            Assert.Throws<ArgumentNullException>(() => collection.Add(null));
        }

        [Test]
        public void AddDuplicateComponent_ThrowsArgumentException()
        {
            var obj = new object();
            var collection = new ComponentCollection<object> {
                { obj },
            };

            Assert.Throws<ArgumentException>(() => collection.Add(obj));
        }

        [Test]
        public void MultipleAddWithoutName_Succeeds()
        {
            var collection = new ComponentCollection<int> {
                { null, 1 },
                { null, 2 },
            };

            Assert.AreEqual(2, collection.Count);
        }

        [Test]
        public void IntIndexer_RetrievesCorrectComponent()
        {
            var collection = new ComponentCollection<int> {
                { "one", 1 },
                { "two", 2 },
            };

            Assert.AreEqual(2, collection[1]);
        }

        [Test]
        public void IntIndexer_ThrowsOnIndexOutOfRange()
        {
            var collection = new ComponentCollection<int> {
                { "one", 1 },
                { "two", 2 },
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => collection[-1].Void());
            Assert.Throws<ArgumentOutOfRangeException>(() => collection[2].Void());
        }

        [Test]
        public void StringIndexer_RetrievesCorrectComponent()
        {
            var collection = new ComponentCollection<int> {
                { "one", 1 },
                { "two", 2 },
            };

            Assert.AreEqual(1, collection["one"]);
        }

        [Test]
        public void StringIndexer_ThrowsOnNullKey()
        {
            var collection = new ComponentCollection<int> {
                { "one", 1 },
                { "two", 2 },
            };

            Assert.Throws<ArgumentNullException>(() => collection[null].Void());
        }

        [Test]
        public void StringIndexer_ThrowsOnKeyNotFound()
        {
            var collection = new ComponentCollection<int> {
                { "one", 1 },
                { "two", 2 },
            };

            Assert.Throws<KeyNotFoundException>(() => collection["three"].Void());
        }

        [Test]
        public void AddAfterLock_ThrowsInvalidOperationException()
        {
            var collection = new ComponentCollection<int>();
            collection.Lock();
            Assert.Throws<InvalidOperationException>(() => collection.Add("one", 1));
        }

        [Test]
        public void Named_ContainsAllNamedItems()
        {
            var collection = new ComponentCollection<int> {
                {"one", 1},
                {"two", 2},
                3,
            };

            Assert.AreEqual(2, collection.Named.Count);
        }

        [Test]
        public void WithSafeNames_ContainsAllItems()
        {
            var collection = new ComponentCollection<int> {
                {"one", 1},
                {"two", 2},
                3,
            };

            Assert.AreEqual(3, collection.WithSafeNames.Count);
        }

        [Test]
        public void GenericEnumerator_EnumeratesAllComponents()
        {
            var collection = new ComponentCollection<int> {
                {"one", 1},
                {"two", 2},
                3,
            };

            Assert.AreEqual(6, collection.Sum());
        }

        [Test]
        public void NonGenericEnumerator_EnumeratesAllComponents()
        {
            var collection = new ComponentCollection<int> {
                {"one", 1},
                {"two", 2},
                3,
            };

            // Don't get smart with LINQ here:
            // LINQ would see the IEnumerable<int> interface and use it,
            // but we want to cover the non-generic GetEnumerator.
            var sum = 0;
            foreach (var num in (IEnumerable) collection)
                sum += (int) num;

            Assert.AreEqual(6, sum);
        }
    }
}