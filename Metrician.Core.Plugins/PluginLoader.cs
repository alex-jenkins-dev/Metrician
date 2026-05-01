// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Reflection;
using Metrician.Core.Graph;
using Metrician.Renderable.Contracts;

namespace Metrician.Core.Plugins
{
    /// <summary>
    /// Reflection-based discovery of <see cref="INodeTemplate"/>,
    /// <see cref="IRenderableFactory{T}"/>, and <see cref="IValueConverter{TFrom, TTo}"/>
    /// implementations across DLLs in a folder. Hosts call this once at startup.
    /// </summary>
    public static class PluginLoader
    {
        public static void LoadFromDirectory(
            string directory,
            IRenderableRegistry renderables,
            IValueConverterRegistry converters,
            ICollection<NodeTemplateContribution> templates,
            PluginExclusions? exclusions = null)
        {
            if (!Directory.Exists(directory)) return;
            exclusions ??= PluginExclusions.Empty;

            foreach (var dll in Directory.GetFiles(directory, "*.dll"))
            {
                if (exclusions.ExcludesAssembly(Path.GetFileNameWithoutExtension(dll)))
                    continue;
                try
                {
                    var assembly = Assembly.LoadFrom(dll);
                    DiscoverAssembly(assembly, renderables, converters, templates, exclusions);
                }
                catch
                {
                    // Skip plugin DLLs that fail to load. A future plugin-management
                    // UI could surface these; silent is acceptable for now.
                }
            }
        }

        private static void DiscoverAssembly(
            Assembly assembly,
            IRenderableRegistry renderables,
            IValueConverterRegistry converters,
            ICollection<NodeTemplateContribution> templates,
            PluginExclusions exclusions)
        {
            if (exclusions.ExcludesAssembly(assembly.GetName().Name ?? ""))
                return;

            Type[] types;
            try { types = assembly.GetExportedTypes(); }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t is not null).ToArray()!;
            }

            foreach (var type in types)
                Discover(type, renderables, converters, templates, exclusions);
        }

        private static void Discover(
            Type type,
            IRenderableRegistry renderables,
            IValueConverterRegistry converters,
            ICollection<NodeTemplateContribution> templates,
            PluginExclusions exclusions)
        {
            if (type.IsAbstract) return;
            if (type.GetConstructor(Type.EmptyTypes) is null) return;
            if (exclusions.ExcludesType(type)) return;

            if (typeof(INodeTemplate).IsAssignableFrom(type))
            {
                var captured = type;
                Func<INodeTemplate> factory = () =>
                    (INodeTemplate)Activator.CreateInstance(captured)!;
                templates.Add(new NodeTemplateContribution(captured.Name, factory));
            }

            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType) continue;
                var def = iface.GetGenericTypeDefinition();

                if (def == typeof(IRenderableFactory<>))
                {
                    var dataType = iface.GetGenericArguments()[0];
                    var instance = Activator.CreateInstance(type)!;
                    var register = typeof(IRenderableRegistry)
                        .GetMethod(nameof(IRenderableRegistry.Register))!
                        .MakeGenericMethod(dataType);
                    register.Invoke(renderables, new[] { instance });
                }
                else if (def == typeof(IValueConverter<,>))
                {
                    var args = iface.GetGenericArguments();
                    var instance = Activator.CreateInstance(type)!;
                    var register = typeof(IValueConverterRegistry)
                        .GetMethod(nameof(IValueConverterRegistry.Register))!
                        .MakeGenericMethod(args[0], args[1]);
                    register.Invoke(converters, new[] { instance });
                }
            }
        }
    }

    public sealed record NodeTemplateContribution(string Name, Func<INodeTemplate> Create);
}
