using EmbedIO.WebApi;

namespace EmbedIO.Tests.TestObjects
{
    public sealed partial class TestRegexModule : WebApiModuleBase
    {
        public TestRegexModule(string baseUrlPath)
            : base(baseUrlPath)
        {
            RegisterControllerType<Controller>();
            LockConfiguration();
        }
    }
}
