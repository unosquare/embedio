using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO.Constants;

namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    public class TestWebModule : WebModuleBase
    {
        public static string RedirectUrl = "redirect";
        public static string RedirectAbsoluteUrl = "redirectAbsolute";
        public static string AnotherUrl = "anotherUrl";

        public TestWebModule()
        {
            IsWatchdogEnabled = false;

            AddHandler("/" + RedirectUrl, HttpVerbs.Get, (context, ct) =>
            {
                context.Redirect("/" + AnotherUrl, false);
                return Task.FromResult(true);
            });

            AddHandler("/" + RedirectAbsoluteUrl, HttpVerbs.Get, (context, ct) =>
            {
                context.Redirect("/" + AnotherUrl);
                return Task.FromResult(true);
            });

            AddHandler("/" + AnotherUrl, HttpVerbs.Get, (server, context) => Task.FromResult(true));
        }

        public override string Name => nameof(TestWebModule);
    }
}