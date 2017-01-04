namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Unosquare.Labs.EmbedIO.Modules;
    using Unosquare.Labs.EmbedIO.Tests.TestObjects;

    [TestFixture]
    public class MultipleStaticRootsFixture
    {
        protected string RootPath;
        protected WebServer WebServer;
        
        protected string WebServerUrl;
        protected string[] InstancesNames = {string.Empty, "A/", "B/", "C/", "A/C", "AAA/A/B/C/", "A/B/C"};

        [SetUp]
        public void Init()
        {
            WebServerUrl = Resources.GetServerAddress();
            TestHelper.SetupStaticFolder();

            var additionalPaths = InstancesNames.ToDictionary(x => "/" + x, TestHelper.SetupStaticFolderInstance);

            WebServer = new WebServer(WebServerUrl);
            WebServer.RegisterModule(new StaticFilesModule(additionalPaths) {UseRamCache = true});
            WebServer.RunAsync();
        }

        [Test]
        public async Task FileContentsMatchInstanceName()
        {
            foreach (var item in InstancesNames)
            {
                using (var htmlClient = new HttpClient())
                {
                    var html = await htmlClient.GetStringAsync(WebServerUrl + item);

                    Assert.AreEqual(html, TestHelper.GetStaticFolderInstanceIndexFileContents(item),
                        "index.html contents match instance name");
                }
            }
        }

        [TearDown]
        public void Kill()
        {
            Thread.Sleep(TimeSpan.FromSeconds(1));
            WebServer.Dispose();
        }
    }
}