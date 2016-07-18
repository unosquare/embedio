using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Labs.EmbedIO.Tests.Properties;
using Unosquare.Labs.EmbedIO.Tests.TestObjects;

namespace Unosquare.Labs.EmbedIO.Tests
{
    [TestFixture]
    public class MultipleStaticRootsFixture
    {
        protected string RootPath;
        protected WebServer WebServer;
        protected TestConsoleLog Logger = new TestConsoleLog();
        protected string[] InstancesNames = { string.Empty, "A/", "B/", "C/", "A/C", "AAA/A/B/C", "A/B/C/"};
        
        [SetUp]
        public void Init()
        {
            TestHelper.SetupStaticFolder();

            var additionalPaths = InstancesNames.ToDictionary(x => "/" + x, TestHelper.SetupStaticFolderInstance);

            WebServer = new WebServer(Resources.ServerAddress, Logger);
            WebServer.RegisterModule(new StaticFilesModule(additionalPaths) { UseRamCache = true });
            WebServer.RunAsync();
        }

        [Test]
        public void FileContentsMatchInstanceName()
        {
            foreach (var item in InstancesNames)
            {
                var request = (HttpWebRequest) WebRequest.Create(Resources.ServerAddress + item);

                using (var response = (HttpWebResponse) request.GetResponse())
                {
                    Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");
                    var html = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    
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