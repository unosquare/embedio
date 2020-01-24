using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using EmbedIO.Utilities;
using Swan.Collections;

namespace EmbedIO.Internal
{
    internal sealed class DummyWebModuleContainer : IWebModuleContainer
    {
        private DummyWebModuleContainer()
        {
        }

        public static IWebModuleContainer Instance { get; } = new DummyWebModuleContainer();

        public IComponentCollection<IWebModule> Modules => throw UnexpectedCall();

        public ConcurrentDictionary<object, object> SharedItems => throw UnexpectedCall();

        private EmbedIOInternalErrorException UnexpectedCall([CallerMemberName] string member = "")
            => SelfCheck.Failure($"Unexpected call to {nameof(DummyWebModuleContainer)}.{member}.");

    }
}