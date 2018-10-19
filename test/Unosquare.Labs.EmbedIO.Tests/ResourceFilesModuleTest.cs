namespace Unosquare.Labs.EmbedIO.Tests
{
    using Constants;
    using Modules;
    using NUnit.Framework;
    using System.Threading.Tasks;
    using TestObjects;

    [TestFixture]
    public class ResourceFilesModuleTest : FixtureBase
    {
        public ResourceFilesModuleTest()
            : base(
                ws =>
                {
                    ws.RegisterModule(new ResourceFilesModule(typeof(ResourceFilesModuleTest).Assembly,
                        "Unosquare.Labs.EmbedIO.Tests.Resources"));
                },
                RoutingStrategy.Wildcard,
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