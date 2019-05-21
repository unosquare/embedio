using System.Text;
using System.Threading.Tasks;

namespace EmbedIO.Tests.TestObjects
{
    public class TestRegexModule : WebModuleBase
    {
        public TestRegexModule()
        {
            AddHandler("/data/{id}", Constants.HttpVerbs.Any, (ctx, ct) =>
            {
                var buffer = Encoding.UTF8.GetBytes(ctx.RequestRegexUrlParams("/data/{id}")["id"].ToString());
                ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);

                return Task.FromResult(true);
            });

            AddHandler("/data/{id}/{time}", Constants.HttpVerbs.Any, (ctx, ct) =>
            {
                var buffer = Encoding.UTF8.GetBytes(ctx.RequestRegexUrlParams("/data/{id}/{time}")["time"].ToString());
                ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);

                return Task.FromResult(true);
            });
            
            AddHandler("/empty", Constants.HttpVerbs.Any, (ctx, ct) =>
            {
                var buffer = Encoding.UTF8.GetBytes("data");
                ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);

                return Task.FromResult(true);
            });
        }

        public override string Name => nameof(TestRoutingModule);
    }
}
