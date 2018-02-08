namespace Unosquare.Labs.EmbedIO.Tests
{
    using Constants;
    using NUnit.Framework;
    using Swan.Formatters;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using TestObjects;

    [TestFixture]
    public class RegexWebApiModuleTest : FixtureBase
    {
        public RegexWebApiModuleTest()
            : base(ws => ws.WithWebApiController<TestRegexController>(), RoutingStrategy.Regex)
        {
        }

        public class GetJsonData : RegexWebApiModuleTest
        {
            [Test]
            public async Task WithoutRegex()
            {
                var jsonString = await GetString(WebServerUrl + TestRegexController.RelativePath + "empty");

                Assert.IsNotEmpty(jsonString);
            }

            [Test]
            public async Task WithRegexId()
            {
                await TestHelper.ValidatePerson(WebServerUrl + TestRegexController.RelativePath + "regex/1");
            }

            [Test]
            public async Task WithOptRegexId()
            {
                using (var client = new HttpClient())
                {
                    // using null value
                    var request = new HttpRequestMessage(HttpMethod.Get,
                        WebServerUrl + TestRegexController.RelativePath + "regexopt");

                    using (var response = await client.SendAsync(request))
                    {
                        Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                        var jsonBody = await response.Content.ReadAsStringAsync();

                        Assert.IsNotNull(jsonBody, "Json Body is not null");
                        Assert.IsNotEmpty(jsonBody, "Json Body is not empty");

                        var remoteList = Json.Deserialize<List<Person>>(jsonBody);

                        Assert.IsNotNull(remoteList, "Json Object is not null");
                        Assert.AreEqual(remoteList.Count, PeopleRepository.Database.Count,
                            "Remote list count equals local list");
                    }

                    // using a value
                    await TestHelper.ValidatePerson(WebServerUrl + TestRegexController.RelativePath + "regexopt/1");
                }
            }

            [Test]
            public async Task AsyncWithRegexId()
            {
                await TestHelper.ValidatePerson(WebServerUrl + TestRegexController.RelativePath + "regexAsync/1");
            }

            [Test]
            public async Task WithRegexDate()
            {
                var person = PeopleRepository.Database.First();
                await TestHelper.ValidatePerson(WebServerUrl + TestRegexController.RelativePath + "regexdate/" +
                                                person.DoB.ToString("yyyy-MM-dd"));
            }

            [Test]
            public async Task WithRegexWithTwoParams()
            {
                var person = PeopleRepository.Database.First();
                await TestHelper.ValidatePerson(WebServerUrl + TestRegexController.RelativePath + "regextwo/" +
                                                person.MainSkill + "/" + person.Age);
            }
        }

        public class Http405 : RegexWebApiModuleTest
        {
            [Test]
            public async Task ValidPathInvalidMethod_Returns405()
            {
                using (var client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Delete,
                        WebServerUrl + TestRegexController.RelativePath + "regex/1");

                    var response = await client.SendAsync(request);

                    Assert.AreEqual(response.StatusCode, HttpStatusCode.MethodNotAllowed);
                }
            }
        }
    }
}