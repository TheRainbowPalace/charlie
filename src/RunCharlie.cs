using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Cairo;
using Gdk;
using Geometry;
using GLib;
using Gtk;
using Application = Gtk.Application;
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
    
    public override void Render(Context cr)
    {
      cr.SetSourceRGB(0.769, 0.282, 0.295);
      cr.Arc(X, Y, Radius, 0, 2 * Math.PI);
      cr.ClosePath();
      cr.Fill();
    }

    public void Update(double deltaTime)
    {
      Radius++;
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


  /// <summary>
  /// 
  /// </summary>
  internal class SimulationWidget : DrawingArea
  {
    private readonly List<RenderObject> _renderObjects =
      new List<RenderObject>();

    private readonly ConcurrentDictionary<string, GameObject> _logicObjects =
      new ConcurrentDictionary<string, GameObject>();

    public double MinObstSize = 5;
    public double MaxObstSize = 40;
    public int ObstCount = 5;
    public int EnemyCount = 3;
    public double PlayerSize = 10;
    public double EnemySize = 8;
    
    public int FieldWidth = 400;
    public int FieldHeight = 400;
    private bool _started;
    private Thread _logicThread;

    /// <summary> Initialize the simulation </summary>
    public void Init()
    {
      if (_started) Stop();
      
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
      
      QueueDraw();
    }

    private void Update()
    {
      while (_started)
      {
        foreach (var key in _logicObjects.Keys)
        {
          if (_logicObjects.TryGetValue(key, out var obj))
          {
            obj.Update(1);
          }
        }
        QueueDraw();
        Thread.Sleep(50);
      }
    }
    
    /// <summary> Start the simulation </summary>
    public void Start()
    {
      _started = true;
      _logicThread = new Thread(Update);
      _logicThread.Start();
    }

    /// <summary> Stop the simulation </summary>
    public void Stop()
    {
      _started = false;
      _logicThread?.Abort();
    }

    protected override bool OnDrawn(Context cr)
    {
      foreach (var element in _renderObjects) element.Render(cr);
      return true;
    }
  }

  /// <summary> RunCharlie is a simulation framework. </summary>
  public class RunCharlie : Application
  {
    public string Title = "";
    public string Description = "";
    public Action<object> Init = config => {};
    public Action<double> Update = dt => {}; 
    public Action<Context> Render = ct => {}; 
    public Action<object> Log = fw => {};
    public ConcurrentDictionary<string, object> Objects = 
      new ConcurrentDictionary<string, object>();
    
    private Button _actionButton;
    private SimulationWidget _simulationWidget;
    
    public RunCharlie() : base(
      "com.jakobrieke.runcharlierun",
      ApplicationFlags.None)
    {
      SetupStyle();

      var mainPage = CreateMain();
      var configPage = CreateConfig();

      var title = new Label("Run Charlie") {Name = "title", Xalign = 0};
      _actionButton = new Button("Start") {Name = "actionBtn"};

      void Stop(object sender, EventArgs args)
      {
        _simulationWidget.Stop();
        Console.WriteLine("Stop");
        _actionButton.Label = "Start";
        _actionButton.Clicked -= Stop;
        _actionButton.Clicked += Start;
      }

      void Start(object sender, EventArgs args)
      {
        _simulationWidget.Start();
        Console.WriteLine("Start");
        _actionButton.Label = "Stop";
        _actionButton.Clicked -= Start;
        _actionButton.Clicked += Stop;
      }
      
      _actionButton.Clicked += Start;

      
      var page = new VBox (false, 0) {HeightRequest = 500};
      page.Add(mainPage);
      page.Add(configPage);
      
      var root = new VBox (false, 10) {Margin = 10, Name = "root"};
      root.Add(title);
      root.Add(page);
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

    private Box CreateMain()
    {
      _simulationWidget = new SimulationWidget();
      _simulationWidget.SetSizeRequest(400, 400);
      _simulationWidget.Init();

      var configTitle = new Label("Configuration")
      {
        Xalign = 0, Valign = Align.Start
      };

      var mainPage = new VBox (false, 10);
      mainPage.PackStart(new Alignment(0, 1, 0, 0), false, false, 0);
      mainPage.PackStart(_simulationWidget, false, false, 0);
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
      return null;
    }
  }
}