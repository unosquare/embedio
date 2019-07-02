using System.Net;
using System.Text;
using System.Threading.Tasks;
using EmbedIO.Routing;
using EmbedIO.WebApi;

namespace EmbedIO.Tests.TestObjects
{
    partial class TestRegexModule
    {
        public class Controller : WebApiController
        {
            [Route(HttpVerbs.Any, "/data/{id}")]
            public Task<bool> Id(string id)
                => HttpContext.SendStringAsync(id, MimeType.PlainText, Encoding.UTF8, CancellationToken);

            [Route(HttpVerbs.Any, "/data/{id}/{time?}")]
            public Task<bool> Time(string id, string time)
                => HttpContext.SendStringAsync(time, MimeType.PlainText, Encoding.UTF8, CancellationToken);

            [Route(HttpVerbs.Any, "/empty")]
            public bool Empty()
            {
                HttpContext.Response.SetEmptyResponse((int) HttpStatusCode.OK);
                return true;
            }
        }
    }
}