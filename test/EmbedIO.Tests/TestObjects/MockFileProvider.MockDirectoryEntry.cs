using System;

namespace EmbedIO.Tests.TestObjects
{
    partial class MockFileProvider
    {
        private abstract class MockDirectoryEntry
        {
            protected MockDirectoryEntry()
            {
                LastModifiedUtc = DateTime.UtcNow;
            }

            public DateTime LastModifiedUtc { get; private set; }

            public void Touch() => LastModifiedUtc = DateTime.UtcNow;
        }
    }
}