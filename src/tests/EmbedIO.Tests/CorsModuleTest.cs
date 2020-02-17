using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using EmbedIO.Tests.TestObjects;
using NUnit.Framework;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class CorsModuleTest : EndToEndFixtureBase
    {
        protected override void OnSetUp()
        {
            Server
                .WithCors(
                    "http://client.cors-api.appspot.com,http://unosquare.github.io,http://run.plnkr.co",
                    "content-type",
                    "post,get")
                .WithWebApi("/api", m => m.RegisterController<TestController>());
        }

        [Test]
        public async Task RequestOptionsVerb_ReturnsOK()
        {
            var request = new HttpRequestMessage(HttpMethod.Options, $"{WebServerUrl}/api/empty");

            request.Headers.Add(HttpHeaderNames.Origin, "http://unosquare.github.io");
            request.Headers.Add(HttpHeaderNames.AccessControlRequestMethod, "post");
            request.Headers.Add(HttpHeaderNames.AccessControlRequestHeaders, "content-type");

            var response = await Client.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Status Code OK");
        }
    }
}