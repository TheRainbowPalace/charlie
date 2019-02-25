using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Timers;
using Gdk;
using Gtk;
using Application = Gtk.Application;
using Thread = System.Threading.Thread;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;


namespace run_charlie
{
  internal class Loader : MarshalByRefObject 
  {
    public object LoadAssembly(string path, string className)
    {
      var assembly = Assembly.Load(AssemblyName.GetAssemblyName(path));
      var type = assembly.GetType(className);
      return Activator.CreateInstance(type);
//      Console.WriteLine(a.GetType());
//      var methodInfo = classType.GetMethod("Init");
//      methodInfo.Invoke(a, new object[]{null});
    }
  }
  
  /// <summary> RunCharlie is a general simulation framework. </summary>
  public class RunCharlie
  {
    public bool Started;
    public int Iteration;
    
    private ISimulation _sim;
    private Thread _logicThread;
    private AppDomain _appDomain;
    private DrawingArea _canvas;
    private Label _iterationLbl;
    private Box _title;
    private TextBuffer _configBuffer;

    public RunCharlie()
    {
      _sim = new DefaultSimulation();
      
      SetupStyle();

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
      window.SetIconFromFile(
        AppDomain.CurrentDomain.BaseDirectory + "/logo.png");
      window.ShowAll();
      
      Init();
    }

    // Todo: Implement this method
    private void LoadModule(string path, string className)
    {
      if (_logicThread != null)
      {
        Console.WriteLine("Please terminate simulation first.");
        return;
      }
      if (_appDomain != null) AppDomain.Unload(_appDomain);
      
//      var ads = new AppDomainSetup
//      {
//        ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
//        DisallowBindingRedirects = false,
//        DisallowCodeDownload = true,
//        ConfigurationFile =
//          AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
//      };
//      _appDomain = AppDomain.CreateDomain("Test", null, ads);
//      var loader = (Loader) _appDomain.CreateInstanceAndUnwrap( 
//        typeof(Loader).Assembly.FullName, typeof(Loader).FullName);
      
//      var obj = loader.LoadAssembly(path, "SineExample");
//      var methodInfo = obj.GetType().GetMethod("Init");
//      methodInfo.Invoke(obj, new object[]{null});
//      Console.WriteLine(methodInfo);
      
      var setup = new AppDomainSetup 
      {
        ApplicationName = path, 
        ConfigurationFile = 
          AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
        ApplicationBase = AppDomain.CurrentDomain.BaseDirectory 
      };
      var appDomain = AppDomain.CreateDomain(
        setup.ApplicationName,
        AppDomain.CurrentDomain.Evidence, 
        setup);
      var x = appDomain.CreateInstanceAndUnwrap(path, "SineExample") as ISimulation;
//      x.Init(new Dictionary<string, string>());

//      ((Label) _title.Children[0]).Text = _sim.Title;
//      ((Label) _title.Children[1]).Text = _sim.Descr;
//      Init();
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
      Iteration = 0;
      AfterUpdate();
    }

    private void Start()
    {
      Started = true;
      _logicThread = new Thread(Update);
      _logicThread.Start();
    }
    
    private void Stop()
    {
      Started = false;
//      _sim.LogicThread?.Abort();
    }
    
    private void Update()
    {
      long deltaTime = 0;
      while (Started)
      {
        var timer = Stopwatch.StartNew();
        try
        {
          _sim.Update(deltaTime);
        }
        catch (Exception e) { Console.WriteLine(e); }

        Iteration++;
        Application.Invoke((sender, args) => AfterUpdate());
        Thread.Sleep(30);
        timer.Stop();
        deltaTime = timer.ElapsedMilliseconds;
      }
      _logicThread = null;
    }

    private void AfterUpdate()
    {
      _canvas.QueueDraw();
      _iterationLbl.Text = "i = " + Iteration;
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
        if (Started) Stop(null, null);
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

      _iterationLbl = new Label("i = " + Iteration) {Halign = Align.End};

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