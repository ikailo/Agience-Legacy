using AutoMapper;
using System.Reflection;

namespace Agience.Client.Mappings;
public static class MappingExtensions
{
    /// <summary>
    /// Use reflection to add all entities with the IMapped interface to the mapper profile of a given Assembly
    /// </summary>
    public static void ApplyMappingsFromAssembly(this Profile profile, Assembly assembly)
    {
        var types = assembly.GetExportedTypes()
            .Where(x => typeof(IMapped).IsAssignableFrom(x))
            .ToList();

        foreach (var type in types)
        {
            var instance = Activator.CreateInstance(type);

            var methodInfo = type.GetMethod("Mapping")
                ?? type.GetInterface("IMapped`1")!.GetMethod("Mapping");

            methodInfo?.Invoke(instance, new object[] { profile });
        }
    }
}
