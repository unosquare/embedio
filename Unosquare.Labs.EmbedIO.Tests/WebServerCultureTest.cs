using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using NUnit.Framework;
using Unosquare.Labs.EmbedIO.Tests.Properties;
using Unosquare.Labs.EmbedIO.Tests.TestObjects;

namespace Unosquare.Labs.EmbedIO.Tests
{
    [TestFixture]
    public class WebServerCultureTest
    {
        protected WebServer WebServer;
        protected TestConsoleLog Logger = new TestConsoleLog();
        const string KoreanDate = "목";

        [SetUp]
        public void Init()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("ko");
            WebServer = new WebServer(Resources.ServerAddress, Logger).WithWebApiController<TestController>();
            WebServer.RunAsync();
        }

        [Test]
        public void GetIndex()
        {
            var customDate = new DateTime(2015, 1, 1);
            var stringDate = customDate.ToString("ddd");
            Assert.AreEqual(stringDate, KoreanDate, "Korean date by default in thread");

            var request = (HttpWebRequest)WebRequest.Create(Resources.ServerAddress + TestController.GetPath);

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var jsonBody = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Assert.IsNotNullOrEmpty(jsonBody, "Json Body is not null or empty");

                var remoteList = JsonConvert.DeserializeObject<List<TestController.Person>>(jsonBody);

                Assert.IsNotNull(remoteList, "Json Object is not null");
                Assert.AreEqual(remoteList.Count, TestController.People.Count, "Remote list count equals local list");
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
