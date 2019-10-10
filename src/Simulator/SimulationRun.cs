using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Cairo;
using DateTime = System.DateTime;
using Path = System.IO.Path;
using Thread = System.Threading.Thread;

namespace charlie.Simulator
{
  /// <summary>
  /// Manages a simulation instance.
  /// </summary>
  public class SimRun
  {
    public readonly SimInstance Instance;
    
    private DataLogger _dataLogger;
    public bool IsWriteLogToFile;
    public string LogSubDirectory;

    public bool Started { get; private set; }
    public bool Running { get; private set; }
    public bool IsRendering;
    public int RenderWidth;
    public int RenderHeight;
    
    /// <summary>
    /// The time between each simulation iteration, is ignored in
    /// Iterate(int x).
    /// </summary>
    public int SimDelay;
    
    public long Iteration { get; private set; }
    public long Initializations { get; private set; }
    public long ElapsedTime { get; private set; }
    public double AverageIterationTime { get; private set; }
    public byte[] ImageData { get; private set; }
    
    public event EventHandler OnInit;
    public event EventHandler OnUpdate;
    public event EventHandler OnEnd;
    public event EventHandler OnAbort;
    public event EventHandler<LogEventArgs> OnLog;
    
    private Thread _simulationThread;
    
    public SimRun(SimInstance instance)
    {
      Instance = instance;
      SimDelay = 10;
      IsRendering = true;
      LogSubDirectory = "";
      RenderWidth = 400;
      RenderHeight = 400;
      Iteration = -1;
      Initializations = 0;
    }
    
    private void Log(string message)
    {
      if (string.IsNullOrEmpty(message)) return;
      
      OnLog?.Invoke(this, new LogEventArgs(message));
      
      if (IsWriteLogToFile) _dataLogger.Log(message);
    }

    private void RenderAllData()
    {
      Log($"<iteration i=\"{Iteration}\">\n{Instance.Log()}\n</iteration>\n");
    }

    /// <summary>
    /// Save the current Render data as a PNG image.
    /// </summary>
    public void SaveImage()
    {
      if (Running)
      {
        Logger.Say("Please stop the simulation first.");
        return;
      }
      if (ImageData == null)
      {
        Logger.Say("Image output is empty.");
        return;
      }

      var rand = new Random();
      var title = "render-" + Instance.GetTitle().Replace(" ", "_") + "-" +
                  Math.Truncate(rand.NextDouble() * 100000000);

      var surface = new ImageSurface(ImageData, Format.ARGB32,
        RenderWidth, RenderHeight, 4 * RenderWidth);
      surface.WriteToPng(Path.Combine(_dataLogger.FileDir, title + ".png"));
      surface.Dispose();
    }
    
    private static Dictionary<string, string> ParseModel(string model)
    {
      var result = new Dictionary<string, string>();
      var lines = model.Split(new[] {Environment.NewLine},
        StringSplitOptions.None);
      
      foreach (var line in lines)
      {
        if (line.StartsWith("#") || line == "") continue;

        var key = new StringBuilder();
        var value = new StringBuilder();
        var parseValue = false;
        foreach (var x in line)
        {
          if (x == '=')
          {
            parseValue = true;
            continue;
          }
          if (x == ' ') continue;
          if (parseValue) value.Append(x);
          else key.Append(x);
        }
        result.Add(key.ToString(), value.ToString());
      }

      return result;
    }

    public static string SimplifyException(string exc)
    {
      var length = exc.IndexOf("at (wrapper managed-to-native)", 
        StringComparison.Ordinal);
      var lines = exc.Substring(0, length).Split('\n');
      var result = "";
      
      for (var i = 0; i < lines.Length; i++)
      {
        var line = Regex.Split(
          lines[i]
            .Replace("at ", "- ")
            .Replace("System.Reflection.TargetInvocationException: " +
                   "Exception has been thrown by the target of an invocat" +
                   "ion. ---> ", "").TrimEnd(), 
          "\\[0x[\\d,a,b,c,d,e,f]{5}\\]")[0];
        
        if (line == "") continue;
        
        result += line;
        if (i < lines.Length - 1) result += "\n";
      }
      
      return result;
    }

    /// <summary>
    /// Initialize the simulation run.
    /// </summary>
    /// <param name="model"></param>
    public void Init(string model)
    {
      _dataLogger = new DataLogger(Instance.Proxy.ClassName, LogSubDirectory);
      Iteration = 0;
      ElapsedTime = 0;
      AverageIterationTime = 0;
      Initializations++;

      const string softwareName = "charlie-simulation-framework";
      const string softwareVersion = "v1.0.0";
      const string softwareAuthor = "Jakob Rieke";
      var os = Environment.OSVersion;
      
      Log("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
      Log("<simulation>\n");
      Log($"<software>{softwareName}; {softwareVersion}; " +
          $"{softwareAuthor}</software>\n");
      Log($"<system>{os.Platform}; {os.VersionString}; " +
          $"{os.Version}</system>\n");
      Log($"<datetime>{DateTime.Now.ToUniversalTime()}</datetime>\n");
      Log($"<title>{Instance.GetTitle()}</title>\n");
      Log($"<meta>{Instance.GetMeta()}</meta>\n");

        
      Log("<configuration>");
      foreach (var line in model.Split('\n')) Log("\n  " + line + "");
      Log("</configuration>\n");
      
      try
      {
        Instance.Init(ParseModel(model));
        ImageData = Instance.Render(RenderWidth, RenderHeight);
        
        RenderAllData();
        OnInit?.Invoke(this, EventArgs.Empty);
        OnUpdate?.Invoke(this, EventArgs.Empty);
      }
      catch (Exception e)
      {
        var exc = SimplifyException(e.ToString());
        Log("<error>\n" + exc + "</error>\n");
        Console.WriteLine(exc);
      }
    }

    /// <summary>
    /// Update the current simulation until Stop() has been called.
    /// Between each step the simulation will pause for 'SimDelay' milliseconds.
    /// After each iteration the result will be rendered and log output
    /// will be written.
    /// </summary>
    public void Update()
    {
      if (Started || Running) return;
      Started = true;
      Running = true;
      
      _simulationThread = new Thread(() =>
      {
        long deltaTime = 0;
        while (Started)
        {
          Iteration++;
          var timer = Stopwatch.StartNew();
          try
          {
            Instance.Update(deltaTime);
            RenderAllData();
            ImageData = IsRendering ? 
              Instance.Render(RenderWidth, RenderHeight) : null;
          }
          catch (Exception e)
          {
            Log("<error>" + e + "</error>");
          }

          if (IsRendering) Thread.Sleep(SimDelay);
          
          timer.Stop();
          deltaTime = timer.ElapsedMilliseconds;
        
          ElapsedTime += deltaTime;
          AverageIterationTime = (.0 + ElapsedTime) / Iteration;
          
          OnUpdate?.Invoke(this, EventArgs.Empty);
        }

        Running = false;
      });
      _simulationThread.Start();
    }

    /// <summary>
    /// Update the current simulation for a number of iterations ('steps').
    /// Only at the end of all iterations will the simulation result 
    /// rendered and the log output be written.
    /// </summary>
    /// <param name="steps"></param>
    public void Update(int steps)
    {
      if (Started || Running) return;
      Started = true;
      Running = true;
      
      _simulationThread = new Thread(() =>
      {
        var timer = Stopwatch.StartNew();
        while (steps > 0 && Started)
        {
          try
          {
            Instance.Update(20);
          }
          catch (Exception e)
          {
            Logger.Warn(e.ToString());
          }
          Iteration++;
          steps--;
        }
        timer.Stop();
        ElapsedTime += timer.ElapsedMilliseconds;

        try
        {
          RenderAllData();
          ImageData = IsRendering ? 
            Instance.Render(RenderWidth, RenderHeight) : null;
        }
        catch (Exception e)
        {
          Log("<error>" + e + "</error>\n");
        }
      
        Started = false;
        Running = false;
        OnUpdate?.Invoke(this, EventArgs.Empty);
      });
      _simulationThread.Start();
    }

    /// <summary>
    /// Update the the simulation instance synchronously (on the main thread)
    /// for a number of iterations.
    /// </summary>
    /// <param name="steps"></param>
    public void UpdateSync(int steps)
    {
      if (Started || Running) return;
      Started = true;
      Running = true;
      
      var timer = Stopwatch.StartNew();
      while (steps > 0 && Started)
      {
        try
        {
          Instance.Update(20);
        }
        catch (Exception e)
        {
          Logger.Warn(e.ToString());
        }
        Iteration++;
        steps--;
      }
      timer.Stop();
      ElapsedTime += timer.ElapsedMilliseconds;

      try
      {
        RenderAllData();
        ImageData = IsRendering ? 
          Instance.Render(RenderWidth, RenderHeight) : null;
      }
      catch (Exception e)
      {
        Log("<error>" + e + "</error>\n");
      }
      
      Started = false;
      Running = false;
      OnUpdate?.Invoke(this, EventArgs.Empty);
    }
    
    public void Stop()
    {
      Started = false;
    }

    public void StopSync()
    {
      Started = false;
      while (Running) Thread.Sleep(20);
    }
    
    public void End()
    {
      try
      {
        Instance.End();
      }
      catch (Exception e)
      {
        Log("<error>" + e + "</error>\n");
      }
      
      Log("<iterations>" + Iteration + "</iterations>\n");
      Log("<elapsed-time>" + ElapsedTime + "</elapsed-time>\n");
      Log("<average-time>" + AverageIterationTime + 
          "</average-time>\n");
      Log("</simulation>");
      _dataLogger.WriteBuffer();
      
      OnEnd?.Invoke(null, EventArgs.Empty);
    }

    public void Abort()
    {
      throw new NotImplementedException();
//      _simulationThread.Interrupt();
//      OnAbort?.Invoke(null, EventArgs.Empty);
    }
  }
}