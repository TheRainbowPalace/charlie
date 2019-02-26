using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Timers;
using Cairo;
using Gdk;
using Gtk;
using Application = Gtk.Application;
using Path = System.IO.Path;
using Thread = System.Threading.Thread;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;


namespace run_charlie
{
  internal class Loader : MarshalByRefObject, ISimulation
  {
    private object _sim;
    private MethodInfo _getTitle;
    private MethodInfo _getDescr;
    private MethodInfo _getConfig;
    private MethodInfo _init;
    private MethodInfo _update;
    private MethodInfo _render;
    private MethodInfo _end;
    private MethodInfo _log;
    
    public void LoadAssembly(string path, string className)
    {
      var assembly = Assembly.Load(AssemblyName.GetAssemblyName(path));
      var type = assembly.GetType(className);
      if (type == null) throw new ArgumentException();
        
      _sim = assembly.CreateInstance(className);
      _getTitle = type.GetMethod("GetTitle");
      _getDescr = type.GetMethod("GetDescr");
      _getConfig = type.GetMethod("GetConfig");
      _init = type.GetMethod("Init");
      _end = type.GetMethod("End");
      _update = type.GetMethod("Update");
      _render = type.GetMethod("Render", new []{typeof(int), typeof(int)});
      _log = type.GetMethod("Log");
    }

    public string GetTitle()
    {
      return (string) _getTitle.Invoke(_sim, new object[0]);
    }

    public string GetDescr()
    {
      return (string) _getDescr.Invoke(_sim, new object[0]);
    }
    
    public string GetConfig()
    {
      return (string) _getConfig.Invoke(_sim, new object[0]);
    }

    public void Init(Dictionary<string, string> config)
    {
      _init.Invoke(_sim, new object[] {config});
    }

    public void End()
    {
      _end.Invoke(_sim, new object[0]);
    }

    public void Update(long dt)
    {
      _update.Invoke(_sim, new object[] {dt});
    }

    public byte[] Render(int width, int height)
    {
      return (byte[]) _render.Invoke(_sim, new object[] {width, height});
    }

    public string Log()
    {
      return (string) _log.Invoke(_sim, new object[0]);
    }
  }

  internal static class Logger
  {
    public static void Say(string text)
    {
      Console.WriteLine(text);
    }
    
    public static void Warn(string text)
    {
//      Console.ForegroundColor = ConsoleColor.Yellow;
      Console.WriteLine(text);
//      Console.ResetColor();
    }

    public static void Moan(string text)
    {
      Console.WriteLine(text);
    }
  }
  
  /// <summary> RunCharlie is a general simulation framework. </summary>
  public class RunCharlie
  {
    private bool _started;
    private long _iteration;
    private long _elapsedTime;
    private ISimulation _sim;
    private Thread _logicThread;
    private AppDomain _appDomain;

    private Box _root;
    private DrawingArea _canvas;
    private Label _iterationLbl;
    private Box _title;
    private TextBuffer _configBuffer;

    public RunCharlie()
    {
      _sim = new DefaultSimulation();
      
      var provider = new CssProvider();
      provider.LoadFromPath(
        AppDomain.CurrentDomain.BaseDirectory + "/style.css");
      StyleContext.AddProviderForScreen(Screen.Default, provider, 800);

      var window = new Window(WindowType.Toplevel)
      {
        WidthRequest = 440,
        HeightRequest = 600,
        Title = "",
        Role = "RunCharlie",
        Resizable = false,
        FocusOnMap = true
      };
      window.Destroyed += (sender, args) =>
      {
        Application.Quit();
        Stop();
      };
      window.Move(100, 100);
      window.SetIconFromFile(
        AppDomain.CurrentDomain.BaseDirectory + "/logo.png");
      window.Realized += (sender, args) =>
      {
        _root = CreateRoot();
        var scroll = new ScrolledWindow
        {
          OverlayScrolling = false,
          KineticScrolling = true,
          VscrollbarPolicy = PolicyType.External,
          MinContentHeight = 600,
          MaxContentWidth = 400,
          Child = _root
        };
        window.Child = scroll;
        scroll.ShowAll();
        Init();
      };
      window.ShowAll();
    }

    private void LoadModule(string complexPath)
    {
      var text = complexPath.Replace(" ", "");
      var index = text.LastIndexOf(':');
      if (index < 0) Logger.Warn("Please specify simulation by path:classname");
        
      LoadModule(text.Substring(0, index),
        text.Substring(index + 1, text.Length - 1 - index));
    }
    
    private void LoadModule(string path, string className)
    {
      if (!File.Exists(path))
      {
        Logger.Warn("Simulation file does not exist.");
        return;
      }
      if (_logicThread != null)
      {
        Logger.Warn("Please terminate simulation first.");
        return;
      }

      var previousSim = _sim;
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
        var appDomain = AppDomain.CreateDomain("SimulationDomain", null, ads);
        var loader = (Loader) appDomain.CreateInstanceAndUnwrap(
          typeof(Loader).Assembly.FullName, typeof(Loader).FullName);

        loader.LoadAssembly(path, className);
        _sim.End();
        _sim = loader;

        if (_appDomain != null) AppDomain.Unload(_appDomain);

        _appDomain = appDomain;
        
        ((Label) _title.Children[0]).Text = _sim.GetTitle();
        ((Label) _title.Children[1]).Text = _sim.GetDescr();
        _configBuffer.Text = _sim.GetConfig();
        Init();
      }
      catch (ArgumentException)
      {
        Logger.Warn("Class \"" + className + "\" does not exists in dll.");
        _sim = previousSim;
      }
      catch (Exception e)
      {
        Logger.Warn(e.ToString());
        _sim = previousSim;
      }
    }
    
    private static Dictionary<string, string> ParseConfig(string config)
    {
      var result = new Dictionary<string, string>();
      var lines = config.Split(new[] {System.Environment.NewLine},
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
    
    private void Init()
    {
      var config = ParseConfig(_configBuffer.Text);
      _sim.Init(config);
      _iteration = 0;
      _elapsedTime = 0;
      AfterUpdate();
    }

    private void Start()
    {
      _started = true;
      _logicThread = new Thread(Update);
      _logicThread.Start();
    }

    private void Start(int steps)
    {
      _logicThread = new Thread(() => Update(steps));
      _logicThread.Start();
    }
    
    private void Stop()
    {
      _started = false;
    }

    private void Update(int steps)
    {
      var timer = Stopwatch.StartNew();
      while (steps > 0)
      {
        try { _sim.Update(20); }
        catch (Exception e) { Logger.Warn(e.ToString()); }
        _iteration++;
        steps--;
      }
      timer.Stop();
      _elapsedTime += timer.ElapsedMilliseconds;
      Application.Invoke((sender, args) => AfterUpdate());
      _logicThread = null;
    }
    
    private void Update()
    {
      long deltaTime = 0;
      while (_started)
      {
        var timer = Stopwatch.StartNew();
        try
        {
          _sim.Update(deltaTime);
        }
        catch (Exception e) { Logger.Warn(e.ToString()); }

        _iteration++;
        Application.Invoke((sender, args) => AfterUpdate());
        Thread.Sleep(20);
        
        timer.Stop();
        deltaTime = timer.ElapsedMilliseconds;
        _elapsedTime += deltaTime;
      }
      _logicThread = null;
    }

    private void AfterUpdate()
    {
      _canvas.QueueDraw();
      _iterationLbl.Text = "i = " + _iteration + 
                           ", e = " + _elapsedTime / 1000 + "s";
    }

    private Box CreateRoot()
    {
      _title = new VBox(false, 5)
      {
        new Label(_sim.GetTitle()) {Name = "title", Xalign = 0},
        new Label
        {
          Text = _sim.GetDescr(),
          Wrap = true, 
          Halign = Align.Start, 
          Xalign = 0
        }
      };
      _title.MarginTop = 15;
      
      var result = new VBox (false, 20)
      {
        MarginStart = 20, 
        MarginEnd = 20
      };
      
      result.PackStart(_title, false, false, 0);
      result.PackStart(CreateModuleControl(), false, false, 0);
      result.PackStart(CreateCanvas(), false, false, 0);
      result.PackStart(CreateControls(), false, false, 0);
      result.PackStart(CreateConfig(), true, true, 0);
      result.ShowAll();
      return result;
    }
    
    private Box CreateModuleControl()
    {
      var defaultPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "../RunCharlie/Examples.dll"
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
        LoadModule(pathEntry.Text);
        _root.QueueDraw();
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
        Logger.Say("Stop ");
        
        this.Stop();
        startBtn.Label = "Start";
        startBtn.Clicked -= Stop;
        startBtn.Clicked += Start;
      }

      void Start(object sender, EventArgs args)
      {
        Logger.Say("Start");
        
        this.Start();
        startBtn.Label = "Stop ";
        startBtn.Clicked -= Start;
        startBtn.Clicked += Stop;
      }
      
      var initBtn = new Button("Init");
      initBtn.Clicked += (sender, args) =>
      {
        if (_started) Stop(null, null);
        var t = new Timer(20);
        t.Elapsed += (o, eventArgs) =>
        {
          if (_logicThread != null)
          {
            Logger.Say("Waiting for update thread to finish.");
            return;
          }
          Init();
          t.Enabled = false;
        };
        t.Enabled = true;
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
        if (_logicThread != null) return;
        if (int.TryParse(stepsEntry.Text, out var x)) this.Start(x);
        else Logger.Warn("Please enter a number >= 0.");
      };
      var runSteps = new HBox(false, 0) {stepsEntry, stepsBtn};
      runSteps.Name = "runSteps";

      _iterationLbl = new Label("i = " + _iteration) {Halign = Align.End};

      var result = new HBox(false, 10);
      result.PackStart(initBtn, false, false, 0);
      result.PackStart(startBtn, false, false, 0);
      result.PackStart(runSteps, false, false, 0);
      result.PackStart(_iterationLbl, true, true, 0);
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

        try
        {
          var data = _sim.Render(
            _canvas.AllocatedWidth, 
            _canvas.AllocatedHeight);
          // Todo set Stride dynamically
          var s = new ImageSurface(data, Format.ARGB32, 
            _canvas.AllocatedWidth, 
            _canvas.AllocatedHeight, 1600);
          args.Cr.SetSourceSurface(s, 0, 0);
          args.Cr.Paint();
          s.Dispose();
        }
        catch (Exception e) { Logger.Warn(e.ToString()); }
      };

      var result = new VBox(false, 7);
      result.PackStart(renderTitle, false, false, 0);
      result.PackStart(_canvas, false, false, 0);
      return result;
    }

    private Box CreateConfig()
    {
      _configBuffer = new TextBuffer(new TextTagTable())
      {
        Text = _sim.GetConfig()
      };

      var title = new Label("Configuration")
      {
        Xalign = 0, Valign = Align.Start
      };
      var textView = new TextView(_configBuffer)
      {
        Monospace = true,
        WidthRequest = 400,
        Name = "configEntry",
        Indent = 3,
        WrapMode = WrapMode.Char
      };
      var result = new VBox(false, 7);
      result.PackStart(title, false, false, 0);
      result.PackStart(textView, true, true, 0);
      return result;
    }
  }
}