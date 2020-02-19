namespace EmbedIO
{
    /// <summary>
    /// <para>Represents a module implementation, i.e. a module as seen internally by EmbedIO.</para>
    /// <para>This API mainly supports the EmbedIO infrastructure; it is not intended to be used directly from your code,
    /// unless to address specific needs in the implementation of EmbedIO plug-ins (e.g. modules).</para>
    /// </summary>
    public interface IWebModuleImpl : IWebModule
    {
        /// <summary>
        /// <para>Sets the container of this module.</para>
        /// <para>This API supports the EmbedIO infrastructure; it is not intended to be used directly from your code.</para>
        /// </summary>
        /// <param name="value">The container to associate this module with.</param>
        /// <seealso cref="IWebModule.Container"/>
        /// <seealso cref="IWebModuleContainer"/>
        void SetContainer(IWebModuleContainer value);
    }
}