using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
                => HttpContext.SendStringAsync(id, MimeTypes.PlainTextType, Encoding.UTF8, CancellationToken);

            [RouteHandler(HttpVerbs.Any, "/data/{id!}/{time}")]
            public Task<bool> Time(string id, string time)
                => HttpContext.SendStringAsync(time, MimeTypes.PlainTextType, Encoding.UTF8, CancellationToken);

            [RouteHandler(HttpVerbs.Any, "/empty")]
            public bool Empty()
            {
                HttpContext.Response.SetEmptyResponse((int) HttpStatusCode.OK);
                return true;
            }
        }
    }
}