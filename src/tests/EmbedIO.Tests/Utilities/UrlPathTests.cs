using System;
using EmbedIO.Utilities;
using NUnit.Framework;

namespace EmbedIO.Tests.Utilities
{
    public class UrlPathTests
    {
        [TestCase(null, false)]
        [TestCase("", false)]
        [TestCase("does/not/start/with/slash", false)]
        [TestCase("/", true)]
        [TestCase("/starts/with/slash", true)]
        public void IsValid_ReturnsCorrectValue(string urlPath, bool expectedResult)
            => Assert.AreEqual(expectedResult, UrlPath.IsValid(urlPath));

        [TestCase(true)]
        [TestCase(false)]
        public void Normalize_OnNullUrlPath_ThrowsArgumentNullException(bool isBasePath)
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference - This is the whole point of this test.
            => Assert.Throws<ArgumentNullException>(() => UrlPath.Normalize(null, isBasePath));
#pragma warning restore CS8625

        [TestCase(true)]
        [TestCase(false)]
        public void Normalize_OnEmptyUrlPath_ThrowsArgumentException(bool isBasePath)
            => Assert.Throws<ArgumentException>(() => UrlPath.Normalize(string.Empty, isBasePath));

        [TestCase(true)]
        [TestCase(false)]
        public void Normalize_OnInvalidUrlPath_ThrowsArgumentException(bool isBasePath)
            => Assert.Throws<ArgumentException>(() => UrlPath.Normalize("does/not/start/with/slash", isBasePath));

        [TestCase("/", false, "/")]
        [TestCase("/", true, "/")]
        [TestCase("/starts/with/slash", false, "/starts/with/slash")]
        [TestCase("/starts/with/slash", true, "/starts/with/slash/")]
        [TestCase("//has/multiple///slashes////", false, "/has/multiple/slashes")]
        [TestCase("//has/multiple//slashes////", true, "/has/multiple/slashes/")]
        public void Normalize_ReturnsCorrectValue(string urlPath, bool isBasePath, string expectedResult)
            => Assert.AreEqual(expectedResult, UrlPath.Normalize(urlPath, isBasePath));

        [TestCase(null, null)]
        [TestCase(null, "/api/")]
        [TestCase("/api/endpoint", null)]
        public void HasPrefix_OnNullParameter_ThrowsArgumentNullException(string urlPath, string baseUrlPath)
            => Assert.Throws<ArgumentNullException>(() => UrlPath.HasPrefix(urlPath, baseUrlPath));

        [TestCase("", "")]
        [TestCase("", "/api/")]
        [TestCase("/api/endpoint", "")]
        public void HasPrefix_OnEmptyParameter_ThrowsArgumentException(string urlPath, string baseUrlPath)
            => Assert.Throws<ArgumentException>(() => UrlPath.HasPrefix(urlPath, baseUrlPath));

        [TestCase("!!!", "!!!")]
        [TestCase("!!!", "/api/")]
        [TestCase("/api/endpoint", "!!!")]
        public void HasPrefix_OnInvalidParameter_ThrowsArgumentException(string urlPath, string baseUrlPath)
            => Assert.Throws<ArgumentException>(() => UrlPath.HasPrefix(urlPath, baseUrlPath));

        [TestCase("/api/v1/endpoint", "/api/v1", true)]
        [TestCase("/api/v1/endpoint", "/api/v1/", true)]
        [TestCase("/api/v1/endpoint", "/api/v2", false)]
        [TestCase("/api/v1/endpoint", "/api/v2/", false)]
        public void HasPrefix_ReturnsCorrectValue(string urlPath, string baseUrlPath, bool expectedResult)
            => Assert.AreEqual(expectedResult, UrlPath.HasPrefix(urlPath, baseUrlPath));

        [TestCase(null, null)]
        [TestCase(null, "/api/")]
        [TestCase("/api/endpoint", null)]
        public void StripPrefix_OnNullParameter_ThrowsArgumentNullException(string urlPath, string baseUrlPath)
            => Assert.Throws<ArgumentNullException>(() => UrlPath.StripPrefix(urlPath, baseUrlPath));

        [TestCase("", "")]
        [TestCase("", "/api/")]
        [TestCase("/api/endpoint", "")]
        public void StripPrefix_OnEmptyParameter_ThrowsArgumentException(string urlPath, string baseUrlPath)
            => Assert.Throws<ArgumentException>(() => UrlPath.StripPrefix(urlPath, baseUrlPath));

        [TestCase("!!!", "!!!")]
        [TestCase("!!!", "/api/")]
        [TestCase("/api/endpoint", "!!!")]
        public void StripPrefix_OnInvalidParameter_ThrowsArgumentException(string urlPath, string baseUrlPath)
            => Assert.Throws<ArgumentException>(() => UrlPath.StripPrefix(urlPath, baseUrlPath));

        [TestCase("/api/v1/endpoint", "/api/v1", "endpoint")]
        [TestCase("/api/v1/endpoint", "/api/v1/", "endpoint")]
        [TestCase("/api/v1", "/api/v1", "")]
        [TestCase("/api/v1", "/api/v1/", "")]
        [TestCase("/api/v1/endpoint", "/api/v2", null)]
        [TestCase("/api/v1/endpoint", "/api/v2/", null)]
        public void StripPrefix_ReturnsCorrectValue(string urlPath, string baseUrlPath, string expectedResult)
            => Assert.AreEqual(expectedResult, UrlPath.StripPrefix(urlPath, baseUrlPath));

        [Test]
        public void Split_OnNullUrlPath_ThrowsArgumentNullException()
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference - This is the whole point of this test.
            => Assert.Throws<ArgumentNullException>(() => UrlPath.Split(null));
#pragma warning restore CS8625

        [Test]
        public void Split_OnEmptyUrlPath_ThrowsArgumentException()
            => Assert.Throws<ArgumentException>(() => UrlPath.Split(""));

        [Test]
        public void Split_OnInvalidUrlPath_ThrowsArgumentException()
            => Assert.Throws<ArgumentException>(() => UrlPath.Split("does/not/start/with/slash"));

        [TestCase("/")]
        [TestCase("/api/v1/endpoint", "api", "v1", "endpoint")]
        [TestCase("///multiple///slashes//get///normalized/", "multiple", "slashes", "get", "normalized")]
        public void Split_ReturnsCorrectValues(string urlPath, params string[] segments)
            => CollectionAssert.AreEqual(segments, UrlPath.Split(urlPath));
    }
}