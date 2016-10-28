namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Unosquare.Labs.EmbedIO.Modules;
    using Unosquare.Labs.EmbedIO.Tests.TestObjects;

    [TestFixture]
    public class LocalSessionModuleTest
    {
        private const string CookieName = "__session";
        protected string RootPath;
        protected WebServer WebServer;
        
        protected string WebServerUrl = Resources.GetServerAddress();
        protected TestConsoleLog Logger = new TestConsoleLog();

        [SetUp]
        public void Init()
        {
            RootPath = TestHelper.SetupStaticFolder();

            WebServer = new WebServer(WebServerUrl, Logger);
            WebServer.RegisterModule(new LocalSessionModule());
            WebServer.RegisterModule(new StaticFilesModule(RootPath));
            WebServer.RunAsync();
        }

        [Test]
        public void HasSessionModule()
        {
            Assert.IsNotNull(WebServer.SessionModule, "Session module is not null");
            Assert.AreEqual(WebServer.SessionModule.Handlers.Count, 1, "Session module has one handler");
        }

        [Test]
        public async Task GetCookie()
        {
            var request = (HttpWebRequest) WebRequest.Create(WebServerUrl);
            request.CookieContainer = new CookieContainer();

            using (var response = (HttpWebResponse) await request.GetResponseAsync())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                Assert.IsNotNull(response.Cookies, "Cookies are not null");
                Assert.Greater(response.Cookies.Count, 0, "Cookies are not empty");

                var content = response.Cookies[CookieName]?.Value;

                Assert.IsNotEmpty(content, "Cookie content is not null");
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