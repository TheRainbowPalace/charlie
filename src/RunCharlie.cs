using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text;
using System.Timers;
using Cairo;
using Gdk;
using GLib;
using Gtk;
using MathNet.Numerics.Random;
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
    private MethodInfo _getMeta;
    private MethodInfo _init;
    private MethodInfo _update;
    private MethodInfo _render;
    private MethodInfo _end;
    private MethodInfo _log;
    
    /// <summary>
    /// Load the simulation from an assembly.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="className"></param>
    /// <exception cref="ArgumentException"></exception>
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

  public class Simulator
  {
    public bool Started { get; private set; }
    public bool Running { get; private set; }
    public long Iteration { get; private set; }
    public long ElapsedTime { get; private set; }
    public double AverageIterationTime { get; private set; }
    public byte[] RenderData { get; private set; }
    public ISimulation Sim { get; private set; }
    
    private AppDomain _appDomain;
    private Thread _simulationThread;

    public event EventHandler OnUpdate;
    
    /// <summary>
    /// The time between each simulation iteration, is ignored in
    /// Iterate(int x).
    /// </summary>
    public int SimDelay = 10;

    private int _renderWidth;
    private int _renderHeight;


    public void LoadDefault()
    {
      Sim = new DefaultSimulation();
    }
    
    public void Load(string complexPath)
    {
      var text = complexPath.Replace(" ", "");
      var index = text.LastIndexOf(':');
      if (index < 0)
      {
        Logger.Warn("Please specify simulation by path:classname");
        return;
      }
        
      Load(text.Substring(0, index),
        text.Substring(index + 1, text.Length - 1 - index));
    }
    
    public void Load(string path, string className)
    {
      if (!File.Exists(path))
      {
        Logger.Warn("Simulation file does not exist.");
        return;
      }
      if (Running)
      {
        Logger.Warn("Please terminate simulation first.");
        return;
      }

      try { Sim.End(); }
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
        Sim = loader;
      }
      catch (ArgumentException)
      {
        Logger.Warn("Class \"" + className + "\" does not exists in dll.");
        LoadDefault();
      }
      catch (Exception e)
      {
        Logger.Warn(e.ToString());
        Sim = new DefaultSimulation();
      }
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

    private static void Log(string message)
    {
      if (!string.IsNullOrEmpty(message)) Logger.Say(message);
    }

    /// <summary>
    /// Save the current Render data as an png image.
    /// </summary>
    public void SaveImage()
    {
      if (Running)
      {
        Log("Please stop simulation first");
        return;
      }

      var rand = new Random();
      var title = "render-" + Sim.GetTitle().Replace(" ", "_") + "-" +
                  Math.Truncate(rand.NextDecimal() * 100000000);
      
      var surface = new ImageSurface(RenderData, Format.ARGB32,
        _renderWidth, _renderHeight, 4 * _renderWidth);
      surface.WriteToPng(title + ".png");
      surface.Dispose();
    }
    
    /// <summary>
    /// Initialize the simulator and the loaded simulation. Must be called
    /// after a simulation has been loaded.
    /// </summary>
    /// <param name="config"></param>
    /// <param name="renderWidth"></param>
    /// <param name="renderHeight"></param>
    public void Init(string config, int renderWidth, int renderHeight)
    {
      _renderWidth = renderWidth;
      _renderHeight = renderHeight;
      Iteration = 0;
      ElapsedTime = 0;
      AverageIterationTime = 0;
      try
      {
        Sim.Init(ParseConfig(config));
        Log(Sim.Log());
        RenderData = Sim.Render(_renderWidth, _renderHeight);
        OnUpdate?.Invoke(this, EventArgs.Empty);
      }
      catch (Exception e)
      {
        Logger.Warn(e.ToString());
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
            Sim.Update(deltaTime);
            Log(Sim.Log());
            RenderData = Sim.Render(_renderWidth, _renderHeight);
          }
          catch (Exception e) { Logger.Warn(e.ToString()); }

          Thread.Sleep(SimDelay);
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
          try { Sim.Update(20); }
          catch (Exception e) { Logger.Warn(e.ToString()); }
          Iteration++;
          steps--;
        }
        timer.Stop();
        ElapsedTime += timer.ElapsedMilliseconds;

        try
        {
          Log(Sim.Log());
          RenderData = Sim.Render(_renderWidth, _renderHeight);
        }
        catch (Exception e) { Logger.Warn(e.ToString()); }
      
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

    public void Abort()
    {
      throw new NotImplementedException();
//      _simulationThread.Interrupt();
    }
  }
  
  public class Logger
  {
    public readonly string OutputFolder;
    public readonly string LogFile;
    
    public int LogInterval = 20;
    public bool logToFile = false;

    public Logger()
    {
      OutputFolder = "~/.runcharlie";
      LogFile = OutputFolder + "/config.json";
    }

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

	public class App
  {
		public static void Main(string[] args)
    {
			Application.Init();
      var app = new RunCharlie();
      Application.Run();
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
  // Todo: Fix NullPointerException when loading "..RunCharlie/Examples."
  //   instead of "..RunCharlie/Examples.dll"
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
  // Todo: Add label for average iteration duration
  // Todo: Add option to reset configuration to default
  // Todo: Add option to save configuration
  /// <summary> RunCharlie is a general simulation framework. </summary>
  public class RunCharlie
  {
    public const string Version = "1.0.0";
    public const string Author = "Jakob Rieke";
    public const string Copyright = "Copyright © 2019 Jakob Rieke";

    private readonly Simulator _simulator;
    
    private DrawingArea _canvas;
    private Label _iterationLbl;
    private Box _title;
    private TextBuffer _configBuffer;

    public RunCharlie()
    {
      _simulator = new Simulator();
      _simulator.LoadDefault();
      _simulator.OnUpdate += (sender, args) =>
      {
        Application.Invoke((s, a) => AfterUpdate());
      };
      
      var provider = new CssProvider();
      provider.LoadFromPath(GetResource("style.css"));
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
        _simulator.Stop();
        Application.Quit();
      };
      window.Move(100, 100);
      window.SetIconFromFile(GetResource("logo.png"));

      var prefPage = CreatePrefPage();
      prefPage.ShowAll();

      var resultsPage = CreateResultPage();
      resultsPage.ShowAll();
      
      var simPage = CreateSimPage();
      var root = new ScrolledWindow
      {
        OverlayScrolling = false,
        VscrollbarPolicy = PolicyType.External,
        HscrollbarPolicy = PolicyType.Never,
        MinContentHeight = 600,
        MaxContentWidth = 400,
        Child = simPage
      };
      
      void SetPage(Widget page)
      {
        if (root.Child == page) return;
        root.Remove(root.Child);
        root.Add(page);
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
      window.Child = root;
      window.ShowAll();
      
      void Init (object o, DrawnArgs a)
      {
        _simulator.Init(
          _simulator.Sim.GetConfig(), 
          _canvas.AllocatedWidth, 
          _canvas.AllocatedHeight);
        _canvas.Drawn -= Init;
      }

      _canvas.Drawn += Init;
    }

    private static string GetResource(string resource)
    {
      return AppDomain.CurrentDomain.BaseDirectory + "resources/" + resource;
    }

    private void AfterUpdate()
    {
      _canvas.QueueDraw();
      _iterationLbl.Text = 
        "i = " + _simulator.Iteration + 
        ", e = " + _simulator.ElapsedTime / 1000 + "s" +
        ", a = " + Math.Round(_simulator.AverageIterationTime, 2) + "ms";
    }

    private Box CreatePrefPage()
    {
      var result = new VBox (false, 10) {Name = "prefPage"};
      result.PackStart(new Label("Preferences")
      {
        Name = "prefPageTitle",
        Halign = Align.Start
      }, false, false, 0);
      
      // General preferences
      
      var generalSubtitleIcon = new Image(
        GetResource("general-prefs-icon.png"));
      generalSubtitleIcon.Pixbuf = generalSubtitleIcon.Pixbuf.ScaleSimple(
        15, 15, InterpType.Bilinear);

      var generalSubtitle = new HBox(false, 7) {Name = "generalSubtitle"};
      generalSubtitle.PackStart(generalSubtitleIcon, false, false, 0);
      generalSubtitle.PackStart(new Label("General") {Xalign = 0},
        false, true, 0);
      
      result.PackStart(generalSubtitle, false, false, 0);

      var theme = new HBox(false, 10) {Halign = Align.Start};
      theme.PackStart(new Label("Theme"), false, true, 0);
      result.PackStart(theme, false, false, 0);
      
      // Simulation preferences

      var simSubtitleIcon = new Image(GetResource("sim-prefs-icon.png"));
      simSubtitleIcon.Pixbuf = simSubtitleIcon.Pixbuf.ScaleSimple(
        15, 15, InterpType.Bilinear);
      
      var simSubtitle = new HBox(false, 7) {Name = "simSubtitle"};
      simSubtitle.PackStart(simSubtitleIcon, false, false, 0);
      simSubtitle.PackStart(new Label("Simulation") 
      {
        Xalign = 0
      }, false, true, 0);
      
      result.PackStart(simSubtitle, false, false, 0);
      
      var delayEntry = new Entry("" + _simulator.SimDelay)
      {
        PlaceholderText = "" + _simulator.SimDelay,
        HasFrame = false
      };
      delayEntry.Activated += (s, a) =>
      {
        Console.WriteLine("Enter pressed");
        if (int.TryParse(delayEntry.Text, out var value) && value >= 0)
        {
          _simulator.SimDelay = value;
        }
      };
      var delayControl = new HBox(false, 0)
      {
        HeightRequest = 10,
        Name = "delayControl"
      };
      delayControl.PackStart(new Label("Delay = "), false, false, 0);
      delayControl.PackStart(delayEntry, true, true, 0);
      result.PackStart(delayControl, false, false, 0);
      
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
        new Label(_simulator.Sim.GetTitle()) {Name = "title", Xalign = 0},
        new Label
        {
          Text = _simulator.Sim.GetDescr(),
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
        _simulator.Load(pathEntry.Text);
        try
        {
          ((Label) _title.Children[0]).Text = _simulator.Sim.GetTitle();
          ((Label) _title.Children[1]).Text = _simulator.Sim.GetDescr();
          _configBuffer.Text = _simulator.Sim.GetConfig();
        }
        catch (Exception e) { Logger.Warn(e.ToString()); }
        _simulator.Init(
          _configBuffer.Text, 
          _canvas.AllocatedWidth, 
          _canvas.AllocatedHeight);
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
        _simulator.Stop();
        startBtn.Label = "Start";
        startBtn.Clicked -= Stop;
        startBtn.Clicked += Start;
      }

      void Start(object sender, EventArgs args)
      {
        _simulator.Start();
        startBtn.Label = "Stop ";
        startBtn.Clicked -= Start;
        startBtn.Clicked += Stop;
      }
      
      var initBtn = new Button("Init");
      initBtn.Clicked += (sender, args) =>
      {
        if (_simulator.Started) Stop(null, null);
        var timer = new Timer(20);
        timer.Elapsed += (o, eventArgs) =>
        {
          if (_simulator.Running)
          {
            Logger.Say("Waiting for update thread to finish.");
            return;
          }

          _simulator.Init(
            _configBuffer.Text, 
            _canvas.AllocatedWidth, 
            _canvas.AllocatedHeight);
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
        if (_simulator.Running) return;
        if (int.TryParse(stepsEntry.Text, out var x)) _simulator.Start(x);
        else Logger.Warn("Please enter a number >= 0.");
      };
      var runSteps = new HBox(false, 0) {stepsEntry, stepsBtn};
      runSteps.Name = "runSteps";

      var pictureBtn = new Button("P")
      {
        TooltipText = "Save the current render output as PNG",
        Name = "pictureBtn"
      };
      pictureBtn.Clicked += (s, a) => _simulator.SaveImage();

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

        if (_simulator.RenderData == null) return;

        var surface = new ImageSurface(_simulator.RenderData, Format.ARGB32,
          _canvas.AllocatedWidth,
          _canvas.AllocatedHeight,
          4 * _canvas.AllocatedWidth);
        args.Cr.SetSourceSurface(surface, 0, 0);
        args.Cr.Paint();
        surface.Dispose();
      };

      _iterationLbl = new Label("i = " + _simulator.Iteration)
      {
        Halign = Align.Start,
        TooltipText = "Iterations, Elapsed time, Average elapsed time"
      };

      var result = new VBox(false, 7) {renderTitle, _canvas, _iterationLbl};
      return result;
    }

    private Box CreateConfig()
    {
      _configBuffer = new TextBuffer(new TextTagTable())
      {
        Text = _simulator.Sim.GetConfig()
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