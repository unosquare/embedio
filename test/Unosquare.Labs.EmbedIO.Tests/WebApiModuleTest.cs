namespace Unosquare.Labs.EmbedIO.Tests
{
    using Modules;
    using NUnit.Framework;
    using Swan.Formatters;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using TestObjects;

    [TestFixture]
    public class WebApiModuleTest : PersonFixtureBase
    {
        public WebApiModuleTest()
            : base(ws => ws.WithWebApiController<TestController>(), Constants.RoutingStrategy.Wildcard)
        {
        }

        public class WebApiWithConstructor : WebApiModuleTest
        {
            [Test]
            public async Task GetWebApiWithCustomHeader_ReturnsNameFromConstructor()
            {
                const string name = nameof(TestControllerWithConstructor);

                WebServerInstance.Module<WebApiModule>().RegisterController((ctx) => new TestControllerWithConstructor(ctx, name));
                using (var client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl + "name");

                    using (var response = await client.SendAsync(request))
                    {
                        Assert.AreEqual(name, response.Headers.FirstOrDefault(x => x.Key == TestControllerWithConstructor.CustomHeader).Value.FirstOrDefault());
                    }
                }
            }

            [Test]
            public async Task GetWebApiWithCacheControlPublic_ReturnsValidResponse()
            {
                WebServerInstance.Module<WebApiModule>().RegisterController((ctx) => new TestControllerWithConstructor(ctx));
                using (var client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl + "namePublic");

                    using (var response = await client.SendAsync(request))
                    {
                        Assert.IsTrue(response.Headers.CacheControl.Public, "Cache is public");

                        Assert.IsFalse(response.Headers.CacheControl.NoStore, "Cache is not No-Store");
                        Assert.IsFalse(response.Headers.CacheControl.NoCache, "Cache is not No-Cache");
                        Assert.IsFalse(response.Headers.CacheControl.MustRevalidate, "Cache is not Must-Revalidate");
                    }
                }
            }

            [Test]
            public async Task GetWebApiWithCacheControlDefault_ReturnsValidResponse()
            {
                WebServerInstance.Module<WebApiModule>().RegisterController((ctx) => new TestControllerWithConstructor(ctx));
                using (var client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl + "name");

                    using (var response = await client.SendAsync(request))
                    {
                        Assert.IsFalse(response.Headers.CacheControl.Public, "Cache is not public");

                        Assert.IsTrue(response.Headers.CacheControl.NoStore);
                        Assert.IsTrue(response.Headers.CacheControl.NoCache);
                        Assert.IsTrue(response.Headers.CacheControl.MustRevalidate);
                    }
                }
            }
        }

        public class HttpPost : WebApiModuleTest
        {
            [Test]
            public async Task JsonData_ReturnsOk()
            {
                using (var client = new HttpClient())
                {
                    var model = new Person { Key = 10, Name = "Test" };
                    var payloadJson = new StringContent(
                        Json.Serialize(model),
                        System.Text.Encoding.UTF8,
                        "application/json");

                    var response = await client.PostAsync(WebServerUrl + TestController.GetPath, payloadJson);

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
                    var request = new HttpRequestMessage(HttpMethod.Delete, WebServerUrl + TestController.GetPath);

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
                    var content = new[]
                    {
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
                    var content = new[]
                    {
                        new KeyValuePair<string, string>("test", "data"),
                        new KeyValuePair<string, string>("id", "1"),
                    };

                    var formContent = new FormUrlEncodedContent(content);

                    var result = await webClient.PostAsync(WebServerUrl + TestController.EchoPath, formContent);
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
    }

    public class HttpGet : PersonFixtureBase
    {
        public HttpGet()
            : base(ws => ws.WithWebApiController<TestController>(), Constants.RoutingStrategy.Wildcard, true)
        {
        }

        [Test]
        public async Task GetJsonData_ReturnsOk()
        {
            var jsonBody = await GetString(TestController.GetPath);

            Assert.IsNotNull(jsonBody, "Json Body is not null");
            Assert.IsNotEmpty(jsonBody, "Json Body is empty");

            var remoteList = Json.Deserialize<List<Person>>(jsonBody);

            Assert.IsNotNull(remoteList, "Json Object is not null");
            Assert.AreEqual(
                remoteList.Count,
                PeopleRepository.Database.Count,
                "Remote list count equals local list");
        }

        [Test]
        public async Task JsonDataWithSelector_ReturnsOk()
        {
            await ValidatePerson(TestController.GetPath + PeopleRepository.Database.First().Key);
        }

        [Test]
        public async Task JsonDataWithMiddleUrl_ReturnsOk()
        {
            var person = PeopleRepository.Database.First();
            await ValidatePerson(TestController.GetMiddlePath.Replace("*", person.Key.ToString()));
        }
    }
}