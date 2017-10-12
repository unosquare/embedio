namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    using System.Text;
    using System.Threading.Tasks;

    public class TestRoutingModule : WebModuleBase
    {
        public TestRoutingModule()
        {
            AddHandler("/data/*", Constants.HttpVerbs.Any, (ctx, ct) =>
            {
                var buffer = Encoding.UTF8.GetBytes("data");
                ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
                
                return Task.FromResult(true);
            });
        }

        public override string Name => nameof(TestRoutingModule);
    }
}
