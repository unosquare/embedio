using NUnit.Framework;

namespace EmbedIO.Tests.Issues
{
    public class Issue318_StartupDeadlock
    {
        [TestCase(HttpListenerMode.EmbedIO)]
        [TestCase(HttpListenerMode.Microsoft)]
        public void WebServer_Start_OnListenerStartFailure_Returns(HttpListenerMode listenerMode)
        {
            void ConfigureServerOptions(WebServerOptions options) => options
                .WithMode(listenerMode)
                .WithUrlPrefix("http://*:12345");

            using var server1 = new WebServer(ConfigureServerOptions);
            using var server2 = new WebServer(ConfigureServerOptions);
            server1.Start();
            server2.Start();
            Assert.AreEqual(WebServerState.Stopped, server2.State);
        }
    }
}