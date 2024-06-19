using Agience.SDK.Models;
using Agience.SDK.NetFramework.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Agience.SDK;

/// <summary>
/// Loads Plugins on runtime using reflection.
/// </summary>
public class PluginRuntimeLoader
{

    KernelPluginCollection _pluginCollection;
    private readonly ILogger<PluginRuntimeLoader> _logger;

    public PluginRuntimeLoader(
        KernelPluginCollection pluginCollection,
        ILogger<PluginRuntimeLoader> logger)
    {
        _pluginCollection = pluginCollection;
        _logger = logger;
    }

    /// <summary>
    /// Scans the Plugins from from the current directory folder, and adds the missing Plugins to the Kernel Collection.
    /// </summary>
    public void SyncPlugins()
    {
        var currentPluginDirectory = Environment.CurrentDirectory + "\\Plugins";

        if (!Directory.Exists(currentPluginDirectory))
            return;

        //Scan each Plugins sub-folder
        foreach (var folderPath in Directory.GetDirectories(currentPluginDirectory))
        {

            var pluginFolderName = new DirectoryInfo(folderPath).Name;

            try
            {
                _logger.LogInformation($"Loading Plugin {pluginFolderName}");

                //Adding the main Plugin library: it must have the same name of the folder + .dll    
                var assembly = Assembly.LoadFrom(folderPath + "\\" + pluginFolderName + ".dll");

                //TODO: Add other dll dependencies in the same folder of the Plugin.              

                List<Type>? iAgienciePluginTypes = null;

                iAgienciePluginTypes = tryGettingPlugins(assembly);

                foreach(var iAgienciePluginType in iAgienciePluginTypes)
                {
                    var pluginObject = Activator.CreateInstance(iAgienciePluginType);
                    _pluginCollection.AddFromObject(pluginObject);
                }    

                _logger.LogInformation($"Loaded Plugin {pluginFolderName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error on adding Plugin {pluginFolderName} => " + ex.ToString());
            }

        }
    }

    /// <summary>
    /// It tries to get the Plugin Types while loads the missing Nuget packages recursively.
    /// </summary>
    /// <param name="assembly"></param>
    private List<Type> tryGettingPlugins(Assembly assembly)
    {
        try
        {
            return getValidIAgiencePluginsFromAssembly(assembly);
        }
        catch (ReflectionTypeLoadException ex) when (ex.Message.Contains("Could not load file or assembly"))
        {
            //Load Missing Nuget and Retry
            var errorIndexText = "Could not load file or assembly '";
            var messagePartOne = ex.Message.Substring(ex.Message.IndexOf(errorIndexText) + errorIndexText.Length); 
            var packageName = messagePartOne.Substring(0, messagePartOne.IndexOf(","));
            var versionIndexText = "Version=";
            var messagePartTwo = messagePartOne.Substring(messagePartOne.IndexOf(versionIndexText) + versionIndexText.Length);
            var version = messagePartTwo.Substring(0, messagePartTwo.IndexOf(","));


            NugetExtensions.InstallNugetPackage(packageName, version, Environment.CurrentDirectory);

            return tryGettingPlugins(assembly);  
        }
    }

    /// <summary>
    /// Get all the valid Plugins from a Assembly.
    /// To be a valid class, it must implement IAgiencePlugin and contains any KernelFunction attribute.
    /// </summary> 
    private List<Type> getValidIAgiencePluginsFromAssembly(Assembly assembly)
    {
        var response = new List<Type>();  

        var iAgenciePluginTypes = assembly.GetTypes().Where(x => x.GetInterfaces().Contains(typeof(IAgiencePlugin))).ToList();

        foreach (var iAgenciePluginType in iAgenciePluginTypes)        
            if (iAgenciePluginType.GetMethods().Any(x => x.GetCustomAttributes<KernelFunctionAttribute>(true).Any()))
                response.Add(iAgenciePluginType);        

        return response;
    }
}
