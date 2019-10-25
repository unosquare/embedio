using System;

namespace EmbedIO.Testing
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

            protected void Touch() => LastModifiedUtc = DateTime.UtcNow;
        }
    }
}