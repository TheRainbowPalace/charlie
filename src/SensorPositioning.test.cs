using System;
using System.Collections.Generic;
using System.Linq;
using Cairo;
using Gdk;
using Geometry;
using GLib;
using LibOptimization.BenchmarkFunction;
using LibOptimization.Optimization;
using LibOptimization.Util;
using ParticleSwarmOptimization;
using Gtk;
using Shadows;
using Action = System.Action;
using Application = Gtk.Application;
using Environment = System.Environment;
using Key = Gdk.Key;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;


namespace sensor_positioning
{
  internal class SensorAreaPlotter : DrawingArea
  {
    public int SizeTeamA = 1;
    public int SizeTeamB = 1;
    public double FieldWidth = 9;
    public double FieldHeight = 6;
    public double PlayerSensorRange = 12;
    public double PlayerSensorFov = 56.3;
    public double PlayerSize = 0.1555;

    public double Expectation = 5;
    
    private SspFct _objective;
    private absOptimization _optimizer;
    private double Scale = 50;
//    public double XOffset = 0;
//    public double YOffset = 0;

    public SensorAreaPlotter()
    {
      Init();
      HeightRequest = (int)(_objective.Raw.Env.Bounds.Max.Y * Scale);
      WidthRequest = (int)(_objective.Raw.Env.Bounds.Max.X * Scale);
    }

    public void Init()
    {
      _objective = new SspFct(SizeTeamA, SizeTeamB, FieldWidth, FieldHeight,
        PlayerSensorRange, PlayerSensorFov, PlayerSize);
      _optimizer = new clsOptDEJADE(_objective)
      {
        LowerBounds = _objective.Raw.Intervals().Select(i => i[0]).ToArray(),
        UpperBounds = _objective.Raw.Intervals().Select(i => i[1]).ToArray()
      };
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
    
    public void Optimize()
    {
      Console.WriteLine("Optimize");
      while (_optimizer.DoIteration(10) == false)
      {
        if (_optimizer.Result.Eval < Expectation) break;
        clsUtil.DebugValue(_optimizer, ai_isOutValue: false);
      }
      clsUtil.DebugValue(_optimizer);
      
      SensorPositioningProblem.PlaceFromVector(
        _optimizer.Result.RawVector.ToArray(), _objective.Raw.TeamA);
      QueueDraw();
    }

    private void DrawCoordinateSystem(Context cr)
    {
      var width = _objective.Raw.Env.Bounds.Max.X * Scale;
      var height = _objective.Raw.Env.Bounds.Max.Y * Scale;
      
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
    
    private void DrawSensors(Context cr)
    {
      cr.SetSourceRGBA(0, 0, 0, 0.7);
      foreach (var polygon in Sensor.Shadows(
        _objective.Raw.TeamA, _objective.Raw.Env))
      {
        if (polygon.Count == 0) continue;
        cr.NewPath();
        cr.MoveTo(polygon[0].X * Scale, polygon[0].Y * Scale);
        for (var i = 1; i < polygon.Count; i++)
        {
          cr.LineTo(polygon[i].X * Scale, polygon[i].Y * Scale);
        }
        if (polygon[0] != polygon[polygon.Count - 1])
        {
          cr.MoveTo(polygon[0].X * Scale, polygon[0].Y * Scale);
        }
        cr.ClosePath();
        cr.Fill();
      }
      
      foreach (var sensor in _objective.Raw.TeamA)
      {
        cr.SetSourceRGB(1, 0, 0);
        cr.Arc(
          sensor.Position.X * Scale,
          sensor.Position.Y * Scale,
          sensor.Size * Scale, 0, 2 * Math.PI);
        cr.ClosePath();
      }
      cr.Fill();
    }

    private void DrawObstacles(Context cr)
    {
      foreach (var obstacle in _objective.Raw.TeamB)
      {
        cr.SetSourceRGB(0.1, 0.1, 1);
        cr.Arc(
          obstacle.Position.X * Scale, 
          obstacle.Position.Y * Scale, 
          obstacle.Size * Scale, 0, 2 * Math.PI);
        cr.ClosePath();
      }
      cr.Fill();
    }

    private void DrawGui(Context cr)
    {
      cr.MoveTo(10, 15);
      cr.TextPath("Expected: " + _objective.Raw.Normalize(Expectation) + "%");
      cr.MoveTo(10, 25);
      cr.TextPath(
        "Seen area: " + _objective.Raw.Normalize(_optimizer.Result.Eval) + "%");
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
    private readonly SensorAreaPlotter _plotter;
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
      
      _plotter = new SensorAreaPlotter();
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
          _plotter.PlayerSensorRange = double.Parse(playerSensorRangeEntry.Text);
          _plotter.PlayerSensorFov = double.Parse(playerSensorFovEntry.Text);
          _plotter.PlayerSize = double.Parse(playerSizeEntry.Text);
          _plotter.Init();
        }
        catch (FormatException)
        {
          Console.WriteLine("Please enter a number.");
        }
      });

      var stepBtn = new Button("Optimize");
      stepBtn.Clicked += (sender, e) => _plotter.Optimize();
      var resetBtn = new Button("Reset");
      resetBtn.Clicked += (sender, e) => resetPlotter();
      var buttons = new HBox {stepBtn, resetBtn};
      buttons.Spacing = 5;
      
      foreach (var widget in new Widget[]{
        buttons, 
        sizeTeamAEntry, sizeTeamBEntry, fieldWidthEntry, fieldHeightEntry,
        playerSensorRangeEntry, playerSensorFovEntry, playerSizeEntry})
      {
        _controls.Add(widget);
      }
      _controls.Spacing = 5;
      _controls.Margin = 5;
    }
  }

  
//  internal class PsoOptimizer : absOptimization
//  {
//    private Swarm _swarm;
//
//    public PsoOptimizer(absObjectiveFunction objective)
//    {
//      ObjectiveFunction = objective;
//    }
//
//    public override void Init()
//    {
//      var obj = ((SspFct) ObjectiveFunction).Raw;
//      _swarm = Pso.SwarmSpso2011(
//        new SearchSpace(obj.Intervals()), obj.FitnessFct);
//    }
//
//    public override bool DoIteration(int iterations = 0)
//    {
//      _swarm.IterateMaxIterations(iterations);
//      return true;
//    }
//
//    public override bool IsRecentError()
//    {
//      return false;
//    }
//
//    public override clsPoint Result { get; }
//    public override List<clsPoint> Results { get; }
//    public override int Iteration { get; set; }
//  }
  
  
  internal class SspFct : absObjectiveFunction
  {
    public readonly StaticSensorPositioning Raw;
    
    public SspFct(
      int sizeTeamA = 1,
      int sizeTeamB = 1,
      double fieldWidth = 9,
      double fieldHeight = 6,
      double playerSensorRange = 12,
      double playerSensorFov = 56.3,
      double playerSize = 0.1555)
    {
      Raw = new StaticSensorPositioning(
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
      return null;
    }

    public override List<List<double>> Hessian(List<double> x)
    {
      return null;
    }
  }
  
  
  public static class SensorPositioningTest
  {
    public static void TestAll()
    {
//      TestWithExternalPso();
//      TestWithPso();    
//      TestWithPsoStepByStep();
      TestWithSimplex();
//      TestWithAdaptiveDifferentialEvolution();
//      TestGraphical();
    }

    private static void TestGraphical()
    {
      Application.Init();
      var app = new App();
      Application.Run();
    }

    private static void TestWithSimplex()
    {
      var objective = new SspFct(2, 5);
      var opt = new clsOptNelderMead(objective);
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
      
      Console.WriteLine("\n--- PSO setup");
      clsUtil.DebugValue(opt);

      opt.DoIteration(500);
      Console.WriteLine("\n--- Results");
      clsUtil.DebugValue(opt);
    }

    private static void TestWithPsoStepByStep()
    {
      StaticSensorPositioning prob = null;
      Swarm pso = null;
      
      var plotter = new Plotter{XScale = 20, YScale = 20, 
        XOffset = -100, YOffset = 60};

      var plot = new Action(() =>
      {
        Console.WriteLine("Iteration: " + pso.Iteration + 
                          ", Best: " + prob.Normalize(pso.GlobalBestValue) + 
                          "% Shadows");
        plotter.Clear();
        SensorPositioningProblem.PlaceFromVector(pso.GlobalBest, prob.TeamA);
        plotter.Plot(prob.Env.Bounds);
        plotter.Plot(Sensor.Shadows(prob.TeamA, prob.Env));
        plotter.Plot(prob.TeamA.Select(o => 
          (IGeometryObject)new Circle(o.Position, o.Size)));
        plotter.Plot(prob.TeamB.Select(o => 
          (IGeometryObject)new Circle(o.Position, o.Size)));
        pso.IterateOnce();
      });
      
      var reset = new Action(() =>
      {
        prob = new StaticSensorPositioning(10, 10);
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
    
    private static void TestWithPso()
    {
      var prob = new StaticSensorPositioning(3, 5);
      var pso = Pso.SwarmSpso2011(
        new SearchSpace(prob.Intervals()),
        prob.FitnessFct);

      Console.WriteLine("--- Setup");
      Console.WriteLine("Sensors: " + prob.TeamA.Count);
      Console.WriteLine("Obstacles: " + prob.TeamB.Count);
      Console.WriteLine("Field: " + prob.Env.Bounds);

      if (prob.TeamB.Count > 0)
      {
        Console.WriteLine("\n--- Obstacles");
        foreach (var ob in prob.TeamB)
        {
          Console.Write(ob.Position + " : " + ob.Size);
        }
        Console.WriteLine();
      }

      Console.WriteLine("\n--- Iterations");
      pso.Initialize();
      Console.WriteLine(
        "Initialized best: " + pso.GlobalBestValue +
        ", Normal: " + prob.Normalize(pso.GlobalBestValue));

      const int iterations = 100;
      for (var i = 0; i < iterations; i++)
      {
        pso.IterateOnce();

        // Print only 10 iterations
        if (i % (iterations / 10) != 0) continue;

        Console.WriteLine(
          "Iteration: " + (i + 1) + ", Shadow Area: "
          + pso.GlobalBestValue + ", Normal: "
          + prob.Normalize(pso.GlobalBestValue) + "%");
      }

      Console.WriteLine("\n--- Best Found");
      foreach (var d in pso.GlobalBest) Console.Write(d + ", ");
      Console.WriteLine();

//        for (var i = 0; i < 20; i++)
//        {
//            for (var j = 0; j < 60; j++)
//            {
//                Console.Write("-");
//            }
//            Console.WriteLine();
//        }
      Console.WriteLine("Collisions: " + prob.Collisions);
    }
  }
}