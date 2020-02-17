using System.Threading.Tasks;
using EmbedIO.Files;
using EmbedIO.Tests.TestObjects;
using EmbedIO.Utilities;
using NUnit.Framework;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class DirectoryBrowserTest : EndToEndFixtureBase
    {
        public DirectoryBrowserTest()
            : base(false)
        {
            ServedFolder = new StaticFolder.WithHtmlFiles(nameof(DirectoryBrowserTest));
        }

        protected StaticFolder.WithHtmlFiles ServedFolder { get; }

        protected override void OnSetUp()
        {
            Server
                .WithStaticFolder("/", StaticFolder.RootPathOf(nameof(DirectoryBrowserTest)), true, m => m
                    .WithDirectoryLister(DirectoryLister.Html)
                    .WithoutDefaultDocument());
        }

        protected override void Dispose(bool disposing)
        {
            ServedFolder.Dispose();
        }

        public class Browse : DirectoryBrowserTest
        {
            [Test]
            public async Task Root_ReturnsFilesList()
            {
                var htmlContent = await Client.GetStringAsync(UrlPath.Root);

                Assert.IsNotEmpty(htmlContent);

                foreach (var file in StaticFolder.WithHtmlFiles.RandomHtmls)
                    Assert.IsTrue(htmlContent.Contains(file));
            }

            [Test]
            public async Task Subfolder_ReturnsFilesList()
            {
                var htmlContent = await Client.GetStringAsync("/sub");

                Assert.IsNotEmpty(htmlContent);

                foreach (var file in StaticFolder.WithHtmlFiles.RandomHtmls)
                    Assert.IsTrue(htmlContent.Contains(file));
            }
        }
    }
}