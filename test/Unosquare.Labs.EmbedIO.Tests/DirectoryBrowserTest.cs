namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using System.Threading.Tasks;
    using Modules;
    using TestObjects;
    using Constants;

    [TestFixture]
    public class DirectoryBrowserTest : FixtureBase
    {
        public DirectoryBrowserTest()
            : base(ws => ws.RegisterModule(new StaticFilesModule(TestHelper.SetupStaticFolder(false), true)),
                RoutingStrategy.Wildcard)
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