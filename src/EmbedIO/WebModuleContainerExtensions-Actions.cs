using System;
using EmbedIO.Actions;
using EmbedIO.Utilities;
using Swan;
using Swan.Abstractions;

namespace EmbedIO
{
    partial class WebModuleContainerExtensions
    {
        /// <summary>
        /// Creates an instance of <see cref="ActionModule"/> and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="baseUrlPath">The base URL path of the module.</param>
        /// <param name="verb">The HTTP verb that will be served by <paramref name="handler"/>.</param>
        /// <param name="handler">The callback used to handle requests.</param>
        /// <returns><paramref name="this"/> with a <see cref="ActionModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="ActionModule"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer WithAction<TContainer>(this TContainer @this, string baseUrlPath, HttpVerbs verb, RequestHandlerCallback handler)
            where TContainer : class, IWebModuleContainer
        {
            @this.Modules.Add(new ActionModule(baseUrlPath, verb, handler));
            return @this;
        }

        /// <summary>
        /// Creates an instance of <see cref="ActionModule"/> with a base URL path of <c>"/"</c>
        /// and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="verb">The HTTP verb that will be served by <paramref name="handler"/>.</param>
        /// <param name="handler">The callback used to handle requests.</param>
        /// <returns><paramref name="this"/> with a <see cref="ActionModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="ActionModule"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer WithAction<TContainer>(this TContainer @this, HttpVerbs verb, RequestHandlerCallback handler)
            where TContainer : class, IWebModuleContainer
            => WithAction(@this, UrlPath.Root, verb, handler);

        /// <summary>
        /// Creates an instance of <see cref="ActionModule"/> that intercepts all requests
        /// under the specified <paramref name="baseUrlPath"/> and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="baseUrlPath">The base URL path of the module.</param>
        /// <param name="handler">The callback used to handle requests.</param>
        /// <returns><paramref name="this"/> with a <see cref="ActionModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="ActionModule"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer OnAny<TContainer>(this TContainer @this, string baseUrlPath, RequestHandlerCallback handler)
            where TContainer : class, IWebModuleContainer
            => WithAction(@this, baseUrlPath, HttpVerbs.Any, handler);

        /// <summary>
        /// Creates an instance of <see cref="ActionModule"/> that intercepts all requests
        /// and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="handler">The callback used to handle requests.</param>
        /// <returns><paramref name="this"/> with a <see cref="ActionModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="ActionModule"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer OnAny<TContainer>(this TContainer @this, RequestHandlerCallback handler)
            where TContainer : class, IWebModuleContainer
            => WithAction(@this, UrlPath.Root, HttpVerbs.Any, handler);

        /// <summary>
        /// Creates an instance of <see cref="ActionModule"/> that intercepts all <c>DELETE</c>requests
        /// under the specified <paramref name="baseUrlPath"/> and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="baseUrlPath">The base URL path of the module.</param>
        /// <param name="handler">The callback used to handle requests.</param>
        /// <returns><paramref name="this"/> with a <see cref="ActionModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="ActionModule"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer OnDelete<TContainer>(this TContainer @this, string baseUrlPath, RequestHandlerCallback handler)
            where TContainer : class, IWebModuleContainer
            => WithAction(@this, baseUrlPath, HttpVerbs.Delete, handler);

        /// <summary>
        /// Creates an instance of <see cref="ActionModule"/> that intercepts all <c>DELETE</c>requests
        /// and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="handler">The callback used to handle requests.</param>
        /// <returns><paramref name="this"/> with a <see cref="ActionModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="ActionModule"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer OnDelete<TContainer>(this TContainer @this, RequestHandlerCallback handler)
            where TContainer : class, IWebModuleContainer
            => WithAction(@this, UrlPath.Root, HttpVerbs.Delete, handler);

        /// <summary>
        /// Creates an instance of <see cref="ActionModule"/> that intercepts all <c>GET</c>requests
        /// under the specified <paramref name="baseUrlPath"/> and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="baseUrlPath">The base URL path of the module.</param>
        /// <param name="handler">The callback used to handle requests.</param>
        /// <returns><paramref name="this"/> with a <see cref="ActionModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="ActionModule"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer OnGet<TContainer>(this TContainer @this, string baseUrlPath, RequestHandlerCallback handler)
            where TContainer : class, IWebModuleContainer
            => WithAction(@this, baseUrlPath, HttpVerbs.Get, handler);

        /// <summary>
        /// Creates an instance of <see cref="ActionModule"/> that intercepts all <c>GET</c>requests
        /// and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="handler">The callback used to handle requests.</param>
        /// <returns><paramref name="this"/> with a <see cref="ActionModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="ActionModule"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer OnGet<TContainer>(this TContainer @this, RequestHandlerCallback handler)
            where TContainer : class, IWebModuleContainer
            => WithAction(@this, UrlPath.Root, HttpVerbs.Get, handler);

        /// <summary>
        /// Creates an instance of <see cref="ActionModule"/> that intercepts all <c>HEAD</c>requests
        /// under the specified <paramref name="baseUrlPath"/> and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="baseUrlPath">The base URL path of the module.</param>
        /// <param name="handler">The callback used to handle requests.</param>
        /// <returns><paramref name="this"/> with a <see cref="ActionModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="ActionModule"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer OnHead<TContainer>(this TContainer @this, string baseUrlPath, RequestHandlerCallback handler)
            where TContainer : class, IWebModuleContainer
            => WithAction(@this, baseUrlPath, HttpVerbs.Head, handler);

        /// <summary>
        /// Creates an instance of <see cref="ActionModule"/> that intercepts all <c>HEAD</c>requests
        /// and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="handler">The callback used to handle requests.</param>
        /// <returns><paramref name="this"/> with a <see cref="ActionModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="ActionModule"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer OnHead<TContainer>(this TContainer @this, RequestHandlerCallback handler)
            where TContainer : class, IWebModuleContainer
            => WithAction(@this, UrlPath.Root, HttpVerbs.Head, handler);

        /// <summary>
        /// Creates an instance of <see cref="ActionModule"/> that intercepts all <c>OPTIONS</c>requests
        /// under the specified <paramref name="baseUrlPath"/> and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="baseUrlPath">The base URL path of the module.</param>
        /// <param name="handler">The callback used to handle requests.</param>
        /// <returns><paramref name="this"/> with a <see cref="ActionModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="ActionModule"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer OnOptions<TContainer>(this TContainer @this, string baseUrlPath, RequestHandlerCallback handler)
            where TContainer : class, IWebModuleContainer
            => WithAction(@this, baseUrlPath, HttpVerbs.Options, handler);

        /// <summary>
        /// Creates an instance of <see cref="ActionModule"/> that intercepts all <c>OPTIONS</c>requests
        /// and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="handler">The callback used to handle requests.</param>
        /// <returns><paramref name="this"/> with a <see cref="ActionModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="ActionModule"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer OnOptions<TContainer>(this TContainer @this, RequestHandlerCallback handler)
            where TContainer : class, IWebModuleContainer
            => WithAction(@this, UrlPath.Root, HttpVerbs.Options, handler);

        /// <summary>
        /// Creates an instance of <see cref="ActionModule"/> that intercepts all <c>PATCH</c>requests
        /// under the specified <paramref name="baseUrlPath"/> and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="baseUrlPath">The base URL path of the module.</param>
        /// <param name="handler">The callback used to handle requests.</param>
        /// <returns><paramref name="this"/> with a <see cref="ActionModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="ActionModule"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer OnPatch<TContainer>(this TContainer @this, string baseUrlPath, RequestHandlerCallback handler)
            where TContainer : class, IWebModuleContainer
            => WithAction(@this, baseUrlPath, HttpVerbs.Patch, handler);

        /// <summary>
        /// Creates an instance of <see cref="ActionModule"/> that intercepts all <c>PATCH</c>requests
        /// and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="handler">The callback used to handle requests.</param>
        /// <returns><paramref name="this"/> with a <see cref="ActionModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="ActionModule"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer OnPatch<TContainer>(this TContainer @this, RequestHandlerCallback handler)
            where TContainer : class, IWebModuleContainer
            => WithAction(@this, UrlPath.Root, HttpVerbs.Patch, handler);

        /// <summary>
        /// Creates an instance of <see cref="ActionModule"/> that intercepts all <c>POST</c>requests
        /// under the specified <paramref name="baseUrlPath"/> and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="baseUrlPath">The base URL path of the module.</param>
        /// <param name="handler">The callback used to handle requests.</param>
        /// <returns><paramref name="this"/> with a <see cref="ActionModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="ActionModule"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer OnPost<TContainer>(this TContainer @this, string baseUrlPath, RequestHandlerCallback handler)
            where TContainer : class, IWebModuleContainer
            => WithAction(@this, baseUrlPath, HttpVerbs.Post, handler);

        /// <summary>
        /// Creates an instance of <see cref="ActionModule"/> that intercepts all <c>POST</c>requests
        /// and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="handler">The callback used to handle requests.</param>
        /// <returns><paramref name="this"/> with a <see cref="ActionModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="ActionModule"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer OnPost<TContainer>(this TContainer @this, RequestHandlerCallback handler)
            where TContainer : class, IWebModuleContainer
            => WithAction(@this, UrlPath.Root, HttpVerbs.Post, handler);

        /// <summary>
        /// Creates an instance of <see cref="ActionModule"/> that intercepts all <c>PUT</c>requests
        /// under the specified <paramref name="baseUrlPath"/> and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="baseUrlPath">The base URL path of the module.</param>
        /// <param name="handler">The callback used to handle requests.</param>
        /// <returns><paramref name="this"/> with a <see cref="ActionModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="ActionModule"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer OnPut<TContainer>(this TContainer @this, string baseUrlPath, RequestHandlerCallback handler)
            where TContainer : class, IWebModuleContainer
            => WithAction(@this, baseUrlPath, HttpVerbs.Put, handler);

        /// <summary>
        /// Creates an instance of <see cref="ActionModule"/> that intercepts all <c>PUT</c>requests
        /// and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="handler">The callback used to handle requests.</param>
        /// <returns><paramref name="this"/> with a <see cref="ActionModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="ActionModule"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer OnPut<TContainer>(this TContainer @this, RequestHandlerCallback handler)
            where TContainer : class, IWebModuleContainer
            => WithAction(@this, UrlPath.Root, HttpVerbs.Put, handler);
    }
}