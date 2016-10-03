using System;
using Unosquare.Labs.EmbedIO.Log;
using Unosquare.Labs.EmbedIO.Tests.TestObjects;

namespace Unosquare.Labs.EmbedIO.Tests
{
    public static class Program
    {
        public static void Main()
        {
            var webServer =
                new WebServer(Resources.GetServerAddress(), new SimpleConsoleLog(), RoutingStrategy.Regex)
                    .WithWebApiController<TestRegexController>();

            webServer.RunAsync();
            
            Console.ReadLine();
        }
    }
}
