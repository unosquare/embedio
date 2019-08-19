using System.Collections.Specialized;

namespace EmbedIO.Internal
{
    internal sealed class LockableNameValueCollection : NameValueCollection
    {
        public void MakeReadOnly() => IsReadOnly = true;
    }
}