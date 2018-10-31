#pragma warning disable 4014
namespace Unosquare.Labs.EmbedIO.Tests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class EasyRoutesTest
    {
        private const string Ok = "Ok";

        [Test]
        public async Task AddOnAny_ResponseOK()
        {
            var server = new TestWebServer();

            server
                .OnAny((ctx, ct) => ctx.StringResponseAsync(Ok, cancellationToken: ct));

            server.RunAsync();

            Assert.AreEqual(Ok, await server.GetClient().GetAsync());
        }

        [Test]
        public async Task AddOnGet_ResponseOK()
        {
            var server = new TestWebServer();

            server
                .OnGet((ctx, ct) => ctx.StringResponseAsync(Ok, cancellationToken: ct));

            server.RunAsync();

            Assert.AreEqual(Ok, await server.GetClient().GetAsync());
        }

        [Test]
        public async Task AddOnPost_ResponseOK()
        {
            var server = new TestWebServer();

            server
                .OnPost((ctx, ct) => ctx.StringResponseAsync(Ok, cancellationToken: ct));

            server.RunAsync();

            var response = await server.GetClient().SendAsync(new TestHttpRequest(Constants.HttpVerbs.Post));

            Assert.AreEqual(Ok, response.GetBodyAsString());
        }

        [Test]
        public async Task AddOnPut_ResponseOK()
        {
            var server = new TestWebServer();

            server
                .OnPost((ctx, ct) => ctx.StringResponseAsync(Ok, cancellationToken: ct));

            server.RunAsync();

            var response = await server.GetClient().SendAsync(new TestHttpRequest(Constants.HttpVerbs.Put));

            Assert.AreEqual(Ok, response.GetBodyAsString());
        }

        [Test]
        public async Task AddOnHead_ResponseOK()
        {
            var server = new TestWebServer();

            server
                .OnPost((ctx, ct) => ctx.StringResponseAsync(Ok, cancellationToken: ct));

            server.RunAsync();

            var response = await server.GetClient().SendAsync(new TestHttpRequest(Constants.HttpVerbs.Head));

            Assert.AreEqual(Ok, response.GetBodyAsString());
        }
        
        [Test]
        public async Task AddOnDelete_ResponseOK()
        {
            var server = new TestWebServer();

            server
                .OnPost((ctx, ct) => ctx.StringResponseAsync(Ok, cancellationToken: ct));

            server.RunAsync();

            var response = await server.GetClient().SendAsync(new TestHttpRequest(Constants.HttpVerbs.Delete));

            Assert.AreEqual(Ok, response.GetBodyAsString());
        }

        [Test]
        public async Task AddOnOptions_ResponseOK()
        {
            var server = new TestWebServer();

            server
                .OnPost((ctx, ct) => ctx.StringResponseAsync(Ok, cancellationToken: ct));

            server.RunAsync();

            var response = await server.GetClient().SendAsync(new TestHttpRequest(Constants.HttpVerbs.Options));

            Assert.AreEqual(Ok, response.GetBodyAsString());
        }

        [Test]
        public async Task AddOnPartial_ResponseOK()
        {
            var server = new TestWebServer();

            server
                .OnPost((ctx, ct) => ctx.StringResponseAsync(Ok, cancellationToken: ct));

            server.RunAsync();

            var response = await server.GetClient().SendAsync(new TestHttpRequest(Constants.HttpVerbs.Partial));

            Assert.AreEqual(Ok, response.GetBodyAsString());
        }
    }
}
#pragma warning restore 4014
