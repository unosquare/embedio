using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Unosquare.Labs.EmbedIO.Tests
{
    public class TestMiddleware : Middleware
    {
        public override async Task Invoke(MiddlewareContext context)
        {
            if (context == null) throw new ArgumentException("Context is null", "context");

            await Task.Delay(10);

            context.HttpContext.JsonResponse(JsonConvert.SerializeObject(new {Status = "OK"}));

            context.Handled = true;
        }
    }
}