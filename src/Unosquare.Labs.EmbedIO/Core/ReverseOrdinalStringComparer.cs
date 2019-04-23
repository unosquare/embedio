namespace Unosquare.Labs.EmbedIO.Core
{
    using System;
    using System.Collections.Generic;

    // Sorts strings in reverse order to obtain the evaluation order of virtual paths
    internal sealed class ReverseOrdinalStringComparer : IComparer<string>
    {
        private static readonly IComparer<string> DirectComparer = StringComparer.Ordinal;

        private ReverseOrdinalStringComparer()
        {
        }

        public static IComparer<string> Instance { get; } = new ReverseOrdinalStringComparer();

        public int Compare(string x, string y) => DirectComparer.Compare(y, x);
    }
}