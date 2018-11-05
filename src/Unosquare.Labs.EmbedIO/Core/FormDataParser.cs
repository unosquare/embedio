namespace Unosquare.Labs.EmbedIO.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal static class FormDataParser
    {
        /// <summary>
        /// Parses the form data given the request body string.
        /// </summary>
        /// <param name="requestBody">The request body.</param>
        /// <param name="contentTypeHeader">The content type header.</param>
        /// <returns>
        /// A collection that represents the request body string.
        /// </returns>
        /// <exception cref="System.NotImplementedException">multipart/form-data Content Type parsing is not yet implemented.</exception>
        internal static Dictionary<string, object> ParseAsDictionary(
            string requestBody,
            string contentTypeHeader = Constants.Strings.UrlEncodedContentType)
        {
            // TODO: implement multipart/form-data parsing
            //// example available here: http://stackoverflow.com/questions/5483851/manually-parse-raw-http-data-with-php

            if (contentTypeHeader.ToLowerInvariant().StartsWith("multipart/form-data"))
                throw new NotImplementedException("multipart/form-data Content Type parsing is not yet implemented");

            // verify there is data to parse
            if (string.IsNullOrWhiteSpace(requestBody)) return null;

            // define a character for KV pairs
            var kvpSeparator = new[] {'='};

            // Create the result object
            var resultDictionary = new Dictionary<string, object>();

            // Split the request body into key-value pair strings
            var keyValuePairStrings = requestBody.Split('&').Where(x => string.IsNullOrWhiteSpace(x) == false);

            foreach (var kvps in keyValuePairStrings)
            {
                // Split by the equals char into key values.
                // Some KVPS will have only their key, some will have both key and value
                // Some other might be repeated which really means an array
                var kvpsParts = kvps.Split(kvpSeparator, 2);

                // We don't want empty KVPs
                if (kvpsParts.Length == 0)
                    continue;

                // Decode the key and the value. Discard Special Characters
                var key = System.Net.WebUtility.UrlDecode(kvpsParts[0]);
                if (key.IndexOf("[", StringComparison.OrdinalIgnoreCase) > 0)
                    key = key.Substring(0, key.IndexOf("[", StringComparison.OrdinalIgnoreCase));

                var value = kvpsParts.Length >= 2 ? System.Net.WebUtility.UrlDecode(kvpsParts[1]) : null;

                // If the result already contains the key, then turn the value of that key into a List of strings
                if (resultDictionary.ContainsKey(key))
                {
                    // Check if this key has a List value already
                    var listValue = resultDictionary[key] as List<string>;
                    if (listValue == null)
                    {
                        // if we don't have a list value for this key, then create one and add the existing item
                        var existingValue = resultDictionary[key] as string;
                        resultDictionary[key] = new List<string>();
                        listValue = (List<string>) resultDictionary[key];
                        listValue.Add(existingValue);
                    }

                    // By this time, we are sure listValue exists. Simply add the item
                    listValue.Add(value);
                }
                else
                {
                    // Simply set the key to the parsed value
                    resultDictionary[key] = value;
                }
            }

            return resultDictionary;
        }
    }
}