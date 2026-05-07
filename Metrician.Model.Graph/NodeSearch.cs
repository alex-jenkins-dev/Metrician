// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Core.Graph;

namespace Metrician.Model.Graph
{
    public enum FieldFilter { All, Title, Vendor, Description, PinName, PinType }

    public sealed record SearchResult(
        float Score,
        string Label,
        string Vendor,
        string MatchedFieldKind,
        string MatchedFieldText,
        INodeTemplate Template);

    public static class NodeSearch
    {
        private sealed record Field(string Text, float Weight, string Kind, bool AllowSubsequence);

        private sealed record TemplatePin(string Name, Type Type, PinDirection Direction);

        private static readonly Dictionary<INodeTemplate, IReadOnlyList<TemplatePin>> _templatePinCache = new();

        // Order matters: longer prefixes must be checked first ("pt:" before "p:").
        private static readonly (string Prefix, FieldFilter Filter)[] Prefixes =
        {
            ("pt:", FieldFilter.PinType),
            ("t:",  FieldFilter.Title),
            ("v:",  FieldFilter.Vendor),
            ("d:",  FieldFilter.Description),
            ("p:",  FieldFilter.PinName),
        };

        public static IReadOnlyList<SearchResult> Run(
            IReadOnlyList<INodeTemplate> templates,
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
                // Prefix-only query (e.g. "t:") browses every template through the
                // requested field type. An unprefixed empty query stays empty.
                if (filter == FieldFilter.All) return Array.Empty<SearchResult>();

                foreach (var template in templates)
                {
                    var fields = CollectTemplateFields(template, filter);
                    foreach (var f in fields)
                    {
                        if (string.IsNullOrEmpty(f.Text)) continue;
                        results.Add(new SearchResult(
                            Score: 0f,
                            template.Title, template.Vendor,
                            f.Kind, f.Text,
                            template));
                    }
                }
            }
            else
            {
                foreach (var template in templates)
                {
                    var fields = CollectTemplateFields(template, filter);
                    if (fields.Count == 0) continue;
                    var (score, bestField) = Score(fields, tokens);
                    if (score > 0)
                    {
                        var matched = fields[bestField];
                        results.Add(new SearchResult(
                            score,
                            template.Title, template.Vendor,
                            matched.Kind, matched.Text,
                            template));
                    }
                }
            }

            return results
                .OrderBy(r => SectionRank(r.MatchedFieldKind))
                .ThenByDescending(r => r.Score)
                .ThenBy(r => r.Label, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.MatchedFieldText, StringComparer.OrdinalIgnoreCase)
                .Take(maxResults)
                .ToList();
        }

        public static int SectionRank(string matchedFieldKind) => matchedFieldKind switch
        {
            "title" => 0,
            "input pin" or "output pin" => 1,
            "input type" or "output type" => 2,
            "vendor" => 3,
            "description" => 4,
            _ => 99,
        };

        public static string SectionLabel(string matchedFieldKind) => matchedFieldKind switch
        {
            "title" => "Titles",
            "input pin" or "output pin" => "Pin names",
            "input type" or "output type" => "Pin types",
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

        private static IReadOnlyList<Field> CollectTemplateFields(INodeTemplate t, FieldFilter filter)
        {
            var fields = new List<Field>();
            if (filter is FieldFilter.All or FieldFilter.Title)
                fields.Add(new(t.Title, 3f, "title", AllowSubsequence: true));
            if (filter is FieldFilter.All or FieldFilter.Vendor)
                fields.Add(new(t.Vendor, 1f, "vendor", AllowSubsequence: true));
            if (filter is FieldFilter.All or FieldFilter.Description)
                fields.Add(new(t.Description, 0.8f, "description", AllowSubsequence: false));

            if (filter is FieldFilter.All or FieldFilter.PinName or FieldFilter.PinType)
            {
                var pins = PreviewTemplatePins(t);
                if (filter is FieldFilter.All or FieldFilter.PinName)
                {
                    foreach (var pin in pins)
                    {
                        string kind = pin.Direction == PinDirection.Input ? "input pin" : "output pin";
                        fields.Add(new(pin.Name, 1.5f, kind, AllowSubsequence: true));
                    }
                }
                if (filter is FieldFilter.All or FieldFilter.PinType)
                {
                    foreach (var pin in pins)
                    {
                        string kind = pin.Direction == PinDirection.Input ? "input type" : "output type";
                        fields.Add(new(pin.Type.Name, 1.5f, kind, AllowSubsequence: true));
                    }
                }
            }
            return fields;
        }

        // Spins up an ephemeral GraphWorld, instantiates the template through the
        // real authoring path, snapshots its pins, and tears the node down. The
        // resulting list is cached so each template pays this once per session.
        private static IReadOnlyList<TemplatePin> PreviewTemplatePins(INodeTemplate template)
        {
            if (_templatePinCache.TryGetValue(template, out var cached)) return cached;

            var pins = new List<TemplatePin>();
            var temp = new GraphWorld();
            try
            {
                var id = temp.Add(template);
                foreach (var pin in temp.Pins.Inputs(id))
                    pins.Add(new TemplatePin(pin.Id.Name, pin.ValueType, PinDirection.Input));
                foreach (var pin in temp.Pins.Outputs(id))
                    pins.Add(new TemplatePin(pin.Id.Name, pin.ValueType, PinDirection.Output));
                temp.Remove(id);
            }
            catch
            {
                // A template that can't instantiate cleanly (e.g. throws during
                // Configure) just contributes no pins to search.
            }

            _templatePinCache[template] = pins;
            return pins;
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
