using System.Collections.Generic;
using NUnit.Framework;
using Unosquare.Labs.EmbedIO.Constants;

namespace Unosquare.Labs.EmbedIO.Tests
{
    [TestFixture]
    public class GzipTest
    {
        [TestCase(CompressionMethod.Gzip)]
        [TestCase(CompressionMethod.Deflate)]
        [TestCase(CompressionMethod.None)]
        public void Compress(CompressionMethod method)
        {
            var buffer = System.Text.Encoding.UTF8.GetBytes("THIS IS DATA");

            var compressBuffer = buffer.Compress(method);

            Assert.IsNotNull(compressBuffer);

            var uncompressBuffer = compressBuffer.Compress(method, System.IO.Compression.CompressionMode.Decompress);

            Assert.IsNotNull(uncompressBuffer);
            Assert.AreEqual(uncompressBuffer, buffer);
        }
    }

    [TestFixture]
    public class RequestWildcard
    {
        [TestCase("/data/1", new[] {"1"})]
        [TestCase("/data/1/2", new[] {"1", "2"})]
        public void UrlParamsWithLastParams(string urlMatch, string[] expected)
        {
            var result = Extensions.RequestWildcardUrlParams(urlMatch, "/data/*");
            Assert.AreEqual(expected.Length, result.Length);
            Assert.AreEqual(expected[0], result[0]);
        }

        [TestCase("/1/data", new[] {"1"})]
        [TestCase("/1/2/data", new[] {"1", "2"})]
        public void UrlParamsWithInitialParams(string urlMatch, string[] expected)
        {
            var result = Extensions.RequestWildcardUrlParams(urlMatch, "/*/data");
            Assert.AreEqual(expected.Length, result.Length);
            Assert.AreEqual(expected[0], result[0]);
        }

        [TestCase("/api/1/data", new[] {"1"})]
        [TestCase("/api/1/2/data", new[] {"1", "2"})]
        public void UrlParamsWithMiddleParams(string urlMatch, string[] expected)
        {
            var result = Extensions.RequestWildcardUrlParams(urlMatch, "/api/*/data");
            Assert.AreEqual(expected.Length, result.Length);
            Assert.AreEqual(expected[0], result[0]);
        }
    }

    [TestFixture]
    public class RquestRegex
    {
        private const string DefaultId = "id";

        [Test]
        public void UrlParamsWithLastParams()
        {
            var result = Extensions.RequestRegexUrlParams("/data/1", "/data/{id}");
            var expected = new Dictionary<string, object> {{DefaultId, "1"}};

            Assert.IsTrue(result.ContainsKey(DefaultId));
            Assert.AreEqual(expected[DefaultId], result[DefaultId]);
        }

        [Test]
        public void UrlParamsWithOptionalLastParams()
        {
            var result = Extensions.RequestRegexUrlParams("/data/1", "/data/{id?}");
            var expected = new Dictionary<string, object> {{DefaultId, "1"}};

            Assert.IsTrue(result.ContainsKey(DefaultId));
            Assert.AreEqual(expected[DefaultId], result[DefaultId]);
        }

        [Test]
        public void UrlParamsWithOptionalLastParamsNullable()
        {
            var result = Extensions.RequestRegexUrlParams("/data/", "/data/{id?}");
            var expected = new Dictionary<string, object> {{DefaultId, string.Empty}};

            Assert.IsTrue(result.ContainsKey(DefaultId));
            Assert.AreEqual(expected[DefaultId], result[DefaultId]);
        }

        [Test]
        public void UrlParamsWithMultipleParams()
        {
            var result = Extensions.RequestRegexUrlParams("/data/1/2", "/data/{id}/{anotherId}");
            var expected = new Dictionary<string, object> {{DefaultId, "1"}, {"anotherId", "2"}};

            Assert.IsTrue(result.ContainsKey(DefaultId));
            Assert.AreEqual(expected[DefaultId], result[DefaultId]);

            Assert.IsTrue(result.ContainsKey("anotherId"));
            Assert.AreEqual(expected["anotherId"], result["anotherId"]);
        }

        [Test]
        public void UrlParamsWithoutParams()
        {
            var result = Extensions.RequestRegexUrlParams("/data/", "/data/");

            Assert.IsTrue(result.Keys.Count == 0);
        }
    }
}