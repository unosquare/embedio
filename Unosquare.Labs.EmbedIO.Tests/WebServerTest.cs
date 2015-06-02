using Unosquare.Labs.EmbedIO.Tests.TestObjects;

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
        private const string DefaultPath = "/";

        [Test]
        public void WebServerDefaultConstructor()
        {
            var instance = new WebServer();
            Assert.AreEqual(instance.Log.GetType(), typeof (NullLog), "Default log is NullLog");
            Assert.IsNotNull(instance.Listener, "It has a HttpListener");
            Assert.IsNotNull(Constants.DefaultMimeTypes, "It has MimeTypes");
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
            const string errorMessage = "THIS IS AN ERROR";
            var instance = WebServer.CreateWithConsole(Resources.ServerAddress);

            Assert.AreEqual(instance.Log.GetType(), typeof(SimpleConsoleLog), "Log is SimpleConsoleLog");

            // TODO: Grab console output
            instance.Log.Error(errorMessage);
            instance.Log.DebugFormat("Test {0}", errorMessage);
            instance.Log.ErrorFormat("Test {0}", errorMessage);
            instance.Log.Info(errorMessage);
            instance.Log.InfoFormat("Test {0}", errorMessage);
            instance.Log.WarnFormat("Test {0}", errorMessage);
        }

        [Test]
        public void WebMap()
        {
            var map = new Map() {Path = DefaultPath, ResponseHandler = (ctx, ws) => false, Verb = HttpVerbs.Any};

            Assert.AreEqual(map.Path, DefaultPath, "Default Path is correct");
            Assert.AreEqual(map.Verb, HttpVerbs.Any, "Default Verb is correct");
        }

        [Test]
        public void WebModuleAddHandler()
        {
            var webModule = new TestWebModule();
            webModule.AddHandler(DefaultPath, HttpVerbs.Any, (ctx, ws) => false);

            Assert.AreEqual(webModule.Handlers.Count, 1, "WebModule has one handler");
            Assert.AreEqual(webModule.Handlers.First().Path, DefaultPath, "Default Path is correct");
            Assert.AreEqual(webModule.Handlers.First().Verb, HttpVerbs.Any, "Default Verb is correct");
        }
    }
}