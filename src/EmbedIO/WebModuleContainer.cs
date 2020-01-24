using EmbedIO.Internal;

namespace EmbedIO
{
    public static class WebModuleContainer
    {
        public static IWebModuleContainer None => DummyWebModuleContainer.Instance;
    }
}