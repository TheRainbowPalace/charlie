using System;
using System.IO;

namespace charlie.Simulator
{
  /// <summary>
  /// 
  /// </summary>
  public class Simulation
  {
    public readonly string ClassName;
    public readonly string Path;
    public readonly string ComplexPath;
    private AppDomain _domain;
    private SimulationProxy _proxy;
    
    public Simulation(string complexPath)
    {
      var parsed = ParseComplexPath(complexPath);
      
      if (parsed == null) 
        throw new ArgumentException("Invalid complex path");
      
      Path = parsed[0];
      ClassName = parsed[1];
      ComplexPath = parsed[0] + parsed[1];
      Load(parsed[0], parsed[1]);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="className"></param>
    /// <exception cref="ArgumentException">
    /// Is thrown if path does not point to a file or if the the specified
    /// class does not exist in the loaded dll.</exception>
    public Simulation(string path, string className)
    {
      ClassName = className;
      Path = path;
      ComplexPath = path + ":" + className;
      Load(path, className);
    }

    public static string[] ParseComplexPath(string complexPath)
    {
      var text = complexPath.Replace(" ", "");
      var index = text.LastIndexOf(':');
      if (index < 0) return null;

      return new []{
        text.Substring(0, index),
        text.Substring(index + 1, text.Length - 1 - index)
      };
    }

    private void Load(string path, string className)
    {
      if (!File.Exists(path))
        throw new ArgumentException("Simulation file does not exist.");

      try
      {
//        var ads = new AppDomainSetup
//        {
//          ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
//          DisallowBindingRedirects = false,
//          DisallowCodeDownload = true,
//          ConfigurationFile =
//            AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
//        };
        
//        _domain = AppDomain.CreateDomain("SimulationDomain", null, ads);

        _domain = AppDomain.CreateDomain("SimulationDomain");
        
        _proxy = (SimulationProxy) _domain.CreateInstanceAndUnwrap(
          typeof(SimulationProxy).Assembly.FullName, 
          typeof(SimulationProxy).FullName);
        _proxy.Load(path, className);
      }
      catch (ArgumentException)
      {
        if (_domain != null) AppDomain.Unload(_domain);
        throw new ArgumentException(
          "Class \"" + className + "\" does not exists in dll.");
      }
    }

    public SimInstance Spawn()
    {
      return new SimInstance(_proxy);
    }
    
    /// <summary>
    /// Unloads the simulation. All runs spawned by the simulation must be
    /// terminated before calling this method since the simulation assembly
    /// is going to be unloaded and so all instances created by the simulation
    /// will tangle in mid air not knowing what has happened to them. In a
    /// uncontrolled rage of confusion they are then going to throw wild
    /// NullPointerExceptions.
    /// After unloading a simulation all it's runs are invalid.
    /// </summary>
    public void Unload()
    {
      AppDomain.Unload(_domain);
    }
  }
}