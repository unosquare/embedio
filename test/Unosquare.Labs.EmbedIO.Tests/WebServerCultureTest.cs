﻿#if NETFX
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.Swan.Formatters;
using NUnit.Framework;
using Unosquare.Labs.EmbedIO.Tests.TestObjects;
using System.Globalization;

namespace Unosquare.Labs.EmbedIO.Tests
{
    [TestFixture]
    public class WebServerCultureTest
    {
        protected WebServer WebServer;

        protected string WebServerUrl;
        private const string KoreanDate = "목";

        [SetUp]
        public void Init()
        {
            WebServerUrl = Resources.GetServerAddress();
            Thread.CurrentThread.CurrentCulture = new CultureInfo("ko");

            WebServer = new WebServer(WebServerUrl).WithWebApiController<TestController>();
            WebServer.RunAsync();
        }

        [Test]
        public async Task GetIndex()
        {
            var customDate = new DateTime(2015, 1, 1);
            var stringDate = customDate.ToString("ddd");
            Assert.AreEqual(stringDate, KoreanDate, "Korean date by default in thread");

            var request = (HttpWebRequest) WebRequest.Create(WebServerUrl + TestController.GetPath);

            using (var response = (HttpWebResponse) await request.GetResponseAsync())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var jsonBody = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Assert.IsNotNull(jsonBody, "Json Body is not null");
                Assert.IsNotEmpty(jsonBody, "Json Body is not empty");

                var remoteList = Json.Deserialize<List<Person>>(jsonBody);

                Assert.IsNotNull(remoteList, "Json Object is not null");
                Assert.AreEqual(remoteList.Count, PeopleRepository.Database.Count, "Remote list count equals local list");
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
#endif