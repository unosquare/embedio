namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    using Newtonsoft.Json;
    using NUnit.Framework;
    using System.Threading.Tasks;

    public class TestMiddleware : Middleware
    {
        public override async Task Invoke(MiddlewareContext context)
        {
            Assert.IsNotNull(context.WebServer, "Webserver is not null");
            Assert.IsNotNull(context.HttpContext, "HttpContext is not null");
            Assert.IsNotNull(context.Handled, "Handled is not null");

            await Task.Delay(10);

            context.HttpContext.JsonResponse(JsonConvert.SerializeObject(new {Status = "OK"}));

            context.Handled = true;
        }
    }
}