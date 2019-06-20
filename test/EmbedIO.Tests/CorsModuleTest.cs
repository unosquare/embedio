using EmbedIO.Tests.TestObjects;
using NUnit.Framework;
using System.Net;
using System.Threading.Tasks;
using EmbedIO.Testing;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class CorsModuleTest : EndToEndFixtureBase
    {
        public CorsModuleTest()
            : base(ws => ws
                .WithCors(
                    "http://client.cors-api.appspot.com,http://unosquare.github.io,http://run.plnkr.co",
                    "content-type",
                    "post,get")
                .WithWebApi("/api", m => m.RegisterController<TestController>()),
                true)
        {
            // placeholder
        }

        [Test]
        public async Task RequestOptionsVerb_ReturnsOK()
        {
            var request = new TestHttpRequest($"{WebServerUrl}/api/empty", HttpVerbs.Options);

            request.Headers.Add(HttpHeaderNames.Origin, "http://unosquare.github.io");
            request.Headers.Add(HttpHeaderNames.AccessControlRequestMethod, "post");
            request.Headers.Add(HttpHeaderNames.AccessControlRequestHeaders, "content-type");

            var response = await SendAsync(request);
            Assert.AreEqual((int)HttpStatusCode.OK, response.StatusCode, "Status Code OK");
        }
    }
}