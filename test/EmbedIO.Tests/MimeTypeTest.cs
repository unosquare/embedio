using NUnit.Framework;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class MimeTypeTest
    {
        [TestCase(null, false)]
        [TestCase("", false)]
        [TestCase("text", false)]
        [TestCase("/", false)]
        [TestCase("text/", false)]
        [TestCase("/text", false)]
        [TestCase("text/html,", false)]
        [TestCase("text,/html", false)]
        [TestCase("*/text", false)]
        [TestCase("*/*", false)]
        [TestCase("text/*", false)]
        [TestCase("text/html", true)]
        public void IsMimeType_ReturnsCorrectValue(string mimeType, bool isMimeType)
        {
            Assert.AreEqual(isMimeType, MimeType.IsMimeType(mimeType, false));
        }

        [TestCase(null, false)]
        [TestCase("", false)]
        [TestCase("text", false)]
        [TestCase("/", false)]
        [TestCase("text/", false)]
        [TestCase("/text", false)]
        [TestCase("text/html,", false)]
        [TestCase("text,/html", false)]
        [TestCase("*/text", false)]
        [TestCase("*/*", true)]
        [TestCase("text/*", true)]
        [TestCase("text/html", true)]
        public void IsMimeTypeOrMediaRange_ReturnsCorrectValue(string mimeType, bool isMimeTypeOrMediaRange)
        {
            Assert.AreEqual(isMimeTypeOrMediaRange, MimeType.IsMimeType(mimeType, true));
        }
    }
}