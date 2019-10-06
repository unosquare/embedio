using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EmbedIO.Testing;
using NUnit.Framework;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class ActionModuleTest
    {
        private const string Ok = "Ok";

        [Test]
        public Task OnAny_ResponseOK()
        {
            void Configure(IWebServer server) => server
                .OnAny(ctx => ctx.SendStringAsync(Ok, MimeType.PlainText, Encoding.UTF8));

            async Task Use(HttpClient client)
            {
                using var response = await client.GetAsync("/").ConfigureAwait(false);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Assert.AreEqual(Ok, responseString);
            }

            return TestWebServer.UseAsync(Configure, Use);
        }

        [Test]
        public Task OnGet_ResponseOK()
        {
            void Configure(IWebServer server) => server
                .OnGet(ctx => ctx.SendStringAsync(Ok, MimeType.PlainText, Encoding.UTF8));

            async Task Use(HttpClient client)
            {
                using var response = await client.GetAsync("/").ConfigureAwait(false);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Assert.AreEqual(Ok, responseString);
            }

            return TestWebServer.UseAsync(Configure, Use);
        }

        [Test]
        public Task OnPost_ResponseOK()
        {
            void Configure(IWebServer server) => server
                .OnPost(ctx => ctx.SendStringAsync(Ok, MimeType.PlainText, Encoding.UTF8));

            async Task Use(HttpClient client)
            {
                using var response = await client.PostAsync("/", null).ConfigureAwait(false);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Assert.AreEqual(Ok, responseString);
            }

            return TestWebServer.UseAsync(Configure, Use);
        }

        [Test]
        public Task OnPut_ResponseOK()
        {
            void Configure(IWebServer server) => server
                .OnPut(ctx => ctx.SendStringAsync(Ok, MimeType.PlainText, Encoding.UTF8));

            async Task Use(HttpClient client)
            {
                using var response = await client.PutAsync("/", null).ConfigureAwait(false);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Assert.AreEqual(Ok, responseString);
            }

            return TestWebServer.UseAsync(Configure, Use);
        }

        [Test]
        public Task OnHead_ResponseOK()
        {
            void Configure(IWebServer server) => server
                .OnHead(ctx => ctx.SendStringAsync(Ok, MimeType.PlainText, Encoding.UTF8));

            async Task Use(HttpClient client)
            {
                using var response = await client.HeadAsync("/").ConfigureAwait(false);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Assert.AreEqual(Ok, responseString);
            }

            return TestWebServer.UseAsync(Configure, Use);
        }

        [Test]
        public Task OnDelete_ResponseOK()
        {
            void Configure(IWebServer server) => server
                .OnDelete(ctx => ctx.SendStringAsync(Ok, MimeType.PlainText, Encoding.UTF8));

            async Task Use(HttpClient client)
            {
                using var response = await client.DeleteAsync("/").ConfigureAwait(false);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Assert.AreEqual(Ok, responseString);
            }

            return TestWebServer.UseAsync(Configure, Use);
        }

        [Test]
        public Task OnOptions_ResponseOK()
        {
            void Configure(IWebServer server) => server
                .OnOptions(ctx=> ctx.SendStringAsync(Ok, MimeType.PlainText, Encoding.UTF8));

            async Task Use(HttpClient client)
            {
                using var response = await client.OptionsAsync("/").ConfigureAwait(false);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Assert.AreEqual(Ok, responseString);
            }

            return TestWebServer.UseAsync(Configure, Use);
        }

        [Test]
        public Task OnPatch_ResponseOK()
        {
            void Configure(IWebServer server) => server
                .OnPatch(ctx => ctx.SendStringAsync(Ok, MimeType.PlainText, Encoding.UTF8));

            async Task Use(HttpClient client)
            {
                using var response = await client.PatchAsync("/", null).ConfigureAwait(false);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Assert.AreEqual(Ok, responseString);
            }

            return TestWebServer.UseAsync(Configure, Use);
        }
    }
}