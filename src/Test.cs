using System;
using System.Collections.Generic;
using Cairo;
using Geometry;
using GLib;
using Gtk;
using Application = Gtk.Application;
using Key = Gdk.Key;
using Rectangle = Geometry.Rectangle;
using Window = Gtk.Window;

namespace sensor_positioning
{
  public class Test
  {
    public static void Main()
    {
//      TestGeometryModule();
//      PsoTest.TestAll();
//      TestShadowModule();
//      SensorPositioningTest.TestAll();
      TestRunCharlie();
    }

    public static void TestRunCharlie()
    {
      Application.Init();
      var simulation = new RunCharlie();
      Application.Run();
    }
    
    public static void TestGeometryModule()
    {
      Vector2Test.TestGradient();
      CircleTest.TestExternalTangents();
      CircleTest.TestToPolygon();
      ArcTest.TestToPolygon();
      SegmentTest.TestIntersection();
      BoundsTest.TestOnBounds();
      BoundsTest.TestSegmentIntersection();
      VectorTest.TestAll();
    }
    
    public static void TestShadowModule()
    {
      ShadowsTest.TestUnseenArea();
      ShadowsTest.TestHiddenArea();
      ShadowsTest.TestCoreShadows();
    }
  }
  
  
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
    public readonly Window Window;
    
    public Plotter() : base("com.jakobrieke.plotter", ApplicationFlags.None)
    {
      Init();
      
      Window = new Window(WindowType.Toplevel)
      {
        WindowPosition = WindowPosition.Center,
        HeightRequest = 250,
        WidthRequest = 450,
        Title = ""
      };
      Window.Drawn += (o, args) => Render(args.Cr);
      Window.ShowAll();

      Window.KeyPressEvent += (o, args) =>
      {
        if (args.Event.Key == Key.a) XOffset += 10;
        else if (args.Event.Key == Key.d) XOffset -= 10;  
        else if (args.Event.Key == Key.w) YOffset += 10;  
        else if (args.Event.Key == Key.s) YOffset -= 10;  
        else if (args.Event.Key == Key.plus) Scale(1);
        else if (args.Event.Key == Key.minus) Scale(-1);
        Window.QueueDraw();
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
      Window.QueueDraw();
    }
    
    public void Plot(IGeometryObject o)
    {
      _elements.Add(o);
      Window.QueueDraw();
    }

    public void Plot(IEnumerable<IGeometryObject> objects)
    {
      _elements.AddRange(objects);
      Window.QueueDraw();
    }

    public void Plot()
    {
      Run();
      AddWindow(Window);
    }

    private void Render(Context cr)
    {
      _centerX = Window.AllocatedWidth / 2.0 + XOffset;
      _centerY = Window.AllocatedHeight / 2.0 + YOffset;
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
      cr.Rectangle(0, 0, Window.AllocatedWidth, Window.AllocatedHeight);
      cr.ClosePath();
      cr.Fill();
      cr.SetSourceRGBA(1, 1, 1, 0.7);
      cr.LineWidth = 0.5;
      cr.NewPath();
      cr.MoveTo(0, _centerY);
      cr.LineTo(Window.AllocatedWidth, _centerY);
      cr.ClosePath();
      cr.Stroke();
      cr.NewPath();
      cr.MoveTo(_centerX, 0);
      cr.LineTo(_centerX, Window.AllocatedHeight);
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
}