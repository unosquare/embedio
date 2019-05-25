using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Utilities;

namespace EmbedIO.Routing
{
    /// <summary>
    /// Implements the logic for resolving a context and a URL path against a route,
    /// possibly handling different contexts via different handlers.
    /// </summary>
    /// <typeparam name="TContext">The type of the context.</typeparam>
    /// <typeparam name="TData">The type of the data used to select a suitable handler
    /// for a context.</typeparam>
    /// <seealso cref="ConfiguredObject" />
    public abstract class RouteResolverBase<TContext, TData> : ConfiguredObject
    {
        private readonly RouteMatcher _matcher;
        private readonly List<(TData data, RoutedHandler<TContext> handler)> _dataHandlerPairs
            = new List<(TData data, RoutedHandler<TContext> handler)>();

        /// <summary>
        /// Initializes a new instance of the <see cref="RouteResolverBase{TContext,TData}"/> class.
        /// </summary>
        /// <param name="route">The route to match URL paths against.</param>
        protected RouteResolverBase(string route)
        {
            _matcher = RouteMatcher.Parse(route);
        }

        /// <summary>
        /// Gets the route this resolver matches URL paths against.
        /// </summary>
        public string Route => _matcher.Route;

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
        /// <seealso cref="RoutedHandler{TContext}"/>
        /// <seealso cref="ResolveAsync"/>
        /// <seealso cref="GetContextData"/>
        /// <seealso cref="MatchContextData"/>
        public void Add(TData data, RoutedHandler<TContext> handler)
        {
            EnsureConfigurationNotLocked();

            handler = Validate.NotNull(nameof(handler), handler);
            _dataHandlerPairs.Add((data, handler));
        }

        /// <summary>
        /// Locks this instance, preventing further handler additions.
        /// </summary>
        public void Lock() => LockConfiguration();

        /// <summary>
        /// Asynchronously matches a URL path against <see cref="Route"/>;
        /// if the match is successful, tries to handle the specified <paramref name="context"/>
        /// using handlers selected according to data extracted from the context.
        /// <para>Registered data / handler pairs are tried in the same order they were added by calling
        /// <see cref="Add"/>.</para>
        /// </summary>
        /// <param name="context">The context to handle.</param>
        /// <param name="path">The URL path to match against <see cref="Route"/>.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> use to cancel the operation.</param>
        /// <returns>A <see cref="Task"/>, representing the ongoing operation,
        /// that will return a result in the form of one of the <see cref="RouteResolutionResult"/> constants.</returns>
        /// <seealso cref="Add"/>
        /// <seealso cref="GetContextData"/>
        /// <seealso cref="MatchContextData"/>
        public async Task<RouteResolutionResult> ResolveAsync(TContext context, string path, CancellationToken cancellationToken)
        {
            LockConfiguration();

            var parameters = _matcher.Match(path);
            if (parameters == null)
                return RouteResolutionResult.RouteNotMatched;

            var contextData = GetContextData(context);
            var result = RouteResolutionResult.NoHandlerSelected;
            foreach (var (data, handler) in _dataHandlerPairs)
            {
                if (!MatchContextData(contextData, data))
                    continue;

                if (await handler(context, path, parameters, cancellationToken).ConfigureAwait(false))
                    return RouteResolutionResult.Success;

                result = RouteResolutionResult.NoHandlerSuccessful;
            }

            return result;
        }

        /// <summary>
        /// <para>Called by <see cref="ResolveAsync"/> to extract data from a context.</para>
        /// <para>The extracted data are then used to select which handlers are suitable
        /// to handle the context.</para>
        /// </summary>
        /// <param name="context">The context to extract data from.</param>
        /// <returns>The extracted data.</returns>
        /// <seealso cref="ResolveAsync"/>
        /// <seealso cref="MatchContextData"/>
        protected abstract TData GetContextData(TContext context);

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