﻿namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Modules;
    using TestObjects;

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
            Swan.Terminal.Settings.DisplayLoggingMessageType = Swan.LogMessageType.None;

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
            Task.Delay(TimeSpan.FromSeconds(1)).Wait();
            WebServer.Dispose();
        }
    }
}