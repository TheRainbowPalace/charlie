using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Cairo;
using MathNet.Numerics.Random;
using DateTime = System.DateTime;
using Path = System.IO.Path;
using Thread = System.Threading.Thread;

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

    [Serializable]
    public struct DataTree
    {
      public string TypeName;
      public string Value;
      public string Description;
      public List<DataTree> Children;

      public override string ToString()
      {
        var result = $"{Value} : {TypeName}";
        if (Children == null) return result;
        
        foreach (var child in Children) result += "\n  - " + child;
        return result;
      }
    }

    private const BindingFlags Flags =
      BindingFlags.NonPublic | BindingFlags.Instance |
      BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty |
      BindingFlags.DeclaredOnly | BindingFlags.ExactBinding |
      BindingFlags.CreateInstance | BindingFlags.SetProperty |
      BindingFlags.SetField | BindingFlags.GetField;
    
    private readonly HashSet<object> _foundValues = new HashSet<object>();

    public string ObjectToString(object value)
    {
      return value.GetType().Name + ":\n" + 
             ObjectToString(value.GetType(), 0, value);
    }
    
    private string ObjectToString(Type type, int depth, object value)
    {
      if (value == null || _foundValues.Contains(value)) return "";
      
      _foundValues.Add(value);
      var result = "";

      var fields = type.GetFields(Flags);
//      var properties = type.GetProperties(Flags);
      
      foreach (var field in fields)
      {
        for (var i = 0; i < depth; i++) result += "  ";
        
        result += "- " + field.FieldType.Name + " : ";
        result += field.Name;

        var fieldType = field.FieldType;
        var fieldValue = field.GetValue(value);

        result += fieldValue != null ? $" = {fieldValue}" : " = null";

        if (fieldType.IsPrimitive)
        {
          result += " --> Primitive type\n";
        }
        else if (fieldType.IsEnum)
        {
          result += " --> Enumeration type\n";
        }
        else if (fieldType.IsValueType)
        {
          result += " --> Struct type\n";
          result += ObjectToString(fieldType, depth + 1, fieldValue);
        }
        else if (fieldType.IsGenericType)
        {
          result += " --> Generic type\n";
          result += ObjectToString(fieldType, depth + 1, fieldValue);
//          foreach (var argument in fieldType.GetGenericArguments())
//          {
//            PrintType(argument, depth + 1);
//          }
        }
        else if (fieldType.IsArray)
        {
          result += " --> Array type\n";
          
          if (fieldValue == null) continue;

          var array = (Array) fieldValue;
          var arrayType = array.GetType().GetElementType();
          
          var i = 0;
          foreach (var elem in array)
          {
            for (var j = 0; j < depth + 1; j++) result += "  ";

            // Use elem.GetType() in the following since array might not contain
            // elements of just one type e.g. an object[] might contain numbers
            // and strings
            
            if (arrayType.IsPrimitive)
            {
              result += $"{i}.";
              result += ObjectToString(elem.GetType(), 0, elem);
            }
            else
            {
              if (elem == null)
              {
                result += $"{i}. {arrayType.Name}: null\n";
              }
              else
              {
                result += $"{i}. {elem.GetType().Name}:\n";
                result += ObjectToString(elem.GetType(), depth + 2, elem);
              }
            }
            i++;
          }
        }
        else if (fieldType.IsClass)
        {
          result += " --> Class type\n";
          ObjectToString(fieldType, depth + 1, fieldValue);
        }
        else result += "\n";
      }
      
//      foreach (var property in properties)
//      {
//        for (var i = 0; i < depth; i++) Console.Write("  ");
//        
//        Console.WriteLine($"* {property.PropertyType.Name} : {property.Name}");
//      }
      _foundValues.Remove(value);
      
      return result;
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

    public string GetState(int index)
    {
      return ObjectToString(_instances[index]);
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

    public string GetState()
    {
      return Proxy.GetState(Id);
    }

    public void Init(Dictionary<string, string> model)
    {
      Proxy.Init(Id, model);
//      Console.WriteLine("TypeTree:");
//      Console.WriteLine(Proxy.GetDataTree(Id));
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

  public class DataLogger
  {
    /// <summary>
    /// The directory where all log output is stored.
    /// </summary>
    public readonly string FileDir;
    
    /// <summary>
    /// The name of the log file.
    /// </summary>
    public readonly string FileName;

    public DataLogger(string projectName, string projectInstanceName,
      int maxFileLength = 50000, int maxBufferLength = 1000)
    {
      MaxFileLength = maxFileLength;
      MaxBufferLength = maxBufferLength;
      _buffer = "";
      
      // -- Create output directory

      FileDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".charlie",
        projectName,
        projectInstanceName);
      
      if (!Directory.Exists(FileDir)) Directory.CreateDirectory(FileDir);
      
      // -- Create file name
      
      var date = DateTime.Now.ToUniversalTime();
      FileName = "run-" + date.Year + 
                 "-" + (date.Month < 10 ? "0" : "") + date.Month + 
                 "-" + (date.Day < 10 ? "0" : "") + date.Day +
                 "T" + (date.Hour < 10 ? "0" : "") + date.Hour + 
                 "-" + (date.Minute < 10 ? "0" : "") + date.Minute + 
                 "-" + (date.Second < 10 ? "0" : "") + date.Second + "Z" +
                 date.Millisecond;
    }
    
    /// <summary>
    /// The maximum length of the internal log buffer. Duo to performance
    /// reasons log entries are first written to RAM and only to file if
    /// the buffer is full or WriteLogBuffer() is called manually. Defaults to
    /// 1000.
    /// </summary>
    public readonly int MaxBufferLength;
    
    /// <summary>
    /// The maximum number of log entries a log file may contain until a new
    /// one is created.
    /// </summary>
    public readonly int MaxFileLength;
    
    private string _buffer;
    private int _fileLength;
    private int _fileCount;

    
    /// <summary>
    /// Write a log entry into the log buffer. The buffer is written to file
    /// if it reaches the MaxBufferLength.
    /// </summary>
    /// <param name="entry"></param>
    public void Log(string entry)
    {
      if (string.IsNullOrEmpty(entry)) return;
      
      _buffer += entry;
      if (_buffer.Length >= MaxBufferLength) WriteBuffer();
    }

    public void WriteBuffer()
    {
      if (_buffer.Length == 0) return;
      
      if (_fileLength + _buffer.Length > MaxFileLength)
      {
        _fileCount++;
        _fileLength = 0;
      }

      var filePath = Path.Combine(
        FileDir,
        FileName + (_fileCount > 0 ? "-" + _fileCount : "") + ".log");
      
      File.AppendAllText(filePath, _buffer);
      
      _buffer = "";
      _fileLength += _buffer.Length;
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
                  Math.Truncate(rand.NextDecimal() * 100000000);

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
        Log("<error>" + e + "</error>\n");
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