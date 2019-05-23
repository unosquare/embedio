using EmbedIO.Modules;

namespace EmbedIO.Tests.TestObjects
{
    public partial class TestRegexModule : WebApiModule
    {
        public TestRegexModule()
        : base("/")
        {
            RegisterController<Controller>();
        }
    }
}
