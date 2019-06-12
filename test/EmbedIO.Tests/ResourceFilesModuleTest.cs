using System.Threading.Tasks;
using EmbedIO.Tests.TestObjects;
using NUnit.Framework;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class ResourceFilesModuleTest : FixtureBase
    {
        public ResourceFilesModuleTest()
            : base(
                ws =>
                    ws.WithEmbeddedResources("/", typeof(ResourceFilesModuleTest).Assembly, "EmbedIO.Tests.Resources"),
                true)
        {
        }

        [Test]
        public async Task GetIndexFile_ReturnsValidContentFromResource()
        {
            var html = await GetString();

            Assert.AreEqual(Resources.Index, html, "Same content index.html");
        }

        [Test]
        public async Task GetSubfolderIndexFile_ReturnsValidContentFromResource()
        {
            var html = await GetString("sub/index.html");

            Assert.AreEqual(Resources.SubIndex, html, "Same content index.html");
        }
    }
}