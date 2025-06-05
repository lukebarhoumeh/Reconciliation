using System;
using System.Collections.Generic;
using System.Linq;

namespace Reconciliation
{
    public static class FuzzyMatcher
    {
        /// <summary>
        /// Find the closest string to <paramref name="target"/> within <paramref name="options"/> using the Levenshtein distance.
        /// </summary>
        /// <param name="target">String to match.</param>
        /// <param name="options">Available options.</param>
        /// <param name="maxDistance">Maximum allowable distance to consider a match.</param>
        /// <returns>Closest match or <c>null</c> if none within <paramref name="maxDistance"/>.</returns>
        public static string? FindClosest(string target, IEnumerable<string> options, int maxDistance = 2)
        {
            if (target is null) throw new ArgumentNullException(nameof(target));
            if (options is null) throw new ArgumentNullException(nameof(options));
            string normalizedTarget = Normalize(target);
            string? best = null;
            int bestDist = int.MaxValue;
            foreach (var option in options)
            {
                string normalizedOption = Normalize(option);
                int dist = Levenshtein(normalizedTarget, normalizedOption);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = option;
                }
            }
            return bestDist <= maxDistance ? best : null;
        }

        internal static int Levenshtein(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];
            for (int i = 0; i <= n; i++) d[i, 0] = i;
            for (int j = 0; j <= m; j++) d[0, j] = j;
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = s[i - 1] == t[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }

        /// <summary>
        /// Determine if two strings are considered a match within the given distance.
        /// </summary>
        /// <param name="a">First string.</param>
        /// <param name="b">Second string.</param>
        /// <param name="maxDistance">Maximum allowed distance.</param>
        public static bool IsFuzzyMatch(string a, string b, int maxDistance = 2)
        {
            if (a is null) throw new ArgumentNullException(nameof(a));
            if (b is null) throw new ArgumentNullException(nameof(b));
            string na = Normalize(a);
            string nb = Normalize(b);
            return Levenshtein(na, nb) <= maxDistance;
        }

        private static string Normalize(string input)
        {
            return new string(input
                .ToLowerInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());
        }
    }
}
