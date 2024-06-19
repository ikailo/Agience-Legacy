namespace Agience.SDK.Models;

/// <summary>
/// Agience Plugin Interface to be recognized as a Kernel Plugin by the SDK.
/// </summary>
public interface IAgiencePlugin
{
    /// <summary>
    /// List of Nuget Packages to install for this Plugin.
    /// </summary>
    List<(string package, string version)> NugetPackages { get; }
}
