using EmbedIO.Tests.TestObjects;
using EmbedIO.WebApi;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace EmbedIO.Tests.Issues
{
    [TestFixture]
    public class Issue427_PassHttpException : EndToEndFixtureBase
    {
        protected override void OnSetUp()
        {
            Server.WithWebApi("/api", o =>
                {
                    o.WithController<TestController>();
                    o.OnUnhandledException = (ctx, ex) => throw HttpException.BadRequest();
                });
        }

        [Test]
        public async Task RequestException_ReturnsBadRequest()
        {
            var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"{WebServerUrl}/api/exception"));

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
