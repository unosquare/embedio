using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using EmbedIO.Authentication;
using EmbedIO.Tests.Internal;
using EmbedIO.Utilities;
using NUnit.Framework;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class BasicAuthenticationModuleTest : FixtureBase
    {
        private const string UserName = "root";
        private const string Password = "password1234";
        private const string WrongPassword = "wrongpaassword";

        public BasicAuthenticationModuleTest()
            : base(ws =>
                {
                    ws.Modules.Add(new BasicAuthenticationModule("/").WithAccount(UserName, Password));
                },
                true)
        {
            // placeholder
        }

        [Test]
        public async Task RequestWithValidCredentials_ReturnsOK()
        {
            var response = await MakeRequest(UserName, Password);
            Assert.AreEqual((int)HttpStatusCode.OK, response.StatusCode, "Status Code OK");
        }

        [Test]
        public async Task RequestWithInvalidCredentials_ReturnsUnauthorized()
        {
            var response = await MakeRequest(UserName, WrongPassword);
            Assert.AreEqual((int)HttpStatusCode.Unauthorized, response.StatusCode, "Status Code Unauthorized");
        }

        [Test]
        public async Task RequestWithNoAuthorizationHeader_ReturnsUnauthorized()
        {
            var response = await MakeRequest(null, null);
            Assert.AreEqual((int)HttpStatusCode.Unauthorized, response.StatusCode, "Status Code Unauthorized");
        }

        private Task<TestHttpResponse> MakeRequest(string userName, string password)
        {
            var request = new TestHttpRequest(WebServerUrl);

            if (userName != null)
            {
                var encodedCredentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{userName}:{password}"));
                var authHeaderValue = new System.Net.Http.Headers.AuthenticationHeaderValue("basic", encodedCredentials);
                request.Headers.Add("Authorization", authHeaderValue.ToString());
            }

            return SendAsync(request);
        }
    }
}