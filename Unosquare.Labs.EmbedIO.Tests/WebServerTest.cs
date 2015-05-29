namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using System.Globalization;
    using System.Linq;
    using Unosquare.Labs.EmbedIO.Log;
    using Unosquare.Labs.EmbedIO.Modules;
    using Unosquare.Labs.EmbedIO.Tests.Properties;

    [TestFixture]
    public class WebServerTest
    {
        private const int DefaultPort = 8888;

        [Test]
        public void WebServerDefaultConstructor()
        {
            var instance = new WebServer();
            Assert.AreEqual(instance.Log.GetType(), typeof (NullLog), "Default log is NullLog");
            Assert.IsNotNull(instance.Listener, "It has a HttpListener");
        }

        [Test]
        public void WebServerConstructorWithPortParam()
        {
            var instance = new WebServer(DefaultPort);

            Assert.AreEqual(instance.UrlPrefixes.Count, 1, "It has one URL Prefix");
            Assert.IsTrue(
                instance.UrlPrefixes.First().Contains(DefaultPort.ToString(CultureInfo.InvariantCulture)),
                "Construct with port number is correct");
        }

        [Test]
        public void WebServerConstructorWithPortAndLogParam()
        {
            var instance = new WebServer(DefaultPort, new TestConsoleLog());

            Assert.AreEqual(instance.UrlPrefixes.Count, 1, "It has one URL Prefix");
            Assert.IsTrue(
                instance.UrlPrefixes.First().Contains(DefaultPort.ToString(CultureInfo.InvariantCulture)),
                "Port number is correct");
            Assert.AreEqual(instance.Log.GetType(), typeof (TestConsoleLog), "Log type is correct");
        }

        [Test]
        public void RegisterAndUnregisterModule()
        {
            var instance = new WebServer();
            instance.RegisterModule(new LocalSessionModule());

            Assert.AreEqual(instance.Modules.Count, 1, "It has one module");

            instance.UnregisterModule(typeof (LocalSessionModule));

            Assert.AreEqual(instance.Modules.Count, 0, "It has not modules");
        }

        [Test]
        public void WebServerStaticMethodWithPortParam()
        {
            Assert.AreEqual(WebServer.Create(DefaultPort).Log.GetType(), typeof (NullLog), "Default log is NullLog");
        }

        [Test]
        public void WebServerStaticMethodWithConsole()
        {
            Assert.AreEqual(WebServer.CreateWithConsole(Resources.ServerAddress).Log.GetType(),
                typeof (SimpleConsoleLog), "Default log is NullLog");
        }
    }
}