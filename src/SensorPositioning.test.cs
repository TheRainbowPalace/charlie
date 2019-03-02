using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cairo;
using Gdk;
using Geometry;
using GLib;
using LibOptimization.Optimization;
using LibOptimization.Util;
using Optimization;
using Gtk;
using Action = System.Action;
using Application = Gtk.Application;
using Key = Gdk.Key;
using Rectangle = Geometry.Rectangle;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;


namespace sensor_positioning
{
  internal class SSPPlotter : DrawingArea
  {
    public int SizeTeamA = 1;
    public int SizeTeamB = 1;
    public double FieldWidth = 9;
    public double FieldHeight = 6;
    public double PlayerSensorRange = 12;
    public double PlayerSensorFov = 56.3;
    public double PlayerSize = 0.1555;

    public double OptValue = 5;
    public int OptSteps = 100;
    public string Optimization = "SPSO 2011";
    public Dictionary<string, Func<SspFct, absOptimization>> Optimizations =
      new Dictionary<string, Func<SspFct, absOptimization>>
      {
        {"Cuckoo Search", obj => new clsOptCS(obj)},
        {"Hill Climbing", obj => new clsOptHillClimbing(obj)},
        {"Simulated Annealing", obj => new clsOptSimulatedAnnealing(obj)},
        {"Pattern Search", obj => new clsOptPatternSearch(obj)},
        {"Simplex", obj => new clsOptNelderMead(obj)},
        {"Simplex Wikipedia", obj => new clsOptNelderMeadWiki(obj)},
        {"SPSO 2006", obj => new SPSO2006(obj)},
        {"SPSO 2011", obj => new SPSO2011(obj)},
        {"PSO", obj => new clsOptPSO(obj)
          {
            InitialPosition = obj.Raw.SearchSpace().RandPos()
          }
        },
        {"PSO A IW", obj => new clsOptPSOAIW(obj)},
        {"PSO Chaotic IW", obj => new clsOptPSOChaoticIW(obj)},
        {"PSO LD IW", obj => new clsOptPSOLDIW(obj)},
//        {"Firefly", obj => new clsOptFA(obj)},
        {"Evolution Strategy", obj => new clsOptES(obj)}, // no criterion
        {"Differential Evolution", obj => new clsOptDE(obj)},
        {"Adaptive Differential Evolution", obj => new clsOptDEJADE(obj)
          {
            LowerBounds = obj.Raw.Intervals().Select(i => i[0]).ToArray(),
            UpperBounds = obj.Raw.Intervals().Select(i => i[1]).ToArray()
          }
        },
        {"Real Coded GA BLX-Alpha + JGG", obj => new clsOptRealGABLX(obj)},
        {"Real Coded GA PCX", obj => new clsOptRealGAPCX(obj)},
        {"Real Coded GA REX", obj => new clsOptRealGAREX(obj)},
        {"Real Coded GA SPX + JGG", obj => new clsOptRealGASPX(obj)
          {
            LowerBounds = obj.Raw.Intervals().Select(i => i[0]).ToArray(),
            UpperBounds = obj.Raw.Intervals().Select(i => i[1]).ToArray()
          }
        },
        {"Real Coded GA UNDX", obj => new clsOptRealGAUNDX(obj)},
      };

    private SspFct _objective;
    private const string DefaultOpt = "Adaptive Differential Evolution";
    private absOptimization _optimizer;
    private double _zoom = 50;

    public SSPPlotter()
    {
      Init();
      HeightRequest = (int) (_objective.Raw.Env.Bounds.Max.Y * _zoom);
      WidthRequest = (int) (_objective.Raw.Env.Bounds.Max.X * _zoom);
    }

    public void Init()
    {
      _objective = new SspFct(SizeTeamA, SizeTeamB, FieldWidth, FieldHeight,
        PlayerSensorRange, PlayerSensorFov, PlayerSize);

      if (Optimizations.ContainsKey(Optimization))
      {
        _optimizer = Optimizations[Optimization](_objective);
      }
      else
      {
        _optimizer = Optimizations[DefaultOpt](_objective);
        Optimization = DefaultOpt;
      }
      _optimizer.Init();

      SensorPositioningProblem.PlaceFromVector(
        _optimizer.Result.RawVector.ToArray(), _objective.Raw.TeamA);

      Console.WriteLine("Sensors: " + _objective.Raw.TeamA.Count);
      Console.WriteLine("Obstacles:");
      foreach (var sensor in _objective.Raw.TeamB)
      {
        Console.WriteLine("- " + sensor.Position + " : " + sensor.Size);
      }

      clsUtil.DebugValue(_optimizer);
      QueueDraw();
    }

    public void OptimizeByValue()
    {
      while (_optimizer.DoIteration() == false)
      {
        if (_optimizer.Result.Eval < OptValue) break;
        clsUtil.DebugValue(_optimizer, ai_isOutValue: false);
      }

      clsUtil.DebugValue(_optimizer);
      SensorPositioningProblem.PlaceFromVector(
        _optimizer.Result.ToArray(), _objective.Raw.TeamA);
      QueueDraw();
    }

    public void OptimizeBySteps()
    {
      var sw = new Stopwatch();
      sw.Start();
      _optimizer.DoIteration(OptSteps);
      sw.Stop();
      Console.WriteLine(
        "Iterations: {0}, Shadow area: {1}%, Elapsed time: {2}ms",
        _optimizer.IterationCount,
        _objective.Raw.Normalize(_optimizer.Result.Eval),
        sw.ElapsedMilliseconds);
      
      SensorPositioningProblem.PlaceFromVector(
        _optimizer.Result.ToArray(), _objective.Raw.TeamA);
      QueueDraw();
    }

    private void DrawCoordinateSystem(Context cr)
    {
      var width = _objective.Raw.Env.Bounds.Max.X * _zoom;
      var height = _objective.Raw.Env.Bounds.Max.Y * _zoom;

      cr.SetSourceRGB(.8, .8, .8);
      cr.Rectangle(0, 0, width, height);
      cr.ClosePath();
      cr.Fill();

      cr.SetSourceRGBA(0, 0, 0, 0.2);
      cr.LineWidth = .5;
      for (var i = 0.0; i < width; i += width / 10)
      {
        cr.NewPath();
        cr.MoveTo(i, 0);
        cr.LineTo(i, height);
        cr.ClosePath();
        cr.Stroke();
      }

      for (var i = 0.0; i < height; i += height / 10)
      {
        cr.NewPath();
        cr.MoveTo(0, i);
        cr.LineTo(width, i);
        cr.ClosePath();
        cr.Stroke();
      }
    }

    private void DrawPolygon(Context cr, Polygon polygon)
    {
      cr.NewPath();
      cr.MoveTo(polygon[0].X * _zoom, polygon[0].Y * _zoom);
      for (var i = 1; i < polygon.Count; i++)
      {
        cr.LineTo(polygon[i].X * _zoom, polygon[i].Y * _zoom);
      }
      cr.ClosePath();
    }
    
    private void DrawSensors(Context cr)
    {
      cr.SetSourceRGBA(0, 0, 0, 0.7);
      foreach (var polygon in Sensor.Shadows(
        _objective.Raw.TeamA, _objective.Raw.Env))
      {
        if (polygon.Count == 0) continue;
        DrawPolygon(cr, polygon);
        cr.Fill();
      }
      
      foreach (var sensor in _objective.Raw.TeamA)
      {
        cr.SetSourceRGB(0, 0, 0);
        cr.SetDash(new double[]{10}, 0);
        cr.LineWidth = .2;
        DrawPolygon(cr, sensor.AreaOfActivity().ToPolygon());
        cr.Stroke();
        
        cr.SetSourceRGB(1, 0, 0);
        cr.Arc(
          sensor.Position.X * _zoom,
          sensor.Position.Y * _zoom,
          sensor.Size * _zoom, 0, 2 * Math.PI);
        cr.ClosePath();
        cr.Fill();
      }
    }

    private void DrawObstacles(Context cr)
    {
      foreach (var obstacle in _objective.Raw.TeamB)
      {
        cr.SetSourceRGB(0.1, 0.1, 1);
        cr.Arc(
          obstacle.Position.X * _zoom,
          obstacle.Position.Y * _zoom,
          obstacle.Size * _zoom, 0, 2 * Math.PI);
        cr.ClosePath();
      }

      cr.Fill();
    }

    private void DrawGui(Context cr)
    {
      cr.MoveTo(10, 15);
      cr.TextPath("Expected: " + _objective.Raw.Normalize(OptValue) + "%");
      cr.MoveTo(10, 25);
      cr.TextPath(
        "Shadow area: " + _objective.Raw.Normalize(_optimizer.Result.Eval) + 
        "%");
      cr.SetSourceRGB(0, 0, 0);
      cr.Fill();
    }

    protected override bool OnDrawn(Context cr)
    {
      DrawCoordinateSystem(cr);
      DrawSensors(cr);
      DrawObstacles(cr);
      DrawGui(cr);
      return true;
    }
  }

  
  internal class App : Application
  {
    private readonly SSPPlotter _plotter;
    private readonly Box _controls;
    private readonly Box _root;

    public App() : base("com.samples.sample", ApplicationFlags.None)
    {
      Register(Cancellable.Current);

      var window = new Window(WindowType.Toplevel)
      {
        WindowPosition = WindowPosition.Center
      };
      AddWindow(window);

      _plotter = new SSPPlotter();
      _controls = new VBox();
      _root = new HBox {_plotter, _controls};

      SetupControls();
      SetupStyle();

      var menu = new GLib.Menu();
      menu.AppendItem(new GLib.MenuItem("Quit", "app.quit"));
      AppMenu = menu;

      var quitAction = new SimpleAction("quit", null);
      quitAction.Activated += (o, args) => Quit();
      AddAction(quitAction);

      window.Add(_root);
      window.DeleteEvent += (o, args) => Quit();
      window.Title = "";
      window.ShowAll();
      window.GrabFocus();
    }

    private void SetupStyle()
    {
      var provider = new CssProvider();
      provider.LoadFromData(
        "box {background-color: #252625;}" +
        "button {" +
        "font-size: 22px;" +
        "color: #939797;" +
        "background: #010101;" +
        "padding: 0 5px;" +
        "border: none;" +
        "text-shadow: none;" +
        "box-shadow: none;" +
        "}" +
        "button:hover {background-color: #1A1B1B;}" +
        "button:active {background-color: #595959;}" +
        "button:disabled {border: none;}" +
        "entry {" +
        "background: #1D1E1E;" +
        "border: none;" +
        "color: #939797;" +
        "padding: 0 5px;" +
        "font-size: 22px;" +
        "caret-color: #939797;" +
        "}" +
        "combobox {" +
        "background: yellow;" +
        "color: green;" +
        "border: none;" +
        "}");
      StyleContext.AddProviderForScreen(Screen.Default, provider, 800);
    }

    private void SetupControls()
    {
      var sizeTeamAEntry = new Entry("" + _plotter.SizeTeamA);
      var sizeTeamBEntry = new Entry("" + _plotter.SizeTeamB);
      var fieldWidthEntry = new Entry("" + _plotter.FieldWidth);
      var fieldHeightEntry = new Entry("" + _plotter.FieldHeight);
      var playerSensorRangeEntry = new Entry("" + _plotter.PlayerSensorRange);
      var playerSensorFovEntry = new Entry("" + _plotter.PlayerSensorFov);
      var playerSizeEntry = new Entry("" + _plotter.PlayerSize);

      var resetPlotter = new Action(() =>
      {
        try
        {
          _plotter.SizeTeamA = int.Parse(sizeTeamAEntry.Text);
          _plotter.SizeTeamB = int.Parse(sizeTeamBEntry.Text);
          _plotter.FieldWidth = double.Parse(fieldWidthEntry.Text);
          _plotter.FieldHeight = double.Parse(fieldHeightEntry.Text);
          _plotter.PlayerSensorRange =
            double.Parse(playerSensorRangeEntry.Text);
          _plotter.PlayerSensorFov = double.Parse(playerSensorFovEntry.Text);
          _plotter.PlayerSize = double.Parse(playerSizeEntry.Text);
          _plotter.Init();
        }
        catch (FormatException)
        {
          Console.WriteLine("Please enter a number.");
        }
      });
      
      var resetBtn = new Button("Reset");
      resetBtn.Clicked += (sender, e) => resetPlotter();

      var optChooser = new ComboBox(_plotter.Optimizations.Keys.ToArray())
      {
        Active = _plotter.Optimizations.Keys.ToList()
          .FindIndex(s => s == _plotter.Optimization),
        HasFrame = true
      };
      optChooser.Changed += (o, args) =>
      {
        var active = _plotter.Optimizations.Keys.ToArray()[optChooser.Active];
        Console.WriteLine("Using: " + active + " optimization");
        _plotter.Optimization = active;
      };
      
      var stepsEntry = new Entry("" + _plotter.OptSteps)
      {
        PlaceholderText = "Steps"
      };
      var stepBtn = new Button("Optimize");
      stepBtn.Clicked += (sender, e) =>
      {
        try {_plotter.OptSteps = int.Parse(stepsEntry.Text);}
        catch (FormatException)
        {
          Console.WriteLine("Please enter a number into the 'steps' field");
        }
        _plotter.OptimizeBySteps();
      };
      var optimizeBySteps = new HBox {stepBtn, stepsEntry};
      optimizeBySteps.Spacing = 5;
      
      var expectationEntry = new Entry("" + _plotter.OptValue)
      {
        PlaceholderText = "Expectation"
      };
      var optimizeByValueBtn = new Button("Optimize");
      optimizeByValueBtn.Clicked += (sender, e) =>
      {
        try {_plotter.OptValue = int.Parse(expectationEntry.Text);}
        catch (FormatException)
        {
          Console.WriteLine(
            "Please enter a number into the 'expectation' field");
        }
        _plotter.OptimizeByValue();
      };
      var optimizeByValue = new HBox {optimizeByValueBtn, expectationEntry};
      optimizeByValue.Spacing = 5;
      
      foreach (var widget in new Widget[]
      {
        optimizeByValue, optimizeBySteps, resetBtn, optChooser,
        sizeTeamAEntry, sizeTeamBEntry, fieldWidthEntry, fieldHeightEntry,
        playerSensorRangeEntry, playerSensorFovEntry, playerSizeEntry
      }) _controls.Add(widget);

      _controls.Spacing = 5;
      _controls.Margin = 5;
    }
  }

  
  internal class Canvas : DrawingArea
  {
    public double XScale = 50;
    private double _yScale = -50;
    public double XOffset;
    public double YOffset;
    
    private readonly List<IGeometryObject> _elements = 
      new List<IGeometryObject>();
    private double _centerX;
    private double _centerY;

    public Canvas()
    {
      Drawn += (o, args) => Render(args.Cr);
      
      KeyPressEvent += (o, args) =>
      {
        if (args.Event.Key == Key.a) XOffset += 10;
        else if (args.Event.Key == Key.d) XOffset -= 10;  
        else if (args.Event.Key == Key.w) YOffset += 10;  
        else if (args.Event.Key == Key.s) YOffset -= 10;  
        else if (args.Event.Key == Key.plus) Scale(1);
        else if (args.Event.Key == Key.minus) Scale(-1);
        QueueDraw();
      };
    }

    public double YScale
    {
      get => _yScale;
      set => _yScale = -1 * value;
    }

    public void Scale(double factor)
    {
      XScale += factor;
      _yScale -= factor;
    }

    public void Clear()
    {
      _elements.Clear();
      QueueDraw();
    }
    
    public void Plot(IGeometryObject o)
    {
      _elements.Add(o);
      QueueDraw();
    }

    public void Plot(IEnumerable<IGeometryObject> objects)
    {
      _elements.AddRange(objects);
      QueueDraw();
    }

    private void Render(Context cr)
    {
      _centerX = AllocatedWidth / 2.0 + XOffset;
      _centerY = AllocatedHeight / 2.0 + YOffset;
      DrawCoordinateSystem(cr);
      
      foreach (var element in _elements)
      {
        switch (element)
        {
          case Segment _:
            DrawSegment(cr, (Segment) element);
            break;
          case Polygon _:
            DrawPolygon(cr, (Polygon) element);
            break;
          case Circle _:
            DrawCircle(cr, (Circle) element);
            break;
          case Rectangle _:
            DrawRectangle(cr, (Rectangle) element);
            break;
          default:
            Console.WriteLine("Unknown element type: " + element.GetType());
            break;
        }
      }
    }

    private void DrawCoordinateSystem(Context cr)
    {
      cr.SetSourceRGBA(0, 0, 0, 0.7);
      cr.Rectangle(0, 0, AllocatedWidth, AllocatedHeight);
      cr.ClosePath();
      cr.Fill();
      cr.SetSourceRGBA(1, 1, 1, 0.7);
      cr.LineWidth = 0.5;
      cr.NewPath();
      cr.MoveTo(0, _centerY);
      cr.LineTo(AllocatedWidth, _centerY);
      cr.ClosePath();
      cr.Stroke();
      cr.NewPath();
      cr.MoveTo(_centerX, 0);
      cr.LineTo(_centerX, AllocatedHeight);
      cr.ClosePath();
      cr.Stroke();
    }
    
    private void DrawPolygon(Context cr, Polygon p)
    {
      if (p.Count == 0) return;

      cr.SetSourceRGBA(0, 0, 0, 0.7);
      cr.NewPath();
      cr.MoveTo(p[0].X * XScale + _centerX,
        p[0].Y * _yScale + _centerY);

      for (var i = 1; i < p.Count; i++)
      {
        cr.LineTo(p[i].X * XScale + _centerX,
          p[i].Y * _yScale + _centerY);
      }

      if (p[0] != p[p.Count - 1])
      {
        cr.MoveTo(p[0].X * XScale + _centerX,
          p[0].Y * _yScale + _centerY);
      }

      cr.ClosePath();
      cr.Fill();
    }

    private void DrawCircle(Context cr, Circle c)
    {
      cr.SetSourceRGBA(1, 0, 0, 0.7);
      cr.Arc(
        c.Position.X * XScale + _centerX, 
        c.Position.Y * _yScale + _centerY, 
        c.Radius * XScale, 0, 2 * Math.PI);
      cr.ClosePath();
      cr.Fill();
    }

    private void DrawRectangle(Context cr, Rectangle r)
    {
      cr.SetSourceRGBA(0, 0, 0, 0.2);
      cr.Rectangle(r.Min.X * XScale + _centerX, 
        r.Min.Y * _yScale + _centerY, 
        r.Width() * XScale, 
        r.Height() * _yScale);
      cr.ClosePath();
      cr.Fill();
    }

    private void DrawSegment(Context cr, Segment s)
    {
      cr.SetSourceRGBA(1, 1, 1, 0.5);
      cr.NewPath();
      cr.MoveTo(s.Start.X * XScale + _centerX, s.Start.Y * _yScale + _centerY);
      cr.LineTo(s.End.X * XScale + _centerX, s.End.Y * _yScale + _centerY);
      cr.ClosePath();
      cr.Stroke();
    }
  }

  
  internal class OptimizationPlotter : Window
  {
    private readonly Box _root;
    private readonly Canvas _canvas;
    private readonly Box _optControls;
    private readonly Box _objControls;

    private SSP _obj;
    private Swarm _opt;
    
    private double[] _previousPos;
    
    public OptimizationPlotter() : base(WindowType.Toplevel)
    {
      _optControls = new VBox();
      _objControls = new VBox();
      _canvas = new Canvas
      {
        WidthRequest = 400, XOffset = -190,
        HeightRequest = 300, YOffset = 140,
        XScale = 40, YScale = 40,
      };
      
//      SetupObjectiveControls();
      SetupOptimizationControls();
      Init();
      
      _root = new HBox {_objControls, _canvas, _optControls};
      Add(_root);
      
      DeleteEvent += (o, args) => Application.Quit();
      HeightRequest = 300;
//      WidthRequest = 450;
      Title = "";
      ShowAll();
    }

    private void Render()
    {
      Console.WriteLine("Global Best: " + _opt.GlobalBestValue);
      
      _canvas.Clear();
      _canvas.Plot(_obj.Env.Bounds);
      _canvas.Plot(Sensor.Shadows(_obj.TeamA, _obj.Env));
      _canvas.Plot(_obj.TeamA.Select(o =>
        (IGeometryObject) new Circle(o.Position, o.Size)));
      _canvas.Plot(_obj.TeamB.Select(o =>
        (IGeometryObject) new Circle(o.Position, o.Size)));
      
      ((Label) _optControls.Children[0]).Text = "Best: " +
        _obj.Normalize(_opt.GlobalBestValue) + "%";
    }

    private void Update()
    {
      _opt.IterateOnce();
      SensorPositioningProblem.PlaceFromVector(_previousPos, 
        _opt.GlobalBest, _obj.TeamA, 0, 0);
      for (var i = 0; i < _obj.TeamA.Count; i++)
      {
        _previousPos[i * 3] = _obj.TeamA[i].Position.X;
        _previousPos[i * 3 + 1] = _obj.TeamA[i].Position.Y;
        _previousPos[i * 3 + 2] = _obj.TeamA[i].Rotation;
      }
    }

    private void Init()
    {
      _obj = new SSP();
      _opt = Pso.SwarmSpso2006(_obj.SearchSpace(), _obj.FitnessFct);
//      _opt.Fitness = vector => _obj.FitnessFct(vector, _opt.GlobalBest);
      
      _opt.Initialize();
      _previousPos = new double[_opt.SearchSpace.Dimensions];
      _opt.GlobalBest.CopyTo(_previousPos, 0);
      SensorPositioningProblem.PlaceFromVector(_opt.GlobalBest, _obj.TeamA);
      Render();
    }

    private void SetupObjectiveControls()
    {
      _objControls.Margin = 5;
    }

    private void SetupOptimizationControls()
    {
      _optControls.Margin = 5;
      
      _optControls.Add(new Label("Best: -"));
      
      var stepBtn = new Button("Step");
      _optControls.Add(stepBtn);
      stepBtn.Clicked += (sender, args) =>
      {
        Update();
        Render();
      };
    }
    
    public void Run()
    {
      var app = new Application("com.jakobrieke.dspplotter", 
        ApplicationFlags.None);
      app.Register(Cancellable.Current);
      app.AddWindow(this);
      
      var menu = new GLib.Menu();
      menu.AppendItem(new GLib.MenuItem("Quit", "app.quit"));
      app.AppMenu = menu;
      
      var quitAction = new SimpleAction("quit", null);
      quitAction.Activated += (o, args) => Application.Quit();
      app.AddAction(quitAction);
      
      Application.Run();
    }
  }

  
  internal class SPSO2006 : absOptimization
  {
    private Swarm _swarm;

    public SPSO2006(absObjectiveFunction objective)
    {
      ObjectiveFunction = objective;
      Results = new List<clsPoint>();
    }

    public override void Init()
    {
      var obj = ((SspFct) ObjectiveFunction).Raw;
      _swarm = Pso.SwarmSpso2006(
        new SearchSpace(obj.Intervals()), obj.FitnessFct);
      _swarm.Initialize();
    }

    public override bool DoIteration(int iterations = 0)
    {
      _swarm.IterateMaxIterations(iterations);
      m_iteration += iterations;
      return true;
    }

    public override bool IsRecentError()
    {
      return false;
    }

    public override clsPoint Result => 
      new clsPoint(ObjectiveFunction, _swarm.GlobalBest);
    
    public override List<clsPoint> Results { get; }
    
    public override int Iteration 
    {
      get => _swarm.Iteration;
      set {}
    }
  }
  
  
  internal class SPSO2007 : absOptimization
  {
    private Swarm _swarm;

    public SPSO2007(absObjectiveFunction objective)
    {
      ObjectiveFunction = objective;
      Results = new List<clsPoint>();
    }

    public override void Init()
    {
      var obj = ((SspFct) ObjectiveFunction).Raw;
      _swarm = Pso.SwarmSpso2007(obj.SearchSpace(), obj.FitnessFct);
      _swarm.Initialize();
    }

    public override bool DoIteration(int iterations = 0)
    {
      _swarm.IterateMaxIterations(iterations);
      m_iteration += iterations;
      return true;
    }

    public override bool IsRecentError()
    {
      return false;
    }

    public override clsPoint Result => 
      new clsPoint(ObjectiveFunction, _swarm.GlobalBest);
    
    public override List<clsPoint> Results { get; }
    
    public override int Iteration 
    {
      get => _swarm.Iteration;
      set {}
    }
  }
  
  
  internal class SPSO2011 : absOptimization
  {
    private Swarm _swarm;

    public SPSO2011(absObjectiveFunction objective)
    {
      ObjectiveFunction = objective;
      Results = new List<clsPoint>();
    }

    public override void Init()
    {
      var obj = ((SspFct) ObjectiveFunction).Raw;
      _swarm = Pso.SwarmSpso2011(obj.SearchSpace(), obj.FitnessFct);
//      _swarm.Topology = p => Pso.AdaptiveRandomTopology(p, 3);
      _swarm.Topology = Pso.RingTopology;
      _swarm.Initialize();
    }

    public override bool DoIteration(int iterations = 0)
    {
      _swarm.IterateMaxIterations(iterations);
      m_iteration += iterations;
      return true;
    }

    public override bool IsRecentError()
    {
      return false;
    }

    public override clsPoint Result => 
      new clsPoint(ObjectiveFunction, _swarm.GlobalBest);

    public override List<clsPoint> Results { get; }
    
    public override int Iteration {
      get => _swarm.Iteration;
      set {}
    }
  }


  internal class SspFct : absObjectiveFunction
  {
    public readonly SSP Raw;

    public SspFct(
      int sizeTeamA = 1,
      int sizeTeamB = 1,
      double fieldWidth = 9,
      double fieldHeight = 6,
      double playerSensorRange = 12,
      double playerSensorFov = 56.3,
      double playerSize = 0.1555)
    {
      Raw = new SSP(
        sizeTeamA, sizeTeamB, fieldWidth, fieldHeight, playerSensorRange,
        playerSensorFov, playerSize);
    }

    public override int NumberOfVariable()
    {
      return Raw.Intervals().Length;
    }

    public override double F(List<double> x)
    {
      try
      {
        return Raw.FitnessFct(x.ToArray());
      }
      catch (Exception)
      {
        return double.PositiveInfinity;
      }
    }

    public override List<double> Gradient(List<double> x)
    {
      throw new NotImplementedException();
    }

    public override List<List<double>> Hessian(List<double> x)
    {
      throw new NotImplementedException();
    }
  }


  public static class SensorPositioningTest
  {
    public static void TestAll()
    {
//      for (var i = 0; i < 1000; i++) TestWithExternalPso();
//      for (var i = 0; i < 500; i++) TestSSP(6, 5, 20);
//      TestSSPStepByStep();
//      TestWithSimplex();
//      TestWithAdaptiveDifferentialEvolution();
//      TestSspGraphical();
      TestOptimizationPlotter();
    }

    private static void TestSspGraphical()
    {
      Application.Init();
      var app = new App();
      Application.Run();
    }

    private static void TestOptimizationPlotter()
    {
      Application.Init();
      new OptimizationPlotter().Run();
    }

    private static void TestWithSimplex()
    {
      var objective = new SspFct(2, 5);
      var opt = new clsOptNelderMead(objective);
      opt.InitialPosition = objective.Raw.SearchSpace().RandPos();
      opt.IsUseCriterion = false;
      opt.Init();
      clsUtil.DebugValue(opt);
      while (opt.DoIteration(10) == false)
      {
        if (opt.Result.Eval < 5) break;
        clsUtil.DebugValue(opt, ai_isOutValue: false);
      }

      clsUtil.DebugValue(opt);
    }

    private static void TestWithAdaptiveDifferentialEvolution()
    {
      var objective = new SspFct(2, 5);
      var opt = new clsOptDEJADE(objective)
      {
        LowerBounds = objective.Raw.Intervals().Select(i => i[0]).ToArray(),
        UpperBounds = objective.Raw.Intervals().Select(i => i[1]).ToArray()
      };
      opt.Init();

      // Print debug
      Console.WriteLine("Sensors: " + objective.Raw.TeamA.Count);
      Console.WriteLine("Obstacles:");
      foreach (var sensor in objective.Raw.TeamB)
      {
        Console.WriteLine("- " + sensor.Position + " : " + sensor.Size);
      }

      clsUtil.DebugValue(opt);

      // Optimize
      while (opt.DoIteration(10) == false)
      {
        if (opt.Result.Eval < 5) break;
        clsUtil.DebugValue(opt, ai_isOutValue: false);
      }

      Console.WriteLine("Best: " + objective.Raw.Normalize(opt.Result.Eval) +
                        "%");
      clsUtil.DebugValue(opt);
    }

    private static void TestWithExternalPso()
    {
      var objective = new SspFct();
      var opt = new clsOptPSO(objective) {IsUseCriterion = false};
      opt.Init();

      Console.WriteLine("--- Fitness function setup");
      Console.WriteLine("Sensors: " + objective.Raw.TeamA.Count);
      Console.WriteLine("Obstacles: " + objective.Raw.TeamB.Count);
      foreach (var ob in objective.Raw.TeamB)
      {
        Console.Write("- " + ob.Position + " : " + ob.Size);
      }

      Console.WriteLine();
      Console.WriteLine("Field: " + objective.Raw.Env.Bounds);

      Console.WriteLine("\n--- Setup");
      clsUtil.DebugValue(opt);

      opt.DoIteration(500);
      Console.WriteLine("--- Results");
      Console.WriteLine("Best: " + objective.Raw.Normalize(opt.Result.Eval) +
                        "% shadow");
      clsUtil.DebugValue(opt);
    }

    private static void TestSSPStepByStep()
    {
      SSP prob = null;
      Swarm pso = null;

      var plotter = new Plotter
      {
        XScale = 20, YScale = 20,
        XOffset = -100, YOffset = 60
      };

      var plot = new Action(() =>
      {
        Console.WriteLine("Iteration: " + pso.Iteration +
                          ", Best: " + prob.Normalize(pso.GlobalBestValue) +
                          "% shadow");
        plotter.Clear();
        SensorPositioningProblem.PlaceFromVector(pso.GlobalBest, prob.TeamA);
        plotter.Plot(prob.Env.Bounds);
        plotter.Plot(Sensor.Shadows(prob.TeamA, prob.Env));
        plotter.Plot(prob.TeamA.Select(o =>
          (IGeometryObject) new Circle(o.Position, o.Size)));
        plotter.Plot(prob.TeamB.Select(o =>
          (IGeometryObject) new Circle(o.Position, o.Size)));
        pso.IterateOnce();
      });

      var reset = new Action(() =>
      {
        prob = new SSP(10, 10);
        pso = Pso.SwarmSpso2011(
          new SearchSpace(prob.Intervals()),
          prob.FitnessFct);

        Console.WriteLine("Reset");
        pso.Initialize();
        plot();
      });

      plotter.Window.KeyPressEvent += (o, args) =>
      {
        if (args.Event.Key == Key.n) plot();
        else if (args.Event.Key == Key.r) reset();
      };

      reset();
      plotter.Plot();
    }

    private static void TestSSP(int sizeTeamA, int sizeTeamB, 
      int iterations = 10, int swarmSize = 40)
    {
      var prob = new SSP(sizeTeamA, sizeTeamB);
      var sw = Pso.SwarmSpso2011(prob.SearchSpace(), prob.FitnessFct);
      sw.Topology = Pso.RingTopology;
      sw.ShouldTopoUpdate = swarm => false;

      Console.WriteLine("--- Swarm");
      Console.WriteLine("Topology: " + sw.Topology.Method.Name);
      Console.WriteLine("Confinement: " + sw.Confinement.Method.Name);
      Console.WriteLine("Update Method: " + sw.Update.Method.Name);
      Console.WriteLine("Size: " + swarmSize);
      Console.WriteLine("W: " + Pso.W);
      Console.WriteLine("C1: " + Pso.C1);
      Console.WriteLine("C2: " + Pso.C2);
      
      Console.WriteLine("\n--- Static Sensor Problem");
      Console.WriteLine("Field: " + prob.Env.Bounds);
      Console.WriteLine("Sensor Range: " + prob.TeamA[0].Range);
      Console.WriteLine("Sensor FOV: " + prob.TeamA[0].Fov);
      Console.WriteLine("Obstacle Size: " + prob.TeamA[0].Size);
      Console.WriteLine("Sensors: " + prob.TeamA.Count);
      Console.WriteLine("Obstacles: " + prob.TeamB.Count);
      foreach (var ob in prob.TeamB)
      {
        Console.WriteLine("- " + ob.Position + ", " + ob.Size);
      }

      Console.WriteLine(
        "\n--- Iteration, Shadow area in %, Elapsed time in ms, Distance");
      var clock = new Stopwatch();
      
      clock.Start();
      sw.Initialize(swarmSize);
      clock.Stop();
      
      Console.WriteLine("- {0}, {1}, {2}, {3}",
        0, prob.Normalize(sw.GlobalBestValue), clock.ElapsedMilliseconds, 0);

      var last = new double[sw.SearchSpace.Dimensions];
      sw.GlobalBest.CopyTo(last, 0);
      var totalDistance = 0.0;
      double distance;
      
      for (var i = 0; i < 10; i++)
      {
        clock.Reset();
        clock.Start();
        sw.IterateMaxIterations(iterations);
        clock.Stop();
        distance = Vector.Distance(last, sw.GlobalBest);
        totalDistance += Vector.Distance(last, sw.GlobalBest);
        
        Console.WriteLine("- {0}, {1}, {2}, {3}",
          sw.Iteration, prob.Normalize(sw.GlobalBestValue), 
          clock.ElapsedMilliseconds, distance);
        
        sw.GlobalBest.CopyTo(last, 0);
      }

      Console.WriteLine("\n--- Results");
      Console.Write(string.Join(",\n", sw.GlobalBest));
      Console.WriteLine();
      Console.WriteLine("Collisions: " + prob.Collisions);
      Console.WriteLine("Total Distance: " + totalDistance);
      Console.WriteLine(
        "Shadow area: " + prob.Normalize(sw.GlobalBestValue) + "%");
    }
  }
}