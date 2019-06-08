using System.Threading.Tasks;
using EmbedIO.Tests.TestObjects;
using NUnit.Framework;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class DirectoryBrowserTest : FixtureBase
    {
        public DirectoryBrowserTest()
            : base(ws => ws.WithStaticFolderAt("/", StaticFolder.RootPathOf(nameof(DirectoryBrowserTest)), useDirectoryBrowser: true))
        {
            ServedFolder = new StaticFolder.WithHtmlFiles(nameof(DirectoryBrowserTest));
        }

        protected StaticFolder.WithHtmlFiles ServedFolder { get; }

        protected override void Dispose(bool disposing)
        {
            ServedFolder.Dispose();
        }

        public class Browse : DirectoryBrowserTest
        {
            [Test]
            public async Task Root_ReturnsFilesList()
            {
                var htmlContent = await GetString(string.Empty);

                Assert.IsNotEmpty(htmlContent);

                foreach (var file in StaticFolder.WithHtmlFiles.RandomHtmls)
                    Assert.IsTrue(htmlContent.Contains(file));
            }

            [Test]
            public async Task Subfolder_ReturnsFilesList()
            {
                var htmlContent = await GetString("sub");

                Assert.IsNotEmpty(htmlContent);

                foreach (var file in StaticFolder.WithHtmlFiles.RandomHtmls)
                    Assert.IsTrue(htmlContent.Contains(file));
            }
        }
    }
}