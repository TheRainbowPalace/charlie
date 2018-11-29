using System;
using System.Collections.Generic;
using System.Linq;
using Cairo;
using Geometry;
using GLib;
using Gtk;
using Shadows;
using Action = System.Action;
using Application = Gtk.Application;
using Environment = Shadows.Environment;
using Key = Gdk.Key;
using Rectangle = Geometry.Rectangle;
using Window = Gtk.Window;

namespace sensor_positioning
{
  class Plotter : Application
  {
    public double XScale = 50;
    private double _yScale = -50;
    public double XOffset;
    public double YOffset;
    
    private readonly List<IGeometryObject> _elements = 
      new List<IGeometryObject>();
    private double _centerX;
    private double _centerY;
    private readonly Window _window;
    
    public Plotter() : base("com.jakobrieke.plotter", ApplicationFlags.None)
    {
      Init();
      
      _window = new Window(WindowType.Toplevel)
      {
        WindowPosition = WindowPosition.Center,
        HeightRequest = 250,
        WidthRequest = 450,
        Title = ""
      };
      _window.Drawn += (o, args) => Render(args.Cr);
      _window.ShowAll();

      _window.KeyPressEvent += (o, args) =>
      {
        if (args.Event.Key == Key.a) XOffset += 10;
        else if (args.Event.Key == Key.d) XOffset -= 10;  
        else if (args.Event.Key == Key.w) YOffset += 10;  
        else if (args.Event.Key == Key.s) YOffset -= 10;  
        else if (args.Event.Key == Key.plus) Scale(1);
        else if (args.Event.Key == Key.minus) Scale(-1);
        _window.QueueDraw();
      };
      
      AddWindow(_window);
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
    
    public void Plot(IGeometryObject o)
    {
      _elements.Add(o);
      _window.QueueDraw();
    }

    public void Plot(IEnumerable<IGeometryObject> objects)
    {
      _elements.AddRange(objects);
      _window.QueueDraw();
    }

    public void Plot()
    {
      Run();
    }

    private void Render(Context cr)
    {
      _centerX = _window.AllocatedWidth / 2.0 + XOffset;
      _centerY = _window.AllocatedHeight / 2.0 + YOffset;
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
      cr.Rectangle(0, 0, _window.AllocatedWidth, _window.AllocatedHeight);
      cr.ClosePath();
      cr.Fill();
      cr.SetSourceRGBA(1, 1, 1, 0.7);
      cr.LineWidth = 0.5;
      cr.NewPath();
      cr.MoveTo(0, _centerY);
      cr.LineTo(_window.AllocatedWidth, _centerY);
      cr.ClosePath();
      cr.Stroke();
      cr.NewPath();
      cr.MoveTo(_centerX, 0);
      cr.LineTo(_centerX, _window.AllocatedHeight);
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
  
  
  public class Shadows_test
  {
    // Todo: [x] Test Sensor class : AreaOfActivity
    // Todo: [ ] Test Sensor class : Shadow
    // Todo: [ ] Test Sensor class : Shadows
    // Todo: [ ] Test Sensor class : ShadowArea
    // Todo: [ ] Test Sensor class : ShadowArea
    
    // Todo: [ ] Test Shadow class : UnseenArea
    // Todo: [ ] Test Shadow class : HiddenArea
    // Todo: [ ] Test Shadow class : Shadows
    // Todo: [ ] Test Shadow class : Shadows
    // Todo: [ ] Test Shadow class : CoreShadows
    // Todo: [ ] Test Shadow class : CoreShadows
    // Todo: [ ] Test Shadow class : CoreShadows
    // Todo: [ ] Test Shadow class : CoreShadowArea
    
    public static void Main()
    {
//      TestArcToPolygon();
//      TestSensorShadow();
//      TestHiddenArea();
      TestTangents();
    }
    
    private static void TestArcToPolygon()
    {
      Console.WriteLine("Arc to polygon test");
      var r1 = new Arc(0, 0, 1, 90).ToPolygon() == new Polygon {
        new Vector2(0, 0), new Vector2(1, 0),
        new Vector2(0.866025403784439, 0.5),
        new Vector2(0.5, 0.866025403784439),
        new Vector2(6.12303176911189E-17, 1)};
      Console.WriteLine("Arc to polygon 01: " + r1);
      
      // Todo: Fix bug when arc radius is >= 180 degrees
      var arc2 = new Arc(0, 0, 1, 190).ToPolygon(0);
      Console.WriteLine("02: " + false);
      
      var r3 = new Arc(-1, -1, 1, 90, 180).ToPolygon(4) == new Polygon {
        new Vector2(-1, -1), new Vector2(-2, -1),
        new Vector2(-1.95105651629515, -1.30901699437495),
        new Vector2(-1.80901699437495, -1.58778525229247),
        new Vector2(-1.58778525229247, -1.80901699437495),
        new Vector2(-1.30901699437495, -1.95105651629515),
        new Vector2(-1, -2)};
      Console.WriteLine("03: " + r3);
      
//      var plotter = new Plotter();
//      plotter.Plot(arc2);
//      plotter.Plot();
    }

    private static void TestSensorShadow()
    {
      var e1 = new Polygon {
        new Vector2(9.99999974737875, 3.75159990522661),
        new Vector2(0.999999974737875, 0.999999974737875),
        new Vector2(2.22289994384482, 4.99999987368938),
        new Vector2(-9.99999974737875, 4.99999987368938),
        new Vector2(-9.99999974737875, -4.99999987368938),
        new Vector2(9.99999974737875, -4.99999987368938),
        new Vector2(9.99999974737875, 3.75159990522661)
      };
      
      var e2 = new Polygon {
        new Vector2(5.99509984855104, 4.99999987368938),
        new Vector2(4.20309989382076, 4.99999987368938),
        new Vector2(1.87859995254257, 2.09719994702027),
        new Vector2(2.09719994702027, 1.87859995254257),
        new Vector2(5.99509984855104, 4.99999987368938)
      };
      
      var e3 = new Polygon {
        new Vector2(2.55249993551843, 2.58889993459889),
        new Vector2(1.90919995176955, 2.12619994628767),
        new Vector2(2.08749994726531, 1.87149995272193),
        new Vector2(2.70419993168616, 2.29159994210931),
        new Vector2(4.43199988803826, -1.19919996970566),
        new Vector2(-7.999999797903, -4.99999987368938),
        new Vector2(-4.9426998751369, 4.99999987368938),
        new Vector2(-9.99999974737875, 4.99999987368938),
        new Vector2(-9.99999974737875, -4.99999987368938),
        new Vector2(9.99999974737875, -4.99999987368938),
        new Vector2(9.99999974737875, 4.99999987368938),
        new Vector2(0.141399996427936, 4.99999987368938),
        new Vector2 (2.55249993551843, 2.58889993459889)
      };
      
      var env = new Environment(-10, -5, 20, 10);
      var s1 = new Sensor(1, 1, 45, 13, 56, 0.1555);
      var s2 = new Sensor(0, 0, 45, 12, 56, 0.1555);
      env.Obstacles.Add(s1);
      env.Obstacles.Add(s2);
      
      // Run Tests
      
      Console.WriteLine("Sensor shadows test");
      
      var shadows = s1.Shadows(env);
      Console.WriteLine("01: " + (shadows[0] == e1));
      
      s2.Position = new Vector2(2, 2);
      shadows = s1.Shadows(env);
      Console.WriteLine("02: " + (shadows[0] == e1 && shadows[1] == e2));

      s1.Position = new Vector2(-8, -5);
      shadows = s1.Shadows(env);
      Console.WriteLine("03: " + (shadows[0] == e3));
      
//      s1 = new Sensor(0, 0, 0, 3, 56, 0.1555);
//      s1.Rotation = 180;
      shadows = s1.Shadows(env);
      Console.WriteLine("\n" + string.Join("\n ", shadows));
      
      // Plot results

      var plotter = new Plotter {XScale = 20, YScale = 20};
      plotter.Plot(env.Bounds);
      plotter.Plot(shadows);
      foreach (var o in env.Obstacles)
      {
        plotter.Plot(new Circle(o.Position.X, o.Position.Y, o.Size));
      }
      plotter.Plot();
    }

    private static void TestHiddenArea()
    {
//      var a1 = new Arc(0, 0, .1, 45, -22.5);
      var s1 = new Vector2(0, 0);
      var o1 = new Circle(0.5, .5, .1);
      var bounds = new Rectangle(-1, -1, 1, 1);
      
      var hidden = Shadows2D.HiddenArea(s1, o1, bounds);
      // Expect:
      // 0.56, 0.42; 1, 0.75; 1, 1; 0.75, 1; 0.42, 0.56; 0.56, 0.42

      s1 = new Vector2(-1, 0);
      o1 = new Circle(0.5, .5, .1);
      hidden = Shadows2D.HiddenArea(s1, o1, bounds);
      // Expect:
      // 0.525559467676119, 0.403321596971643;
      // 1, 0.528752376445897;
      // 1, 0.810533337839817;
      // 0.462440532323881, 0.592678403028357;
      // 0.525559467676119, 0.403321596971643
      
      s1 = new Vector2(-1, -1);
      o1 = new Circle(0.5, .5, .1);
      hidden = Shadows2D.HiddenArea(s1, o1, bounds);
      // Expect:
      // 0.567298733668057, 0.426034599665276; 1, 0.819735534817705; 1, 1;
      // 0.819735534817704, 1; 0.426034599665276, 0.567298733668057;
      // 0.567298733668057, 0.426034599665276
      
      // Test what happens when sensor is inside obstacle
      s1 = new Vector2(-0.9, -0.85);
      o1 = new Circle(-0.9, -0.9, .1);
      hidden = Shadows2D.HiddenArea(s1, o1, bounds);
      // Expect:
      // -1, -1; 1, -1; 1, 1; -1, 1
      
      s1 = new Vector2(.1, .1);
      o1 = new Circle(-1, 0, .1);
      hidden = Shadows2D.HiddenArea(s1, o1, bounds);
      // Expect:
      // -1, 0.1; -1, 0.1; -1, -0.101666666666667;
      // -0.981967213114754, -0.0983606557377049; -1, 0.1
      
      s1 = new Vector2(-1, -1);
      o1 = new Circle(1, 1, .1);
      hidden = Shadows2D.HiddenArea(s1, o1, bounds);
      // Error here
      
      s1 = new Vector2(-1, -1);
      o1 = new Circle(-1, 0, .1);
      hidden = Shadows2D.HiddenArea(s1, o1, bounds);
      // Error here
      
      // Test what happens when sensor is on the edge of the obstacle
      s1 = new Vector2(-1 + .100001, 0);
      o1 = new Circle(-1, 0, .1);
      hidden = Shadows2D.HiddenArea(s1, o1, bounds);
      // Error here
      
      s1 = new Vector2(.1, 0);
      o1 = new Circle(0, 0, .1);
      hidden = Shadows2D.HiddenArea(s1, o1, bounds);
      // 0.1, 0; NaN, NaN; 1, -1; 0.1, -1; 0.1, -2.44921270764475E-17; 0.1, 0
      // Error here
      
      s1 = new Vector2(-0.9, .5);
      o1 = new Circle(-1, 0, .1);
      hidden = Shadows2D.HiddenArea(s1, o1, bounds);
      // Error here
      
      Console.WriteLine(hidden);
      var plotter = new Plotter {XScale = 70, YScale = 70};
      plotter.Plot(bounds);
      plotter.Plot(new Circle(s1, .05));
      plotter.Plot(hidden);
      plotter.Plot(o1);
      plotter.Plot();
    }

    private static Segment[] Tangents(Vector2 p, Circle c)
    {
      var d = Vector2.Distance(p, c.Position);
      var l = Math.Sqrt(Math.Pow(d, 2) + Math.Pow(c.Radius, 2));
      var alpha = Math.Tanh(c.Radius / d);
      var beta = Math.Sinh(c.Position.Y / d);
      var t1 = new Segment(p, p.Move(alpha + beta, l, false));
      var t2 = new Segment(p, p.Move(beta - alpha, l, false));
      
      return new[] {t1, t2};
    }
    
    private static void TestTangents()
    {
      var s1 = new Vector2(0, 0);
      var o1 = new Circle(0.5, .5, .1);
      var o2 = new Circle(-0.3, -0.3, .1);
      var bounds = new Rectangle(-1, -1, 1, 1);

//      var t1 = Tangents(s1, o1);
//      var t2 = Tangents(s1, o2);
      var t1 = Circle.Tangents(s1, o1);
      var t2 = Circle.Tangents(s1, o2);
      
      var plotter = new Plotter {XScale = 80, YScale = 80};
      plotter.Plot(bounds);
      plotter.Plot(new Circle(s1, .05));
      plotter.Plot(t1[0]);
      plotter.Plot(t1[1]);
      plotter.Plot(t2[0]);
      plotter.Plot(t2[1]);
      plotter.Plot(o1);
      plotter.Plot(o2);
      plotter.Plot();
    }
  }
}