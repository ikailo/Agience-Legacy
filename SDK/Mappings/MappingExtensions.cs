using AutoMapper;
using System.Reflection;

namespace Agience.SDK.Mappings;
public static class MappingExtensions
{
    /// <summary>
    /// Use reflection to add all entities with the IMapped interface to the mapper profile of a given Assembly
    /// </summary>
    public static void ApplyMappingsFromAssembly(this Profile profile, Assembly assembly)
    {
        var types = assembly.GetExportedTypes()
            .Where(x => typeof(IMapped).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
            .ToList();

        foreach (var type in types)
        {
            var instance = Activator.CreateInstance(type);
            var methodInfo = type.GetMethod("Mapping")
                ?? type.GetInterface("IMapped")!.GetMethod("Mapping");

            methodInfo?.Invoke(instance, new object[] { profile });
        }
    }
}
