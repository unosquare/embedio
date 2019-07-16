using System;
using System.Collections.Generic;
using System.Linq;
using EmbedIO.Utilities;
using NUnit.Framework;

namespace EmbedIO.Tests.Utilities
{
    public class FromStringTest
    {
        private const string SomeString = "Any string, as long as it is not convertible to Int32";
        private const int IntValue = 42;
        private const string StringValue = "42";

        private static IEnumerable<int> ValidIntValues { get; } = new[] { 42, 17, -3 };

        private static string[] ValidStringValues
            => ValidIntValues.Select(i => i.ToString()).ToArray();

        private static string[] InvalidStringValues => new[] { "foo", "42", null, "bar" };

        [Test]
        public void CanConvertTo_NonGeneric_OnNullType_ThrowsArgumentNullException()
            => Assert.Throws<ArgumentNullException>(() => FromString.CanConvertTo(null));

        [Test]
        public void CanConvertTo_NonGeneric_OnNonConvertibleType_ReturnsFalse()
            => Assert.IsFalse(FromString.CanConvertTo(typeof(Action)));

        [Test]
        public void CanConvertTo_NonGeneric_OnConvertibleType_ReturnsTrue()
            => Assert.IsTrue(FromString.CanConvertTo(typeof(int)));

        [Test]
        public void CanConvertTo_Generic_OnNonConvertibleType_ReturnsFalse()
            => Assert.IsFalse(FromString.CanConvertTo<Action>());

        [Test]
        public void CanConvertTo_Generic_OnConvertibleType_ReturnsTrue()
            => Assert.IsTrue(FromString.CanConvertTo<int>());

        [Test]
        public void TryConvertTo_NonGeneric_OnNullType_ThrowsArgumentNullException()
            => Assert.Throws<ArgumentNullException>(() => FromString.TryConvertTo(null, SomeString, out _));

        [Test]
        public void TryConvertTo_NonGeneric_OnNonConvertibleType_ReturnsFalse()
            => Assert.IsFalse(FromString.TryConvertTo(typeof(Action), SomeString, out _));

        [Test]
        public void TryConvertTo_NonGeneric_OnInvalidString_ReturnsFalse()
            => Assert.IsFalse(FromString.TryConvertTo(typeof(int), SomeString, out _));

        [Test]
        public void TryConvertTo_NonGeneric_OnSuccess_ReturnsTrue()
            => Assert.IsTrue(FromString.TryConvertTo(typeof(int), StringValue, out _));

        [Test]
        public void TryConvertTo_NonGeneric_OnSuccess_YieldsCorrectValue()
        {
            FromString.TryConvertTo(typeof(int), StringValue, out var result);
            Assert.AreEqual(IntValue, result);
        }

        [Test]
        public void TryConvertTo_Generic_OnNonConvertibleType_ReturnsFalse()
            => Assert.IsFalse(FromString.TryConvertTo<Action>(SomeString, out _));

        [Test]
        public void TryConvertTo_Generic_OnInvalidString_ReturnsFalse()
            => Assert.IsFalse(FromString.TryConvertTo<int>(SomeString, out _));

        [Test]
        public void TryConvertTo_Generic_OnSuccess_ReturnsTrue()
            => Assert.IsTrue(FromString.TryConvertTo<int>(StringValue, out _));

        [Test]
        public void TryConvertTo_Generic_OnSuccess_YieldsCorrectValue()
        {
            FromString.TryConvertTo<int>(StringValue, out var result);
            Assert.AreEqual(IntValue, result);
        }

        [Test]
        public void ConvertTo_NonGeneric_OnNullType_ThrowsArgumentNullException()
            => Assert.Throws<ArgumentNullException>(() => FromString.ConvertTo(null, SomeString));

        [Test]
        public void ConvertTo_NonGeneric_OnNonConvertibleType_ThrowsStringConversionException()
            => Assert.Throws<StringConversionException>(() => FromString.ConvertTo(typeof(Action), SomeString));

        [Test]
        public void ConvertTo_NonGeneric_OnInvalidString_ThrowsStringConversionException()
            => Assert.Throws<StringConversionException>(() => FromString.ConvertTo(typeof(int), SomeString));

        [Test]
        public void ConvertTo_NonGeneric_OnSuccess_ReturnsCorrectValue()
            => Assert.AreEqual(IntValue, FromString.ConvertTo(typeof(int), StringValue));

        [Test]
        public void ConvertTo_Generic_OnNonConvertibleType_ThrowsStringConversionException()
            => Assert.Throws<StringConversionException>(() => FromString.ConvertTo<Action>(SomeString));

        [Test]
        public void ConvertTo_Generic_OnInvalidString_ThrowsStringConversionException()
            => Assert.Throws<StringConversionException>(() => FromString.ConvertTo<int>(SomeString));

        [Test]
        public void ConvertTo_Generic_OnSuccess_ReturnsCorrectValue()
            => Assert.AreEqual(IntValue, FromString.ConvertTo<int>(StringValue));

        [Test]
        public void TryConvertTo_ArrayNonGeneric_OnNullType_ThrowsArgumentNullException()
            => Assert.Throws<ArgumentNullException>(() => FromString.TryConvertTo(null, ValidStringValues, out _));

        [Test]
        public void TryConvertTo_ArrayNonGeneric_OnNonConvertibleType_ReturnsFalse()
            => Assert.IsFalse(FromString.TryConvertTo(typeof(Action), ValidStringValues, out _));

        [Test]
        public void TryConvertTo_ArrayNonGeneric_OnInvalidStrings_ReturnsFalse()
            => Assert.IsFalse(FromString.TryConvertTo(typeof(int), InvalidStringValues, out _));

        [Test]
        public void TryConvertTo_ArrayNonGeneric_OnNullStrings_ReturnsFalse()
            => Assert.IsFalse(FromString.TryConvertTo(typeof(int), (string[])null, out _));

        [Test]
        public void TryConvertTo_ArrayNonGeneric_OnSuccess_ReturnsTrue()
            => Assert.IsTrue(FromString.TryConvertTo(typeof(int), ValidStringValues, out _));

        [Test]
        public void TryConvertTo_ArrayNonGeneric_OnSuccess_YieldsCorrectValues()
        {
            FromString.TryConvertTo(typeof(int), ValidStringValues, out var result);
            CollectionAssert.AreEqual(ValidIntValues, (int[])result);
        }

        [Test]
        public void TryConvertTo_ArrayGeneric_OnNonConvertibleType_ReturnsFalse()
            => Assert.IsFalse(FromString.TryConvertTo<Action>(ValidStringValues, out _));

        [Test]
        public void TryConvertTo_ArrayGeneric_OnInvalidStrings_ReturnsFalse()
            => Assert.IsFalse(FromString.TryConvertTo<int>(InvalidStringValues, out _));

        [Test]
        public void TryConvertTo_ArrayGeneric_OnNullStrings_ReturnsFalse()
            => Assert.IsFalse(FromString.TryConvertTo<int>((string[])null, out _));

        [Test]
        public void TryConvertTo_ArrayGeneric_OnSuccess_ReturnsTrue()
            => Assert.IsTrue(FromString.TryConvertTo<int>(ValidStringValues, out _));

        [Test]
        public void TryConvertTo_ArrayGeneric_OnSuccess_YieldsCorrectValues()
        {
            FromString.TryConvertTo<int>(ValidStringValues, out var result);
            CollectionAssert.AreEqual(ValidIntValues, (int[])result);
        }

        [Test]
        public void ConvertTo_ArrayNonGeneric_OnNullType_ThrowsArgumentNullException()
            => Assert.Throws<ArgumentNullException>(() => FromString.ConvertTo(null, ValidStringValues));

        [Test]
        public void ConvertTo_ArrayNonGeneric_OnNonConvertibleType_ThrowsStringConversionException()
            => Assert.Throws<StringConversionException>(() => FromString.ConvertTo(typeof(Action), ValidStringValues));

        [Test]
        public void ConvertTo_ArrayNonGeneric_OnInvalidStrings_ThrowsStringConversionException()
            => Assert.Throws<StringConversionException>(() => FromString.ConvertTo(typeof(int), InvalidStringValues));

        [Test]
        public void ConvertTo_ArrayNonGeneric_OnNullStrings_ReturnsNull()
            => Assert.IsNull(FromString.ConvertTo(typeof(int), (string[])null));

        [Test]
        public void ConvertTo_ArrayNonGeneric_OnSuccess_ReturnsCorrectValues()
            => CollectionAssert.AreEqual(ValidIntValues, (int[])FromString.ConvertTo(typeof(int), ValidStringValues));

        [Test]
        public void ConvertTo_ArrayGeneric_OnNonConvertibleType_ThrowsStringConversionException()
            => Assert.Throws<StringConversionException>(() => FromString.ConvertTo<Action>(ValidStringValues));

        [Test]
        public void ConvertTo_ArrayGeneric_OnInvalidStrings_ThrowsStringConversionException()
            => Assert.Throws<StringConversionException>(() => FromString.ConvertTo<int>(InvalidStringValues));

        [Test]
        public void ConvertTo_ArrayGeneric_OnNullStrings_ReturnsNull()
            => Assert.IsNull(FromString.ConvertTo<int>((string[])null));

        [Test]
        public void ConvertTo_ArrayGeneric_OnSuccess_ReturnsCorrectValues()
            => CollectionAssert.AreEqual(ValidIntValues, FromString.ConvertTo<int>(ValidStringValues));
    }
}