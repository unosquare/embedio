using EmbedIO.Files;
using EmbedIO.Tests.TestObjects;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using EmbedIO.Testing;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class ResourceFileProviderTest
    {
        private readonly IFileProvider _fileProvider = new ResourceFileProvider(
            Assembly.GetExecutingAssembly(),
            "EmbedIO.Tests.Resources");

        private readonly IMimeTypeProvider _mimeTypeProvider = new MockMimeTypeProvider();

        [TestCase("/index.html", "index.html")]
        [TestCase("/sub/index.html", "index.html")]
        public void MapFile_ReturnsCorrectFileInfo(string urlPath, string name)
        {
            var info = _fileProvider.MapUrlPath(urlPath, _mimeTypeProvider);

            Assert.IsNotNull(info, "info != null");
            Assert.IsTrue(info.IsFile, "info.IsFile == true");
            Assert.AreEqual(name, info.Name, "info.Name has the correct value");
            Assert.AreEqual(Resource.GetLength(urlPath), info.Length, "info.Length has the correct value");
        }

        [TestCase("/index.html")]
        [TestCase("/sub/index.html")]
        public void OpenFile_ReturnsCorrectContent(string urlPath)
        {
            var info = _fileProvider.MapUrlPath(urlPath, _mimeTypeProvider);

            var expectedText = Resource.GetText(urlPath, Encoding.UTF8);
            string actualText;
            using (var stream = _fileProvider.OpenFile(info.Path))
            using (var reader = new StreamReader(stream, Encoding.UTF8, false, WebServer.StreamCopyBufferSize, true))
            {
                actualText = reader.ReadToEnd();
            }

            Assert.AreEqual(expectedText, actualText, "Content is the same as embedded resource");
        }

        [Test]
        public void GetDirectoryEntries_ReturnsEmptyEnumerable()
        {
            var entries = _fileProvider.GetDirectoryEntries(string.Empty, _mimeTypeProvider);
            Assert.IsFalse(entries.Any(), "There are no entries");
        }
    }
}