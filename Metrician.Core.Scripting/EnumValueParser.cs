// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Scripting
{
    public static class EnumValueParser
    {
        public static bool TryParse<TEnum>(string text, out TEnum value)
            where TEnum : struct, Enum
        {
            string trimmed = text?.Trim() ?? string.Empty;

            if (trimmed.Length > 0)
            {
                foreach (string name in Enum.GetNames<TEnum>())
                {
                    if (string.Equals(name, trimmed, StringComparison.OrdinalIgnoreCase))
                    {
                        value = Enum.Parse<TEnum>(name);
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        public static bool TryParse(Type enumType, string text, out object? value)
        {
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));

            if (!enumType.IsEnum)
                throw new ArgumentException($"{enumType} is not an enum type.", nameof(enumType));

            string trimmed = text?.Trim() ?? string.Empty;
            if (trimmed.Length > 0)
            {
                foreach (string name in Enum.GetNames(enumType))
                {
                    if (string.Equals(name, trimmed, StringComparison.OrdinalIgnoreCase))
                    {
                        value = Enum.Parse(enumType, name);
                        return true;
                    }
                }
            }

            value = null;
            return false;
        }
    }
}
