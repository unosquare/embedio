using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using System.Threading.Tasks;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class GzipTest
    {
        private readonly byte[] _buffer = System.Text.Encoding.UTF8.GetBytes("THIS IS DATA");

        [TestCase(CompressionMethod.Gzip)]
        [TestCase(CompressionMethod.Deflate)]
        [TestCase(CompressionMethod.None)]
        public async Task Compress(CompressionMethod method)
        {
            using (var ms = new MemoryStream(_buffer))
            {
                var compressBuffer = await ms.CompressAsync(method);

                Assert.IsNotNull(compressBuffer);

                var decompressBuffer = await compressBuffer.CompressAsync(method, System.IO.Compression.CompressionMode.Decompress);

                Assert.AreEqual(decompressBuffer.ToArray(), _buffer);
            }
        }
    }

    [TestFixture]
    public class RequestRegex
    {
        private const string DefaultId = "id";

        [Test]
        public void UrlParamsWithLastParams()
        {
            var result = "/data/1".RequestRegexUrlParams("/data/{id}");
            var expected = new Dictionary<string, object> {{DefaultId, "1"}};

            Assert.IsTrue(result.ContainsKey(DefaultId));
            Assert.AreEqual(expected[DefaultId], result[DefaultId]);
        }

        [Test]
        public void UrlParamsWithOptionalLastParams()
        {
            var result = "/data/1".RequestRegexUrlParams("/data/{id?}");
            var expected = new Dictionary<string, object> {{DefaultId, "1"}};

            Assert.IsTrue(result.ContainsKey(DefaultId));
            Assert.AreEqual(expected[DefaultId], result[DefaultId]);
        }

        [Test]
        public void UrlParamsWithOptionalLastParamsNullable()
        {
            var result = "/data/".RequestRegexUrlParams("/data/{id?}");
            var expected = new Dictionary<string, object> {{DefaultId, string.Empty}};

            Assert.IsTrue(result.ContainsKey(DefaultId));
            Assert.AreEqual(expected[DefaultId], result[DefaultId]);
        }

        [Test]
        public void UrlParamsWithMultipleParams()
        {
            var result = "/data/1/2".RequestRegexUrlParams("/data/{id}/{anotherId}");
            var expected = new Dictionary<string, object> {{DefaultId, "1"}, {"anotherId", "2"}};

            Assert.IsTrue(result.ContainsKey(DefaultId));
            Assert.AreEqual(expected[DefaultId], result[DefaultId]);

            Assert.IsTrue(result.ContainsKey("anotherId"));
            Assert.AreEqual(expected["anotherId"], result["anotherId"]);
        }

        [Test]
        public void UrlParamsWithoutParams()
        {
            var result = "/data/".RequestRegexUrlParams("/data/");

            Assert.IsTrue(result.Keys.Count == 0);
        }
    }
}