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
        private const string InitialExistingKey = "one";
        private const string InitialExistingKeyWithDifferentCasing = "ONE";
        private const string InitialExistingValue = "uno";
        private const string DifferentValueForInitialExistingKey = "otro";

        private const string InitialKeyWithNullValue = "three";

        private const string InitialNonExistingKey = "five";
        private const string InitialNonExistingValue = "cinco";

        private const string SomeValue = "algo";

        private static readonly IReadOnlyDictionary<string, string> InitialData = new Dictionary<string, string> {
            { "one", "uno" },
            { "two", "dos" },
            { "three", null },
            { "four", "cuatro" },
        };

        private static readonly IReadOnlyList<KeyValuePair<string, string>> InitialImportedData = InitialData.Where(pair => pair.Value != null).ToArray();
        private static readonly int InitialCount = InitialImportedData.Count;
        private static readonly IEnumerable<string> InitialKeys = InitialImportedData.Select(pair => pair.Key);
        private static readonly IEnumerable<string> InitialValues = InitialImportedData.Select(pair => pair.Value);

        [Test]
        public void DefaultConstructor_Succeeds()
        {
            Assert.DoesNotThrow(
                () => new DataDictionary<string, string>().Void());
        }

        [Test]
        public void DefaultConstructor_CreatesEmptyDictionary()
        {
            var dict = new DataDictionary<string, string>();
            Assert.AreEqual(0, dict.Count);
        }

        [Test]
        public void ConstructorWithCollection_OnNullCollection_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new DataDictionary<string, string>((IEnumerable<KeyValuePair<string, string>>)null).Void());
        }

        [Test]
        public void ConstructorWithCollection_Succeeds()
        {
            Assert.DoesNotThrow(
                () => new DataDictionary<string, string>(InitialData).Void());
        }

        [Test]
        public void ConstructorWithCollection_CopiesNonNullValues()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.AreEqual(InitialCount, dict.Count);
        }

        [Test]
        public void ConstructorWithCollection_DoesNotCopyNullValues()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.IsFalse(dict.TryGetValue(InitialKeyWithNullValue, out _));
        }

        [Test]
        public void ConstructorWithComparer_OnNullComparer_Succeeds()
        {
            Assert.DoesNotThrow(
                () => new DataDictionary<string, string>((IEqualityComparer<string>)null).Void());
        }

        [Test]
        public void ConstructorWithComparer_Succeeds()
        {
            Assert.DoesNotThrow(
                () => new DataDictionary<string, string>(StringComparer.OrdinalIgnoreCase).Void());
        }

        [Test]
        public void ConstructorWithComparer_AppliesComparer()
        {
            var dict = new DataDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            dict.TryAdd(InitialExistingKey, InitialExistingValue);
            Assert.IsTrue(dict.TryGetValue(InitialExistingKeyWithDifferentCasing, out _));
        }

        [Test]
        public void ConstructorWithCollectionAndComparer_OnNullCollection_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new DataDictionary<string, string>(null, StringComparer.Ordinal).Void());
        }

        [Test]
        public void ConstructorWithCollectionAndComparer_OnNullComparer_Succeeds()
        {
            Assert.DoesNotThrow(
                () => new DataDictionary<string, string>(InitialData, null).Void());
        }

        [Test]
        public void ConstructorWithCollectionAndComparer_Succeeds()
        {
            Assert.DoesNotThrow(
                () => new DataDictionary<string, string>(InitialData, StringComparer.OrdinalIgnoreCase).Void());
        }

        [Test]
        public void ConstructorWithCollectionAndComparer_CopiesNonNullValues()
        {
            var dict = new DataDictionary<string, string>(InitialData, StringComparer.OrdinalIgnoreCase);
            Assert.AreEqual(InitialCount, dict.Count);
        }

        [Test]
        public void ConstructorWithCollectionAndComparer_DoesNotCopyNullValues()
        {
            var dict = new DataDictionary<string, string>(InitialData, StringComparer.OrdinalIgnoreCase);
            Assert.IsFalse(dict.TryGetValue(InitialKeyWithNullValue, out _));
        }

        [Test]
        public void ConstructorWithCollectionAndComparer_AppliesComparer()
        {
            var dict = new DataDictionary<string, string>(InitialData, StringComparer.OrdinalIgnoreCase);
            Assert.IsTrue(dict.TryGetValue(InitialExistingKeyWithDifferentCasing, out _));
        }

        [Test]
        public void ConstructorWithCapacity_OnNegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DataDictionary<string, string>(-1).Void());
        }

        [Test]
        public void ConstructorWithCapacity_Succeeds()
        {
            Assert.DoesNotThrow(
                () => new DataDictionary<string, string>(5).Void());
        }

        [Test]
        public void ConstructorWithCapacityCollectionAndComparer_OnNegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DataDictionary<string, string>(-1, new Dictionary<string, string>(), StringComparer.Ordinal).Void());
        }

        [Test]
        public void ConstructorWithCapacityCollectionAndComparer_OnNullCollection_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new DataDictionary<string, string>(10, null, StringComparer.Ordinal).Void());
        }

        [Test]
        public void ConstructorWithCapacityCollectionAndComparer_OnNullComparer_Succeeds()
        {
            Assert.DoesNotThrow(
                () => new DataDictionary<string, string>(12, InitialData, null).Void());
        }

        [Test]
        public void ConstructorWithCapacityCollectionAndComparer_Succeeds()
        {
            Assert.DoesNotThrow(
                () => new DataDictionary<string, string>(12, InitialData, StringComparer.OrdinalIgnoreCase).Void());
        }

        [Test]
        public void ConstructorWithCapacityCollectionAndComparer_CopiesNonNullValues()
        {
            var dict = new DataDictionary<string, string>(10, InitialData, StringComparer.OrdinalIgnoreCase);
            Assert.AreEqual(InitialCount, dict.Count);
        }

        [Test]
        public void ConstructorWithCapacityCollectionAndComparer_DoesNotCopyNullValues()
        {
            var dict = new DataDictionary<string, string>(10, InitialData, StringComparer.InvariantCultureIgnoreCase);
            Assert.IsFalse(dict.TryGetValue(InitialKeyWithNullValue, out _));
        }

        [Test]
        public void ConstructorWithCapacityCollectionAndComparer_AppliesComparer()
        {
            var dict = new DataDictionary<string, string>(10, InitialData, StringComparer.InvariantCultureIgnoreCase);
            Assert.IsTrue(dict.TryGetValue(InitialExistingKeyWithDifferentCasing, out _));
        }

        [Test]
        public void Count_WhenEmpty_IsZero()
        {
            var dict = new DataDictionary<string, string>();
            Assert.AreEqual(0, dict.Count);
        }

        [Test]
        public void Count_AfterAdd_HasCorrectValue()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.AreEqual(InitialCount, dict.Count);
        }

        [Test]
        public void Count_AfterRemove_HasCorrectValue()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            dict.TryRemove(InitialExistingKey, out _);
            Assert.AreEqual(InitialCount - 1, dict.Count);
        }

        [Test]
        public void IsEmpty_WhenEmpty_IsTrue()
        {
            var dict = new DataDictionary<string, string>();
            Assert.IsTrue(dict.IsEmpty);
        }

        [Test]
        public void IsEmpty_WhenNotEmpty_IsFalse()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.IsFalse(dict.IsEmpty);
        }

        [Test]
        public void Keys_ContainsAllKeys()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            CollectionAssert.AreEqual(InitialKeys, dict.Keys, StringComparer.Ordinal);
        }

        [Test]
        public void Values_ContainsAllValues()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            CollectionAssert.AreEqual(InitialValues, dict.Values, StringComparer.Ordinal);
        }

        [Test]
        public void GetItem_OnNullKey_ThrowsArgumentNullException()
        {
            var dict = new DataDictionary<string, string>();
            Assert.Throws<ArgumentNullException>(
                () => dict[null].Void());
        }

        [Test]
        public void GetItem_OnExistingKey_ReturnsCorrectValue()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.AreEqual(InitialExistingValue, dict[InitialExistingKey]);
        }

        [Test]
        public void GetItem_OnNonExistingKey_ReturnsNull()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.IsNull(dict[InitialNonExistingKey]);
        }

        [Test]
        public void SetItem_OnNullKey_ThrowsArgumentNullException()
        {
            var dict = new DataDictionary<string, string>();
            Assert.Throws<ArgumentNullException>(
                () => dict[null] = SomeValue);
        }

        [Test]
        public void SetItem_OnNonExistingKeyAndNonNullValue_AddsKeyAndValue()
        {
            var dict = new DataDictionary<string, string> {
                [InitialExistingKey] = InitialExistingValue,
            };
            Assert.AreEqual(InitialExistingValue, dict[InitialExistingKey]);
        }

        [Test]
        public void SetItem_OnNonExistingKeyAndNullValue_DoesNothing()
        {
            var dict = new DataDictionary<string, string> {
                [InitialExistingKey] = null,
            };
            Assert.IsTrue(dict.IsEmpty);
        }

        [Test]
        public void SetItem_OnExistingKeyAndNonNullValue_ReplacesValue()
        {
            var dict = new DataDictionary<string, string>(InitialData) {
                [InitialExistingKey] = DifferentValueForInitialExistingKey,
            };
            Assert.AreEqual(DifferentValueForInitialExistingKey, dict[InitialExistingKey]);
        }

        [Test]
        public void SetItem_OnExistingKeyAndNullValue_RemovesKey()
        {
            var dict = new DataDictionary<string, string>(InitialData) {
                [InitialExistingKey] = null,
            };
            Assert.IsFalse(dict.TryGetValue(InitialExistingKey, out _));
        }

        [Test]
        public void Clear_RemovesAllData()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            dict.Clear();
            Assert.IsTrue(dict.IsEmpty);
        }

        [Test]
        public void ContainsKey_OnExistingKey_ReturnsTrue()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.IsTrue(dict.ContainsKey(InitialExistingKey));
        }

        [Test]
        public void ContainsKey_OnNonExistingKey_ReturnsFalse()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.IsFalse(dict.ContainsKey(InitialNonExistingKey));
        }

        [Test]
        public void GetOrAdd_OnExistingKey_ReturnsExistingValue()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.AreEqual(InitialExistingValue, dict.GetOrAdd(InitialExistingKey, DifferentValueForInitialExistingKey));
        }

        [Test]
        public void GetOrAdd_OnNonExistingKeyAndNonNullValue_ReturnsValue()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.AreEqual(InitialNonExistingValue, dict.GetOrAdd(InitialNonExistingKey, InitialNonExistingValue));
        }

        [Test]
        public void GetOrAdd_OnNonExistingKeyAndNonNullValue_AddsKeyAndValue()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            dict.GetOrAdd(InitialNonExistingKey, InitialNonExistingValue);
            Assert.AreEqual(InitialNonExistingValue, dict[InitialNonExistingKey]);
        }

        [Test]
        public void GetOrAdd_OnNonExistingKeyAndNullValue_ReturnsNull()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.AreEqual(null, dict.GetOrAdd(InitialNonExistingKey, null));
        }

        [Test]
        public void GetOrAdd_OnNonExistingKeyAndNullValue_DoesNotAdd()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            dict.GetOrAdd(InitialNonExistingKey, null);
            Assert.AreEqual(InitialCount, dict.Count);
        }

        [Test]
        public void Remove_OnNullKey_ThrowsArgumentNullException()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.Throws<ArgumentNullException>(
                () => dict.Remove(null));
        }

        [Test]
        public void Remove_OnExistingKey_ReturnsTrue()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.IsTrue(dict.Remove(InitialExistingKey));
        }

        [Test]
        public void Remove_OnExistingKey_RemovesKey()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            dict.Remove(InitialExistingKey);
            Assert.AreEqual(InitialCount - 1, dict.Count);
        }

        [Test]
        public void Remove_OnNonExistingKey_ReturnsFalse()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.IsFalse(dict.Remove(InitialNonExistingKey));
        }

        [Test]
        public void Remove_OnNonExistingKey_DoesNotRemove()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            dict.Remove(InitialNonExistingKey);
            Assert.AreEqual(InitialCount, dict.Count);
        }

        [Test]
        public void TryAdd_OnNullKey_ThrowsArgumentNullException()
        {
            var dict = new DataDictionary<string, string>();
            Assert.Throws<ArgumentNullException>(
                () => dict.TryAdd(null, SomeValue));
        }

        [Test]
        public void TryAdd_OnExistingKey_ReturnsFalse()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.IsFalse(dict.TryAdd(InitialExistingKey, DifferentValueForInitialExistingKey));
        }

        [Test]
        public void TryAdd_OnExistingKey_DoesNotAdd()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            dict.TryAdd(InitialExistingKey, DifferentValueForInitialExistingKey);
            Assert.AreEqual(InitialCount, dict.Count);
        }

        [Test]
        public void TryAdd_OnExistingKey_DoesNotModifyValue()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            dict.TryAdd(InitialExistingKey, DifferentValueForInitialExistingKey);
            Assert.AreEqual(InitialExistingValue, dict[InitialExistingKey]);
        }

        [Test]
        public void TryAdd_OnNonExistingKeyAndNonNullValue_ReturnsTrue()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.IsTrue(dict.TryAdd(InitialNonExistingKey, InitialNonExistingValue));
        }

        [Test]
        public void TryAdd_OnNonExistingKeyAndNonNullValue_AddsKeyAndValue()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            dict.TryAdd(InitialNonExistingKey, InitialNonExistingValue);
            Assert.AreEqual(InitialNonExistingValue, dict[InitialNonExistingKey]);
        }

        [Test]
        public void TryAdd_OnNonExistingKeyAndNullValue_ReturnsTrue()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.IsTrue(dict.TryAdd(InitialNonExistingKey, null));
        }

        [Test]
        public void TryAdd_OnNonExistingKeyAndNullValue_DoesNotAdd()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            dict.TryAdd(InitialNonExistingKey, null);
            Assert.AreEqual(InitialCount, dict.Count);
        }

        [Test]
        public void TryGetValue_OnNullKey_ThrowsArgumentNullException()
        {
            var dict = new DataDictionary<string, string>();
            Assert.Throws<ArgumentNullException>(
                () => dict.TryGetValue(null, out _));
        }

        [Test]
        public void TryGetValue_OnExistingKey_ReturnsTrue()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.IsTrue(dict.TryGetValue(InitialExistingKey, out _));
        }

        [Test]
        public void TryGetValue_OnExistingKey_YieldsCorrectValue()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            dict.TryGetValue(InitialExistingKey, out var value);
            Assert.AreEqual(InitialExistingValue, value);
        }

        [Test]
        public void TryGetValue_OnNonExistingKey_ReturnsFalse()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.IsFalse(dict.TryGetValue(InitialNonExistingKey, out _));
        }

        [Test]
        public void TryRemove_OnNullKey_ThrowsArgumentNullException()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.Throws<ArgumentNullException>(
                () => dict.Remove(null));
        }

        [Test]
        public void TryRemove_OnExistingKey_ReturnsTrue()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.IsTrue(dict.TryRemove(InitialExistingKey, out _));
        }

        [Test]
        public void TryRemove_OnExistingKey_RemovesValue()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            dict.TryRemove(InitialExistingKey, out _);
            Assert.IsNull(dict[InitialExistingKey]);
        }

        [Test]
        public void TryRemove_OnExistingKey_YieldsRemovedValue()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            dict.TryRemove(InitialExistingKey, out var value);
            Assert.AreEqual(InitialExistingValue, value);
        }

        [Test]
        public void TryRemove_OnNonExistingKey_ReturnsFalse()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.IsFalse(dict.TryRemove(InitialNonExistingKey, out _));
        }

        [Test]
        public void TryUpdate_OnNullKey_ThrowsArgumentNullException()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.Throws<ArgumentNullException>(
                () => dict.TryUpdate(null, DifferentValueForInitialExistingKey, InitialExistingValue));
        }

        [Test]
        public void TryUpdate_OnNonExistingKey_ReturnsFalse()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.IsFalse(
                dict.TryUpdate(InitialNonExistingKey, SomeValue, InitialNonExistingValue));
        }

        [Test]
        public void TryUpdate_OnExistingKeyAndNonMatchingComparisonValue_ReturnsFalse()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.IsFalse(
                dict.TryUpdate(InitialExistingKey, DifferentValueForInitialExistingKey, SomeValue));
        }

        [Test]
        public void TryUpdate_OnExistingKeyAndNonMatchingComparisonValue_DoesNotUpdate()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            dict.TryUpdate(InitialExistingKey, DifferentValueForInitialExistingKey, SomeValue);
            Assert.AreEqual(InitialExistingValue, dict[InitialExistingKey]);
        }

        [Test]
        public void TryUpdate_OnExistingKeyAndMatchingComparisonValue_ReturnsTrue()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            Assert.IsTrue(
                dict.TryUpdate(InitialExistingKey, DifferentValueForInitialExistingKey, InitialExistingValue));
        }

        [Test]
        public void TryUpdate_OnExistingKeyAndMatchingComparisonValue_UpdatesValue()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            dict.TryUpdate(InitialExistingKey, DifferentValueForInitialExistingKey, InitialExistingValue);
            Assert.AreEqual(DifferentValueForInitialExistingKey, dict[InitialExistingKey]);
        }

        [Test]
        public void IDictionary_Add_OnNullKey_ThrowsArgumentNullException()
        {
            IDictionary<string, string> dict = new DataDictionary<string, string>();
            Assert.Throws<ArgumentNullException>(
                () => dict.Add(null, SomeValue));
        }

        [Test]
        public void IDictionary_Add_OnExistingKey_ThrowsArgumentException()
        {
            IDictionary<string, string> dict = new DataDictionary<string, string>(InitialData);
            Assert.Throws<ArgumentException>(
                () => dict.Add(InitialExistingKey, DifferentValueForInitialExistingKey));
        }

        [Test]
        public void IDictionary_Add_OnNonExistingKeyAndNonNullValue_AddsKeyAndValue()
        {
            IDictionary<string, string> dict = new DataDictionary<string, string>(InitialData);
            dict.Add(InitialNonExistingKey, InitialNonExistingValue);
            Assert.AreEqual(InitialNonExistingValue, dict[InitialNonExistingKey]);
        }

        [Test]
        public void IDictionary_Add_OnNonExistingKeyAndNullValue_DoesNotAdd()
        {
            IDictionary<string, string> dict = new DataDictionary<string, string>(InitialData);
            dict.Add(InitialNonExistingKey, null);
            Assert.IsFalse(dict.TryGetValue(InitialNonExistingKey, out _));
        }

        [Test]
        public void IReadOnlyDictionary_Keys_ContainsAllKeys()
        {
            IReadOnlyDictionary<string, string> dict = new DataDictionary<string, string>(InitialData);
            CollectionAssert.AreEqual(InitialKeys, dict.Keys, StringComparer.Ordinal);
        }

        [Test]
        public void IReadOnlyDictionary_Values_ContainsAllValues()
        {
            IReadOnlyDictionary<string, string> dict = new DataDictionary<string, string>(InitialData);
            CollectionAssert.AreEqual(InitialValues, dict.Values, StringComparer.Ordinal);
        }

        [Test]
        public void ICollection_IsReadOnly_ReturnsFalse()
        {
            ICollection<KeyValuePair<string, string>> dict = new DataDictionary<string, string>();
            Assert.IsFalse(dict.IsReadOnly);
        }

        [Test]
        public void ICollection_Add_OnNullKey_ThrowsArgumentNullException()
        {
            ICollection<KeyValuePair<string, string>> dict = new DataDictionary<string, string>();
            Assert.Throws<ArgumentNullException>(
                () => dict.Add(new KeyValuePair<string, string>(null, SomeValue)));
        }

        [Test]
        public void ICollection_Add_OnExistingKey_ThrowsArgumentException()
        {
            ICollection<KeyValuePair<string, string>> dict = new DataDictionary<string, string>(InitialData);
            Assert.Throws<ArgumentException>(
                () => dict.Add(new KeyValuePair<string, string>(InitialExistingKey, DifferentValueForInitialExistingKey)));
        }

        [Test]
        public void ICollection_Add_OnNonExistingKeyAndNonNullValue_AddsKeyAndValue()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            ICollection<KeyValuePair<string, string>> collection = dict;
            collection.Add(new KeyValuePair<string, string>(InitialNonExistingKey, InitialNonExistingValue));
            Assert.AreEqual(InitialNonExistingValue, dict[InitialNonExistingKey]);
        }

        [Test]
        public void ICollection_Add_OnNonExistingKeyAndNullValue_DoesNotAdd()
        {
            var dict = new DataDictionary<string, string>();
            ICollection<KeyValuePair<string, string>> collection = dict;
            collection.Add(new KeyValuePair<string, string>(InitialNonExistingKey, null));
            Assert.IsFalse(dict.TryGetValue(InitialNonExistingKey, out _));
        }

        [Test]
        public void ICollection_Contains_OnNullKey_ThrowsArgumentNullException()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            ICollection<KeyValuePair<string, string>> collection = dict;
            var kvp = new KeyValuePair<string, string>(null, InitialExistingValue);
            Assert.Throws<ArgumentNullException>(
                () => collection.Contains(kvp).Void());
        }

        [Test]
        public void ICollection_Contains_OnExistingKeyValuePair_ReturnsTrue()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            ICollection<KeyValuePair<string, string>> collection = dict;
            var kvp = new KeyValuePair<string, string>(InitialExistingKey, InitialExistingValue);
            Assert.IsTrue(collection.Contains(kvp));
        }

        [Test]
        public void ICollection_Contains_OnExistingKeyWithOtherValue_ReturnsFalse()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            ICollection<KeyValuePair<string, string>> collection = dict;
            var kvp = new KeyValuePair<string, string>(InitialExistingKey, InitialNonExistingValue);
            Assert.IsFalse(collection.Contains(kvp));
        }

        [Test]
        public void ICollection_Contains_OnNonExistingKeyValuePair_ReturnsFalse()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            ICollection<KeyValuePair<string, string>> collection = dict;
            var kvp = new KeyValuePair<string, string>(InitialNonExistingKey, InitialNonExistingValue);
            Assert.IsFalse(collection.Contains(kvp));
        }

        [Test]
        public void ICollection_CopyTo_OnNullArray_ThrowsArgumentNullException()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            ICollection<KeyValuePair<string, string>> collection = dict;
            Assert.Throws<ArgumentNullException>(
                () => collection.CopyTo(null, 0));
        }

        [Test]
        public void ICollection_CopyTo_OnNegativeIndex_ThrowsArgumentOutOfRangeException()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            ICollection<KeyValuePair<string, string>> collection = dict;
            var a = new KeyValuePair<string, string>[InitialCount];
            Assert.Throws<ArgumentOutOfRangeException>(
                () => collection.CopyTo(a, -1));
        }

        [Test]
        public void ICollection_CopyTo_OnNoRoomInArray_ThrowsArgumentException()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            ICollection<KeyValuePair<string, string>> collection = dict;
            var a = new KeyValuePair<string, string>[InitialCount];
            Assert.Throws<ArgumentException>(
                () => collection.CopyTo(a, 1));
        }

        [Test]
        public void ICollection_Remove_OnNullKey_ThrowsArgumentNullException()
        {
            ICollection<KeyValuePair<string, string>> dict = new DataDictionary<string, string>();
            Assert.Throws<ArgumentNullException>(
                () => dict.Remove(new KeyValuePair<string, string>(null, SomeValue)));
        }

        [Test]
        public void ICollection_Remove_OnExistingKeyValuePair_ReturnsTrue()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            ICollection<KeyValuePair<string, string>> collection = dict;
            var kvp = new KeyValuePair<string, string>(InitialExistingKey, InitialExistingValue);
            Assert.IsTrue(collection.Remove(kvp));
        }

        [Test]
        public void ICollection_Remove_OnExistingKeyValuePair_RemovesKey()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            ICollection<KeyValuePair<string, string>> collection = dict;
            var kvp = new KeyValuePair<string, string>(InitialExistingKey, InitialExistingValue);
            collection.Remove(kvp);
            Assert.IsFalse(dict.TryGetValue(InitialExistingKey, out _));
        }

        [Test]
        public void ICollection_Remove_OnExistingKeyWithOtherValue_ReturnsFalse()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            ICollection<KeyValuePair<string, string>> collection = dict;
            var kvp = new KeyValuePair<string, string>(InitialExistingKey, InitialNonExistingValue);
            Assert.IsFalse(collection.Remove(kvp));
        }

        [Test]
        public void ICollection_Remove_OnExistingKeyWithOtherValue_DoesNotRemove()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            ICollection<KeyValuePair<string, string>> collection = dict;
            var kvp = new KeyValuePair<string, string>(InitialExistingKey, InitialNonExistingValue);
            collection.Remove(kvp);
            Assert.AreEqual(InitialCount, collection.Count);
        }

        [Test]
        public void ICollection_Remove_OnNonExistingKey_ReturnsFalse()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            ICollection<KeyValuePair<string, string>> collection = dict;
            var kvp = new KeyValuePair<string, string>(InitialNonExistingKey, InitialNonExistingValue);
            Assert.IsFalse(collection.Remove(kvp));
        }

        [Test]
        public void ICollection_Remove_OnNonExistingKey_DoesNotRemove()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            ICollection<KeyValuePair<string, string>> collection = dict;
            var kvp = new KeyValuePair<string, string>(InitialNonExistingKey, InitialNonExistingValue);
            collection.Remove(kvp);
            Assert.AreEqual(InitialCount, collection.Count);
        }

        [Test]
        public void IEnumerable_GetEnumerator_EnumeratesAllData()
        {
            var dict = new DataDictionary<string, string>(InitialData);
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

            CollectionAssert.AreEqual(InitialImportedData, list);
        }

        [Test]
        public void NonGenericIEnumerable_GetEnumerator_EnumeratesAllData()
        {
            var dict = new DataDictionary<string, string>(InitialData);
            IEnumerable enumerable = dict;

            // Neither CollectionAssert nor LINQ give certainty
            // about the actual GetEnumerator called (whether generic or non-generic).
            // So, for the sake of code coverage, let's use it directly.
            var list = new List<object>();
            var enumerator = enumerable.GetEnumerator();
            enumerator.Reset();
            while (enumerator.MoveNext())
                list.Add(enumerator.Current);

            CollectionAssert.AreEqual(InitialImportedData, list);
        }
    }
}