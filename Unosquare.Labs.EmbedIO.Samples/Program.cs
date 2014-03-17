namespace Unosquare.Labs.EmbedIO.Samples
{
    using log4net;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    class Program
    {
        private static readonly ILog Log = Logger.For<Program>();

        static void Main(string[] args)
        {
            using (var server = new WebServer("http://localhost:9696/", Log))
            {

                // First, we will configure our web server by adding Modules.
                // Please not order does matter.

                // If we want to enable sessions, wesimply register the LocalSessionModule
                // Beware that this is an in-memory session storage mechanism
                // You can use the server.GetSession() method to get the SessionInfo object and manupulate it.
                server.Modules.Add(new Modules.LocalSessionWebModule());

                // Register the Web Api Module. See the Setup method to find out how to do it
                // It registers the WebApiModule and registers the controller(s) -- that's all.
                RestApiSample.Setup(server);

                server.Run();
                Console.ReadKey(true);
            }

            Logger.Shutdown();
        }
    }
}
