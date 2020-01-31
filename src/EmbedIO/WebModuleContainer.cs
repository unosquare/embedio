using EmbedIO.Internal;

namespace EmbedIO
{
    /// <summary>
    /// Provides useful constants for dealing with module containers.
    /// </summary>
    public static class WebModuleContainer
    {
        /// <summary>
        /// <para>Gets an <see cref="IWebModuleContainer"/> interface that does not and cannot contain
        /// any module.</para>
        /// <para>This field is useful to initialize non-nullable fields or properties
        /// of type <see cref="IWebModuleContainer"/>.</para>
        /// </summary>
        public static readonly IWebModuleContainer None = DummyWebModuleContainer.Instance;
    }
}