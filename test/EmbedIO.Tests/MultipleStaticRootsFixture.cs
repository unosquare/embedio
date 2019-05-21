using System.Linq;
using System.Threading.Tasks;
using EmbedIO.Modules;
using EmbedIO.Tests.TestObjects;
using NUnit.Framework;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class MultipleStaticRootsFixture : FixtureBase
    {
        private static readonly string[] InstancesNames = {string.Empty, "A/", "B/", "C/", "A/C", "AAA/A/B/C/", "A/B/C"};

        public MultipleStaticRootsFixture()
            : base(ws =>
                    ws.Modules.Add(nameof(StaticFilesModule),
                        new StaticFilesModule("/", InstancesNames.ToDictionary(x => "/" + x, TestHelper.SetupStaticFolderInstance), FileCachingMode.Complete)),
                true)
        {
        }

        [Test]
        public async Task FileContentsMatchInstanceName()
        {
            foreach (var item in InstancesNames)
            {
                var html = await GetString(item);

                Assert.AreEqual(
                    TestHelper.GetStaticFolderInstanceIndexFileContents(item),
                    html, 
                    "index.html contents match instance name");
            }
        }
    }
}