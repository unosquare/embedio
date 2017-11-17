namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Modules;
    using TestObjects;

    [TestFixture]
    public class MultipleStaticRootsFixture : FixtureBase
    {
        protected string[] InstancesNames = {string.Empty, "A/", "B/", "C/", "A/C", "AAA/A/B/C/", "A/B/C"};

        public MultipleStaticRootsFixture() 
            : base(ws=> 
            ws.RegisterModule(
                new StaticFilesModule(new[] 
                { string.Empty, "A/", "B/", "C/", "A/C", "AAA/A/B/C/", "A/B/C" }
                .ToDictionary(x => "/" + x, TestHelper.SetupStaticFolderInstance)) { UseRamCache = true })
            , Constants.RoutingStrategy.Wildcard)
        {
        }
        
        [Test]
        public async Task FileContentsMatchInstanceName()
        {
            foreach (var item in InstancesNames)
            {
                    var html = await GetString(item);

                    Assert.AreEqual(html, TestHelper.GetStaticFolderInstanceIndexFileContents(item),
                        "index.html contents match instance name");
            }
        }
    }
}