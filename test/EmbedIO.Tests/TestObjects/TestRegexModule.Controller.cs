using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Net;
using EmbedIO.Routing;
using EmbedIO.WebApi;

namespace EmbedIO.Tests.TestObjects
{
    partial class TestRegexModule
    {
        public class Controller : WebApiController
        {
            public Controller(IHttpContext context, CancellationToken cancellationToken)
                : base(context, cancellationToken)
            {
            }

            [RouteHandler(HttpVerbs.Any, "/data/{id!}")]
            public Task<bool> Id(string id)
                => HttpContext.Response.StringResponseAsync(id, "text/plain", Encoding.UTF8, false, CancellationToken);

            [RouteHandler(HttpVerbs.Any, "/data/{id!}/{time}")]
            public Task<bool> Time(string id, string time)
                => HttpContext.Response.StringResponseAsync(time, "text/plain", Encoding.UTF8, false, CancellationToken);

            [RouteHandler(HttpVerbs.Any, "/empty")]
            public bool Empty()
            {
                HttpContext.Response.StandardResponseWithoutBody((int) HttpStatusCode.OK);
                return true;
            }
        }
    }
}