using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EmbedIO.Authentication;
using EmbedIO.Utilities;
using NUnit.Framework;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class BasicAuthenticationModuleTest : EndToEndFixtureBase
    {
        private const string UserName = "root";
        private const string Password = "password1234";

        public BasicAuthenticationModuleTest()
            : base(ws =>
                {
                    ws.Modules.Add(new BasicAuthenticationModule("/").WithAccount(UserName, Password));
                    ws.OnAny((ctx, path, ct) =>
                    {
                        ctx.Response.SetEmptyResponse((int)HttpStatusCode.OK);

                        return Task.FromResult(true);
                    });
                },
                true)
        {
            // placeholder
        }

        [Test]
        public async Task RequestWithValidCredentials_ReturnsOK()
        {
            var response = await MakeRequest(UserName, Password);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Status Code OK");
        }

        [Test]
        public async Task RequestWithInvalidCredentials_ReturnsUnauthorized()
        {
            const string wrongPassword = "wrongpaassword";

            var response = await MakeRequest(UserName, wrongPassword);
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode, "Status Code Unauthorized");
        }

        [Test]
        public async Task RequestWithNoAuthorizationHeader_ReturnsUnauthorized()
        {
            var response = await MakeRequest(null, null);
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode, "Status Code Unauthorized");
        }

        private Task<HttpResponseMessage> MakeRequest(string userName, string password)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl);

            if (userName == null) return Client.SendAsync(request);

            var encodedCredentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{userName}:{password}"));
            var authHeaderValue = new System.Net.Http.Headers.AuthenticationHeaderValue("basic", encodedCredentials);
            request.Headers.Add("Authorization", authHeaderValue.ToString());

            return Client.SendAsync(request);
        }
    }
}