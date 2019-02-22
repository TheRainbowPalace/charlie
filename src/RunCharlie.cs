using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Cairo;
using Gdk;
using Geometry;
using GLib;
using Gtk;
using Application = Gtk.Application;
using Task = System.Threading.Tasks.Task;
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
    public Thread LogicThread;
    public bool Started;
    public string Title = "";
    public string Description = "";
  }
  
  /// <summary> RunCharlie is a simulation framework. </summary>
  public class RunCharlie : Application
  {
    private const string AppId = "com.jakobrieke.runcharlie";
    private Simulation _sim;
    private DrawingArea _canvas;
    private Button _actionButton;
    
    public RunCharlie(Simulation sim) : base(AppId, ApplicationFlags.None)
    {
      _sim = sim;
      
      SetupStyle();

      var mainPage = CreateMain();
      var configPage = CreateConfig();

      Init();
      
      var title = new Label("Run Charlie") {Name = "title", Xalign = 0};
      _actionButton = new Button("Start") {Name = "actionBtn"};

      void Stop(object sender, EventArgs args)
      {
        Console.WriteLine("Stop");
        
        this.Stop();
        _actionButton.Label = "Start";
        _actionButton.Clicked -= Stop;
        _actionButton.Clicked += Start;
      }

      void Start(object sender, EventArgs args)
      {
        Console.WriteLine("Start");
        
        this.Start();
        _actionButton.Label = "Stop";
        _actionButton.Clicked -= Start;
        _actionButton.Clicked += Stop;
      }
      
      _actionButton.Clicked += Start;
      
//      var page = new VBox (false, 0) {HeightRequest = 500};
//      page.Add(mainPage);
//      page.Add(configPage);
      
      var root = new VBox (false, 10) {Margin = 10, Name = "root"};
      root.Add(title);
      root.Add(mainPage);
      root.Add(configPage);
      root.Add(_actionButton);

      var window = new Window(WindowType.Toplevel)
      {
        WindowPosition = WindowPosition.Center,
        WidthRequest = 420,
        Title = ""
      };
      window.Add(root);
      window.KeyPressEvent += (o, args) => Console.WriteLine(args.Event.Key);
      window.Resizable = false;
      window.ShowAll();
    }
    
    private new void Init()
    {
      if (_sim.Started) Stop();
      // parse JSON config here
      _sim.Init(null);
      _canvas.QueueDraw();
    }

    private void Start()
    {
      _sim.Started = true;
      _sim.LogicThread = new Thread(Update);
      _sim.LogicThread.Start();
    }
    
    private void Stop()
    {
      _sim.Started = false;
      _sim.LogicThread?.Abort();
    }
    
    private void Update()
    {
      var timer = new Stopwatch();
      timer.Start();
      while (_sim.Started)
      {
        Console.WriteLine(timer.ElapsedMilliseconds);

        try
        {
          _sim.Update(timer.ElapsedMilliseconds);
          timer.Restart();
        }
        catch (Exception e) { Console.WriteLine(e); }

        Task.Factory.StartNew(() => _canvas.QueueDraw());
        Thread.Sleep(50);
      }
      timer.Stop();
    }
    
    private Box CreateMain()
    {
      _canvas = new DrawingArea();
      _canvas.Drawn += (o, args) =>
      {
        try { _sim.Render(args.Cr); }
        catch (Exception e) { Console.WriteLine(e); }
      };
      _canvas.SetSizeRequest(400, 400);
      
      var configTitle = new Label("Configuration")
      {
        Xalign = 0, Valign = Align.Start
      };

      var mainPage = new VBox (false, 10);
      mainPage.PackStart(new Alignment(0, 1, 0, 0), false, false, 0);
      mainPage.PackStart(_canvas, false, false, 0);
      mainPage.PackStart(configTitle, false, false, 0);

      return mainPage;
    }

    private Box CreateConfig()
    {
      // Todo: Fix cursor not visible (find css property for cursor color) 
      
      var buffer = new TextBuffer(new TextTagTable())
      {
        Text = "{\n  \"MinObstSize\": 10\n}"
      };
      var entry = new TextView(buffer)
      {
        HeightRequest = 400, WidthRequest = 400, Name = "configEntry"
      };
      entry.Indent = 3;
      var scrollCont = new ScrolledWindow {entry};

      var configPage = new VBox(false, 10) {scrollCont};
      return configPage;
    }

    private void SetupStyle()
    {
      var provider = new CssProvider();
      provider.LoadFromData(@"
        window {background-color: #333333;}
        #title {
          font-size: xx-large;
          font-weight: 100;
        }
        label {
          color: #C4C4C4;
          font-family: Andale Mono, Monospace;
        }
        #actionBtn {
          font-size: 22px;
          color: #939797;
          background: #010101;
          padding: 5px 5px;
          border: none;
          text-shadow: none;
          box-shadow: none;
          border-radius: 10px;
        }
        #actionBtn:hover {background-color: #1A1B1B;}
        #actionBtn:active {background-color: #595959;}
        #actionBtn:disabled {border: none;}
        
        textview {
          background: transparent;
        }
        
        textview text {
          background: transparent;
          color: #C4C4C4;
        }
        textview text selection {
          background: #C4484B;
        }
       ");
      StyleContext.AddProviderForScreen(Screen.Default, provider, 800);
    }
    
    public static RunCharlie Example()
    {
      var sim = new Simulation();
      sim.Init = o =>
      {
        var charlie = new Charlie {X = 200, Y = 200};
        sim.Objects.Clear();
        sim.Objects.TryAdd("charlie", charlie);
      };
      sim.Render = ctx =>
      {
        if (sim.Objects.TryGetValue("charlie", out var obj))
        {
          ((Charlie) obj).Render(ctx);
        }
      };
      sim.Update = delta =>
      {
        if (sim.Objects.TryGetValue("charlie", out var obj))
        {
          ((Charlie) obj).Update(delta);
        }
      };
      var app = new RunCharlie(sim);
      return app;
    }
  }
}