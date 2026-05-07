// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Core.Graph;

namespace Metrician.Model.Graph
{
    public enum FieldFilter { All, Title, Vendor, Description }

    public sealed record SearchResult(
        float Score,
        string TypeName,
        string Title,
        string Vendor,
        string MatchedFieldKind,
        string MatchedFieldText);

    public static class NodeSearch
    {
        private sealed record Field(string Text, float Weight, string Kind, bool AllowSubsequence);

        private static readonly (string Prefix, FieldFilter Filter)[] Prefixes =
        {
            ("t:", FieldFilter.Title),
            ("v:", FieldFilter.Vendor),
            ("d:", FieldFilter.Description),
        };

        public static IReadOnlyList<SearchResult> Run(
            INodeCatalog catalog,
            string query,
            int maxResults = 50)
        {
            if (string.IsNullOrWhiteSpace(query)) return Array.Empty<SearchResult>();

            var (filter, body) = ParsePrefix(query.TrimStart());

            var tokens = body
                .ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var results = new List<SearchResult>();

            if (tokens.Length == 0)
            {
                if (filter == FieldFilter.All) return Array.Empty<SearchResult>();

                foreach (var info in catalog.All)
                {
                    var fields = CollectFields(info, filter);
                    foreach (var f in fields)
                    {
                        if (string.IsNullOrEmpty(f.Text)) continue;
                        results.Add(new SearchResult(
                            Score: 0f,
                            info.TypeName,
                            info.Title, info.Vendor,
                            f.Kind, f.Text));
                    }
                }
            }
            else
            {
                foreach (var info in catalog.All)
                {
                    var fields = CollectFields(info, filter);
                    if (fields.Count == 0) continue;
                    var (score, bestField) = Score(fields, tokens);
                    if (score > 0)
                    {
                        var matched = fields[bestField];
                        results.Add(new SearchResult(
                            score,
                            info.TypeName,
                            info.Title, info.Vendor,
                            matched.Kind, matched.Text));
                    }
                }
            }

            return results
                .OrderBy(r => SectionRank(r.MatchedFieldKind))
                .ThenByDescending(r => r.Score)
                .ThenBy(r => r.Title, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.MatchedFieldText, StringComparer.OrdinalIgnoreCase)
                .Take(maxResults)
                .ToList();
        }

        public static int SectionRank(string matchedFieldKind) => matchedFieldKind switch
        {
            "title" => 0,
            "vendor" => 1,
            "description" => 2,
            _ => 99,
        };

        public static string SectionLabel(string matchedFieldKind) => matchedFieldKind switch
        {
            "title" => "Titles",
            "vendor" => "Vendors",
            "description" => "Descriptions",
            _ => "Other",
        };

        private static (FieldFilter Filter, string Body) ParsePrefix(string raw)
        {
            foreach (var (prefix, filter) in Prefixes)
            {
                if (raw.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return (filter, raw[prefix.Length..]);
            }
            return (FieldFilter.All, raw);
        }

        private static IReadOnlyList<Field> CollectFields(NodeTypeInfo info, FieldFilter filter)
        {
            var fields = new List<Field>();
            if (filter is FieldFilter.All or FieldFilter.Title)
                fields.Add(new(info.Title, 3f, "title", AllowSubsequence: true));
            if (filter is FieldFilter.All or FieldFilter.Vendor)
                fields.Add(new(info.Vendor, 1f, "vendor", AllowSubsequence: true));
            if (filter is FieldFilter.All or FieldFilter.Description)
                fields.Add(new(info.Description, 0.8f, "description", AllowSubsequence: false));
            return fields;
        }

        private static (float Score, int BestField) Score(IReadOnlyList<Field> fields, string[] tokens)
        {
            float total = 0;
            var contribution = new float[fields.Count];
            foreach (var token in tokens)
            {
                float bestForToken = 0;
                int bestFieldForToken = -1;
                for (int i = 0; i < fields.Count; i++)
                {
                    var f = fields[i];
                    if (string.IsNullOrEmpty(f.Text)) continue;
                    float s = ScoreOne(f.Text.ToLowerInvariant(), token, f.AllowSubsequence);
                    if (s > 0)
                    {
                        float w = s * f.Weight;
                        if (w > bestForToken)
                        {
                            bestForToken = w;
                            bestFieldForToken = i;
                        }
                    }
                }
                if (bestForToken == 0) return (0, -1);
                total += bestForToken;
                contribution[bestFieldForToken] += bestForToken;
            }

            int best = 0;
            float bestSum = -1;
            for (int i = 0; i < contribution.Length; i++)
            {
                if (contribution[i] > bestSum)
                {
                    bestSum = contribution[i];
                    best = i;
                }
            }
            return (total, best);
        }

        private static float ScoreOne(string haystackLower, string tokenLower, bool allowSubsequence)
        {
            if (haystackLower.StartsWith(tokenLower)) return 4f;
            if (haystackLower.Contains(tokenLower)) return 3f;
            if (allowSubsequence && IsSubsequence(tokenLower, haystackLower)) return 1f;
            return 0f;
        }

        private static bool IsSubsequence(string needle, string haystack)
        {
            int i = 0;
            foreach (var c in haystack)
            {
                if (i < needle.Length && c == needle[i]) i++;
                if (i == needle.Length) return true;
            }
            return i == needle.Length;
        }
    }
}
