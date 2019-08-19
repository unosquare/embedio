using NUnit.Framework;
using Swan;

namespace EmbedIO.Tests
{
    [SetUpFixture]
    public class SetUpFixture
    {
        [OneTimeSetUp]
        public void OnBeforeAnyTests()
        {
            // Terminal.Settings.DisplayLoggingMessageType = LogMessageType.None;
        }
    }
}