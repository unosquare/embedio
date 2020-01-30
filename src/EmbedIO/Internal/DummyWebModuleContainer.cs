using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Swan;
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

        private InternalErrorException UnexpectedCall([CallerMemberName] string member = "")
            => SelfCheck.Failure($"Unexpected call to {nameof(DummyWebModuleContainer)}.{member}.");

    }
}