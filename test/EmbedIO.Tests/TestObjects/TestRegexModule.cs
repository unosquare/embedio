using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO.Tests.TestObjects
{
    public class TestRegexModule : WebModuleBase
    {
        public TestRegexModule()
        : base("/")
        {
        }

        public override Task<bool> HandleRequestAsync(IHttpContext context, string path, CancellationToken ct)
        {
            // TODO: Riccardo, I'm not sure how this could be implemented
            //AddHandler("/data/{id}", Constants.HttpVerbs.Any, (ctx, ct) =>
            //{
            //    var buffer = Encoding.UTF8.GetBytes(ctx.RequestRegexUrlParams("/data/{id}")["id"].ToString());
            //    ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);

            //    return Task.FromResult(true);
            //});

            //AddHandler("/data/{id}/{time}", Constants.HttpVerbs.Any, (ctx, ct) =>
            //{
            //    var buffer = Encoding.UTF8.GetBytes(ctx.RequestRegexUrlParams("/data/{id}/{time}")["time"].ToString());
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
