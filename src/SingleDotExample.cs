using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Cairo;
using Geometry;
using sensor_positioning;

namespace run_charlie
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
  
  public class SingleDotExample
  {
    private static ConcurrentDictionary<string, object> objects = 
      new ConcurrentDictionary<string, object>();
    
    public static string Title = "Single dot";
    public static string Descr = 
      "Single dot is an example simulation to preview and test the " +
      "RunCharlie software. It presents you with a single red dot that " +
      "increases and decreases in size.";
    
    public static void Init(Dictionary<string, string> config)
    {
      objects.Clear();
      objects.TryAdd("charlie", new Charlie {X = 200, Y = 200});
      objects.TryAdd("field", new Field(400, 400));
    }

    public static void Update(long dt)
    {
      if (objects.TryGetValue("charlie", out var obj))
      {
        ((Charlie) obj).Update(dt);
      }

      Thread.Sleep(20);
    }

    public static void Render(Context ctx)
    {
      if (objects.TryGetValue("field", out var obj2))
        ((RenderObject) obj2).Render(ctx);
      if (objects.TryGetValue("charlie", out var obj1))
        ((RenderObject) obj1).Render(ctx);
    }
  }
}