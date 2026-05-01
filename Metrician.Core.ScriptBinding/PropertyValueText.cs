// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Drawing;
using System.Globalization;
using System.Numerics;
using Metrician.Core.Scripting;

namespace Metrician.Core.ScriptBinding
{
    public static class PropertyValueText
    {
        public static object? Parse(string text, Type targetType)
        {
            if (targetType is null) throw new ArgumentNullException(nameof(targetType));

            string trimmed = (text ?? string.Empty).Trim();
            bool quoted = trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"';
            string body = quoted ? trimmed.Substring(1, trimmed.Length - 2) : trimmed;

            Type underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlying == typeof(string)) return body;

            if (underlying == typeof(bool))
            {
                if (string.Equals(body, "true", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(body, "false", StringComparison.OrdinalIgnoreCase)) return false;
                throw new FormatException("expected 'true' or 'false'");
            }

            if (underlying == typeof(char))
            {
                if (body.Length != 1) throw new FormatException("expected a single character");
                return body[0];
            }

            if (underlying.IsEnum)
            {
                if (EnumValueParser.TryParse(underlying, body, out var enumValue))
                    return enumValue!;
                throw new FormatException(
                    $"'{body}' is not a member of {underlying.Name}");
            }

            if (underlying == typeof(Vector2)) return ParseVector2(body);
            if (underlying == typeof(Vector3)) return ParseVector3(body);
            if (underlying == typeof(Vector4)) return ParseVector4(body);

            if (underlying == typeof(Color)) return ParseColor(body);
            if (underlying == typeof(PointF)) return ParsePointF(body);
            if (underlying == typeof(Point)) return ParsePoint(body);
            if (underlying == typeof(Size)) return ParseSize(body);
            if (underlying == typeof(SizeF)) return ParseSizeF(body);

            if (underlying == typeof(IntPtr))
                return IntPtr.Parse(body, NumberStyles.Integer, CultureInfo.InvariantCulture);
            if (underlying == typeof(UIntPtr))
                return UIntPtr.Parse(body, NumberStyles.Integer, CultureInfo.InvariantCulture);

            return Convert.ChangeType(body, underlying, CultureInfo.InvariantCulture);
        }

        public static string Format(object? value)
        {
            if (value is null) return string.Empty;
            return value switch
            {
                bool b => b ? "true" : "false",
                string s => FormatString(s),
                char c => c.ToString(),
                Vector2 v2 => FormatVector2(v2),
                Vector3 v3 => FormatVector3(v3),
                Vector4 v4 => FormatVector4(v4),
                Color color => FormatColor(color),
                PointF pointF => FormatPointF(pointF),
                Point point => FormatPoint(point),
                SizeF sizeF => FormatSizeF(sizeF),
                Size size => FormatSize(size),
                Enum e => e.ToString(),
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? string.Empty,
            };
        }

        public static bool IsSupported(Type type)
        {
            if (type is null) throw new ArgumentNullException(nameof(type));
            Type underlying = Nullable.GetUnderlyingType(type) ?? type;
            if (underlying.IsPrimitive) return true;
            if (underlying.IsEnum) return true;
            return underlying == typeof(string)
                || underlying == typeof(decimal)
                || underlying == typeof(Vector2)
                || underlying == typeof(Vector3)
                || underlying == typeof(Vector4)
                || underlying == typeof(Color)
                || underlying == typeof(Point)
                || underlying == typeof(PointF)
                || underlying == typeof(Size)
                || underlying == typeof(SizeF);
        }

        private static string FormatString(string s)
        {
            bool needsQuote = s.Length == 0
                || char.IsWhiteSpace(s[0])
                || char.IsWhiteSpace(s[^1])
                || s.Contains("//", StringComparison.Ordinal);
            return needsQuote ? $"\"{s}\"" : s;
        }

        private static Vector2 ParseVector2(string body)
        {
            float[] parts = ParseFloatList(body, 2, "Vector2");
            return new Vector2(parts[0], parts[1]);
        }

        private static Vector3 ParseVector3(string body)
        {
            float[] parts = ParseFloatList(body, 3, "Vector3");
            return new Vector3(parts[0], parts[1], parts[2]);
        }

        private static Vector4 ParseVector4(string body)
        {
            float[] parts = ParseFloatList(body, 4, "Vector4");
            return new Vector4(parts[0], parts[1], parts[2], parts[3]);
        }

        private static float[] ParseFloatList(string body, int expected, string typeLabel)
        {
            string[] tokens = body.Split(',');
            if (tokens.Length != expected)
                throw new FormatException(
                    $"expected {expected} comma-separated values for {typeLabel}");
            float[] result = new float[expected];
            for (int i = 0; i < expected; i++)
                result[i] = float.Parse(
                    tokens[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
            return result;
        }

        private static string FormatVector2(Vector2 v) =>
            $"{v.X.ToString(CultureInfo.InvariantCulture)}, {v.Y.ToString(CultureInfo.InvariantCulture)}";

        private static string FormatVector3(Vector3 v) =>
            $"{v.X.ToString(CultureInfo.InvariantCulture)}, {v.Y.ToString(CultureInfo.InvariantCulture)}, {v.Z.ToString(CultureInfo.InvariantCulture)}";

        private static string FormatVector4(Vector4 v) =>
            $"{v.X.ToString(CultureInfo.InvariantCulture)}, {v.Y.ToString(CultureInfo.InvariantCulture)}, {v.Z.ToString(CultureInfo.InvariantCulture)}, {v.W.ToString(CultureInfo.InvariantCulture)}";

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

        private static string FormatColor(Color c) =>
            c.A == 255
                ? $"#{c.R:X2}{c.G:X2}{c.B:X2}"
                : $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

        private static PointF ParsePointF(string body)
        {
            float[] parts = ParseFloatList(body, 2, "PointF");
            return new PointF(parts[0], parts[1]);
        }

        private static string FormatPointF(PointF p) =>
            $"{p.X.ToString(CultureInfo.InvariantCulture)}, {p.Y.ToString(CultureInfo.InvariantCulture)}";

        private static Point ParsePoint(string body)
        {
            int[] parts = ParseIntList(body, 2, "Point");
            return new Point(parts[0], parts[1]);
        }

        private static string FormatPoint(Point p) =>
            $"{p.X.ToString(CultureInfo.InvariantCulture)}, {p.Y.ToString(CultureInfo.InvariantCulture)}";

        private static SizeF ParseSizeF(string body)
        {
            float[] parts = ParseFloatList(body, 2, "SizeF");
            return new SizeF(parts[0], parts[1]);
        }

        private static string FormatSizeF(SizeF s) =>
            $"{s.Width.ToString(CultureInfo.InvariantCulture)}, {s.Height.ToString(CultureInfo.InvariantCulture)}";

        private static Size ParseSize(string body)
        {
            int[] parts = ParseIntList(body, 2, "Size");
            return new Size(parts[0], parts[1]);
        }

        private static string FormatSize(Size s) =>
            $"{s.Width.ToString(CultureInfo.InvariantCulture)}, {s.Height.ToString(CultureInfo.InvariantCulture)}";

        private static int[] ParseIntList(string body, int expected, string typeLabel)
        {
            string[] tokens = body.Split(',');
            if (tokens.Length != expected)
                throw new FormatException(
                    $"expected {expected} comma-separated values for {typeLabel}");
            int[] result = new int[expected];
            for (int i = 0; i < expected; i++)
                result[i] = int.Parse(
                    tokens[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture);
            return result;
        }
    }
}
