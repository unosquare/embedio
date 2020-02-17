using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Security.Internal;

namespace EmbedIO.Security
{
    /// <summary>
    /// A module to ban clients by IP address, based on TCP requests-per-second or RegEx matches on log messages.
    /// </summary>
    /// <seealso cref="WebModuleBase" />
    public class IPBanningModule : WebModuleBase, IDisposable
    {
        /// <summary>
        /// The default ban minutes.
        /// </summary>
        public const int DefaultBanMinutes = 30;

        private const string NoConfigurationFound = "No configuration was found for the base route provided.";

        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="IPBanningModule" /> class.
        /// </summary>
        /// <param name="baseRoute">The base route.</param>
        /// <param name="whitelist">A collection of valid IPs that never will be banned.</param>
        /// <param name="banMinutes">Minutes that an IP will remain banned.</param>
        public IPBanningModule(string baseRoute = "/",
                               IEnumerable<string>? whitelist = null,
                               int banMinutes = DefaultBanMinutes)
            : base(baseRoute)
        {
            Configuration = IPBanningExecutor.RetrieveInstance(baseRoute, banMinutes);

            AddToWhitelist(whitelist);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="IPBanningModule"/> class.
        /// </summary>
        ~IPBanningModule()
        {
            Dispose(false);
        }

        /// <inheritdoc />
        public override bool IsFinalHandler => false;

        /// <summary>
        /// Gets the client address.
        /// </summary>
        /// <value>
        /// The client address.
        /// </value>
        public IPAddress? ClientAddress { get; private set; }

        internal IPBanningConfiguration Configuration { get; }

        /// <summary>
        /// Registers the criterion.
        /// </summary>
        /// <param name="criterion">The criterion.</param>
        public void RegisterCriterion(IIPBanningCriterion criterion) => 
            Configuration.RegisterCriterion(criterion);

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets the list of current banned IPs.
        /// </summary>
        /// <param name="baseRoute">The base route.</param>
        /// <returns>
        /// A collection of <see cref="BanInfo" /> in the blacklist.
        /// </returns>
        /// <exception cref="ArgumentException">baseRoute</exception>
        public static IEnumerable<BanInfo> GetBannedIPs(string baseRoute = "/") => 
            IPBanningExecutor.TryGetInstance(baseRoute, out var instance) 
            ? instance.BlackList 
            : throw new ArgumentException(NoConfigurationFound, nameof(baseRoute));

        /// <summary>
        /// Tries to ban an IP explicitly.
        /// </summary>
        /// <param name="address">The IP address to ban.</param>
        /// <param name="banMinutes">Minutes that the IP will remain banned.</param>
        /// <param name="baseRoute">The base route.</param>
        /// <param name="isExplicit"><c>true</c> if the IP was explicitly banned.</param>
        /// <returns>
        ///   <c>true</c> if the IP was added to the blacklist; otherwise, <c>false</c>.
        /// </returns>
        public static bool TryBanIP(IPAddress address, int banMinutes, string baseRoute = "/", bool isExplicit = true) =>
            TryBanIP(address, DateTime.Now.AddMinutes(banMinutes), baseRoute, isExplicit);

        /// <summary>
        /// Tries to ban an IP explicitly.
        /// </summary>
        /// <param name="address">The IP address to ban.</param>
        /// <param name="banDuration">A <see cref="TimeSpan" /> specifying the duration that the IP will remain banned.</param>
        /// <param name="baseRoute">The base route.</param>
        /// <param name="isExplicit"><c>true</c> if the IP was explicitly banned.</param>
        /// <returns>
        ///   <c>true</c> if the IP was added to the blacklist; otherwise, <c>false</c>.
        /// </returns>
        public static bool TryBanIP(IPAddress address, TimeSpan banDuration, string baseRoute = "/", bool isExplicit = true) =>
            TryBanIP(address, DateTime.Now.Add(banDuration), baseRoute, isExplicit);

        /// <summary>
        /// Tries to ban an IP explicitly.
        /// </summary>
        /// <param name="address">The IP address to ban.</param>
        /// <param name="banUntil">A <see cref="DateTime" /> specifying the expiration time of the ban.</param>
        /// <param name="baseRoute">The base route.</param>
        /// <param name="isExplicit"><c>true</c> if the IP was explicitly banned.</param>
        /// <returns>
        ///   <c>true</c> if the IP was added to the blacklist; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">baseRoute</exception>
        public static bool TryBanIP(IPAddress address, DateTime banUntil, string baseRoute = "/", bool isExplicit = true)
        {
            if (!IPBanningExecutor.TryGetInstance(baseRoute, out var instance))
                throw new ArgumentException(NoConfigurationFound, nameof(baseRoute));

            return instance.TryBanIP(address, isExplicit, banUntil);
        }

        /// <summary>
        /// Tries to unban an IP explicitly.
        /// </summary>
        /// <param name="address">The IP address.</param>
        /// <param name="baseRoute">The base route.</param>
        /// <returns>
        ///   <c>true</c> if the IP was removed from the blacklist; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">baseRoute</exception>
        public static bool TryUnbanIP(IPAddress address, string baseRoute = "/") => 
            IPBanningExecutor.TryGetInstance(baseRoute, out var instance) 
            ? instance.TryRemoveBlackList(address) 
            : throw new ArgumentException(NoConfigurationFound, nameof(baseRoute));

        internal void AddToWhitelist(IEnumerable<string>? whitelist) =>
            Configuration.AddToWhitelistAsync(whitelist).GetAwaiter().GetResult();

        /// <inheritdoc />
        protected override void OnStart(CancellationToken cancellationToken)
        {
            Configuration.Lock();

            base.OnStart(cancellationToken);
        }

        /// <inheritdoc />
        protected override Task OnRequestAsync(IHttpContext context)
        {
            ClientAddress = context.Request.RemoteEndPoint.Address;
            return Configuration.CheckClient(ClientAddress);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                IPBanningExecutor.TryRemoveInstance(BaseRoute);
                Configuration.Dispose();
            }

            _disposed = true;
        }
    }
}