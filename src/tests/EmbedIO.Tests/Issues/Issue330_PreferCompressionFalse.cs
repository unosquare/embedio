using EmbedIO.Utilities;
using NUnit.Framework;

namespace EmbedIO.Tests.Issues
{
    public class Issue330_PreferCompressionFalse
    {
        [Test]
        public void QValueList_TryNegotiateContentEncoding_WhenPreferCompressionFalse_OnNoCompressionSpecified_ReturnsTrue()
        {
            var list = new QValueList(true, "gzip, deflate");
            Assert.IsTrue(list.TryNegotiateContentEncoding(false, out _, out _));
        }

        [Test]
        public void QValueList_TryNegotiateContentEncoding_WhenPreferCompressionFalse_OnNoCompressionSpecified_YieldsNone()
        {
            var list = new QValueList(true, "gzip, deflate");
            list.TryNegotiateContentEncoding(false, out var compressionMethod, out _);
            Assert.AreEqual(CompressionMethod.None, compressionMethod);
        }

        [Test]
        public void QValueList_TryNegotiateContentEncoding_WhenPreferCompressionFalse_OnNoCompressionSpecified_YieldsIdentity()
        {
            var list = new QValueList(true, "gzip, deflate");
            list.TryNegotiateContentEncoding(false, out _, out var name);
            Assert.AreEqual(CompressionMethodNames.None, name);
        }
    }
}