#if NET47
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
    public class WebServerCultureTest : FixtureBase
    { 
        private const string KoreanDate = "목";

        public WebServerCultureTest() 
            : base(ws => ws.WithWebApiController<TestController>(), Constants.RoutingStrategy.Wildcard)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("ko");
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
    }
}
#endif