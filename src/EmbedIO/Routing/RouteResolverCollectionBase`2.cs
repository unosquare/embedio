using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmbedIO.Internal;
using EmbedIO.Utilities;
using Swan;
using Swan.Collections;
using Swan.Configuration;

namespace EmbedIO.Routing
{
    /// <summary>
    /// Implements the logic for resolving a context and a URL path against a list of routes,
    /// possibly handling different HTTP methods via different handlers.
    /// </summary>
    /// <typeparam name="TData">The type of the data used to select a suitable handler
    /// for a context.</typeparam>
    /// <typeparam name="TResolver">The type of the route resolver.</typeparam>
    /// <seealso cref="Utilities.ComponentCollection{T}" />
    public abstract class RouteResolverCollectionBase<TData, TResolver> : ConfiguredObject
        where TResolver : RouteResolverBase<TData>
    {
        private readonly List<TResolver> _resolvers = new List<TResolver>();

        /// <summary>
        /// Associates some data and a route to a handler.
        /// </summary>
        /// <param name="data">Data used to determine which contexts are
        /// suitable to be handled by <paramref name="handler"/>.</param>
        /// <param name="matcher">The <see cref="RouteMatcher"/>to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="matcher"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="InvalidOperationException">The <see cref="CreateResolver"/> method
        /// returned <see langword="null"/>.</exception>
        /// <seealso cref="ResolveAsync"/>
        /// <seealso cref="Add(TData,RouteMatcher,SyncRouteHandlerCallback)"/>
        /// <seealso cref="RouteResolverBase{TData}.Add(TData,RouteHandlerCallback)"/>
        public void Add(TData data, RouteMatcher matcher, RouteHandlerCallback handler)
        {
            matcher = Validate.NotNull(nameof(matcher), matcher);
            handler = Validate.NotNull(nameof(handler), handler);
            GetResolver(matcher).Add(data, handler);
        }

        /// <summary>
        /// Associates some data and a route to a synchronous handler.
        /// </summary>
        /// <param name="data">Data used to determine which contexts are
        /// suitable to be handled by <paramref name="handler"/>.</param>
        /// <param name="matcher">The <see cref="RouteMatcher"/>to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="matcher"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="InvalidOperationException">The <see cref="CreateResolver"/> method
        /// returned <see langword="null"/>.</exception>
        /// <seealso cref="ResolveAsync"/>
        /// <seealso cref="Add(TData,RouteMatcher,RouteHandlerCallback)"/>
        /// <seealso cref="RouteResolverBase{TData}.Add(TData,SyncRouteHandlerCallback)"/>
        public void Add(TData data, RouteMatcher matcher, SyncRouteHandlerCallback handler)
        {
            matcher = Validate.NotNull(nameof(matcher), matcher);
            handler = Validate.NotNull(nameof(handler), handler);
            GetResolver(matcher).Add(data, handler);
        }

        /// <summary>
        /// Asynchronously matches a URL path against <see cref="Route"/>;
        /// if the match is successful, tries to handle the specified <paramref name="context"/>
        /// using handlers selected according to data extracted from the context.
        /// <para>Registered resolvers are tried in the same order they were added by calling
        /// <see cref="Utilities.IComponentCollection{T}.Add"/>.</para>
        /// </summary>
        /// <param name="context">The context to handle.</param>
        /// <returns>A <see cref="Task"/>, representing the ongoing operation,
        /// that will return a result in the form of one of the <see cref="RouteResolutionResult"/> constants.</returns>
        /// <seealso cref="RouteResolverBase{TData}.ResolveAsync"/>
        public async Task<RouteResolutionResult> ResolveAsync(IHttpContext context)
        {
            var result = RouteResolutionResult.RouteNotMatched;
            foreach (var resolver in _resolvers)
            {
                var resolverResult = await resolver.ResolveAsync(context).ConfigureAwait(false);
                OnResolverCalled(context, resolver, resolverResult);
                if (resolverResult == RouteResolutionResult.Success)
                    return RouteResolutionResult.Success;

                // This is why RouteResolutionResult constants must not be reordered.
                if (resolverResult > result)
                    result = resolverResult;
            }

            return result;
        }

        /// <summary>
        /// Locks this collection, preventing further additions.
        /// </summary>
        public void Lock() => LockConfiguration();

        /// <inheritdoc />
        protected override void OnBeforeLockConfiguration()
        {
            foreach (var resolver in _resolvers)
                resolver.Lock();
        }

        /// <summary>
        /// <para>Called by <see cref="Add(TData,RouteMatcher,RouteHandlerCallback)"/>
        /// and <see cref="Add(TData,RouteMatcher,SyncRouteHandlerCallback)"/> to create an instance
        /// of <typeparamref name="TResolver"/> that can resolve the specified route.</para>
        /// <para>If this method returns <see langword="null"/>, an <see cref="InvalidOperationException"/>
        /// is thrown by the calling method.</para>
        /// </summary>
        /// <param name="matcher">The <see cref="RouteMatcher"/>to match URL paths against.</param>
        /// <returns>A newly-constructed instance of <typeparamref name="TResolver"/>.</returns>
        protected abstract TResolver CreateResolver(RouteMatcher matcher);
        
        /// <summary>
        /// <para>Called by <see cref="ResolveAsync"/> when a resolver's
        /// <see cref="RouteResolverBase{TData}.ResolveAsync">ResolveAsync</see> method has been called
        /// to resolve a context.</para>
        /// <para>This callback method may be used e.g. for logging or testing.</para>
        /// </summary>
        /// <param name="context">The context to handle.</param>
        /// <param name="resolver">The resolver just called.</param>
        /// <param name="result">The result returned by <paramref name="resolver"/>.<see cref="RouteResolverBase{TData}.ResolveAsync">ResolveAsync</see>.</param>
        protected virtual void OnResolverCalled(IHttpContext context, TResolver resolver, RouteResolutionResult result)
        {
        }

        private TResolver GetResolver(RouteMatcher matcher)
        {
            var resolver = _resolvers.FirstOrDefault(r => r.Matcher.Equals(matcher));
            if (resolver != null)
                return resolver;

            resolver = CreateResolver(matcher);
            _resolvers.Add(resolver ?? throw SelfCheck.Failure($"{nameof(CreateResolver)} returned null."));
            return resolver;
        }
    }
}
