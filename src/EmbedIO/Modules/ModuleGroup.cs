using System;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Internal;
using EmbedIO.Utilities;

namespace EmbedIO.Modules
{
    /// <summary>
    /// <para>Groups modules under a common base URL path.</para>
    /// <para>The <see cref="IWebModule.BaseUrlPath">BaseUrlPath</see> property
    /// of modules contained in a <c>ModuleGroup</c> is relative to the
    /// <c>ModuleGroup</c>'s <see cref="IWebModule.BaseUrlPath">BaseUrlPath</see> property.
    /// For example, given the following code:</para>
    /// <para><code>new ModuleGroup("/download")
    ///     .WithStaticFilesAt("/docs", "/var/my/documents");</code></para>
    /// <para>files contained in the <c>/var/my/documents</c> folder will be
    /// available to clients under the <c>/download/docs/</c> URL.</para>
    /// </summary>
    /// <seealso cref="WebModuleBase" />
    /// <seealso cref="IDisposable" />
    /// <seealso cref="IWebModuleContainer" />
    public class ModuleGroup : WebModuleBase, IDisposable, IWebModuleContainer
    {
        readonly WebModuleCollection _modules;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleGroup"/> class.
        /// </summary>
        /// <param name="baseUrlPath">The base URL path served by this module.</param>
        /// <seealso cref="IWebModule.BaseUrlPath" />
        public ModuleGroup(string baseUrlPath)
            : base(baseUrlPath)
        {
            _modules = new WebModuleCollection(nameof(ModuleGroup), BaseUrlPath);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="ModuleGroup"/> class.
        /// </summary>
        ~ModuleGroup()
        {
            Dispose(false);
        }

        /// <inheritdoc />
        public IComponentCollection<IWebModule> Modules => _modules;

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources;
        /// <see langword="false"/> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            _modules.Dispose();
        }

        /// <inheritdoc />
        public override void Start(CancellationToken ct)
        {
            _modules.StartAll(ct);
        }

        /// <inheritdoc />
        public override Task<bool> HandleRequestAsync(IHttpContext context, string path, CancellationToken ct)
            => _modules.DispatchRequestAsync(context, ct);
    }
}