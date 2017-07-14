using NUnit.Framework;
using Unosquare.Labs.EmbedIO.Constants;

namespace Unosquare.Labs.EmbedIO.Tests
{
    [TestFixture]
    public class ExtensionTest
    {
        [TestCase(CompressionMethod.Gzip)]
        [TestCase(CompressionMethod.Deflate)]
        [TestCase(CompressionMethod.None)]
        public void CompressGzipTest(CompressionMethod method)
        {
            var buffer = System.Text.Encoding.UTF8.GetBytes("THIS IS DATA");

            var compressBuffer = buffer.Compress(method);

            Assert.IsNotNull(compressBuffer);

            var uncompressBuffer = compressBuffer.Compress(method, System.IO.Compression.CompressionMode.Decompress);

            Assert.IsNotNull(uncompressBuffer);
            Assert.AreEqual(uncompressBuffer, buffer);
        }
    }
}