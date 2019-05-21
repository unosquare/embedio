using System.Threading.Tasks;
using EmbedIO.Modules;
using EmbedIO.Tests.TestObjects;
using NUnit.Framework;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class DirectoryBrowserTest : FixtureBase
    {
        public DirectoryBrowserTest()
            : base(ws => ws.RegisterModule(new StaticFilesModule(TestHelper.SetupStaticFolder(false), true)),
                WebApiRoutingStrategy.Wildcard,
                true)
        {
        }

        public class Browse : DirectoryBrowserTest
        {
            [Test]
            public async Task Root_ReturnsFilesList()
            {
                var htmlContent = await GetString(string.Empty);

                Assert.IsNotEmpty(htmlContent);

                foreach (var file in TestHelper.RandomHtmls)
                    Assert.IsTrue(htmlContent.Contains(file));
            }

            [Test]
            public async Task Subfolder_ReturnsFilesList()
            {
                var htmlContent = await GetString("sub");

                Assert.IsNotEmpty(htmlContent);

                foreach (var file in TestHelper.RandomHtmls)
                    Assert.IsTrue(htmlContent.Contains(file));
            }
        }
    }
}