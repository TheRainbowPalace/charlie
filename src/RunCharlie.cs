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
using Key = Gtk.Key;
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

  // Done: Fix Rendering is not done on the logic thread
  // Done: Block Load button until simulation is loaded
  // Done: Fix first initialization is done to early
  // Done: Fix simulations are not hot reloading
  // Done: Add about section
  // Done: Fix text overflow on long config input lines
  // Done: Select window after creation
  // Todo: Remove delta time from Simulation.Update
  // Todo: Stop simulation thread on Ctrl+C
  // Todo: Hide titlebar on MacOs
  // Todo: Fix low quality rendering duo to ImageSurface size
  // Todo: Create root node asynchronously
  // Todo: Fix text overflow on too many iterations / time
  // Todo: Add app settings component (dark or light mode etc.)  
  // Todo: Add task-runner component (Allows to run scheduled simulations)
  // Todo: Add button to abort simulation
  // Todo: Add a commandline version of RunCharlie (> charlie file -params)
  // Todo: Add magnetic scroll
  // Todo: Add simulation speed option
  // Todo: Add option to set output (logging & picture) path
  // Todo: Add option to enable and disable logging
  // Todo: Add selector for logging output format
  // Todo: Add button to save pictures
  // Todo: Add timeline to go back in time
  // Todo: Add syntax highlighting to config editor
  /// <summary> RunCharlie is a general simulation framework. </summary>
  public class RunCharlie
  {
    public const string Version = "1.0.0";
    public const string Author = "Jakob Rieke";
    public const string Copyright = "Copyright © 2019 Jakob Rieke";
    private bool _started;
    private long _iteration;
    private long _elapsedTime;
    private ISimulation _sim;
    private Thread _simulationThread;
    private AppDomain _appDomain;
    private byte[] _renderData;

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
        Resizable = false
      };
      window.Destroyed += (sender, args) =>
      {
        Stop();
        Application.Quit();
      };
      window.Move(100, 100);
      window.SetIconFromFile(
        AppDomain.CurrentDomain.BaseDirectory + "/logo.png");

      var prefPage = new VBox {Name = "prefPage"};
      prefPage.Add(new Label("Preferences"));
      prefPage.ShowAll();
      
      var resultsPage = new VBox {Name = "resultsPage"};
      resultsPage.Add(new Label("Results"));
      resultsPage.ShowAll();
      
      var simPageContent = CreateRoot();
      var simPage = new ScrolledWindow
      {
        OverlayScrolling = false,
        KineticScrolling = true,
        VscrollbarPolicy = PolicyType.External,
        HscrollbarPolicy = PolicyType.Never,
        MinContentHeight = 600,
        MaxContentWidth = 400,
        Child = simPageContent
      };
      void SetPage(Widget page)
      {
        if (window.Child == page) return;
        window.Remove(window.Child);
        window.Add(page);
      }

      ModifierType mod;
      window.KeyPressEvent += (o, a) =>
      {
        mod = a.Event.State & Accelerator.DefaultModMask;
        if (mod != ModifierType.ControlMask) return;
        if (a.Event.Key == Gdk.Key.Key_1) SetPage(prefPage);
        else if (a.Event.Key == Gdk.Key.Key_2) SetPage(simPage);
        else if (a.Event.Key == Gdk.Key.Key_3) SetPage(resultsPage);
      };
      window.Child = simPage;
      window.ShowAll();
      
      void Init (object o, DrawnArgs a)
      {
        this.Init();
        _canvas.Drawn -= Init;
      }

      _canvas.Drawn += Init;
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
      if (_simulationThread != null)
      {
        Logger.Warn("Please terminate simulation first.");
        return;
      }

      try { _sim.End(); }
      catch (Exception e) { Logger.Warn(e.ToString()); }
      
      try
      {
        if (_appDomain != null) AppDomain.Unload(_appDomain);
        
        var ads = new AppDomainSetup
        {
          ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
          DisallowBindingRedirects = false,
          DisallowCodeDownload = true,
          ConfigurationFile =
            AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
        };
        _appDomain = AppDomain.CreateDomain("SimulationDomain", null, ads);
        var loader = (Loader) _appDomain.CreateInstanceAndUnwrap(
          typeof(Loader).Assembly.FullName, typeof(Loader).FullName);

        loader.LoadAssembly(path, className);
        _sim = loader;
      }
      catch (ArgumentException)
      {
        Logger.Warn("Class \"" + className + "\" does not exists in dll.");
        _sim = new DefaultSimulation();
      }
      catch (Exception e)
      {
        Logger.Warn(e.ToString());
        _sim = new DefaultSimulation();
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
      _iteration = 0;
      _elapsedTime = 0;
      try
      {
        _sim.Init(config);
        _renderData = _sim.Render(
          _canvas.AllocatedWidth,
          _canvas.AllocatedHeight);
      }
      catch (Exception e) { Logger.Warn(e.ToString()); }
      AfterUpdate();
    }

    private void Start()
    {
      _started = true;
      _simulationThread = new Thread(Update);
      _simulationThread.Start();
    }

    private void Start(int steps)
    {
      _simulationThread = new Thread(() => Update(steps));
      _simulationThread.Start();
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

      try
      {
        _renderData = _sim.Render(
          _canvas.AllocatedWidth,
          _canvas.AllocatedHeight);
      }
      catch (Exception e) { Logger.Warn(e.ToString()); }
      
      Application.Invoke((sender, args) => AfterUpdate());
      _simulationThread = null;
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
          _renderData = _sim.Render(
            _canvas.AllocatedWidth, 
            _canvas.AllocatedHeight);
        }
        catch (Exception e) { Logger.Warn(e.ToString()); }

        Thread.Sleep(20);
        _iteration++;
        
        timer.Stop();
        deltaTime = timer.ElapsedMilliseconds;
        _elapsedTime += deltaTime;
        Application.Invoke((sender, args) => AfterUpdate());
      }
      _simulationThread = null;
    }

    private void AfterUpdate()
    {
      _canvas.QueueDraw();
      _iterationLbl.Text = 
        "i = " + _iteration + ", e = " + _elapsedTime / 1000 + "s";
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

      var result = new VBox(false, 20) {Name = "root"};
      result.Add(_title);
      result.Add(CreateModuleControl());
      result.Add(CreateCanvas());
      result.Add(CreateControls());
      result.Add(CreateConfig());
      result.Add(CreateAbout());
      result.SizeAllocated += (o, a) => result.QueueDraw();
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
        try
        {
          ((Label) _title.Children[0]).Text = _sim.GetTitle();
          ((Label) _title.Children[1]).Text = _sim.GetDescr();
          _configBuffer.Text = _sim.GetConfig();
        }
        catch (Exception e) { Logger.Warn(e.ToString()); }
        Init();
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
        this.Stop();
        startBtn.Label = "Start";
        startBtn.Clicked -= Stop;
        startBtn.Clicked += Start;
      }

      void Start(object sender, EventArgs args)
      {
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
          if (_simulationThread != null)
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
        if (_simulationThread != null) return;
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

        if (_renderData == null) return;
        
        // Todo set Stride dynamically
        var s = new ImageSurface(_renderData, Format.ARGB32, 
          _canvas.AllocatedWidth, 
          _canvas.AllocatedHeight, 1600);
        args.Cr.SetSourceSurface(s, 0, 0);
        args.Cr.Paint();
        s.Dispose();
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
        WidthRequest = 400,
        Indent = 3,
        WrapMode = WrapMode.WordChar
      };
      textView.CompositedChanged += (o, a) => Console.WriteLine("Changed " + a);
      var result = new VBox(false, 7);
      result.PackStart(title, false, false, 0);
      result.PackStart(textView, true, true, 0);
      return result;
    }

    private Box CreateAbout()
    {
      var aboutTitle = new Label("About")
      {
        Name = "aboutTitle", Halign = Align.Start
      };
      var result =  new VBox(false, 1)
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