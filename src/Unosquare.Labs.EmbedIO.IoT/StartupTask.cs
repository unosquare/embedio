using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Unosquare.Swan;
using Unosquare.Labs.EmbedIO.Modules;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace Unosquare.Labs.EmbedIO.IoT
{
    public sealed class StartupTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            "Running a test".Info();
            var server = new WebServer(9090)
                .WithWebApiController<ApiController>();

            server.RegisterModule(new FallbackModule((ctx, ct) =>
            {
                ctx.JsonResponse(new { Hola = "Message" });
                return true;
            }));

            await server.RunAsync();

            deferral.Complete();
        }
    }
}
