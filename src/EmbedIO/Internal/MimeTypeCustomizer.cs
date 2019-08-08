using System.Collections.Generic;
using EmbedIO.Utilities;
using Swan.Configuration;

namespace EmbedIO.Internal
{
    internal sealed class MimeTypeCustomizer : ConfiguredObject, IMimeTypeCustomizer
    {
        private readonly Dictionary<string, string> _customMimeTypes = new Dictionary<string, string>();
        private readonly Dictionary<(string, string), bool> _data = new Dictionary<(string, string), bool>();

        private bool? _defaultPreferCompression;

        public string GetMimeType(string extension)
        {
            _customMimeTypes.TryGetValue(Validate.NotNull(nameof(extension), extension), out var result);
            return result;
        }

        public bool TryDetermineCompression(string mimeType, out bool preferCompression)
        {
            var (type, subtype) = MimeType.UnsafeSplit(
                Validate.MimeType(nameof(mimeType), mimeType, false));

            if (_data.TryGetValue((type, subtype), out preferCompression))
                return true;

            if (_data.TryGetValue((type, "*"), out preferCompression))
                return true;

            if (!_defaultPreferCompression.HasValue)
                return false;

            preferCompression = _defaultPreferCompression.Value;
            return true;
        }

        public void AddCustomMimeType(string extension, string mimeType)
        {
            EnsureConfigurationNotLocked();
            _customMimeTypes[Validate.NotNullOrEmpty(nameof(extension), extension)]
                = Validate.MimeType(nameof(mimeType), mimeType, false);
        }

        public void PreferCompression(string mimeType, bool preferCompression)
        {
            EnsureConfigurationNotLocked();
            var (type, subtype) = MimeType.UnsafeSplit(
                Validate.MimeType(nameof(mimeType), mimeType, true));

            if (type == "*")
            {
                _defaultPreferCompression = preferCompression;
            }
            else
            {
                _data[(type, subtype)] = preferCompression;
            }
        }

        public void Lock() => LockConfiguration();
    }
}