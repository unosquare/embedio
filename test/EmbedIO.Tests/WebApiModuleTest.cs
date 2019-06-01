using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using EmbedIO.Tests.TestObjects;
using EmbedIO.WebApi;
using NUnit.Framework;
using Unosquare.Swan.Formatters;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class WebApiModuleTest : PersonFixtureBase
    {
        public WebApiModuleTest()
            : base(ws => ws.WithWebApi("/api", m => m.WithController<TestController>()))
        {
        }
        
        public class HttpPost : WebApiModuleTest
        {
            [Test]
            public async Task JsonData_ReturnsOk()
            {
                using (var client = new HttpClient())
                {
                    var model = new Person {Key = 10, Name = "Test"};
                    var payloadJson = new StringContent(
                        Json.Serialize(model),
                        System.Text.Encoding.UTF8,
                        MimeTypes.JsonType);

                    var response = await client.PostAsync(WebServerUrl + "/api/regex", payloadJson);

                    var result = Json.Deserialize<Person>(await response.Content.ReadAsStringAsync());
                    Assert.IsNotNull(result);
                    Assert.AreEqual(result.Name, model.Name);
                }
            }
        }

        public class Http405 : WebApiModuleTest
        {
            [Test]
            public async Task ValidPathInvalidMethod_Returns405()
            {
                using (var client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Delete, WebServerUrl + "/api/regex");

                    var response = await client.SendAsync(request);

                    Assert.AreEqual(response.StatusCode, HttpStatusCode.MethodNotAllowed);
                }
            }
        }

        public class FormData : WebApiModuleTest
        {
            [TestCase("id", "id")]
            [TestCase("id[0]", "id[1]")]
            public async Task MultipleIndexedValues_ReturnsOk(string label1, string label2)
            {
                using (var webClient = new HttpClient())
                {
                    var content = new[] {
                        new KeyValuePair<string, string>("test", "data"),
                        new KeyValuePair<string, string>(label1, "1"),
                        new KeyValuePair<string, string>(label2, "2"),
                    };

                    var formContent = new FormUrlEncodedContent(content);

                    var result = await webClient.PostAsync(WebServerUrl + TestController.EchoPath, formContent);
                    Assert.IsNotNull(result);
                    var data = await result.Content.ReadAsStringAsync();
                    var obj = Json.Deserialize<FormDataSample>(data);
                    Assert.IsNotNull(obj);
                    Assert.AreEqual(content.First().Value, obj.test);
                    Assert.AreEqual(2, obj.id.Count);
                    Assert.AreEqual(content.Last().Value, obj.id.Last());
                }
            }

            [Test]
            public async Task TestDictionaryFormData_ReturnsOk()
            {
                using (var webClient = new HttpClient())
                {
                    var content = new[] {
                        new KeyValuePair<string, string>("test", "data"),
                        new KeyValuePair<string, string>("id", "1"),
                    };

                    var formContent = new FormUrlEncodedContent(content);

                    var result =
                        await webClient.PostAsync(WebServerUrl + "api/" + TestController.EchoPath, formContent);

                    Assert.IsNotNull(result);
                    var data = await result.Content.ReadAsStringAsync();
                    var obj = Json.Deserialize<Dictionary<string, string>>(data);
                    Assert.AreEqual(2, obj.Keys.Count);

                    Assert.AreEqual(content.First().Key, obj.First().Key);
                    Assert.AreEqual(content.First().Value, obj.First().Value);
                }
            }
        }

        internal class FormDataSample
        {
            public string test { get; set; }
            public List<string> id { get; set; }
        }

        public class GetJsonData : WebApiModuleTest
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
    }
}