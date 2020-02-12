using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using EmbedIO.Utilities;
using Swan;

namespace EmbedIO.Internal
{
    internal sealed class DummyWebModuleContainer : IWebModuleContainer
    {
        public static readonly IWebModuleContainer Instance = new DummyWebModuleContainer();

        private DummyWebModuleContainer()
        {
        }

        public IComponentCollection<IWebModule> Modules => throw UnexpectedCall();

        public ConcurrentDictionary<object, object> SharedItems => throw UnexpectedCall();

        public void Dispose()
        {
        }

        private InternalErrorException UnexpectedCall([CallerMemberName] string member = "")
            => SelfCheck.Failure($"Unexpected call to {nameof(DummyWebModuleContainer)}.{member}.");
    }
}