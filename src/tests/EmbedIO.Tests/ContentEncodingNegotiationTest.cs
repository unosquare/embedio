using EmbedIO.Utilities;
using NUnit.Framework;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class ContentEncodingNegotiationTest
    {
        [TestCase("identity;q=1, *;q=0", true, CompressionMethod.None, CompressionMethodNames.None)]
        [TestCase("identity;q=1, *;q=0", false, CompressionMethod.None, CompressionMethodNames.None)]
        public void ContentEncodingNegotiation_Succeeds(
            string requestHeaders,
            bool preferCompression,
            CompressionMethod expectedCompressionMethod,
            string expectedCompressionMethodName)
        {
            var list = new QValueList(true, requestHeaders);
            var negotiated = list.TryNegotiateContentEncoding(preferCompression, out var actualCompressionMethod, out var actualCompressionMethodName);
            Assert.AreEqual(true, negotiated);
            Assert.AreEqual(expectedCompressionMethod, actualCompressionMethod);
            Assert.AreEqual(expectedCompressionMethodName, actualCompressionMethodName);
        }

        [TestCase("*;q=0", true)]
        [TestCase("*;q=0", false)]
        public void ContentEncodingNegotiation_Fails(string requestHeaders, bool preferCompression)
        {
            var list = new QValueList(true, requestHeaders);
            var negotiated = list.TryNegotiateContentEncoding(preferCompression, out _, out _);
            Assert.AreEqual(false, negotiated);
        }
    }
}