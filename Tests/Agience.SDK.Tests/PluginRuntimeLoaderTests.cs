using Microsoft.SemanticKernel;
using FluentAssertions;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions.Common;

namespace Agience.SDK.Tests;

public class PluginRuntimeLoaderTests
{

    string _pluginFolderName = Environment.CurrentDirectory + "\\Plugins";

    [Fact]
    public void SyncPlugins_DoNothing_IfNoPluginFolder()
    {
        var serviceProvider = arrangeServiceProvider();
        var kernelPluginCollection = serviceProvider.GetRequiredService<KernelPluginCollection>();
        var pluginRuntimeLoader = serviceProvider.GetRequiredService<PluginRuntimeLoader>();

        cleanPluginFolder();

        pluginRuntimeLoader.SyncPlugins();

        kernelPluginCollection.Should().BeEmpty();
    }

    [Fact]
    public void SyncPlugins_AddsPrimaryPluginsLibrary()
    {
        var serviceProvider = arrangeServiceProvider();
        var kernelPluginCollection = serviceProvider.GetRequiredService<KernelPluginCollection>();
        var pluginRuntimeLoader = serviceProvider.GetRequiredService<PluginRuntimeLoader>();

        cleanPluginFolder();

        var libraryAdded = addPrimaryPluginsLibraryToPluginFolder();
        if (!libraryAdded)
            return;

        pluginRuntimeLoader.SyncPlugins();

        kernelPluginCollection.Should().HaveCountGreaterThan(0);
        kernelPluginCollection.Should().Contain(x => x.Name == "CharacterLength");
        kernelPluginCollection.Should().Contain(x => x.Name == "CountWords");
    }

    private ServiceProvider arrangeServiceProvider()
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<KernelPluginCollection>()
            .AddSingleton<PluginRuntimeLoader>()
        .BuildServiceProvider();
        return serviceProvider;
    }

    private void cleanPluginFolder()
    {
        if (Directory.Exists(_pluginFolderName))
        {
            var directoryInfo = new DirectoryInfo(_pluginFolderName);
            directoryInfo.Delete(true);
        }
    }

    /// <summary>
    /// Copy the compiled Agience Primary Plugin into the Plugins folder for testing
    /// </summary>
    /// <returns>True if copied, otherwise false</returns>
    private bool addPrimaryPluginsLibraryToPluginFolder()
    {
        if (!Directory.Exists(_pluginFolderName))
            Directory.CreateDirectory(_pluginFolderName);

        var libraryFile = new FileInfo(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.Parent.Parent.FullName + "\\Core\\Plugins\\Primary\\bin\\Local\\net8.0\\Agience.Plugins.Primary.dll");

        if (!libraryFile.Exists)
            return false;

        var libraryFileNameWithoutExtension = Path.GetFileNameWithoutExtension(libraryFile.Name);

        var libraryFolder = Directory.CreateDirectory(_pluginFolderName + "\\" + libraryFileNameWithoutExtension);

        libraryFile.CopyTo(libraryFolder.FullName + "\\" + libraryFile.Name);

        return true;
    }
}