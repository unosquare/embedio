using EmbedIO.WebApi;

namespace EmbedIO.Tests.TestObjects
{
    public sealed partial class TestRegexModule : WebApiModuleBase
    {
        public TestRegexModule(string baseRoute)
            : base(baseRoute)
        {
            RegisterControllerType<Controller>();
            LockConfiguration();
        }
    }
}
