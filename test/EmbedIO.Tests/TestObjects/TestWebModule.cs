using System.Threading.Tasks;
using EmbedIO.Constants;

namespace EmbedIO.Tests.TestObjects
{
    public class TestWebModule : WebModuleBase
    {
        public const string RedirectUrl = "redirect";
        public const string RedirectAbsoluteUrl = "redirectAbsolute";
        public const string AnotherUrl = "anotherUrl";

        public TestWebModule()
        {
            AddHandler("/" + RedirectUrl, 
                HttpVerbs.Get,
                (context, ct) => Task.FromResult(context.Redirect("/" + AnotherUrl, false)));

            AddHandler("/" + RedirectAbsoluteUrl, 
                HttpVerbs.Get,
                (context, ct) => Task.FromResult(context.Redirect("/" + AnotherUrl)));

            AddHandler("/" + AnotherUrl, HttpVerbs.Get, (server, context) => Task.FromResult(true));
        }

        public override string Name => nameof(TestWebModule);
    }
}