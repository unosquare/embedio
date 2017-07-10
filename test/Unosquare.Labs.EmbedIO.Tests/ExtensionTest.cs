using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

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

            var compressBuffer = buffer.Compress(method, System.IO.Compression.CompressionMode.Compress);

            Assert.IsNotNull(compressBuffer);

            var uncompressBuffer = compressBuffer.Compress(method, System.IO.Compression.CompressionMode.Decompress);

            Assert.IsNotNull(uncompressBuffer);
            Assert.AreEqual(uncompressBuffer, buffer);
        }
    }
}