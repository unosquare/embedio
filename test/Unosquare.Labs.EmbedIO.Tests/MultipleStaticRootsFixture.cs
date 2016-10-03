using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Labs.EmbedIO.Tests.TestObjects;

namespace Unosquare.Labs.EmbedIO.Tests
{
    [TestFixture]
    public class MultipleStaticRootsFixture
    {
        protected string RootPath;
        protected WebServer WebServer;
        
        protected string WebServerUrl = Resources.GetServerAddress();
        protected string[] InstancesNames = {string.Empty, "A/", "B/", "C/", "A/C", "AAA/A/B/C/", "A/B/C"};

        [SetUp]
        public void Init()
        {
            TestHelper.SetupStaticFolder();

            var additionalPaths = InstancesNames.ToDictionary(x => "/" + x, TestHelper.SetupStaticFolderInstance);

            WebServer = new WebServer(WebServerUrl, new TestConsoleLog());
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