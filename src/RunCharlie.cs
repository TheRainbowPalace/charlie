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
    private MethodInfo _getMeta;
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
      _getMeta = type.GetMethod("GetMeta");
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

    public string GetMeta()
    {
      return (string) _getMeta.Invoke(_sim, new object[0]);
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

  internal static class Settings
  {
    // Simulation Settings
    public static int SimSpeed = 10;
    
    // Logging Settings
    public static int LogInterval = 20;
    public static bool logToUi = true;
    public static bool logToFile = false;
    public static string logFilePath = null;
    
    // General Settings
    public const string outputPath = "~/.runcharlie";
    public static string uiTheme = "dark";
    public static int textSize = 12;
    
    private static void Parse(string settingsFile)
    {
      var stream = new FileStream(settingsFile, FileMode.Open);
      if (!stream.CanRead) return;
      stream.Close();
    }
    
    public static void Init()
    {
      const string configFile = outputPath + "/config.json";
      if (File.Exists(configFile)) Parse(configFile);
    }

    public static void Save()
    {
      throw new NotImplementedException();
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
  // Done: Set Stride dynamically when rendering simulation
  // Done: Add app settings component (dark or light mode etc.)  
  // Todo: Remove delta time from Simulation.Update
  // Todo: Stop simulation thread on Ctrl+C
  // Todo: Hide titlebar on MacOs
  // Todo: Fix low quality rendering duo to ImageSurface size
  // Todo: Create root node asynchronously
  // Todo: Fix text overflow on too many iterations / time
  // Todo: Clear events when switching between pages (fixes assertion error
  //   GDK_IS_FRAME_CLOCK)
  // Todo: Fix "Assertion failed: (_cairo_path_fixed_last_op (path) ==
  //   CAIRO_PATH_OP_LINE_TO), function _cairo_path_fixed_drop_line_to,
  //   file cairo-path-fixed.c, line 392." when running Growth simulation
  // Todo: Abort simulation if a certain stopping time is passed
  // Todo: Add task-runner component (Allows to run scheduled simulations)
  // Todo: Add a commandline version of RunCharlie (> charlie file -params)
  // Todo: Add magnetic scroll
  // Todo: Add simulation speed option
  // Todo: Add option to set output (logging & picture) path
  // Todo: Add option to enable and disable logging to file
  // Todo: Add option to enable and disable logging on simulation screen
  // Todo: Add selector for logging output format
  // Todo: Add button & function to save pictures
  // Todo: Add option to compress saved images
  // Todo: Add option for image output format
  // Todo: Add button & function to save simulation video
  // Todo: Add option to compress saved videos
  // Todo: Add option for video output format
  // Todo: Add timeline to go back in time
  // Todo: Add syntax highlighting to config editor
  // Todo: Add layout ability to scale
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
      Settings.Init();
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

      var prefPage = CreatePrefPage();
      prefPage.ShowAll();

      var resultsPage = CreateResultPage();
      resultsPage.ShowAll();
      
      var simPageContent = CreateSimPage();
      var simPage = new ScrolledWindow
      {
        OverlayScrolling = false,
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
//          if (_iteration % Settings.LogInterval == 0) Logger.Say(_sim.Log());
          
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

    private static Box CreatePrefPage()
    {
      var outputFormat = new HBox(false, 10);
      outputFormat.PackStart(new Label("Output format") {Halign = Align.Start}, 
        true, true, 0);
      outputFormat.PackStart(new ToggleButton("JSON"), false, false, 0);
      outputFormat.PackStart(new ToggleButton("YAML"), false, false, 0);
      outputFormat.PackStart(new ToggleButton("XML"), false, false, 0);

      var toggles = new HBox(false, 10);
      toggles.PackStart(new ToggleButton("Night Mode"), false, false, 0);
      toggles.PackStart(new ToggleButton("Log"), false, false, 0);
      
      // Add log interval
      
      var result = new VBox (false, 10) {Name = "prefPage"};
      result.PackStart(new Label("Preferences")
      {
        Name = "prefPageTitle", 
        Halign = Align.Start
      }, false, false, 0);
      result.PackStart(toggles, false, false, 0);
      result.PackStart(outputFormat, false, false, 0);
      result.PackStart(new Entry("~/.runcharlie")
      {
        PlaceholderText = "/path/to/save/your/results",
        HasFrame = false
      }, false, false, 0);
      
      return result;
    }

    private static Box CreateResultPage()
    {
      var result = new VBox {Name = "resultsPage"};
      result.Add(new Label("Results"));
      return result;
    }
    
    private Box CreateSimPage()
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
        var timer = new Timer(20);
        timer.Elapsed += (o, eventArgs) =>
        {
          if (_simulationThread != null)
          {
            Logger.Say("Waiting for update thread to finish.");
            return;
          }

          Init();
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

        var surface = new ImageSurface(_renderData, Format.ARGB32,
          _canvas.AllocatedWidth,
          _canvas.AllocatedHeight,
          4 * _canvas.AllocatedWidth);
        args.Cr.SetSourceSurface(surface, 0, 0);
        args.Cr.Paint();
        surface.Dispose();
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