using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO.Tests.TestObjects
{
    public class TestRoutingModule : WebModuleBase
    {
        public TestRoutingModule()
            : base("/") { }

        public override Task<bool> HandleRequestAsync(IHttpContext context, string path, CancellationToken ct)
        {
            // TODO: Riccardo, I'm not sure how this could be implemented
            //AddHandler("/data/*", Constants.HttpVerbs.Any, (ctx, ct) =>
            //{
            //    var buffer = Encoding.UTF8.GetBytes(ctx.RequestWildcardUrlParams("/data/*").LastOrDefault() ?? string.Empty);
            //    ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);

            //    return Task.FromResult(true);
            //});

            //AddHandler("/empty", Constants.HttpVerbs.Any, (ctx, ct) =>
            //{
            //    var buffer = Encoding.UTF8.GetBytes("data");
            //    ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);

            //    return Task.FromResult(true);
            //});
            throw new NotImplementedException();

        }
    }
}