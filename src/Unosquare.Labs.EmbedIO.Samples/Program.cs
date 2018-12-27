namespace Unosquare.Labs.EmbedIO.Samples
{
    using Modules;
    using System.Security.Cryptography.X509Certificates;
    using System.Text.RegularExpressions;
    using Swan;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal class Program
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        private static async Task Main(string[] args)
        {
            var url = args.Length > 0 ? args[0] : "https://*:7876/";

            AppDbContext.InitDatabase();

            var ctSource = new CancellationTokenSource();
            ctSource.Token.Register(() => "Shutting down".Info());

            // Set a task waiting for press key to exit
#pragma warning disable 4014
            Task.Run(() =>
#pragma warning restore 4014
            {
                // Wait for any key to be pressed before disposing of our web server.
                Console.ReadLine();

                ctSource.Cancel();
            }, ctSource.Token);

            var certificate = GetCertificate("767b9a3ad23a0cfc597df8be23d58984503c7ad8");
            var webOptions = new WebServerOptions(url) {Certificate = certificate};

            // Our web server is disposable. 
            using (var server = new WebServer(webOptions))
            {
                // First, we will configure our web server by adding Modules.
                // Please note that order DOES matter.
                // ================================================================================================
                // If we want to enable sessions, we simply register the LocalSessionModule
                // Beware that this is an in-memory session storage mechanism so, avoid storing very large objects.
                // You can use the server.GetSession() method to get the SessionInfo object and manipulate it.
                server.RegisterModule(new LocalSessionModule());

                // Set the CORS Rules
                server.RegisterModule(new CorsModule(
                    // Origins, separated by comma without last slash
                    "http://unosquare.github.io,http://run.plnkr.co",
                    // Allowed headers
                    "content-type, accept",
                    // Allowed methods
                    "post"));

                // Register the static files server. See the html folder of this project. Also notice that 
                // the files under the html folder have Copy To Output Folder = Copy if Newer
                StaticFilesSample.Setup(server, useGzip: Runtime.IsUsingMonoRuntime == false);

                // Register the Web Api Module. See the Setup method to find out how to do it
                // It registers the WebApiModule and registers the controller(s) -- that's all.
                server.WithWebApiController<PeopleController>();

                // Register the WebSockets module. See the Setup method to find out how to do it
                // It registers the WebSocketsModule and registers the server for the given paths(s)
                WebSocketsSample.Setup(server);

                server.RegisterModule(new FallbackModule((ctx, ct) => ctx.JsonResponse(new { Message = "Error" })));

                // Fire up the browser to show the content!
                var browser = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo(url.Replace("*", "localhost"))
                    {
                        UseShellExecute = true
                    }
                };

                browser.Start();

                // Once we've registered our modules and configured them, we call the RunAsync() method.
                if (!ctSource.IsCancellationRequested)
                    await server.RunAsync(ctSource.Token);

                "Bye".Info();

                Terminal.Flush();
            }
        }

        public static X509Certificate2 GetCertificate(string thumbprint)
        {
            // strip any non-hexadecimal values and make uppercase
            thumbprint = Regex.Replace(thumbprint, @"[^\da-fA-F]", string.Empty).ToUpper();
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);

            try
            {
                store.Open(OpenFlags.ReadOnly);

                var certCollection = store.Certificates;
                var signingCert = certCollection.Find(X509FindType.FindByThumbprint, thumbprint, false);
                if (signingCert.Count == 0)
                {
                    throw new Exception(string.Format("Cert with thumbprint: '{0}' not found in local machine cert store.", thumbprint));
                }

                return signingCert[0];
            }
            finally
            {
                store.Close();
            }
        }
    }
}
