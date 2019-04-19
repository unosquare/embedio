namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using Swan.Formatters;
    using TestObjects;
    using System.Threading.Tasks;
    using Modules;

    public class IWebServerTest
    {
        [Test]
        public void SetupInMemoryWebServer_ReturnsValidInstance()
        {
            using (var webserver = new TestWebServer())
            {
                Assert.IsNotNull(webserver);
            }
        }

        [Test]
        public void RegisterWebModule_ReturnsValidInstance()
        {
            using (var webserver = new TestWebServer())
            {
                webserver.RegisterModule(new FallbackModule((ctx, ct) => ctx.JsonResponseAsync(nameof(TestWebServer), ct)));

                Assert.AreEqual(1, webserver.Modules.Count);
            }
        }

        [Test]
        public void UnregisterWebModule_ReturnsValidInstance()
        {
            using (var webserver = new TestWebServer())
            {
                webserver.RegisterModule(new FallbackModule((ctx, ct) => ctx.JsonResponseAsync(nameof(TestWebServer), ct)));
                webserver.UnregisterModule(typeof(FallbackModule));

                Assert.AreEqual(0, webserver.Modules.Count);
            }
        }

        [Test]
        public void RegisterSessionModule_ReturnsValidInstance()
        {
            using (var webserver = new TestWebServer())
            {
                webserver.RegisterModule(new LocalSessionModule());

                Assert.NotNull(webserver.SessionModule);
            }
        }

        [Test]
        public void UnregisterSessionModule_ReturnsValidInstance()
        {
            using (var webserver = new TestWebServer())
            {
                webserver.RegisterModule(new LocalSessionModule());
                webserver.UnregisterModule(typeof(LocalSessionModule));

                Assert.IsNull(webserver.SessionModule);
            }
        }

        [Test]
        public async Task RunsServerAndRequestData_ReturnsValidData()
        {
            using (var webserver = new TestWebServer())
            {
                webserver.OnAny((ctx, ct) => ctx.JsonResponseAsync(new Person {Name = nameof(Person)}, ct));

#pragma warning disable 4014
                webserver.RunAsync();
#pragma warning restore 4014

                var client = webserver.GetClient();

                var data = await client.GetAsync("/");
                Assert.IsNotNull(data);

                var person = Json.Deserialize<Person>(data);
                Assert.IsNotNull(person);

                Assert.AreEqual(person.Name, nameof(Person));
            }
        }
    }
}