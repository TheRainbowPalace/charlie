using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using Cairo;
using Gdk;
using Geometry;
using Gtk;
using Application = Gtk.Application;
using Color = Gdk.Color;
using Key = Gtk.Key;
using Thread = System.Threading.Thread;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;


namespace sensor_positioning
{
  internal abstract class RenderObject
  {
    public double X;
    public double Y;
    public abstract void Render(Context cr);
  }


  internal interface GameObject
  {
    void Update(double deltaTime);
  }

  /// <summary>
  /// The main character of the game.
  /// </summary>
  internal class Charlie : RenderObject, GameObject
  {
    public double Radius = 10;
    private bool _grow = true;
    
    public override void Render(Context cr)
    {
      cr.SetSourceRGB(0.769, 0.282, 0.295);
      cr.Arc(X, Y, Radius, 0, 2 * Math.PI);
      cr.ClosePath();
      cr.Fill();
    }

    public void Update(double deltaTime)
    {
      _grow = Radius > 100 || Radius < 10 ? !_grow : _grow;
      if (_grow) Radius++;
      else Radius--;
    }
  }


  /// <summary>
  /// The antagonists.
  /// </summary>
  internal class Enemy : RenderObject
  {
    public double Radius = 10;
    
    public override void Render(Context cr)
    {
      cr.SetSourceRGB(0.005, 1.0, 0.85);
      cr.Arc(X, Y, Radius / 2, 0, 2 * Math.PI);
      cr.ClosePath();
      cr.Fill();
      cr.Arc(X, Y, Radius, 0, 2 * Math.PI);
      cr.ClosePath();
      cr.LineWidth = 1;
      cr.Stroke();
//      cr.SetSourceRGBA(0.005, 1.0, 0.85, 0.2);
    }
    
    private void DrawPolygon(Context cr, Polygon p)
    {
      if (p.Count == 0) return;

      cr.SetSourceRGBA(0, 0, 0, 0.7);
      cr.NewPath();
      cr.MoveTo(p[0].X,
        p[0].Y);

      for (var i = 1; i < p.Count; i++)
      {
        cr.LineTo(p[i].X,
          p[i].Y);
      }

      if (p[0] != p[p.Count - 1])
      {
        cr.MoveTo(p[0].X,
          p[0].Y);
      }

      cr.ClosePath();
      cr.Fill();
    }

    private void DrawSegment(Context cr, Segment s)
    {
      cr.SetSourceRGBA(1, 1, 1, 0.5);
      cr.NewPath();
      cr.MoveTo(s.Start.X, s.Start.Y);
      cr.LineTo(s.End.X, s.End.Y);
      cr.ClosePath();
      cr.Stroke();
    }
  }

  /// <summary>
  /// A plain area to walk on.
  /// </summary>
  internal class Field : RenderObject
  {
    public double Width;
    public double Height;

    public Field(double width, double height)
    {
      Width = width;
      Height = height;
    }

    public override void Render(Context cr)
    {
      cr.Rectangle(X, Y, Width, Height);
      cr.LineWidth = 3;
      cr.SetSourceRGB(0.2, 0.2, 0.2);
      cr.FillPreserve();
      cr.SetSourceRGB(0.721, 0.722, 0.721);
      cr.Stroke();
    }
  }

  /// <summary>
  /// Something to collide with.d
  /// </summary>
  internal class Obstacle : RenderObject
  {
    public double Radius = 10;
    
    public override void Render(Context cr)
    {
      cr.SetSourceRGB(0.721, 0.722, 0.721);
      cr.Arc(X, Y, Radius, 0, 2 * Math.PI);
      cr.ClosePath();
      cr.LineWidth = 2;
      cr.Stroke();
    }
  }


  internal class Deprecated
  {
    private readonly List<RenderObject> _renderObjects =
      new List<RenderObject>();

    private readonly ConcurrentDictionary<string, GameObject> _logicObjects =
      new ConcurrentDictionary<string, GameObject>();

    public int FieldWidth = 400;
    public int FieldHeight = 400;
    public double MinObstSize = 5;
    public double MaxObstSize = 40;
    public int ObstCount = 5;
    public int EnemyCount = 3;
    public double PlayerSize = 10;
    public double EnemySize = 8;
    
    /// <summary> Initialize the simulation </summary>
    public void Init()
    {
      _renderObjects.Clear();
      
      // Create field
      _renderObjects.Add(new Field(FieldWidth, FieldHeight));
      
      // Create obstacles
      for (var i = 0; i < ObstCount; i++)
      {
        var o = new Obstacle
        {
          X = new PcgRandom().GetDouble(0, FieldWidth),
          Y = new PcgRandom().GetDouble(0, FieldHeight),
          Radius = new PcgRandom().GetDouble(MinObstSize, MaxObstSize)
        };
        _renderObjects.Add(o);
      }
      
      // Create enemies
      for (var i = 0; i < EnemyCount; i++)
      {
        var e = new Enemy
        {
          X = new PcgRandom().GetDouble(0, FieldWidth),
          Y = new PcgRandom().GetDouble(0, FieldHeight),
          Radius = EnemySize
        };
        _renderObjects.Add(e);
      }
      
      // Create Charlie
      var charlie = new Charlie
      {
        X = FieldWidth / 2f, 
        Y = FieldHeight / 2f, 
        Radius = PlayerSize
      };
      _renderObjects.Add(charlie);
      _logicObjects.TryAdd("character", charlie);
    }
  }

  public class Simulation
  {
    public readonly ConcurrentDictionary<string, object> Objects = 
      new ConcurrentDictionary<string, object>();
    public Action<object> Init = config => {};
    public Action<long> Update = dt => {};
    public Action<Context> Render = ct => {};
    public Action<object> Log = fw => {};
    public string Title = "";
    public string Description = "";
    public bool Started;
    public int Iteration = 0;
  }
  
  /// <summary> RunCharlie is a simulation framework. </summary>
  public class RunCharlie
  {
    private Simulation _sim;
    private DrawingArea _canvas;
    private Label _iterationLbl;
    private Thread _logicThread;
    private TextBuffer _configBuffer;
    
    public RunCharlie(Simulation sim)
    {
      _sim = sim;
      
      SetupStyle();

      var title = new VBox(false, 5)
      {
        new Label("Single dot") {Name = "title", Xalign = 0},
        new Label
        {
          Text = 
            "Single dot is an example simulation to preview and test the " +
            "RunCharlie software. It presents you with a single red dot that " +
            "increases and decreases in size.",
          Wrap = true, 
          Halign = Align.Start, 
          Xalign = 0
        }
      };
      title.MarginTop = 15;
      var root = new VBox (false, 15)
      {
        Name = "root",
        MarginStart = 20, 
        MarginEnd = 20
      };
      root.PackStart(title, false, false, 0);
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
        Child = new ScrolledWindow 
        {
          KineticScrolling = true,
          VscrollbarPolicy = PolicyType.External,
          MinContentHeight = 600,
          MaxContentWidth = 400,
          Child = root
        }
      };
      window.Destroyed += (sender, args) => Application.Quit();
      window.Move(100, 100);
      window.ShowAll();
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
      var pathEntry = new Entry("/home")
      {
        PlaceholderText = "/path/to/your/module/..."
      };
      var loadBtn = new Button("Load");
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
  font-family: Andale Mono;
}

#title {
  font-size: xx-large;
  font-weight: 100;
}

label {
  color: #C4C4C4;
  font-family: Andale Mono, Monospace;
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
  padding-left: 15px;
}
entry:selected {outline: none;}

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
       ");
      StyleContext.AddProviderForScreen(Screen.Default, provider, 800);
    }
    
    public static void Example()
    {
      var sim = new Simulation();
      sim.Init = o =>
      {
        sim.Objects.Clear();
        sim.Objects.TryAdd("charlie", new Charlie {X = 200, Y = 200});
        sim.Objects.TryAdd("field", new Field(400, 400));
      };
      sim.Render = ctx =>
      {
        if (sim.Objects.TryGetValue("field", out var obj2))
          ((RenderObject) obj2).Render(ctx);
        if (sim.Objects.TryGetValue("charlie", out var obj1))
          ((RenderObject) obj1).Render(ctx);
      };
      sim.Update = delta =>
      {
        if (sim.Objects.TryGetValue("charlie", out var obj))
        {
          ((Charlie) obj).Update(delta);
        }
        Thread.Sleep(20);
      };
      var app = new RunCharlie(sim);
    }
  }
}