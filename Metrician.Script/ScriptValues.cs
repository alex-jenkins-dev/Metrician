// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Globalization;

namespace Metrician.Script
{
    /// <summary>
    /// Round-trippable parse and format for value types the script accepts on
    /// the right-hand side of a property assignment.
    /// </summary>
    internal static class ScriptValues
    {
        public static object Parse(string raw, Type targetType)
        {
            string s = raw.Trim();
            bool quoted = s.Length >= 2 && s[0] == '"' && s[^1] == '"';
            string body = quoted ? s.Substring(1, s.Length - 2) : s;

            var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlying == typeof(string)) return body;
            if (underlying == typeof(bool))
            {
                if (string.Equals(body, "true", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(body, "false", StringComparison.OrdinalIgnoreCase)) return false;
                throw new FormatException("expected 'true' or 'false'");
            }
            if (underlying.IsEnum) return Enum.Parse(underlying, body, ignoreCase: true);
            if (underlying == typeof(char))
            {
                if (body.Length != 1) throw new FormatException("expected a single character");
                return body[0];
            }
            if (underlying == typeof(Color)) return ParseColor(body);
            if (underlying == typeof(PointF)) return ParsePointF(body);
            return Convert.ChangeType(body, underlying, CultureInfo.InvariantCulture);
        }

        // Accepts #RRGGBB, #AARRGGBB, 0xRRGGBB, 0xAARRGGBB, or a known colour name.
        private static Color ParseColor(string body)
        {
            if (body.Length > 0 && body[0] == '#')
                return ParseHexColor(body[1..]);
            if (body.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return ParseHexColor(body[2..]);

            var named = Color.FromName(body);
            if (named.IsKnownColor) return named;

            throw new FormatException(
                "expected '#RRGGBB', '#AARRGGBB', '0xRRGGBB', '0xAARRGGBB', " +
                "or a known colour name like 'Red' or 'LimeGreen'");
        }

        private static Color ParseHexColor(string hex)
        {
            if (hex.Length != 6 && hex.Length != 8)
                throw new FormatException(
                    "hex colour must be 6 (RRGGBB) or 8 (AARRGGBB) digits");
            uint v = uint.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int a = hex.Length == 8 ? (int)((v >> 24) & 0xFF) : 255;
            int r = (int)((v >> 16) & 0xFF);
            int g = (int)((v >> 8) & 0xFF);
            int b = (int)(v & 0xFF);
            return Color.FromArgb(a, r, g, b);
        }

        private static PointF ParsePointF(string body)
        {
            int comma = body.IndexOf(',');
            if (comma < 0) throw new FormatException("expected 'x, y' for PointF");
            float x = float.Parse(body[..comma].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
            float y = float.Parse(body[(comma + 1)..].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
            return new PointF(x, y);
        }

        public static string Format(object? value)
        {
            if (value is null) return "";
            return value switch
            {
                bool b => b ? "true" : "false",
                string s => FormatString(s),
                char c => c.ToString(),
                Color col => FormatColor(col),
                PointF p => FormatPointF(p),
                Enum e => e.ToString(),
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? "",
            };
        }

        // Quote when leading/trailing whitespace or a '//' would confuse the parser.
        private static string FormatString(string s)
        {
            bool needsQuote = s.Length == 0
                || char.IsWhiteSpace(s[0])
                || char.IsWhiteSpace(s[^1])
                || s.Contains("//", StringComparison.Ordinal);
            return needsQuote ? $"\"{s}\"" : s;
        }

        private static string FormatColor(Color c) =>
            c.A == 255
                ? $"#{c.R:X2}{c.G:X2}{c.B:X2}"
                : $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

        private static string FormatPointF(PointF p) =>
            $"{p.X.ToString(CultureInfo.InvariantCulture)}, {p.Y.ToString(CultureInfo.InvariantCulture)}";

        /// <summary>
        /// True when <paramref name="t"/> (or its underlying type) is round-trippable.
        /// </summary>
        public static bool IsSupported(Type t)
        {
            var u = Nullable.GetUnderlyingType(t) ?? t;
            if (u.IsPrimitive) return true;
            if (u.IsEnum) return true;
            return u == typeof(string)
                || u == typeof(decimal)
                || u == typeof(Color)
                || u == typeof(PointF);
        }
    }
}
