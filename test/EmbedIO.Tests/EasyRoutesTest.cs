using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace EmbedIO.Tests
{
#pragma warning disable 4014

    [TestFixture]
    public class EasyRoutesTest
    {
        private const string Ok = "Ok";

        [Test]
        public async Task AddOnAny_ResponseOK()
        {
            using (var server = new TestWebServer())
            {
                server
                    .OnAny((ctx, path, ct) => ctx.SendStringAsync(Ok, MimeTypes.PlainTextType, Encoding.UTF8, ct));

                server.RunAsync();

                Assert.AreEqual(Ok, await server.GetClient().GetAsync());
            }
        }

        [Test]
        public async Task AddOnGet_ResponseOK()
        {
            using (var server = new TestWebServer())
            {
                server
                    .OnGet((ctx, path, ct) => ctx.SendStringAsync(Ok, MimeTypes.PlainTextType, Encoding.UTF8, ct));

                server.RunAsync();

                Assert.AreEqual(Ok, await server.GetClient().GetAsync());
            }
        }

        [Test]
        public async Task AddOnPost_ResponseOK()
        {
            using (var server = new TestWebServer())
            {
                server
                    .OnPost((ctx, path, ct) => ctx.SendStringAsync(Ok, MimeTypes.PlainTextType, Encoding.UTF8, ct));

                server.RunAsync();

                var response = await server.GetClient().SendAsync(new TestHttpRequest(HttpVerbs.Post));

                Assert.AreEqual(Ok, response.GetBodyAsString());
            }
        }

        [Test]
        public async Task AddOnPut_ResponseOK()
        {
            using (var server = new TestWebServer())
            {
                server
                    .OnPut((ctx, path, ct) => ctx.SendStringAsync(Ok, MimeTypes.PlainTextType, Encoding.UTF8, ct));

                server.RunAsync();

                var response = await server.GetClient().SendAsync(new TestHttpRequest(HttpVerbs.Put));

                Assert.AreEqual(Ok, response.GetBodyAsString());
            }
        }

        [Test]
        public async Task AddOnHead_ResponseOK()
        {
            using (var server = new TestWebServer())
            {
                server
                    .OnHead((ctx, path, ct) => ctx.SendStringAsync(Ok, MimeTypes.PlainTextType, Encoding.UTF8, ct));

                server.RunAsync();

                var response = await server.GetClient().SendAsync(new TestHttpRequest(HttpVerbs.Head));

                Assert.AreEqual(Ok, response.GetBodyAsString());
            }
        }

        [Test]
        public async Task AddOnDelete_ResponseOK()
        {
            using (var server = new TestWebServer())
            {
                server
                    .OnDelete((ctx, path, ct) => ctx.SendStringAsync(Ok, MimeTypes.PlainTextType, Encoding.UTF8, ct));

                server.RunAsync();

                var response = await server.GetClient().SendAsync(new TestHttpRequest(HttpVerbs.Delete));

                Assert.AreEqual(Ok, response.GetBodyAsString());
            }
        }

        [Test]
        public async Task AddOnOptions_ResponseOK()
        {
            using (var server = new TestWebServer())
            {
                server
                    .OnOptions((ctx, path, ct) => ctx.SendStringAsync(Ok, MimeTypes.PlainTextType, Encoding.UTF8, ct));

                server.RunAsync();

                var response = await server.GetClient().SendAsync(new TestHttpRequest(HttpVerbs.Options));

                Assert.AreEqual(Ok, response.GetBodyAsString());
            }
        }

        [Test]
        public async Task AddOnPatch_ResponseOK()
        {
            using (var server = new TestWebServer())
            {
                server
                    .OnPatch((ctx, path, ct) => ctx.SendStringAsync(Ok, MimeTypes.PlainTextType, Encoding.UTF8, ct));

                server.RunAsync();

                var response = await server.GetClient().SendAsync(new TestHttpRequest(HttpVerbs.Patch));

                Assert.AreEqual(Ok, response.GetBodyAsString());
            }
        }
    }
#pragma warning restore 4014
}