using System;
using EmbedIO.Cors;
using EmbedIO.Utilities;
using Swan;

namespace EmbedIO
{
    partial class WebModuleContainerExtensions
    {
        /// <summary>
        /// Creates an instance of <see cref="CorsModule"/> and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="baseRoute">The base route of the module.</param>
        /// <param name="origins">The valid origins. Default is <c>"*"</c>, meaning all origins.</param>
        /// <param name="headers">The valid headers. Default is <c>"*"</c>, meaning all headers.</param>
        /// <param name="methods">The valid method. Default is <c>"*"</c>, meaning all methods.</param>
        /// <returns><paramref name="this"/> with a <see cref="CorsModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="CorsModule"/>
        public static TContainer WithCors<TContainer>(
            this TContainer @this,
            string baseRoute,
            string origins,
            string headers,
            string methods)
            where TContainer : class, IWebModuleContainer
        {
            @this.Modules.Add(new CorsModule(baseRoute, origins, headers, methods));
            return @this;
        }

        /// <summary>
        /// Creates an instance of <see cref="CorsModule"/> and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="origins">The valid origins. Default is <c>"*"</c>, meaning all origins.</param>
        /// <param name="headers">The valid headers. Default is <c>"*"</c>, meaning all headers.</param>
        /// <param name="methods">The valid method. Default is <c>"*"</c>, meaning all methods.</param>
        /// <returns><paramref name="this"/> with a <see cref="CorsModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="CorsModule"/>
        public static TContainer WithCors<TContainer>(
            this TContainer @this,
            string origins = CorsModule.All,
            string headers = CorsModule.All,
            string methods = CorsModule.All)
            where TContainer : class, IWebModuleContainer
            => WithCors(@this, UrlPath.Root, origins, headers, methods);
    }
}