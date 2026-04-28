// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Text;
using System.Text.RegularExpressions;

namespace Metrician.Plugins
{
    /// <summary>
    /// Glob patterns that mask plugin contributions during discovery.
    /// Patterns ending in <c>.dll</c> match the assembly's simple name and
    /// block the whole DLL up front; others match a type's namespace or full
    /// name. <c>*</c> is the only wildcard. File format: one pattern per
    /// line, <c>//</c> for comments.
    /// </summary>
    public sealed class PluginExclusions
    {
        public static PluginExclusions Empty { get; } =
            new(Array.Empty<Regex>(), Array.Empty<Regex>());

        private readonly Regex[] _assemblyPatterns;
        private readonly Regex[] _typePatterns;

        private PluginExclusions(Regex[] assemblyPatterns, Regex[] typePatterns)
        {
            _assemblyPatterns = assemblyPatterns;
            _typePatterns = typePatterns;
        }

        public bool IsEmpty => _assemblyPatterns.Length == 0 && _typePatterns.Length == 0;

        /// <summary>
        /// Returns the parsed exclusions from <paramref name="path"/>, or
        /// <see cref="Empty"/> if it does not exist.
        /// </summary>
        public static PluginExclusions FromFile(string path)
        {
            if (!File.Exists(path)) return Empty;
            return Parse(File.ReadAllText(path));
        }

        public static PluginExclusions Parse(string text)
        {
            var asm = new List<Regex>();
            var typ = new List<Regex>();
            foreach (var raw in text.Split('\n'))
            {
                int slash = raw.IndexOf("//", StringComparison.Ordinal);
                string trimmed = (slash >= 0 ? raw[..slash] : raw).Trim();
                if (trimmed.Length == 0) continue;

                if (trimmed.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    asm.Add(GlobToRegex(trimmed[..^4]));
                else
                    typ.Add(GlobToRegex(trimmed));
            }
            return new PluginExclusions(asm.ToArray(), typ.ToArray());
        }

        /// <summary>
        /// True when <paramref name="assemblyName"/> matches a <c>*.dll</c> pattern.
        /// Type patterns are not consulted here.
        /// </summary>
        public bool ExcludesAssembly(string assemblyName)
        {
            foreach (var p in _assemblyPatterns)
                if (p.IsMatch(assemblyName)) return true;
            return false;
        }

        /// <summary>
        /// True when <paramref name="type"/>'s namespace or full name matches a pattern,
        /// or when its assembly is excluded.
        /// </summary>
        public bool ExcludesType(Type type)
        {
            if (IsEmpty) return false;

            string asm = type.Assembly.GetName().Name ?? "";
            foreach (var p in _assemblyPatterns)
                if (p.IsMatch(asm)) return true;

            string ns = type.Namespace ?? "";
            string full = type.FullName ?? type.Name;
            foreach (var p in _typePatterns)
                if (p.IsMatch(ns) || p.IsMatch(full)) return true;

            return false;
        }

        // Anchored at both ends: 'Foo' matches only 'Foo';
        // 'Foo*' matches 'Foo' and anything starting with it.
        private static Regex GlobToRegex(string glob)
        {
            var sb = new StringBuilder("^");
            foreach (char c in glob)
            {
                if (c == '*') sb.Append(".*");
                else sb.Append(Regex.Escape(c.ToString()));
            }
            sb.Append('$');
            return new Regex(sb.ToString(), RegexOptions.Compiled);
        }
    }
}
