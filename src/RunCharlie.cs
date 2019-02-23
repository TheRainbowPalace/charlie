using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Timers;
using Cairo;
using Gdk;
using Gtk;
using Application = Gtk.Application;
using Thread = System.Threading.Thread;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;


namespace run_charlie
{
  public interface ISimulation
  {
    string GetTitle();
    string GetDescr();
    string GetConfig();
    void Init(Dictionary<string, string> config);
    void Update(long deltaTime);
    void Render(Context ctx);
    void Log();
  }
  
  public class Simulation
  {
    public Action<Dictionary<string, string>> Init = config => {};
    public Action<long> Update = dt => {};
    public Action<Context> Render = ct => {};
    public Action<object> Log = fw => {};
    public string Title = "Run Charlie";

    public string Descr = "RunCharlie is multi purpose simulation app. It " +
                          "tries not to apply to many rules on how the " +
                          "simulation is run and structured.";
    public bool Started;
    public int Iteration;
  }
  
  internal class Loader : MarshalByRefObject 
  {
    private Assembly _assembly;

    public void LoadAssembly(string path)
    {
      _assembly = Assembly.Load(AssemblyName.GetAssemblyName(path));
    }

    public string GetStaticField(string typeName, string fieldName)
    {
      return _assembly.GetType(typeName).GetField(fieldName,
        BindingFlags.Public | BindingFlags.Static)?.GetValue(null).ToString();
    }

    public void RunStaticMethod(string typeName, 
      string methodName, 
      object[] parameters)
    {
      _assembly.GetType(typeName)?
        .GetMethod(methodName, BindingFlags.Static | BindingFlags.Public)?
        .Invoke(null, parameters);
    }
  }
  
  /// <summary> RunCharlie is a simulation framework. </summary>
  public class RunCharlie
  {
    private Simulation _sim;
    private Thread _logicThread;
    private AppDomain _appDomain;
    private DrawingArea _canvas;
    private Label _iterationLbl;
    private Box _title;
    private TextBuffer _configBuffer;

    public RunCharlie()
    {
      _sim = new Simulation();
      
      SetupStyle();

      _title = new VBox(false, 5)
      {
        new Label(_sim.Title) {Name = "title", Xalign = 0},
        new Label
        {
          Text = _sim.Descr,
          Wrap = true, 
          Halign = Align.Start, 
          Xalign = 0
        }
      };
      _title.MarginTop = 15;
      
      var root = new VBox (false, 20)
      {
        Name = "root",
        MarginStart = 20, 
        MarginEnd = 20
      };
      root.PackStart(_title, false, false, 0);
      root.PackStart(CreateModuleControl(), false, false, 0);
      root.PackStart(CreateCanvas(), false, false, 0);
      root.PackStart(CreateControls(), false, false, 0);
      root.PackStart(CreateConfig(), true, true, 0);
      
      Init();

      var window = new Window(WindowType.Toplevel)
      {
        WidthRequest = 440,
        Title = "",
        Role = "runcharlie",
        Resizable = false,
        FocusOnMap = true,
        Child = new ScrolledWindow
        {
          OverlayScrolling = false,
          KineticScrolling = true,
          VscrollbarPolicy = PolicyType.External,
          MinContentHeight = 600,
          MaxContentWidth = 400,
          Child = root
        }
      };
      window.Destroyed += (sender, args) =>
      {
        Application.Quit();
        Stop();
      };
      window.Move(100, 100);
      window.ShowAll();
    }

    private void LoadModule(string path, string className)
    {
      if (_logicThread != null)
      {
        Console.WriteLine("Please terminate simulation first.");
        return;
      }
      if (_appDomain != null) AppDomain.Unload(_appDomain);
      
      var ads = new AppDomainSetup
      {
        ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
        DisallowBindingRedirects = false,
        DisallowCodeDownload = true,
        ConfigurationFile =
          AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
      };
      _appDomain = AppDomain.CreateDomain("Test", null, ads);
      var loader = (Loader) _appDomain.CreateInstanceAndUnwrap( 
        typeof(Loader).Assembly.FullName, typeof(Loader).FullName);
      
      loader.LoadAssembly(path);
      
      _sim = new Simulation
      {
        Title = loader.GetStaticField(className, "Title"),
        Descr = loader.GetStaticField(className, "Descr"),
        Init = config => loader.RunStaticMethod(
          className, "Init", new object[]{config}),
        Update = dt => loader.RunStaticMethod(
          className, "Update", new object[]{dt}),
//        Render = ctx => loader.RunStaticMethod(
//          className, "Render", new object[]{ctx})
      };
      ((Label) _title.Children[0]).Text = _sim.Title;
      ((Label) _title.Children[1]).Text = _sim.Descr;
      Init();
    }
    
    // Todo: Implement method
    private void ParseConfig(string config)
    {
      var lines = config.Split(
        new[] {System.Environment.NewLine},
        StringSplitOptions.None);
      var i = 0;
      foreach (var line in lines)
      {
        i++;
        if (line.StartsWith("#")) Console.WriteLine("Comment: " + i);
//        else
//        {
//          var index = line.IndexOf('=');
//          if (index < 0) Console.WriteLine("Invalid line: " + i);
//          else Console.WriteLine(
//            "Key: " + line.Substring(0, index) + 
//            ", Value: " + line.Substring(index + 1, line.Length - index + 1));
//        }
      }
    }
    
    private void Init()
    {
      // parse config buffer here
//      ParseConfig(_configBuffer.Text);
      
      _sim.Init(null);
      _sim.Iteration = 0;
      AfterUpdate();
    }

    private void Start()
    {
      _sim.Started = true;
      _logicThread = new Thread(Update);
      _logicThread.Start();
    }
    
    private void Stop()
    {
      _sim.Started = false;
//      _sim.LogicThread?.Abort();
    }
    
    private void Update()
    {
      var timer = new Stopwatch();
      timer.Start();
      while (_sim.Started)
      {
        try
        {
          _sim.Update(timer.ElapsedMilliseconds);
          timer.Restart();
        }
        catch (Exception e) { Console.WriteLine(e); }

        _sim.Iteration++;
        Application.Invoke((sender, args) => AfterUpdate());
      }
      timer.Stop();
      _logicThread = null;
    }

    private void AfterUpdate()
    {
      _canvas.QueueDraw();
      _iterationLbl.Text = "i = " + _sim.Iteration;
    }

    private Box CreateModuleControl()
    {
      var pathEntry = new Entry(
        "/Users/littlebrother/Projects/4-Uni/bachelor/" +
        "sensor-positioning/bin/SineExample.dll")
      {
        PlaceholderText = "/path/to/your/module/...", 
        HasFocus = false
      };
      
      var loadBtn = new Button("Load");
      loadBtn.Clicked += (sender, args) =>
      {
        try {LoadModule(pathEntry.Text, "SineExample");}
        catch (Exception e) {Console.WriteLine(e);}
      };
      
      var result = new HBox(false, 15);
      result.PackStart(pathEntry, true, true, 0);
      result.PackStart(loadBtn, false, false, 0);
      return result;
    }
    
    private Box CreateControls()
    {
      var startBtn = new Button("Start");
      startBtn.Clicked += Start;

      void Stop(object sender, EventArgs args)
      {
        Console.WriteLine("Stop");
        
        this.Stop();
        startBtn.Label = "Start";
        startBtn.Clicked -= Stop;
        startBtn.Clicked += Start;
      }

      void Start(object sender, EventArgs args)
      {
        Console.WriteLine("Start");
        
        this.Start();
        startBtn.Label = "Stop";
        startBtn.Clicked -= Start;
        startBtn.Clicked += Stop;
      }
      
      var initBtn = new Button("Init");
      initBtn.Clicked += (sender, args) =>
      {
        if (_sim.Started) Stop(null, null);
        var t = new Timer(20);
        t.Elapsed += (o, eventArgs) =>
        {
          if (_logicThread != null)
          {
            Console.WriteLine("Waiting for update thread to finish.");
            return;
          }
          Init();
          t.Enabled = false;
        };
        t.Enabled = true;
      };

      _iterationLbl = new Label("i = " + _sim.Iteration) {Halign = Align.End};

      var result = new HBox(false, 10);
      result.PackStart(initBtn, false, false, 0);
      result.PackStart(startBtn, false, false, 0);
      result.PackStart(_iterationLbl, true, true, 0);
      result.HeightRequest = 10;
      return result;
    }
    
    private Box CreateCanvas()
    {
      var renderTitle = new Label("Rendering") {Halign = Align.Start};
      
      _canvas = new DrawingArea {Name = "canvas"};
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
        
        try { _sim.Render(args.Cr); }
        catch (Exception e) { Console.WriteLine(e); }
      };
      _canvas.SetSizeRequest(400, 400);

      var result = new VBox(false, 7);
      result.PackStart(renderTitle, false, false, 0);
      result.PackStart(_canvas, false, false, 0);
      return result;
    }

    private Box CreateConfig()
    {
      _configBuffer = new TextBuffer(new TextTagTable())
      {
        Text = "charlieSize: 12" +
               "\nminObstSize: 10" +
               "\nmaxObstSize: 50" +
               "\nsizeIncrease: 1"
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
//      textView.KeyPressEvent += (o, args) =>
//      {
//        Console.WriteLine(args.Event.Key);
//        if (args.Event.Key == Gdk.Key.rightarrow &&
//            args.Event.State == ModifierType.MetaMask)
//        {
//          Console.WriteLine("go to right");
//        }
//      };
      var result = new VBox(false, 7);
      result.PackStart(title, false, false, 0);
      result.PackStart(textView, true, true, 0);
      return result;
    }

    private void SetupStyle()
    {
      var provider = new CssProvider();
      provider.LoadFromData(@"
window {
  background-color: #333333;
  font-family: Andale Mono, Monospace;
}

#title {
  font-size: xx-large;
  font-weight: 100;
}

label {
  color: #C4C4C4;
}

button {
  font-size: 22px;
  color: #939797;
  background: #010101;
  padding: 2px 20px;
  border: none;
  text-shadow: none;
  box-shadow: none;
  border-radius: 30px;
}
button:hover {background-color: #1A1B1B;}
button:active {background-color: #595959;}
button:disabled {border: none;}

entry {
  background: #010101;
  color: #939797;
  caret-color: white;
  border: none;
  border-radius: 0;
  padding: 2px 15px;
  outline: none;
}

textview {
  background: #C4C4C4;
  padding: 10px 5px 10px 5px;
  caret-color: black;
}
textview text {
  background: transparent;
  color: #1A1B1B;
}
textview text selection {
  background: #C4484B;
}

scrolledwindow undershoot, scrolledwindow overshoot {
  background-image: none;
  background: none;
}
       ");
      StyleContext.AddProviderForScreen(Screen.Default, provider, 800);
    }
  }
}