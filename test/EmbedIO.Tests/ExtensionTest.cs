using System.IO;
using System.Threading;
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
                var compressBuffer = await ms.CompressAsync(method, true, CancellationToken.None);

                Assert.IsNotNull(compressBuffer);

                var decompressBuffer =
                    await compressBuffer.CompressAsync(method, false, CancellationToken.None);

                Assert.AreEqual(decompressBuffer.ToArray(), _buffer);
            }
        }
    }
}