using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EmbedIO.Internal;
using EmbedIO.Utilities;
using Swan.Configuration;

namespace EmbedIO.Routing
{
    /// <summary>
    /// Implements the logic for resolving the requested path of a HTTP context against a route,
    /// possibly handling different contexts via different handlers.
    /// </summary>
    /// <typeparam name="TData">The type of the data used to select a suitable handler
    /// for the context.</typeparam>
    /// <seealso cref="ConfiguredObject" />
    public abstract class RouteResolverBase<TData> : ConfiguredObject
    {
        private readonly List<(TData data, RouteHandlerCallback handler)> _dataHandlerPairs
            = new List<(TData data, RouteHandlerCallback handler)>();

        /// <summary>
        /// Initializes a new instance of the <see cref="RouteResolverBase{TData}"/> class.
        /// </summary>
        /// <param name="matcher">The <see cref="RouteMatcher"/> to match URL paths against.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="matcher"/> is <see langword="null"/>.</para>
        /// </exception>
        protected RouteResolverBase(RouteMatcher matcher)
        {
            Matcher = Validate.NotNull(nameof(matcher), matcher);
        }

        /// <summary>
        /// Gets the <see cref="RouteMatcher"/> used to match routes.
        /// </summary>
        public RouteMatcher Matcher { get; }

        /// <summary>
        /// Gets the route this resolver matches URL paths against.
        /// </summary>
        public string Route => Matcher.Route;

        /// <summary>
        /// Gets a value indicating whether <see cref="Route"/> is a base route.
        /// </summary>
        public bool IsBaseRoute => Matcher.IsBaseRoute;

        /// <summary>
        /// <para>Associates some data to a handler.</para>
        /// <para>The <see cref="ResolveAsync"/> method calls <see cref="GetContextData"/>
        /// to extract data from the context; then, for each registered data / handler pair,
        /// <see cref="MatchContextData"/> is called to determine whether <paramref name="handler"/>
        /// should be called.</para>
        /// </summary>
        /// <param name="data">Data used to determine which contexts are
        /// suitable to be handled by <paramref name="handler"/>.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <see langword="null"/>.</exception>
        /// <seealso cref="RouteHandlerCallback"/>
        /// <seealso cref="ResolveAsync"/>
        /// <seealso cref="GetContextData"/>
        /// <seealso cref="MatchContextData"/>
        public void Add(TData data, RouteHandlerCallback handler)
        {
            EnsureConfigurationNotLocked();

            handler = Validate.NotNull(nameof(handler), handler);
            _dataHandlerPairs.Add((data, handler));
        }

        /// <summary>
        /// <para>Associates some data to a synchronous handler.</para>
        /// <para>The <see cref="ResolveAsync"/> method calls <see cref="GetContextData"/>
        /// to extract data from the context; then, for each registered data / handler pair,
        /// <see cref="MatchContextData"/> is called to determine whether <paramref name="handler"/>
        /// should be called.</para>
        /// </summary>
        /// <param name="data">Data used to determine which contexts are
        /// suitable to be handled by <paramref name="handler"/>.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <see langword="null"/>.</exception>
        /// <seealso cref="RouteHandlerCallback"/>
        /// <seealso cref="ResolveAsync"/>
        /// <seealso cref="GetContextData"/>
        /// <seealso cref="MatchContextData"/>
        public void Add(TData data, SyncRouteHandlerCallback handler)
        {
            EnsureConfigurationNotLocked();

            handler = Validate.NotNull(nameof(handler), handler);
            _dataHandlerPairs.Add((data, (ctx, route) => {
                handler(ctx, route);
                return Task.CompletedTask;
            }));
        }

        /// <summary>
        /// Locks this instance, preventing further handler additions.
        /// </summary>
        public void Lock() => LockConfiguration();

        /// <summary>
        /// Asynchronously matches a URL path against <see cref="Route"/>;
        /// if the match is successful, tries to handle the specified <paramref name="context"/>
        /// using handlers selected according to data extracted from the context.
        /// <para>Registered data / handler pairs are tried in the same order they were added.</para>
        /// </summary>
        /// <param name="context">The context to handle.</param>
        /// <returns>A <see cref="Task"/>, representing the ongoing operation,
        /// that will return a result in the form of one of the <see cref="RouteResolutionResult"/> constants.</returns>
        /// <seealso cref="Add(TData,RouteHandlerCallback)"/>
        /// <seealso cref="Add(TData,SyncRouteHandlerCallback)"/>
        /// <seealso cref="GetContextData"/>
        /// <seealso cref="MatchContextData"/>
        public async Task<RouteResolutionResult> ResolveAsync(IHttpContext context)
        {
            LockConfiguration();

            var match = Matcher.Match(context.RequestedPath);
            if (match == null)
                return RouteResolutionResult.RouteNotMatched;

            var contextData = GetContextData(context);
            var result = RouteResolutionResult.NoHandlerSelected;
            foreach (var (data, handler) in _dataHandlerPairs)
            {
                if (!MatchContextData(contextData, data))
                    continue;

                try
                {
                    await handler(context, match).ConfigureAwait(false);
                    return RouteResolutionResult.Success;
                }
                catch (RequestHandlerPassThroughException)
                {
                    result = RouteResolutionResult.NoHandlerSuccessful;
                }
            }

            return result;
        }

        /// <summary>
        /// <para>Called by <see cref="ResolveAsync"/> to extract data from a context.</para>
        /// <para>The extracted data are then used to select which handlers are suitable
        /// to handle the context.</para>
        /// </summary>
        /// <param name="context">The HTTP context to extract data from.</param>
        /// <returns>The extracted data.</returns>
        /// <seealso cref="ResolveAsync"/>
        /// <seealso cref="MatchContextData"/>
        protected abstract TData GetContextData(IHttpContext context);

        /// <summary>
        /// Called by <see cref="ResolveAsync"/> to match data extracted from a context
        /// against data associated with a handler.
        /// </summary>
        /// <param name="contextData">The data extracted from the context.</param>
        /// <param name="handlerData">The data associated with the handler.</param>
        /// <returns><see langword="true"/> if the handler should be called to handle the context;
        /// otherwise, <see langword="false"/>.</returns>
        protected abstract bool MatchContextData(TData contextData, TData handlerData);
    }
}