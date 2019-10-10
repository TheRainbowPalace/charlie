using System;
using System.Collections.Generic;
using System.IO;
using charlie.Simulator;
using Json.Net;

namespace charlie.Graphical
{
  internal class Observable<T>
  {
    private T _value;
    /// <summary>
    /// An event which is called after the value the observable has been
    /// changed.
    /// </summary>
    public event EventHandler OnSet;

    public Observable(T value)
    {
      _value = value;
    }

    public Observable()
    {
    }

    public T Get()
    {
      return _value;
    }

    public void Set(T value)
    {
      _value = value;
      OnSet?.Invoke(this, EventArgs.Empty);
    }

    public bool IsNull()
    {
      return _value == null;
    }
  }
  
  /// <summary>
  /// Holds all data shared by separate parts of the Charlie Graphical App.
  /// </summary>
  internal class CharlieModel
  {
    private static readonly string PrefenceDir = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
      ".charlie");
    private static readonly string PreferenceFile = Path.Combine(
      PrefenceDir, "config.txt");
    public readonly string Version = "0.2.4";
    public readonly string Author = "Jakob Rieke";
    public readonly string Copyright = "Copyright © 2019 Jakob Rieke";
    public int WindowX = 20;
    public int WindowY = 80;
    public int WindowWidth = 380;
    public int WindowHeight = 600;
    public readonly Observable<string[]> PreviousLoaded =
      new Observable<string[]>();
    public readonly int PrevLoadedMaxLength = 5;
    public int SimDelay = 10;
    public bool WriteLogToFile;
    public bool LogEveryIteration;
    public int RenderHeight = 800;
    public int RenderWidth = 800;
    public readonly string DefaultSimPath = Path.Combine(
      AppDomain.CurrentDomain.BaseDirectory, "charlie.exe");
    public readonly string DefaultSimClass = "charlie.HelloWorld";
    public Simulation Sim;
    public readonly Observable<SimRun> ActiveRun = new Observable<SimRun>();
    public readonly List<SimRun> ScheduledRuns = new List<SimRun>();

    public override string ToString()
    {
      var result =
        $"WindowX={WindowX}\n" +
        $"WindowY={WindowY}\n" +
        $"WindowWidth={WindowWidth}\n" +
        $"WindowHeight={WindowHeight}\n" + 
        $"SimDelay={SimDelay}\n" +
        $"WriteLogToFile={WriteLogToFile}\n" +
        $"LogEveryIteration={LogEveryIteration}";
      return result;
    }

    private class Preferences
    {
      public int WindowX;
      public int WindowY;
      public int WindowWidth;
      public int WindowHeight;
      public string[] PreviousLoadedSims;
      public int SimDelay;
      public bool LogEveryIteration;
      public int RenderWidth;
      public int RenderHeight; 
    }
    
    public static CharlieModel LoadFromFile()
    {
      var model = new CharlieModel();
      if (!Directory.Exists(PrefenceDir) 
          || !File.Exists(PreferenceFile)) return model;

      var text = File.ReadAllText(PreferenceFile);
      var pref = JsonNet.Deserialize<Preferences>(text);
      
      model.WindowX = pref.WindowX;
      model.WindowY = pref.WindowY;
      model.WindowWidth = pref.WindowWidth;
      model.WindowHeight = pref.WindowHeight;
      model.PreviousLoaded.Set(pref.PreviousLoadedSims);
      model.SimDelay = pref.SimDelay;
      model.LogEveryIteration = pref.LogEveryIteration;
      model.RenderWidth = pref.RenderWidth;
      model.RenderHeight = pref.RenderHeight;

      return model;
    }

    public static void SaveToFile(CharlieModel model)
    {
      var pref = new Preferences
      {
        WindowX = model.WindowX,
        WindowY = model.WindowY,
        WindowHeight = model.WindowHeight,
        WindowWidth = model.WindowWidth,
        PreviousLoadedSims = model.PreviousLoaded.Get(),
        SimDelay = model.SimDelay,
        LogEveryIteration = model.LogEveryIteration,
        RenderHeight = model.RenderHeight,
        RenderWidth = model.RenderWidth
      };

      if (!Directory.Exists(PrefenceDir))
        Directory.CreateDirectory(PrefenceDir);
      
      File.WriteAllText(PreferenceFile, JsonNet.Serialize(pref));
    }
  }
}