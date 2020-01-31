using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// <para>Represents a list of names with associated quality values extracted from an HTTP header,
    /// e.g. <c>gzip; q=0.9, deflate</c>.</para>
    /// <para>See <see href="https://tools.ietf.org/html/rfc7231#section-5.3">RFC7231, section 5.3</see>.</para>
    /// <para>This class ignores and discards extensions (<c>accept-ext</c> in RFC7231 terminology).</para>
    /// <para>If a name has one or more parameters (e.g. <c>text/html;level=1</c>) it is not
    /// further parsed: parameters will appear as part of the name.</para>
    /// </summary>
    public sealed class QValueList
    {
        /// <summary>
        /// <para>A value signifying "anything will do" in request headers.</para>
        /// <para>For example, a request header of
        /// <c>Accept-Encoding: *;q=0.8, gzip</c> means "I prefer GZip compression;
        /// if it is not available, any other compression (including no compression at all)
        /// is OK for me".</para>
        /// </summary>
        public const string Wildcard = "*";

        // This will match a quality value between two semicolons
        // or between a semicolon and the end of a string.
        // Match groups will be:
        // Groups[0] = The matching string
        // Groups[1] = If group is successful, "0"; otherwise, the weight is 1.000
        // Groups[2] = If group is successful, the decimal digits after 0
        // The part of string before the match contains the value and parameters (if any).
        // The part of string after the match contains the extensions (if any).
        // If there is no match, the whole string is just value and parameters (if any).
        private static readonly Regex QualityValueRegex = new Regex(
            @";[ \t]*q=(?:(?:1(?:\.(?:0{1,3}))?)|(?:(0)(?:\.(\d{1,3}))?))[ \t]*(?:;|,|$)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

        /// <summary>
        /// Initializes a new instance of the <see cref="QValueList"/> class
        /// by parsing comma-separated request header values.
        /// </summary>
        /// <param name="useWildcard">If set to <see langword="true"/>, a value of <c>*</c>
        /// will be treated as signifying "anything".</param>
        /// <param name="headerValues">A list of comma-separated header values.</param>
        /// <seealso cref="UseWildcard"/>
        public QValueList(bool useWildcard, string headerValues)
        {
            UseWildcard = useWildcard;
            QValues = Parse(headerValues);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QValueList"/> class
        /// by parsing comma-separated request header values.
        /// </summary>
        /// <param name="useWildcard">If set to <see langword="true"/>, a value of <c>*</c>
        /// will be treated as signifying "anything".</param>
        /// <param name="headerValues">An enumeration of header values.
        /// Note that each element of the enumeration may in turn be
        /// a comma-separated list.</param>
        /// <seealso cref="UseWildcard"/>
        public QValueList(bool useWildcard, IEnumerable<string> headerValues)
        {
            UseWildcard = useWildcard;
            QValues = Parse(headerValues);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QValueList"/> class
        /// by parsing comma-separated request header values.
        /// </summary>
        /// <param name="useWildcard">If set to <see langword="true"/>, a value of <c>*</c>
        /// will be treated as signifying "anything".</param>
        /// <param name="headerValues">An array of header values.
        /// Note that each element of the array may in turn be
        /// a comma-separated list.</param>
        /// <seealso cref="UseWildcard"/>
        public QValueList(bool useWildcard, params string[] headerValues)
            : this(useWildcard, headerValues as IEnumerable<string>)
        {
        }

        /// <summary>
        /// Gets a dictionary associating values with their relative weight
        /// (an integer ranging from 0 to 1000) and their position in the
        /// list of header values from which this instance has been constructed.
        /// </summary>
        /// <remarks>
        /// <para>This property does not usually need to be used directly;
        /// use the <see cref="IsCandidate"/>, <see cref="FindPreferred"/>,
        /// <see cref="FindPreferredIndex(IEnumerable{string})"/>, and
        /// <see cref="FindPreferredIndex(string[])"/> methods instead.</para>
        /// </remarks>
        /// <seealso cref="IsCandidate"/>
        /// <seealso cref="FindPreferred"/>
        /// <seealso cref="FindPreferredIndex(IEnumerable{string})"/>
        /// <seealso cref="FindPreferredIndex(string[])"/>
        public IReadOnlyDictionary<string, (int Weight, int Ordinal)> QValues { get; }

        /// <summary>
        /// Gets a value indicating whether <c>*</c> is treated as a special value
        /// with the meaning of "anything".
        /// </summary>
        public bool UseWildcard { get; }

        /// <summary>
        /// Determines whether the specified value is a possible candidate.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns><see langword="true"/>if <paramref name="value"/> is a candidate;
        /// otherwise, <see langword="false"/>.</returns>
        public bool IsCandidate(string value)
            => TryGetCandidateValue(Validate.NotNull(nameof(value), value), out var candidate) && candidate.Weight > 0;

        /// <summary>
        /// Attempts to determine whether the weight of a possible candidate.
        /// </summary>
        /// <param name="value">The value whose weight is to be determined.</param>
        /// <param name="weight">When this method returns <see langword="true"/>,
        /// the weight of the candidate.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is a candidate;
        /// otherwise, <see langword="false"/>.</returns>
        public bool TryGetWeight(string value, out int weight)
        {
            var result = TryGetCandidateValue(Validate.NotNull(nameof(value), value), out var candidate);
            weight = candidate.Weight;
            return result;
        }

        /// <summary>
        /// Finds the value preferred by the client among an enumeration of values.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <returns>The value preferred by the client, or <see langword="null"/>
        /// if none of the provided <paramref name="values"/> is accepted.</returns>
        public string? FindPreferred(IEnumerable<string> values)
            => FindPreferredCore(values, out var result) >= 0 ? result : null;

        /// <summary>
        /// Finds the index of the value preferred by the client in a list of values.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <returns>The index of the value preferred by the client, or -1
        /// if none of the values in <paramref name="values"/> is accepted.</returns>
        public int FindPreferredIndex(IEnumerable<string> values) => FindPreferredCore(values, out _);

        /// <summary>
        /// Finds the index of the value preferred by the client in an array of values.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <returns>The index of the value preferred by the client, or -1
        /// if none of the values in <paramref name="values"/> is accepted.</returns>
        public int FindPreferredIndex(params string[] values) => FindPreferredIndex(values as IReadOnlyList<string>);

        private static IReadOnlyDictionary<string, (int Weight, int Ordinal)> Parse(string headerValues)
        {
            var result = new Dictionary<string, (int Weight, int Ordinal)>();
            ParseCore(headerValues, result);
            return result;
        }

        private static IReadOnlyDictionary<string, (int Weight, int Ordinal)> Parse(IEnumerable<string> headerValues)
        {
            var result = new Dictionary<string, (int Weight, int Ordinal)>();

            if (headerValues == null) return result;

            foreach (var headerValue in headerValues)
                ParseCore(headerValue, result);

            return result;
        }

        private static void ParseCore(string text, IDictionary<string, (int Weight, int Ordinal)> dictionary)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var length = text.Length;
            var position = 0;
            var ordinal = 0;
            while (position < length)
            {
                var stop = text.IndexOf(',', position);
                if (stop < 0)
                    stop = length;

                string name;
                var weight = 1000;
                var match = QualityValueRegex.Match(text, position, stop - position);
                if (match.Success)
                {
                    var groups = match.Groups;
                    var wholeMatch = groups[0];
                    name = text.Substring(position, wholeMatch.Index - position).Trim();
                    if (groups[1].Success)
                    {
                        weight = 0;
                        if (groups[2].Success)
                        {
                            var digits = groups[2].Value;
                            var n = 0;
                            while (n < digits.Length)
                            {
                                weight = (10 * weight) + (digits[n] - '0');
                                n++;
                            }

                            while (n < 3)
                            {
                                weight = 10 * weight;
                                n++;
                            }
                        }
                    }
                }
                else
                {
                    name = text.Substring(position, stop - position).Trim();
                }

                if (!string.IsNullOrEmpty(name))
                    dictionary[name] = (weight, ordinal);

                position = stop + 1;
                ordinal++;
            }
        }

        private static int CompareQualities((int Weight, int Ordinal) a, (int Weight, int Ordinal) b)
        {
            if (a.Weight > b.Weight)
                return 1;

            if (a.Weight < b.Weight)
                return -1;

            if (a.Ordinal < b.Ordinal)
                return 1;

            if (a.Ordinal > b.Ordinal)
                return -1;

            return 0;
        }

        private int FindPreferredCore(IEnumerable<string> values, out string? result)
        {
            values = Validate.NotNull(nameof(values), values);

            result = null;
            var best = -1;

            // Set initial values such as a weight of 0 can never win over them
            (int Weight, int Ordinal) bestValue = (0, int.MinValue);
            var i = 0;
            foreach (var value in values)
            {
                if (value == null)
                    continue;

                if (TryGetCandidateValue(value, out var candidateValue) && CompareQualities(candidateValue, bestValue) > 0)
                {
                    result = value;
                    best = i;
                    bestValue = candidateValue;
                }

                i++;
            }

            return best;
        }

        private bool TryGetCandidateValue(string value, out (int Weight, int Ordinal) candidate)
            => QValues.TryGetValue(value, out candidate)
            || (UseWildcard && QValues.TryGetValue(Wildcard, out candidate));
    }
}