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
        /// <para>Gets or sets the container of this module.</para>
        /// <para>This API supports the EmbedIO infrastructure; it is not intended to be used directly from your code.</para>
        /// </summary>
        /// <seealso cref="IWebModule.Container"/>
        /// <seealso cref="IWebModuleContainer"/>
        new IWebModuleContainer Container { get; set; }
    }
}