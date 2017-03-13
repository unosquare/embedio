namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    public class TestWebModule : WebModuleBase
    {
        public static string RedirectUrl = "redirect";
        public static string RedirectAbsoluteUrl = "redirectAbsolute";
        public static string AnotherUrl = "anotherUrl";

        public TestWebModule()
        {
            AddHandler("/" + RedirectUrl, HttpVerbs.Get, (server, context) =>
            {
                context.Redirect("/" + AnotherUrl, false);
                return true;
            });

            AddHandler("/" + RedirectAbsoluteUrl, HttpVerbs.Get, (server, context) =>
            {
                context.Redirect("/" + AnotherUrl, true);
                return true;
            });

            AddHandler("/" + AnotherUrl, HttpVerbs.Get, (server, context) => true);
        }

        public override string Name => nameof(TestWebModule);
    }
}
