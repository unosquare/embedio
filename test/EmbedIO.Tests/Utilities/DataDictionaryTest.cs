using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EmbedIO.Tests.TestObjects;
using EmbedIO.Utilities;
using NUnit.Framework;

namespace EmbedIO.Tests.Utilities
{
    public class DataDictionaryTest
    {
        [Test]
        public void DefaultConstructor_Succeeds()
        {
            var dict = new DataDictionary<string, string>();
            Assert.AreEqual(0, dict.Count, "Newly-created collection is empty.");
        }

        [Test]
        public void ConstructorWithNullCollection_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new DataDictionary<string, string>((IEnumerable<KeyValuePair<string, string>>)null).Void(),
                "Null collection causes exception.");
        }

        [Test]
        public void ConstructorWithCollection_Succeeds()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
            };
            var dict = new DataDictionary<string, string>(data);
            Assert.AreEqual(3, dict.Count, "Data have been copied.");
        }

        [Test]
        public void ConstructorWithCollection_DoesNotCopyNullValues()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", null },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            Assert.AreEqual(3, dict.Count, "Only non-null values have been copied.");
            Assert.IsFalse(dict.TryGetValue("three", out _), "Key with null value has not been copied.");
        }

        [Test]
        public void ConstructorWithNullComparer_Succeeds()
        {
            Assert.DoesNotThrow(
                () => new DataDictionary<string, string>((IEqualityComparer<string>)null).Void(),
                "Constructor with null comparer succeeds.");
        }

        [Test]
        public void ConstructorWithComparer_AppliesComparer()
        {
            var dict = new DataDictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            dict.TryAdd("one", "uno");
            Assert.IsTrue(dict.TryGetValue("ONE", out _), "Uses the given comparer.");
        }

        [Test]
        public void ConstructorWithCollectionAndComparer_Succeeds()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
            };
            var dict = new DataDictionary<string, string>(data, StringComparer.InvariantCultureIgnoreCase);
            Assert.AreEqual(1, dict.Count, "Data have been copied.");
            Assert.IsTrue(dict.TryGetValue("ONE", out _), "Uses the given comparer.");
        }

        [Test]
        public void ConstructorWithCollectionAndComparer_DoesNotCopyNullValues()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", null },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data, StringComparer.InvariantCultureIgnoreCase);
            Assert.AreEqual(3, dict.Count, "Only non-null values have been copied.");
            Assert.IsFalse(dict.TryGetValue("three", out _), "Key with null value has not been copied.");
            Assert.IsTrue(dict.TryGetValue("ONE", out _), "Uses the given comparer.");
        }

        [Test]
        public void ConstructorWithNullCollectionAndComparer_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new DataDictionary<string, string>(null, StringComparer.Ordinal).Void(),
                "Null collection causes exception.");
        }

        [Test]
        public void ConstructorWithNegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DataDictionary<string, string>(-1).Void(),
                "Negative capacity causes exception.");
        }

        [Test]
        public void ConstructorWithCapacity_Succeeds()
        {
            Assert.DoesNotThrow(
                () => new DataDictionary<string, string>(5).Void(),
                "Constructor with capacity succeeds.");
        }

        [Test]
        public void ConstructorWithCapacityCollectionAndComparer_Succeeds()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
            };
            var dict = new DataDictionary<string, string>(12, data, StringComparer.InvariantCultureIgnoreCase);
            Assert.AreEqual(1, dict.Count);
            Assert.IsFalse(dict.TryAdd("ONE", "UNO"));
        }

        [Test]
        public void ConstructorWithCapacityCollectionAndComparer_DoesNotCopyNullValues()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", null },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(10, data, StringComparer.InvariantCultureIgnoreCase);
            Assert.AreEqual(3, dict.Count, "Only non-null values have been copied.");
            Assert.IsFalse(dict.TryGetValue("three", out _), "Key with null value has not been copied.");
            Assert.IsTrue(dict.TryGetValue("ONE", out _), "Uses the given comparer.");
        }

        [Test]
        public void ConstructorWithNegativeCapacityCollectionAndComparer_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DataDictionary<string, string>(-1, new Dictionary<string, string>(), StringComparer.Ordinal).Void(),
                "Negative capacity causes exception.");
        }

        [Test]
        public void ConstructorWithCapacityNullCollectionAndComparer_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new DataDictionary<string, string>(10, null, StringComparer.Ordinal).Void(),
                "Null collection causes exception.");
        }

        [Test]
        public void Count_WhenEmpty_IsZero()
        {
            var dict = new DataDictionary<string, string>();
            Assert.AreEqual(0, dict.Count, "Count of an empty collection is zero.");
        }

        [Test]
        public void Count_AfterAdd_HasCorrectValue()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
            };
            var dict = new DataDictionary<string, string>(data);
            Assert.AreEqual(1, dict.Count, "Count of a collection with one item is 1.");
        }

        [Test]
        public void Count_AfterRemove_HasCorrectValue()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
            };
            var dict = new DataDictionary<string, string>(data);
            dict.TryRemove("two", out _);
            Assert.AreEqual(1, dict.Count, "Count after adding two items and removing one is 1.");
        }

        [Test]
        public void IsEmpty_WhenEmpty_IsTrue()
        {
            var dict = new DataDictionary<string, string>();
            Assert.IsTrue(dict.IsEmpty, "IsEmpty of an empty collection is true.");
        }

        [Test]
        public void IsEmpty_WhenNotEmpty_IsFalse()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
            };
            var dict = new DataDictionary<string, string>(data);
            Assert.IsFalse(dict.IsEmpty, "ISEmpty of a non-empty collection is false.");
        }

        [Test]
        public void Keys_ContainsAllKeys()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            CollectionAssert.AreEqual(data.Keys, dict.Keys, StringComparer.Ordinal, "Keys collection contains all keys.");
        }

        [Test]
        public void Values_ContainsAllValues()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            CollectionAssert.AreEqual(data.Values, dict.Values, StringComparer.Ordinal, "Values collection contains all values.");
        }

        [Test]
        public void GetItem_WithNullKey_ThrowsArgumentNullException()
        {
            var dict = new DataDictionary<string, string>();
            Assert.Throws<ArgumentNullException>(
                () => dict[null].Void(),
                "Null key causes exception.");
        }

        [Test]
        public void GetItem_WithExistingKey_ReturnsCorrectValue()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            Assert.AreEqual("tres", dict["three"], "Got the correct item.");
        }

        [Test]
        public void GetItem_WithNonExistingKey_ReturnsNull()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            Assert.IsNull(dict["five"], "Got null for non-existing key.");
        }

        [Test]
        public void SetItem_WithNullKey_ThrowsArgumentNullException()
        {
            var dict = new DataDictionary<string, string>();
            Assert.Throws<ArgumentNullException>(
                () => dict[null] = "algo",
                "Null key causes exception.");
        }

        [Test]
        public void SetItem_WithNonExistingKeyAndNonNullValue_AddsKeyAndValue()
        {
            var dict = new DataDictionary<string, string> {
                ["one"] = "uno",
            };
            Assert.AreEqual("uno", dict["one"], "Item was correctly set.");
        }

        [Test]
        public void SetItem_WithNonExistingKeyAndNullValue_DoesNothing()
        {
            var dict = new DataDictionary<string, string> {
                ["one"] = null,
            };
            Assert.IsTrue(dict.IsEmpty, "Key with null value was not added.");
        }

        [Test]
        public void SetItem_WithExistingKeyAndNonNullValue_ReplacesValue()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            dict["two"] = "DOS";
            Assert.AreEqual("DOS", dict["two"], "Value was replaced.");
        }

        [Test]
        public void SetItem_WithExistingKeyAndNullValue_RemovesKey()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            dict["two"] = null;
            Assert.IsFalse(dict.TryGetValue("two", out _), "Key was removed.");
        }

        [Test]
        public void Clear_RemovesAllData()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            dict.Clear();
            Assert.IsTrue(dict.IsEmpty, "Collection is empty after calling Clear.");
        }

        [Test]
        public void ContainsKey_WithExistingKey_ReturnsTrue()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            Assert.IsTrue(dict.ContainsKey("two"), "ContainsKey with existing key returned true.");
        }

        [Test]
        public void ContainsKey_WithNonExistingKey_ReturnsFalse()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            Assert.IsFalse(dict.ContainsKey("five"), "ContainsKey with non-existing key returned false.");
        }

        [Test]
        public void GetOrAdd_WithExistingKey_ReturnsExistingValue()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            Assert.AreEqual("uno", dict.GetOrAdd("one", "otro"), "GetOrAdd returned existing value.");
        }

        [Test]
        public void GetOrAdd_WithNonExistingKeyAndNonNullValue_AddsAndReturnsValue()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            Assert.AreEqual("cinco", dict.GetOrAdd("five", "cinco"), "GetOrAdd with non-existing key returned given value.");
            Assert.AreEqual("cinco", dict["five"], "GetOrAdd added given key and value.");
        }

        [Test]
        public void GetOrAdd_WithNonExistingKeyAndNullValue_ReturnsNull()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            Assert.AreEqual(null, dict.GetOrAdd("five", null), "GetOrAdd with non-existing key and null value returned null.");
            Assert.AreEqual(4, dict.Count, "GetOrAdd with non-existing key and null value did not add the key.");
        }

        [Test]
        public void Remove_WithNullKey_ThrowsArgumentNullException()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            Assert.Throws<ArgumentNullException>(
                () => dict.Remove(null),
                "Null key causes exception.");
        }

        [Test]
        public void Remove_WithExistingKey_RemovesKeyAndReturnsTrue()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            Assert.IsTrue(dict.Remove("one"), "Remove with existing key returned true.");
            Assert.AreEqual(3, dict.Count, "Remove with existing key removed the key.");
        }

        [Test]
        public void Remove_WithNonExistingKey_ReturnsFalse()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            Assert.IsFalse(dict.Remove("five"), "Remove with non-existing key returned false.");
            Assert.AreEqual(4, dict.Count, "Remove with non-existing key did not remove the key.");
        }

        [Test]
        public void TryAdd_WithNullKey_ThrowsArgumentNullException()
        {
            var dict = new DataDictionary<string, string>();
            Assert.Throws<ArgumentNullException>(
                () => dict.TryAdd(null, "algo"),
                "Null key causes exception.");
        }

        [Test]
        public void TryAdd_WithExistingKey_ReturnsFalse()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            Assert.IsFalse(dict.TryAdd("one", "otro"), "TryAdd with existing key returned false.");
            Assert.AreEqual(4, dict.Count, "TryAdd with existing key did not add any key,");
            Assert.AreEqual("uno", dict["one"], "TryAdd with existing key did not change existing value.");
        }

        [Test]
        public void TryAdd_WithNonExistingKeyAndNonNullValue_AddsValueAndReturnsTrue()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            Assert.IsTrue(dict.TryAdd("five", "cinco"), "TryAdd with non-existing key and non-null value returned true.");
            Assert.AreEqual(5, dict.Count, "TryAdd with non-existing key and non-null value added a key.");
            Assert.AreEqual("cinco", dict["five"], "TryAdd with existing key and non-null value added the given key and value.");
        }

        [Test]
        public void TryAdd_WithNonExistingKeyAndNullValue_ReturnsTrue()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            Assert.IsTrue(dict.TryAdd("five", null), "TryAdd with non-existing key and null value returned true.");
            Assert.AreEqual(4, dict.Count, "TryAdd with non-existing key and null value did not add key.");
        }

        [Test]
        public void TryGetValue_WithNullKey_ThrowsArgumentNullException()
        {
            var dict = new DataDictionary<string, string>();
            Assert.Throws<ArgumentNullException>(
                () => dict.TryGetValue(null, out _),
                "Null key causes exception.");
        }

        [Test]
        public void TryGetValue_WithExistingKey_SetsValueAndReturnsTrue()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            Assert.IsTrue(dict.TryGetValue("two", out var value), "TryGetValue with existing key returned true.");
            Assert.AreEqual("dos", value, "TryGetValue with existing key yielded existing value.");
        }

        [Test]
        public void TryGetValue_WithNonExistingKey_ReturnsFalse()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            Assert.IsFalse(dict.TryGetValue("five", out _), "TryGetValue with non-existing key returned false.");
        }

        [Test]
        public void TryRemove_WithNullKey_ThrowsArgumentNullException()
        {
            var dict = new DataDictionary<string, string>();
            Assert.Throws<ArgumentNullException>(
                () => dict.Remove(null),
                "Null key causes exception.");
        }

        [Test]
        public void TryRemove_WithExistingKey_RemovesValueAndReturnsTrue()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            Assert.IsTrue(dict.TryRemove("two", out var value), "TryRemove with existing key returned true.");
            Assert.AreEqual("dos", value, "TryRemove with existing key yielded removed value.");
            Assert.AreEqual(null, dict["two"], "TryRemove with existing key removed key.");
        }

        [Test]
        public void TryRemove_WithNonExistingKey_ReturnsFalse()
        {
            var dict = new DataDictionary<string, string>();
            Assert.IsFalse(dict.TryRemove("one", out _), "TryRemove with non-existing key returned false.");
        }

        [Test]
        public void TryUpdate_WithNullKey_ThrowsArgumentNullException()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            Assert.Throws<ArgumentNullException>(
                () => dict.TryUpdate(null, "otro", "uno"),
                "Null key causes exception.");
        }

        [Test]
        public void TryUpdate_WithNonExistingKey_ReturnsFalse()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            Assert.IsFalse(
                dict.TryUpdate("other", "otro", "uno"),
                "TryUpdate with non-existing key returned false.");
        }

        [Test]
        public void TryUpdate_WithExistingKeyAndNonMatchingComparisonValue_ReturnsFalse()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            Assert.IsFalse(
                dict.TryUpdate("one", "otro", "unooooo"),
                "TryUpdate with existing key and non-matching comparisonValue returned false.");
        }

        [Test]
        public void TryUpdate_WithExistingKeyAndMatchingComparisonValue_UpdatesValueAndReturnsTrue()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            Assert.IsTrue(
                dict.TryUpdate("one", "otro", "uno"),
                "TryUpdate with existing key and matching comparisonValue returned true.");
            Assert.AreEqual("otro", dict["one"], "TryUpdate with existing key and matching comparisonValue updated value.");
        }

        [Test]
        public void IDictionary_Add_WithNullKey_ThrowsArgumentNullException()
        {
            IDictionary<string, string> dict = new DataDictionary<string, string>();
            Assert.Throws<ArgumentNullException>(
                () => dict.Add(null, "algo"),
                "IDictionary<,>.Add with null key threw ArgumentNullException.");
        }

        [Test]
        public void IDictionary_Add_WithExistingKey_ThrowsArgumentException()
        {
            IDictionary<string, string> dict = new DataDictionary<string, string> {
                ["one"] = "uno",
            };
            Assert.Throws<ArgumentException>(
                () => dict.Add("one", "otro"),
                "IDictionary<,>.Add with existing key threw ArgumentException.");
        }

        [Test]
        public void IDictionary_Add_WithNonExistingKeyAndNonNullValue_AddsKeyAndValue()
        {
            IDictionary<string, string> dict = new DataDictionary<string, string>();
            dict.Add("one", "uno");
            Assert.AreEqual(
                "uno",
                dict["one"],
                "IDictionary<,>.Add with non-existing key and non-null value added given key and value.");
        }

        [Test]
        public void IDictionary_Add_WithNonExistingKeyAndNullValue_DoesNotAddKey()
        {
            IDictionary<string, string> dict = new DataDictionary<string, string>();
            dict.Add("one", null);
            Assert.IsFalse(
                dict.TryGetValue("one", out _),
                "IDictionary<,>.Add with non-existing key and null value did not add key.");
        }

        [Test]
        public void IReadOnlyDictionary_Keys_ContainsAllKeys()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            IReadOnlyDictionary<string, string> dict = new DataDictionary<string, string>(data);
            CollectionAssert.AreEqual(
                data.Keys,
                dict.Keys,
                StringComparer.Ordinal,
                "IReadOnlyDictionary<,>.Keys collection contains all keys.");
        }

        [Test]
        public void IReadOnlyDictionary_Values_ContainsAllValues()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            IReadOnlyDictionary<string, string> dict = new DataDictionary<string, string>(data);
            CollectionAssert.AreEqual(
                data.Values,
                dict.Values,
                StringComparer.Ordinal,
                "IReadOnlyDictionary<,>.Values collection contains all values.");
        }

        [Test]
        public void ICollection_IsReadOnly_ReturnsFalse()
        {
            ICollection<KeyValuePair<string, string>> dict = new DataDictionary<string, string>();
            Assert.IsFalse(dict.IsReadOnly, "ICollection<KeyValuePair<,>>.IsReadOnly returned false.");
        }

        [Test]
        public void ICollection_Add_WithNullKey_ThrowsArgumentException()
        {
            ICollection<KeyValuePair<string, string>> dict = new DataDictionary<string, string>();
            Assert.Throws<ArgumentException>(
                () => dict.Add(new KeyValuePair<string, string>(null, "algo")),
                "ICollection<KeyValuePair<,>>.Add with null key threw ArgumentException.");
        }

        [Test]
        public void ICollection_Add_WithExistingKey_ThrowsArgumentException()
        {
            ICollection<KeyValuePair<string, string>> dict = new DataDictionary<string, string> {
                ["one"] = "uno",
            };
            Assert.Throws<ArgumentException>(
                () => dict.Add(new KeyValuePair<string, string>("one", "otro")),
                "ICollection<KeyValuePair<,>>.Add with existing key threw ArgumentException.");
        }

        [Test]
        public void ICollection_Add_WithNonExistingKeyAndNonNullValue_AddsKeyAndValue()
        {
            var dict = new DataDictionary<string, string>();
            ICollection<KeyValuePair<string, string>> collection = dict;
            collection.Add(new KeyValuePair<string, string>("one", "uno"));
            Assert.AreEqual(
                "uno",
                dict["one"],
                "ICollection<KeyValuePair<,>>.Add with non-existing key and non-null value added given key and value.");
        }

        [Test]
        public void ICollection_Add_WithNonExistingKeyAndNullValue_DoesNotAddKey()
        {
            var dict = new DataDictionary<string, string>();
            ICollection<KeyValuePair<string, string>> collection = dict;
            collection.Add(new KeyValuePair<string, string>("one", null));
            Assert.IsFalse(
                dict.TryGetValue("one", out _),
                "ICollection<KeyValuePair<,>>.Add with non-existing key and null value did not add key.");
        }

        [Test]
        public void ICollection_Contains_WithExistingKeyValuePair_ReturnsTrue()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            ICollection<KeyValuePair<string, string>> collection = dict;
            var kvp = new KeyValuePair<string, string>("three", "tres");
            Assert.IsTrue(
                collection.Contains(kvp),
                "ICollection<KeyValuePair<,>>.Contains with existing key / value pair returned true.");
        }

        [Test]
        public void ICollection_Contains_WithNonExistingKeyValuePair_ReturnsFalse()
        {
            var dict = new DataDictionary<string, string> {
                ["one"] = "uno",
                ["two"] = "dos",
                ["three"] = "tres",
                ["four"] = "cuatro",
            };
            ICollection<KeyValuePair<string, string>> collection = dict;
            var kvp = new KeyValuePair<string, string>("three", "otros");
            Assert.IsFalse(
                collection.Contains(kvp),
                "ICollection<KeyValuePair<,>>.Contains with non-existing key / value pair returned false.");
        }

        [Test]
        public void ICollection_CopyTo_WithNullArray_ThrowsArgumentNullException()
        {
            var dict = new DataDictionary<string, string>();
            ICollection<KeyValuePair<string, string>> collection = dict;
            Assert.Throws<ArgumentNullException>(
                () => collection.CopyTo(null, 0),
                "ICollection<KeyValuePair<,>>.CopyTo with null array threw ArgumentNullException.");
        }

        [Test]
        public void ICollection_CopyTo_WithNegativeIndex_ThrowsArgumentOutOfRangeException()
        {
            var dict = new DataDictionary<string, string>();
            ICollection<KeyValuePair<string, string>> collection = dict;
            var a = new KeyValuePair<string, string>[1];
            Assert.Throws<ArgumentOutOfRangeException>(
                () => collection.CopyTo(a, -1),
                "ICollection<KeyValuePair<,>>.CopyTo with negative index threw ArgumentOutOfRangeException.");
        }

        [Test]
        public void ICollection_CopyTo_WithNoRoomInArray_ThrowsArgumentException()
        {
            var dict = new DataDictionary<string, string> {
                ["one"] = "uno",
                ["two"] = "dos",
                ["three"] = "tres",
                ["four"] = "cuatro",
            };
            ICollection<KeyValuePair<string, string>> collection = dict;
            var a = new KeyValuePair<string, string>[4];
            Assert.Throws<ArgumentException>(
                () => collection.CopyTo(a, 2),
                "ICollection<KeyValuePair<,>>.CopyTo with not enough room in array threw ArgumentException.");
        }

        [Test]
        public void ICollection_Remove_WithNullKey_ThrowsArgumentException()
        {
            ICollection<KeyValuePair<string, string>> dict = new DataDictionary<string, string>();
            Assert.Throws<ArgumentException>(
                () => dict.Remove(new KeyValuePair<string, string>(null, "algo")),
                "ICollection<KeyValuePair<,>>.Remove with null key threw ArgumentException.");
        }

        [Test]
        public void ICollection_Remove_WithExistingKeyValuePair_RemovesKeyAndReturnsTrue()
        {
            var dict = new DataDictionary<string, string> {
                ["one"] = "uno",
                ["two"] = "dos",
                ["three"] = "tres",
                ["four"] = "cuatro",
            };
            ICollection<KeyValuePair<string, string>> collection = dict;
            var kvp = new KeyValuePair<string, string>("two", "dos");
            Assert.IsTrue(
                collection.Remove(kvp),
                "ICollection<KeyValuePair<,>>.Remove with existing key / value pair returned true.");
            Assert.AreEqual(null, dict["two"], "ICollection<KeyValuePair<,>>.Remove with existing key / value pair removed key.");
        }

        [Test]
        public void ICollection_Remove_WithNonExistingKey_ReturnsFalse()
        {
            var dict = new DataDictionary<string, string>();
            ICollection<KeyValuePair<string, string>> collection = dict;
            var kvp = new KeyValuePair<string, string>("one", "algo");
            Assert.IsFalse(
                collection.Remove(kvp),
                "ICollection<KeyValuePair<,>>.Remove with non-existing key / value pair returned false.");
        }

        [Test]
        public void IEnumerable_GetEnumerator_EnumeratesAllData()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            IEnumerable<KeyValuePair<string, string>> enumerable = dict;

            // Neither CollectionAssert nor LINQ give certainty
            // about the actual GetEnumerator called (whether generic or non-generic).
            // So, for the sake of code coverage, let's use it directly.
            var list = new List<KeyValuePair<string, string>>();
            using (var enumerator = enumerable.GetEnumerator())
            {
                enumerator.Reset();
                while (enumerator.MoveNext())
                    list.Add(enumerator.Current);
            }

            CollectionAssert.AreEqual(
                data,
                list, 
                "IEnumerable<KeyValuePair<,>>.GetEnumerator returned a valid enumerator.");
        }

        [Test]
        public void NonGenericIEnumerable_GetEnumerator_EnumeratesAllData()
        {
            var data = new Dictionary<string, string> {
                { "one", "uno" },
                { "two", "dos" },
                { "three", "tres" },
                { "four", "cuatro" },
            };
            var dict = new DataDictionary<string, string>(data);
            IEnumerable enumerable = dict;

            // Neither CollectionAssert nor LINQ give certainty
            // about the actual GetEnumerator called (whether generic or non-generic).
            // So, for the sake of code coverage, let's use it directly.
            var list = new List<object>();
            var enumerator = enumerable.GetEnumerator();
            enumerator.Reset();
            while (enumerator.MoveNext())
                list.Add(enumerator.Current);

            CollectionAssert.AreEqual(
                data,
                list,
                "IEnumerable.GetEnumerator returned a valid enumerator.");
        }
    }
}