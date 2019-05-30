using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EmbedIO.Tests.TestObjects;
using EmbedIO.WebApi;
using NUnit.Framework;
using Unosquare.Swan.Formatters;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class RegexWebApiModuleTest : PersonFixtureBase
    {
        public RegexWebApiModuleTest()
            : base(ws => ws.WithWebApi("/api", m => m.WithController<TestController>()), true)
        {
        }

        public class GetJsonData : RegexWebApiModuleTest
        {
            [Test]
            public async Task WithoutRegex_ReturnsOk()
            {
                var jsonString = await GetString("/api/empty");

                Assert.IsNotEmpty(jsonString);
            }

            [Test]
            public async Task WithRegexId_ReturnsOk()
            {
                await ValidatePerson("/api/regex/1");
            }

            [Test]
            public async Task WithOptRegexIdAndValue_ReturnsOk()
            {
                await ValidatePerson("/api/regexopt/1");
            }

            [Test]
            public async Task WithOptRegexIdAndNonValue_ReturnsOk()
            {
                var jsonBody = await GetString("/api/regexopt");
                var remoteList = Json.Deserialize<List<Person>>(jsonBody);

                Assert.AreEqual(
                    remoteList.Count,
                    PeopleRepository.Database.Count,
                    "Remote list count equals local list");
            }

            [Test]
            public async Task WithRegexDate_ReturnsOk()
            {
                var person = PeopleRepository.Database.First();
                await ValidatePerson($"/api/regexdate/{person.DoB:yyyy-MM-dd}");
            }

            [Test]
            public async Task WithRegexWithTwoParams_ReturnsOk()
            {
                var person = PeopleRepository.Database.First();
                await ValidatePerson($"/api/regextwo/{person.MainSkill}/{person.Age}");
            }

            [Test]
            public async Task WithRegexWithOptionalParams_ReturnsOk()
            {
                var person = PeopleRepository.Database.First();

                await ValidatePerson($"/api/egexthree/{person.MainSkill}");
            }
        }

        public class Http405 : RegexWebApiModuleTest
        {
            [Test]
            public async Task ValidWebApiPathInvalidMethod_Returns405()
            {
                var request = new TestHttpRequest(WebServerUrl + "/api/regex/1", HttpVerbs.Delete);

                var response = await SendAsync(request);

                Assert.AreEqual((int)HttpStatusCode.MethodNotAllowed, response.StatusCode);
            }
        }
    }
}