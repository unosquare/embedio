using NUnit.Framework;
using System.Net.Http;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Labs.EmbedIO.Tests.TestObjects;

namespace Unosquare.Labs.EmbedIO.Tests
{
    [TestFixture]
    public class DirectoryBrowserTest
    {
        protected string RootPath;
        protected WebServer WebServer;

        protected string WebServerUrl;

        [SetUp]
        public void Init()
        {
            Swan.Terminal.Settings.DisplayLoggingMessageType = Swan.LogMessageType.None;

            WebServerUrl = Resources.GetServerAddress();
            RootPath = TestHelper.SetupStaticFolder(false);

            WebServer = new WebServer(WebServerUrl);
            WebServer.RegisterModule(new StaticFilesModule(RootPath, true));
            var runTask = WebServer.RunAsync();
        }
        
        [TearDown]
        public void Kill()
        {
            Task.Delay(500).Wait();
            WebServer?.Dispose();
        }

        public class Browse : DirectoryBrowserTest
        {
            [Test]
            public async Task Root_ReturnsFilesList()
            {
                var httpClient = new HttpClient();
                var htmlContent = await httpClient.GetStringAsync(WebServerUrl);

                Assert.IsNotEmpty(htmlContent);

                foreach (var file in TestHelper.RandomHtmls)
                    Assert.IsTrue(htmlContent.Contains(file));
            }

            [Test]
            public async Task Subfolder_ReturnsFilesList()
            {
                var httpClient = new HttpClient();
                var htmlContent = await httpClient.GetStringAsync(WebServerUrl + "sub");

                Assert.IsNotEmpty(htmlContent);

                foreach (var file in TestHelper.RandomHtmls)
                    Assert.IsTrue(htmlContent.Contains(file));
            }
        }
    }
}
