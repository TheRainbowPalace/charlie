using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Timers;
using Cairo;
using Gdk;
using GLib;
using Gtk;
using MathNet.Numerics.Random;
using Application = Gtk.Application;
using DateTime = System.DateTime;
using Device = Gdk.Device;
using Path = System.IO.Path;
using Thread = System.Threading.Thread;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;

namespace charlie
{
  /// <summary>
  /// A basic interface to load 
  /// Normally you would not use this class directly but the Simulation class
  /// instead.
  /// </summary>
  public class SimulationProxy : MarshalByRefObject
  {
    public string ClassName { get; private set; }
    public string Path { get; private set; }
    public string ComplexPath { get; private set; }
    private Dictionary<int, object> _instances;
    private int _idCounter;
    private Assembly _assembly;
    private Type _type;
    private MethodInfo _getTitle;
    private MethodInfo _getDescr;
    private MethodInfo _getConfig;
    private MethodInfo _getMeta;
    private MethodInfo _init;
    private MethodInfo _end;
    private MethodInfo _update;
    private MethodInfo _render;
    private MethodInfo _log;


    /// <summary>
    /// Load the simulation from an assembly.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="className"></param>
    /// <exception cref="ArgumentException"></exception>
    public void Load(string path, string className)
    {
      Path = path;
      ClassName = className;
      ComplexPath = path + ":" + className;
      _instances = new Dictionary<int, object>();
      _idCounter = 0;
      _assembly = Assembly.Load(AssemblyName.GetAssemblyName(path));
      _type = _assembly.GetType(className);
      
      if (_type == null) throw new ArgumentException();

      _getTitle = _type.GetMethod("GetTitle");
      _getDescr = _type.GetMethod("GetDescr");
      _getConfig = _type.GetMethod("GetConfig");
      _getMeta = _type.GetMethod("GetMeta");
      _init = _type.GetMethod("Init");
      _end = _type.GetMethod("End");
      _update = _type.GetMethod("Update");
      _render = _type.GetMethod("Render", new []{typeof(int), typeof(int)});
      _log = _type.GetMethod("Log");
    }

    /// <summary>
    /// Spawns a new simulation instance and returns an identifier (i).
    /// The instance is stored internally and can be used by calling
    /// Simulation.GetTitle(i), Simulation.GetDescr(i), etc. .
    /// </summary>
    public int Spawn()
    {
      _instances.Add(_idCounter, _assembly.CreateInstance(ClassName));
      _idCounter++;
      return _instances.Count - 1;
    }

    /// <summary>
    /// Remove a simulation instance from the internal buffer by its id.
    /// </summary>
    public void Remove(int id)
    {
      _instances.Remove(id);
    }

    /// <summary>
    /// Get a list of IDs for all spawned instances.
    /// </summary>
    /// <returns></returns>
    public int[] SimIds()
    {
      return _instances.Keys.ToArray();
    }
    
    /// <summary>
    /// The number of loaded simulations.
    /// </summary>
    /// <returns></returns>
    public int SimCount()
    {
      return _instances.Count;
    }
    
    public string GetTitle(int index)
    {
      return (string) _getTitle.Invoke(_instances[index], new object[0]);
    }

    public string GetDescr(int index)
    {
      return (string) _getDescr.Invoke(_instances[index], new object[0]);
    }
    
    public string GetConfig(int index)
    {
      return (string) _getConfig.Invoke(_instances[index], new object[0]);
    }

    public string GetMeta(int index)
    {
      return (string) _getMeta.Invoke(_instances[index], new object[0]);
    }

    public void Init(int index, Dictionary<string, string> config)
    {
      _init.Invoke(_instances[index], new object[] {config});
    }

    public void End(int index)
    {
      _end.Invoke(_instances[index], new object[0]);
    }

    public void Update(int index, long dt)
    {
      _update.Invoke(_instances[index], new object[] {dt});
    }

    public byte[] Render(int index, int width, int height)
    {
      return (byte[]) _render.Invoke(_instances[index], 
        new object[] {width, height});
    }

    public string Log(int index)
    {
      return (string) _log.Invoke(_instances[index], new object[0]);
    }
  }
  
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
      if (index < 0) throw new ArgumentException(
        "Complex path should be of form /path/to/dll-file:classname");

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
        var ads = new AppDomainSetup
        {
          ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
          DisallowBindingRedirects = false,
          DisallowCodeDownload = true,
          ConfigurationFile =
            AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
        };
        
        _domain = AppDomain.CreateDomain("SimulationDomain", null, ads);
        
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
  
  /// <summary>
  /// A simulation instance, created from a Simulation.
  /// </summary>
  public class SimInstance : ISimulation
  {
    public readonly SimulationProxy Proxy;
    public readonly int Id;

    
    public SimInstance(SimulationProxy proxy)
    {
      Proxy = proxy;
      Id = proxy.Spawn();
    }


    public string GetTitle()
    {
      return Proxy.GetTitle(Id);
    }

    public string GetDescr()
    {
      return Proxy.GetDescr(Id);
    }

    public string GetMeta()
    {
      return Proxy.GetMeta(Id);
    }

    public string GetConfig()
    {
      return Proxy.GetConfig(Id);
    }

    public void Init(Dictionary<string, string> model)
    {
      Proxy.Init(Id, model);
    }

    public void End()
    {
      Proxy.End(Id);
    }

    public void Update(long deltaTime)
    {
      Proxy.Update(Id, deltaTime);
    }

    public byte[] Render(int width, int height)
    {
      return Proxy.Render(Id, width, height);
    }

    public string Log()
    {
      return Proxy.Log(Id);
    }
  }
  
  public class SimLogger
  {
    /// <summary>
    /// A unique ID which is used to generate the log file names.
    /// </summary>
    public string SessionId { get; private set; }
    
    /// <summary>
    /// The directory where all log output is stored.
    /// </summary>
    public string OutputDir { get; private set; }
    
    /// <summary>
    /// The log file format to store the log output on a hard-drive.
    /// Currently only JSON is allowed.
    /// </summary>
    public string LogFileFormat;
    
    /// <summary>
    /// The maximum number of log entries a log file may contain until a new
    /// one is created.
    /// </summary>
    public int MaxLogFileLength;
    
    /// <summary>
    /// Log only every n'th log entry would LogSteps e.g. be 1 every log entry
    /// would be saved, would it be 5 only every 5th log entry would be saved
    /// and so on. Defaults to 1.
    /// </summary>
    public int LogEveryN;

    /// <summary>
    /// The maximum length of the internal log buffer. Duo to performance
    /// reasons log entries are first written to RAM and only to file if
    /// the buffer is full or WriteLogBuffer() is called manually. Defaults to
    /// 1000.
    /// </summary>
    public int MaxBufferLength;
    
    private string[] _logBuffer;
    private int _bufferPosition;
    private int _fileLength;
    private int _fileCount;

    
    public SimLogger(string projectName)
    {
      LogFileFormat = "JSON";
      MaxLogFileLength = 50000;
      LogEveryN = 1;
      MaxBufferLength = 1000;
      SetOutputDir(projectName);
      SetSessionId();
      
      _logBuffer = new string[MaxBufferLength];
      _bufferPosition = 0;
    }

    private void SetSessionId()
    {
      var date = DateTime.Now.ToUniversalTime();
      SessionId = "run-" + date.Year + 
                  "-" + (date.Month < 10 ? "0" : "") + date.Month + 
                  "-" + (date.Day < 10 ? "0" : "") + date.Day +
                  "T" + (date.Hour < 10 ? "0" : "") + date.Hour + 
                  "-" + (date.Minute < 10 ? "0" : "") + date.Minute + 
                  "-" + (date.Second < 10 ? "0" : "") + date.Second + "Z";
    }
    
    private void SetOutputDir(string projectName)
    {
      OutputDir = Path.Combine(
        Environment.GetFolderPath(
          Environment.SpecialFolder.UserProfile), 
        ".charlie", 
        projectName);
      
      if (!Directory.Exists(OutputDir)) Directory.CreateDirectory(OutputDir);
    }
    
    /// <summary>
    /// Write a log entry into the log buffer. The buffer is written to file
    /// if it reaches the MaxBufferLength.
    /// </summary>
    /// <param name="entry"></param>
    public void Log(string entry)
    {
      if (string.IsNullOrEmpty(entry)) return;
      
      _logBuffer[_bufferPosition] = entry;
      _bufferPosition++;
      if (_bufferPosition == MaxBufferLength) WriteLogBuffer();
    }

    public void WriteLogBuffer()
    {
      if (_fileLength + _logBuffer.Length > MaxLogFileLength)
      {
        _fileCount++;
        _fileLength = 0;
      }

      var filePath = Path.Combine(
        OutputDir,
        SessionId + (_fileCount > 0 ? "-" + _fileCount : "") + ".log");
      
      File.AppendAllLines(filePath, _logBuffer);
      
      _logBuffer = new string[MaxBufferLength];
      _bufferPosition = 0;
      _fileLength += _logBuffer.Length;
    }
  }

  public class LogEventArgs : EventArgs
  {
    public readonly string Message;

    public LogEventArgs(string message)
    {
      Message = message;
    }
  }
  
  /// <summary>
  /// Manages an simulation instance.
  /// </summary>
  public class SimRun
  {
    public readonly SimInstance Instance;
    public readonly SimLogger Logger;

    public bool Started { get; private set; }
    public bool Running { get; private set; }
    public bool IsRendering;
    public bool IsLogging;
    public int RenderWidth;
    public int RenderHeight;
    
    /// <summary>
    /// The time between each simulation iteration, is ignored in
    /// Iterate(int x).
    /// </summary>
    public int SimDelay;
    
    public long Iteration { get; private set; }
    public long ElapsedTime { get; private set; }
    public double AverageIterationTime { get; private set; }
    public byte[] RenderData { get; private set; }
    
    public event EventHandler OnInit;
    public event EventHandler OnUpdate;
    public event EventHandler<LogEventArgs> OnLog; 
    public event EventHandler OnEnd;
    public event EventHandler OnAbort;
    
    private Thread _simulationThread;
    
    
    public SimRun(SimInstance instance)
    {
      Instance = instance;
      
      Logger = new SimLogger(instance.Proxy.ClassName);
      SimDelay = 10;
      IsRendering = true;
      RenderWidth = 400;
      RenderHeight = 400;
      Iteration = -1;
    }
    
    private void Log(string message)
    {
      OnLog?.Invoke(this, new LogEventArgs(message));
      if (IsLogging) Logger.Log(message);
    }

    /// <summary>
    /// Save the current Render data as a PNG image.
    /// </summary>
    public void SaveImage()
    {
      if (Running)
      {
        charlie.Logger.Say("Please stop the simulation first.");
        return;
      }
      if (RenderData == null)
      {
        charlie.Logger.Say("The render output is empty.");
        return;
      }

      var rand = new Random();
      var title = "render-" + Instance.GetTitle().Replace(" ", "_") + "-" +
                  Math.Truncate(rand.NextDecimal() * 100000000);

      var surface = new ImageSurface(RenderData, Format.ARGB32,
        RenderWidth, RenderHeight, 4 * RenderWidth);
      surface.WriteToPng(Path.Combine(Logger.OutputDir, title + ".png"));
      surface.Dispose();
    }
    
    private static Dictionary<string, string> ParseConfig(string config)
    {
      var result = new Dictionary<string, string>();
      var lines = config.Split(new[] {Environment.NewLine},
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
    
    /// <summary>
    /// Initialize the simulation run.
    /// </summary>
    /// <param name="model"></param>
    public void Init(string model)
    {
      Iteration = 0;
      ElapsedTime = 0;
      AverageIterationTime = 0;
      
      try
      {
        Log("<title>" + Instance.GetTitle() + "</title>");
        Log("<meta>" + Instance.GetMeta() + "</meta>");
        Log("<model>" + model + "</model>");
        Instance.Init(ParseConfig(model));
        Log(Instance.Log());
        RenderData = Instance.Render(RenderWidth, RenderHeight);
        OnInit?.Invoke(this, EventArgs.Empty);
        OnUpdate?.Invoke(this, EventArgs.Empty);
      }
      catch (Exception e)
      {
        Log("<error>" + e + "</error>");
      }
    }

    /// <summary>
    /// Run the current simulation until Stop() has been called.
    /// Between each step the simulation will pause for 'SimDelay' milliseconds.
    /// After each iteration the result will be rendered and log output
    /// will be written.
    /// </summary>
    public void Start()
    {
      if (Started || Running) return;
      Started = true;
      Running = true;
      
      _simulationThread = new Thread(() =>
      {
        long deltaTime = 0;
        while (Started)
        {
          var timer = Stopwatch.StartNew();
          try
          {
            Instance.Update(deltaTime);
            Log(Instance.Log());
            RenderData = IsRendering ? 
              Instance.Render(RenderWidth, RenderHeight) : null;
          }
          catch (Exception e)
          {
            Log("<error>" + e + "</error>");
          }

          if (IsRendering) Thread.Sleep(SimDelay);
          
          timer.Stop();
          deltaTime = timer.ElapsedMilliseconds;
        
          Iteration++;
          ElapsedTime += deltaTime;
          AverageIterationTime = (.0 + ElapsedTime) / Iteration;
          
          OnUpdate?.Invoke(this, EventArgs.Empty);
        }

        Running = false;
      });
      _simulationThread.Start();
    }

    /// <summary>
    /// Run the current simulation for a number of iterations ('steps').
    /// Only at the end of all iterations will the simulation result 
    /// rendered and the log output be written.
    /// </summary>
    /// <param name="steps"></param>
    public void Start(int steps)
    {
      if (Started || Running) return;
      Started = true;
      Running = true;
      
      _simulationThread = new Thread(() =>
      {
        var timer = Stopwatch.StartNew();
        while (steps > 0 && Started)
        {
          try { Instance.Update(20); }
          catch (Exception e) { charlie.Logger.Warn(e.ToString()); }
          Iteration++;
          steps--;
        }
        timer.Stop();
        ElapsedTime += timer.ElapsedMilliseconds;

        try
        {
          Log(Instance.Log());
          RenderData = IsRendering ? 
            Instance.Render(RenderWidth, RenderHeight) : null;
        }
        catch (Exception e)
        {
          Log("<error>" + e + "</error>");
        }
      
        Started = false;
        Running = false;
        OnUpdate?.Invoke(this, EventArgs.Empty);
      });
      _simulationThread.Start();
    }
    
    public void Stop()
    {
      Started = false;
    }
    
    public void End()
    {
      try
      {
        Instance.End();
      }
      catch (Exception e)
      {
        Log("<error iteration=\"Last\">" + e + "</error>");
      }
      
      Log("<iterations>" + Iteration + "</iterations>");
      Log("<elapsed-time>" + ElapsedTime + "</elapsed-time>");
      Log("<average-time>" + AverageIterationTime + 
          "</average-time>");
      Logger.WriteLogBuffer();
      
      OnEnd?.Invoke(null, EventArgs.Empty);
    }

    public void Abort()
    {
      throw new NotImplementedException();
//      _simulationThread.Interrupt();
//      OnAbort?.Invoke(null, EventArgs.Empty);
    }
  }

  public static class Logger
  {
    public static void Say(string text)
    {
      Console.WriteLine(text);
    }
    
    public static void Warn(string text)
    {
      Console.WriteLine(text);
    }

    public static void Error(string text)
    {
      Console.WriteLine(text);
    }
  }

  /// <summary>
  /// Charlie, a general simulation framework.
  /// </summary>
	public static class Charlie
  {
		public static void Main(string[] args)
    {
      if (args.Contains("--nogui"))
      {
        var app = new CharlieTextApp();
        app.Run();
      }
      else
      {
        Application.Init();
        var app = new CharlieGraphicalApp();
        Application.Run();
      }
    }
  }

  public class CharlieTextApp
  {
    private bool _running;
    private readonly List<Simulation> _sims;
    private readonly List<SimRun> _runs;
    
    public CharlieTextApp()
    {
      _running = true;
      _sims = new List<Simulation>();
      _runs = new List<SimRun>();
    }

    private void LoadAndInstantiate(string complexPath)
    {
      var parsed = Simulation.ParseComplexPath(complexPath);
      LoadAndInstantiate(parsed[0], parsed[1]);
    }
    
    private void LoadAndInstantiate(string path, string className)
    {
      foreach (var s in _sims)
      {
        if (s.Path == path && s.ClassName == className) return;
      }
      var sim = new Simulation(path, className);
      _sims.Add(sim);
      _runs.Add(new SimRun(sim.Spawn()));
    }
    
    private void LoadDefault()
    {
      var path = AppDomain.CurrentDomain.BaseDirectory + "Examples.dll";
      const string className = "charlie.DefaultSimulation";
      LoadAndInstantiate(path, className);
    }

    private static void RenderInfo(string message)
    {
      Console.ForegroundColor = ConsoleColor.Yellow;
      Console.WriteLine(message);
      Console.ResetColor();
      Console.Write("Press enter to continue ");
      Console.ReadLine();
    }
    
    private static void RenderTitle(string message)
    {
      Console.ForegroundColor = ConsoleColor.Yellow;
      Console.WriteLine(message);
      Console.ResetColor();
    }
    
    private static void Render(string message)
    {
      Console.WriteLine(message);
    }

    private void RenderSimulations()
    {
      if (_sims.Count == 0) return;
      RenderTitle("Simulations:");
      foreach (var sim in _sims) Render("- " + sim.ComplexPath);
    }
    
    private void RenderRuns()
    {
      if (_runs.Count == 0) return;
      RenderTitle("Runs:");
      foreach (var run in _runs)
      {
        Render("- " + run.Instance.Proxy.ClassName + " " + run.Instance.Id + 
               ", " + run.Iteration + 
               ", " + run.ElapsedTime +
               ", " + run.Running);
        Render("  " + 
               (run.IsLogging ? "Logging yes" : "Logging no") + ", " + 
               (run.IsRendering ? "Rendering yes" : "Rendering no") + ", " +
               "RenderSize: " + run.RenderWidth + "x" + run.RenderHeight +
               ", Delay: " + run.SimDelay);
      }
    }

    private static void RenderHelp()
    {
      RenderInfo("Use syntax: 'load', 'load x', 'unload', 'spawn', " +
                 "'start x', 'stop x'");
    }
    
    public void Run()
    {
      while (_running)
      {
        Console.Clear();
        Render("Charlie - A Simulation Engine");
        Render("=============================");
        RenderSimulations();
        RenderRuns();
        
        Console.Write("> ");
        var input = Console.ReadLine();
        if (input == null) return;
        
        var line = input.Split(' ');
        if (line[0] == "q" || line[0] == "Q") _running = false;
        else if (line[0] == "load")
        {
          if (line.Length == 1) LoadDefault();
          else if (line.Length == 2)
          {
            LoadAndInstantiate(line[1]);
          }
          else
          {
            RenderInfo(
              "Please use 'load' or 'load x' where x is a complex path to a " +
              "simulation e.g. /dir/to/my/sim.dll:class");
          }
        }
        else if (line[0] == "unload")
        {
          if (_sims.Count < 1) continue;
          
          _runs.RemoveAll(run =>
            run.Instance.Proxy.ClassName == _sims.Last().ClassName);
          _sims.Last().Unload();
          _sims.Remove(_sims.Last());
        }
        else if (line[0] == "spawn")
        {
          if (_sims.Count < 1) continue;
          
          var run = new SimRun(_sims.Last().Spawn());
          _runs.Add(run);
        }
        else if(line[0] == "init")
        {
          if (line.Length != 2)
          {
            RenderInfo("Use syntax: 'init x' where x is the run-id");
            continue;
          }

          if (!int.TryParse(line[1], out var runId)) continue;
          foreach (var run in _runs)
          {
            if (run.Instance.Id != runId) continue;
            run.Init(run.Instance.GetConfig());
            break;
          }
        }
        else if (line[0] == "start")
        {
          if (line.Length != 2)
          {
            RenderInfo("Use syntax: 'start x' where x is the run-id");
            continue;
          }
          
          if (!int.TryParse(line[1], out var runId)) continue;
          foreach (var run in _runs)
          {
            if (run.Instance.Id != runId) continue;
            if (run.Iteration < 1)
            {
              RenderInfo("Please initialize instance first");
              break;
            }
            run.Start();
            break;
          }
        }
        else if (line[0] == "stop")
        {
          if (line.Length != 2) 
          {
            Render("Use syntax: 'stop x' where x is the run-id");
            continue;
          }

          if (!int.TryParse(line[1], out var runId)) continue;
          foreach (var run in _runs)
          {
            if (run.Instance.Id != runId) continue;
            run.Stop();
            break;
          }
        }
        else if (line[0] != "")
        {
          RenderHelp();
        }
      }
    }
  }

  internal class LogTextView : DrawingArea
  {
    public int NumberOfLines = 100;
    public int FontSize = 10;
    public int Padding = 3;
    private List<string> _lines = new List<string>();

    public LogTextView()
    {
      for (var i = 0; i < NumberOfLines; i++) _lines.Add(".");
    }

    public void Log(string message)
    {
      _lines.Add(message);
      if (_lines.Count > NumberOfLines) _lines.Remove(_lines[0]); 
      QueueDraw();
    }

    public void Clear()
    {
      _lines.Clear();
      for (var i = 0; i < NumberOfLines; i++) _lines.Add(".");
      QueueDraw();
    }
    
    protected override bool OnDrawn(Context ctx)
    {
      ctx.SelectFontFace("Andale Mono", FontSlant.Normal, FontWeight.Normal);
      ctx.SetFontSize(FontSize);
      ctx.SetSourceRGB(.65, .65, .65);
      
      var lineHeight = FontSize + Padding;
      for (var i = 1; i <= _lines.Count; i++)
      {
        ctx.MoveTo(0, AllocatedHeight - i * lineHeight);
        ctx.ShowText(_lines[_lines.Count - i]);
      }
      
      return true;
    }
  }
  
  internal class LogOutput : Box
  {
    private LogTextView _logTextView;
    
    public LogOutput() : base(Orientation.Vertical, 0)
    {
      _logTextView = new LogTextView {HeightRequest = 150};
      _logTextView.ShowAll();

      var title = new Label("Log output");
      var clearBtn = new Button("clear");
      clearBtn.Clicked += (sender, args) => _logTextView.Clear();
      var hideBtn = new Button("show");
      
      var titleBar = new HBox(false, 0) {Name = "titleBar"};
      titleBar.PackStart(title, false, false, 0);
      titleBar.PackEnd(hideBtn, false, false, 0);
      titleBar.PackEnd(clearBtn, false, false, 0);
      
      var visible = false;
      hideBtn.Clicked += (sender, args) =>
      {
        visible = !visible;
        if (visible)
        {
          PackStart(_logTextView, true, true, 0);
          hideBtn.Label = "hide";
        }
        else
        {
          Remove(_logTextView);
          hideBtn.Label = "show";
        }
      };

      PackStart(titleBar, false, false, 0);
      
      Name = "logOutput";
    }

    public void Log(string message)
    {
      _logTextView.Log(message);
    }
  }
  
  public class CharlieGraphicalApp
  {
    public const string Version = "1.0.0";
    public const string Author = "Jakob Rieke";
    public const string Copyright = "Copyright © 2019 Jakob Rieke";

    private Simulation _sim;
    private SimRun _activeRun;
    
    private DrawingArea _canvas;
    private LogOutput _logOutput;
    private Label _iterationLbl;
    private Box _title;
    private TextBuffer _configBuffer;


    public CharlieGraphicalApp()
    {
      LoadDefault();
      
      var provider = new CssProvider();
      provider.LoadFromPath(GetResource("style.css"));
      StyleContext.AddProviderForScreen(Screen.Default, provider, 800);

      var window = new Window(WindowType.Toplevel)
      {
        DefaultWidth = 380,
        DefaultHeight = 600,
        Title = "",
        Role = "Charlie",
        Resizable = true
      };
      window.Move(20, 80);
      window.SetIconFromFile(GetResource("logo.png"));
      window.Child = CreateRoot();
      window.Destroyed += (sender, args) => Quit();
      window.ShowAll();
    }

    private void Quit()
    {
      _activeRun.Stop();
      Application.Quit();
    }
    
    private void LoadDefault()
    {
      Load(AppDomain.CurrentDomain.BaseDirectory + "/Examples.dll",
        "charlie.DefaultSimulation");
    }
    
    private void Load(string complexPath)
    {
      try
      {
        var parsed = Simulation.ParseComplexPath(complexPath);
        Load(parsed[0], parsed[1]);
      }
      catch (ArgumentException)
      {
        Logger.Warn("Invalid path, please specify class inside dll");
      }
    }
    
    /// <summary>
    /// Load a simulation from a given .dll file and a classname and create
    /// a simulation run (_activeRun).
    /// Call AfterLoad() and initialize _activeRun. 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="className"></param>
    private void Load(string path, string className)
    {
      _sim?.Unload();
      try
      {
        _sim = new Simulation(path, className);
        _activeRun = new SimRun(_sim.Spawn())
        {
          RenderHeight = 800, 
          RenderWidth = 800
        };
        _activeRun.OnUpdate += (sender, args) =>
        {
          Application.Invoke((s, a) => AfterUpdate());
        };
        _activeRun.OnLog += (sender, args) =>
        {
          Application.Invoke((s, a) => _logOutput.Log(args.Message));
        };
      }
      catch (ArgumentException e)
      {
        Logger.Warn(e.Message);
        _sim = null;
        LoadDefault();
      }
    }
    
    private static string GetResource(string resource)
    {
      return AppDomain.CurrentDomain.BaseDirectory + "resources/" + resource;
    }

    /// <summary>
    /// Called after a simulation was loaded but not initialized.
    /// </summary>
    private void AfterLoad()
    {
      ((Label) _title.Children[0]).Text = _activeRun.Instance.GetTitle();
      ((Label) _title.Children[1]).Text = _activeRun.Instance.GetDescr();
      _configBuffer.Text = _activeRun.Instance.GetConfig();
    }
    
    private void AfterUpdate()
    {
      _canvas.QueueDraw();
      _iterationLbl.Text = 
        "i = " + _activeRun.Iteration + 
        ", e = " + _activeRun.ElapsedTime / 1000 + "s" +
        ", a = " + Math.Round(_activeRun.AverageIterationTime, 2) + "ms";
    }
    
    private Widget CreateRoot()
    {
      _title = new VBox(false, 5)
      {
        new Label(_activeRun.Instance.GetTitle()) {Name = "title", Xalign = 0},
        new Label
        {
          Text = _activeRun.Instance.GetDescr(),
          Wrap = true, 
          Halign = Align.Start, 
          Xalign = 0
        }
      };

      _logOutput = new LogOutput();
      
      var content = new VBox(false, 20) {Name = "root"};
      content.Add(_title);
      content.Add(CreateModuleControl());
      content.Add(CreateCanvas());
      content.Add(CreateControls());
      content.Add(CreateModelInput());
      content.Add(CreateConfig());
      content.Add(CreateAbout());
      content.SizeAllocated += (o, a) => content.QueueDraw();

      var mainArea = new ScrolledWindow
      {
        OverlayScrolling = false,
        VscrollbarPolicy = PolicyType.External,
        HscrollbarPolicy = PolicyType.Never,
        MinContentWidth = 380,
        MinContentHeight = 420,
        Child = content
      };

      var result = new VBox(false, 0);
      result.PackStart(mainArea, true, true, 0);
      result.PackEnd(_logOutput, false, false, 0);
      return result;
    }

    private Box CreateConfig()
    {
//      var generalSubtitleIcon = new Image(
//        GetResource("general-prefs-icon.png"));
//      generalSubtitleIcon.Pixbuf = generalSubtitleIcon.Pixbuf.ScaleSimple(
//        15, 15, InterpType.Bilinear);

      var title = new Label("Configuration") 
      {
        Name = "configTitle",
        Halign = Align.Start
      };
      
      var delayEntry = new Entry("" + _activeRun.SimDelay)
      {
        PlaceholderText = "" + _activeRun.SimDelay,
        HasFrame = false
      };
      delayEntry.Activated += (s, a) =>
      {
        if (int.TryParse(delayEntry.Text, out var value) && value >= 0)
        {
          _activeRun.SimDelay = value;
        }
      };
      var delayControl = new HBox(false, 0);
      delayControl.PackStart(new Label(
          "Delay between iterations in ms "), false, false, 0);
      delayControl.PackStart(delayEntry, false, false, 0);

      var logToFileLabel = new Label("Write log to file ");
      var logToFileButton = new ToggleButton(
        _activeRun.IsLogging ? "Yes" : "No");
      logToFileButton.Clicked += (o, a) =>
      {
        _activeRun.IsLogging = !_activeRun.IsLogging;
        logToFileButton.Label = _activeRun.IsLogging ? "Yes" : "No";
      };
      var logToFileControl = new HBox(false, 0);
      logToFileControl.PackStart(logToFileLabel, false, false, 0);
      logToFileControl.PackStart(logToFileButton, false, false, 0);

      var result = new VBox (false, 0) {Name = "config"};
      result.PackStart(title, false, false, 0);
      result.PackStart(delayControl, false, false, 0);
      result.PackStart(logToFileControl, false, false, 0);
      result.ShowAll();
      
      return result;
    }

    private Box CreateModuleControl()
    {
      var defaultPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "Examples.dll"
      ) + " : run_charlie.DefaultSimulation";
      var pathEntry = new Entry(defaultPath)
      {
        PlaceholderText = "/path/to/your/module.dll", 
        HasFocus = false,
        HasFrame = false
      };
      pathEntry.FocusGrabbed += (sender, args) => 
        pathEntry.SelectRegion(pathEntry.TextLength, pathEntry.TextLength);
      
      var loadBtn = new Button("Load");
      loadBtn.Clicked += (sender, args) =>
      {
        if (_activeRun.Running)
        {
          Logger.Warn("Please stop the simulation first");
          return;
        }
        
        Load(pathEntry.Text);
        AfterLoad();
        _activeRun.Init(_configBuffer.Text);
      };
      
      var result = new HBox(false, 15);
      result.PackStart(pathEntry, true, true, 0);
      result.PackStart(loadBtn, false, false, 0);
      return result;
    }
    
    private Box CreateControls()
    {
      var startBtn = new Button("Start") {HasFocus = true};
      startBtn.Clicked += Start;

      void Stop(object sender, EventArgs args)
      {
        _activeRun.Stop();
        startBtn.Label = "Start";
        startBtn.Clicked -= Stop;
        startBtn.Clicked += Start;
      }

      void Start(object sender, EventArgs args)
      {
        _activeRun.Start();
        startBtn.Label = "Stop ";
        startBtn.Clicked -= Start;
        startBtn.Clicked += Stop;
      }
      
      var initBtn = new Button("Init");
      initBtn.Clicked += (sender, args) =>
      {
        if (_activeRun.Started) Stop(null, null);
        var timer = new Timer(20);
        timer.Elapsed += (o, eventArgs) =>
        {
          if (_activeRun.Running)
          {
            Logger.Say("Waiting for update thread to finish.");
            return;
          }

          _activeRun.Init(_configBuffer.Text);
          timer.Enabled = false;
        };
        timer.Enabled = true;
      };

      var stepsEntry = new Entry("10")
      {
        WidthChars = 4,
        HasFrame = false,
        Alignment = 0.5f
      };
      var stepsBtn = new Button("R") {TooltipText = "Run x iterations"};
      stepsBtn.Clicked += (sender, args) =>
      {
        if (_activeRun.Running) return;
        if (int.TryParse(stepsEntry.Text, out var x)) _activeRun.Start(x);
        else Logger.Warn("Please enter a number >= 0.");
      };
      var runSteps = new HBox(false, 0) {stepsEntry, stepsBtn};
      runSteps.Name = "runSteps";

      var pictureBtn = new Button("P")
      {
        TooltipText = "Save the current render output as PNG",
        Name = "pictureBtn"
      };
      pictureBtn.Clicked += (s, a) => _activeRun.SaveImage();

      var result = new HBox(false, 10);
      result.PackStart(initBtn, false, false, 0);
      result.PackStart(startBtn, false, false, 0);
      result.PackStart(runSteps, false, false, 0);
      result.PackEnd(pictureBtn, false, false, 0);
      result.HeightRequest = 10;
      return result;
    }
    
    private Box CreateCanvas()
    {
      var renderTitle = new Label("Rendering") {Halign = Align.Start};
      
      _canvas = new DrawingArea {Name = "canvas"};
      _canvas.SetSizeRequest(400, 400);
      _canvas.Drawn += (o, args) =>
      {
        args.Cr.Rectangle(0, 0, 
          _canvas.AllocatedWidth, 
          _canvas.AllocatedHeight);
        args.Cr.LineWidth = 3;
        args.Cr.SetSourceRGB(0.2, 0.2, 0.2);
        args.Cr.FillPreserve();
        args.Cr.SetSourceRGB(0.721, 0.722, 0.721);
        args.Cr.Stroke();

        if (_activeRun.RenderData == null) return;

        var surface = new ImageSurface(
          _activeRun.RenderData, Format.ARGB32,
          _activeRun.RenderWidth, 
          _activeRun.RenderHeight,
          4 * _activeRun.RenderWidth);
        args.Cr.Scale(.5, .5);
        args.Cr.SetSourceSurface(surface, 0, 0);
        args.Cr.Paint();
        surface.Dispose();
      };

      _iterationLbl = new Label("i = " + _activeRun.Iteration)
      {
        Halign = Align.Start,
        TooltipText = "Iterations, Elapsed time, Average elapsed time"
      };
      
      void Init (object o, DrawnArgs a)
      {
        _activeRun.Init(_activeRun.Instance.GetConfig());
        _canvas.Drawn -= Init;
      }
      _canvas.Drawn += Init;

      var result = new VBox(false, 7)
      {
        renderTitle, _canvas, _iterationLbl
      };
      return result;
    }

    private Box CreateModelInput()
    {
      _configBuffer = new TextBuffer(new TextTagTable())
      {
        Text = _activeRun.Instance.GetConfig()
      };

      var title = new Label("Model")
      {
        Xalign = 0, Valign = Align.Start
      };
      var textView = new TextView(_configBuffer)
      {
        WidthRequest = 400,
        Indent = 3,
        WrapMode = WrapMode.WordChar
      };
      var result = new VBox(false, 7);
      result.PackStart(title, false, false, 0);
      result.PackStart(textView, true, true, 0);
      return result;
    }

    private static Box CreateAbout()
    {
      var aboutTitle = new Label("About")
      {
        Name = "aboutTitle", Halign = Align.Start
      };
      var result = new VBox(false, 1)
      {
        aboutTitle,
        new Label("RunCharlie v" + Version) {Halign = Align.Start},
        new Label(Author) {Halign = Align.Start},
        new Label(Copyright) {Halign = Align.Start}
      };
      result.Name = "about";
      return result;
    }
  }
}