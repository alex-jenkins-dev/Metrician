// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Reflection;

using Metrician.Graph.Contracts;
using Metrician.Renderable.Contracts;

namespace Metrician.Plugins
{
    /// <summary>
    /// Discovers <see cref="INode"/> subclasses, <see cref="IRenderableFactory{T}"/>
    /// implementations, and <see cref="IValueConverter{TFrom, TTo}"/> implementations
    /// in assemblies. A type qualifies if it is public, non-abstract, has a
    /// parameterless ctor, and is not tagged <see cref="MetricianPluginExcludeAttribute"/>.
    /// </summary>
    public sealed class PluginLoader
    {
        private readonly PluginExclusions _exclusions;

        public PluginLoader() : this(PluginExclusions.Empty) { }

        public PluginLoader(PluginExclusions exclusions)
        {
            _exclusions = exclusions ?? PluginExclusions.Empty;
        }

        /// <summary>Scans one loaded assembly for plugin contributions.</summary>
        public PluginLoadResult LoadAssembly(Assembly assembly)
        {
            var result = new PluginLoadResult();
            if (_exclusions.ExcludesAssembly(assembly.GetName().Name ?? ""))
                return result;

            Type[] types;
            try
            {
                types = assembly.GetExportedTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray()!;
                foreach (var loaderEx in ex.LoaderExceptions)
                    if (loaderEx is not null)
                        result.AddError($"{assembly.GetName().Name}: {loaderEx.Message}");
            }
            catch (Exception ex)
            {
                result.AddError($"{assembly.GetName().Name}: {ex.Message}");
                return result;
            }

            foreach (var type in types)
                Discover(type, result);

            return result;
        }

        /// <summary>Loads a DLL from disk and scans it.</summary>
        public PluginLoadResult LoadFromFile(string dllPath)
        {
            var result = new PluginLoadResult();
            if (_exclusions.ExcludesAssembly(Path.GetFileNameWithoutExtension(dllPath)))
                return result;
            try
            {
                var asm = Assembly.LoadFrom(dllPath);
                result.Merge(LoadAssembly(asm));
            }
            catch (Exception ex)
            {
                result.AddError($"{Path.GetFileName(dllPath)}: {ex.Message}");
            }
            return result;
        }

        /// <summary>
        /// Scans every *.dll in <paramref name="directory"/>.
        /// Failures on one DLL do not affect the others.
        /// </summary>
        public PluginLoadResult LoadFromDirectory(string directory)
        {
            var result = new PluginLoadResult();
            if (!Directory.Exists(directory)) return result;

            foreach (var dll in Directory.GetFiles(directory, "*.dll"))
                result.Merge(LoadFromFile(dll));

            return result;
        }

        private void Discover(Type type, PluginLoadResult result)
        {
            if (type.IsAbstract) return;
            if (type.GetCustomAttribute<MetricianPluginExcludeAttribute>() is not null) return;
            if (_exclusions.ExcludesType(type)) return;

            var ctor = type.GetConstructor(Type.EmptyTypes);
            if (ctor is null) return;

            if (typeof(INode).IsAssignableFrom(type))
            {
                try
                {
                    var (label, vendor) = ResolveNodeMetadata(type, ctor);
                    var nodeType = type;
                    Func<INode> factory = () => (INode)Activator.CreateInstance(nodeType)!;
                    result.AddNode(new DiscoveredNode(label, vendor, nodeType, factory));
                }
                catch (Exception ex)
                {
                    result.AddError($"{type.FullName}: failed to discover INode - {ex.Message}");
                }
            }

            // A type can close IRenderableFactory<T> or IValueConverter<TFrom, TTo>
            // over multiple type arguments; iterate every closed instantiation.
            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType) continue;
                var def = iface.GetGenericTypeDefinition();

                if (def == typeof(IRenderableFactory<>))
                {
                    try
                    {
                        var dataType = iface.GetGenericArguments()[0];
                        var instance = ctor.Invoke(null);
                        result.AddFactory(new DiscoveredFactory(dataType, instance, type));
                    }
                    catch (Exception ex)
                    {
                        result.AddError($"{type.FullName}: failed to discover IRenderableFactory - {ex.Message}");
                    }
                }
                else if (def == typeof(IValueConverter<,>))
                {
                    try
                    {
                        var args = iface.GetGenericArguments();
                        var instance = ctor.Invoke(null);
                        result.AddConverter(new DiscoveredConverter(args[0], args[1], instance, type));
                    }
                    catch (Exception ex)
                    {
                        result.AddError($"{type.FullName}: failed to discover IValueConverter - {ex.Message}");
                    }
                }
            }
        }

        private static (string label, string vendor) ResolveNodeMetadata(
            Type type, ConstructorInfo ctor)
        {
            // Probe an instance to read Title and Vendor as declared by the node.
            var probe = (INode)ctor.Invoke(null);

            var attr = type.GetCustomAttribute<MetricianNodeMenuAttribute>();
            var label = attr?.Label ?? probe.Title;

            return (label, probe.Vendor);
        }
    }
}
