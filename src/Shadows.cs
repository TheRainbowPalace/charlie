using System;
using System.Collections.Generic;
using System.Linq;
using Geometry;
using Vector2 = Geometry.Vector2;


namespace Shadows
{
  public class Environment
  {
    public readonly Rectangle Bounds;
    public readonly List<Obstacle> Obstacles = new List<Obstacle>();

    public Environment(Rectangle bounds)
    {
      Bounds = bounds;
    }

    public Environment(double x, double y, double width, double height)
    {
      Bounds = new Rectangle(x, y, x + width, y + height);
    }

    public double Area()
    {
      return Bounds.Area();
    }
  }


  public class Obstacle
  {
    public Vector2 Position;
    public readonly double Size;

    public Obstacle(Vector2 position, double size)
    {
      Position = position;
      Size = size;
    }

    public Obstacle(double x, double y, double size)
    {
      Position = new Vector2(x, y);
      Size = size;
    }
  }


  public class Sensor : Obstacle
  {
    public double Rotation;
    public readonly double Range;
    public readonly double Fov;

    public Sensor(Vector2 position, double rotation, double range,
      double fov, double size) : base(position, size)
    {
      Range = range;
      Fov = fov;
      Rotation = rotation;
    }

    public Sensor(double x, double y, double rotation, double range,
      double fov, double size) : base(new Vector2(x, y), size)
    {
      Range = range;
      Fov = fov;
      Rotation = rotation;
    }

    /// <summary>
    /// Get the area monitored by the sensor.
    /// </summary>
    /// <returns></returns>
    public Arc AreaOfActivity()
    {
      return new Arc(Position, Range, Fov, Rotation - Fov / 2);
    }

    /// <summary>
    /// Calculate the shadows for this sensor inside an environment.
    /// </summary>
    /// <param name="env"></param>
    /// <returns></returns>
    public List<Polygon> Shadows(Environment env)
    {
      var obstacles = new List<Circle>();
      env.Obstacles.ForEach(o =>
      {
        if (o != this)
          obstacles.Add(
            new Circle(o.Position.X, o.Position.Y, o.Size));
      });

      return Shadows2D.Shadows(AreaOfActivity(), 
        obstacles, env.Bounds);
    }

    /// <summary>
    /// Calculate the shadows for a list of sensors in an environment.
    /// </summary>
    /// <param name="sensors"></param>
    /// <param name="env"></param>
    /// <returns></returns>
    public static List<Polygon> Shadows(List<Sensor> sensors,
      Environment env)
    {
      var shadows = sensors.Select(sensor => sensor.Shadows(env)).ToList();
      var result = shadows[0];
      for (var i = 1; i < shadows.Count; i++)
      {
        result = Polygon.Intersection(result, shadows[i]);
      }

      return result;
    }

    /// <summary>
    /// Calculate the shadow area for a list of sensors in an environment.
    /// </summary>
    /// <param name="sensors"></param>
    /// <param name="env"></param>
    /// <returns></returns>
    public static double ShadowArea(List<Sensor> sensors, Environment env)
    {
      return Polygon.Area(Shadows(sensors, env));
    }

    /// <summary>
    /// Calculate the shadow area for a list of shadows.
    /// </summary>
    /// <param name="shadows"></param>
    /// <returns></returns>
    public static double ShadowArea(List<Polygon> shadows)
    {
      return Polygon.Area(shadows);
    }

    public override string ToString()
    {
      return "Position: " + Position + ", Size: " + Size +
             ", Rotation: " + Rotation + ", Range: " + Range
             + ", Fov: " + Fov;
    }
  }


  public static class Shadows2D
  {
    private static bool Equals(double a, double b, double tolerance = 0.001f)
    {
      return Math.Abs(a - b) < tolerance;
    }


    public static Polygon UnseenArea(Arc sensor, Rectangle bounds)
    {
//      result.Add(result[0]);
      return Polygon.Difference(bounds.ToPolygon(), sensor.ToPolygon())[0];
    }

    /// <summary>
    /// Calculate the area that is hidden trough an obstacle. 
    /// </summary>
    /// <remarks>
    /// Sensor and obstacle must be inside bounds (including borders).
    /// </remarks>
    /// <param name="sensorPosition"></param>
    /// <param name="obstacle"></param>
    /// <param name="bounds"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static Polygon HiddenArea(
      Vector2 sensorPosition, Circle obstacle, Rectangle bounds)
    {
      var d = Vector2.Distance(sensorPosition, obstacle.Position);
      if (d < obstacle.Radius) return bounds.ToPolygon();

      // Todo: Find a better solution than this!
//		if (Equals(obstacle.Position.X, bounds.Min.X) ||
//		    Equals(obstacle.Position.Y, bounds.Min.Y) ||
//		    Equals(obstacle.Position.X, bounds.Max.X) ||
//		    Equals(obstacle.Position.Y, bounds.Max.Y)) 
//			return new Polygon();

      var tangs = Circle.Tangents(sensorPosition, obstacle);

      var s1 = new Segment(sensorPosition, tangs[0].End);
      var s2 = new Segment(sensorPosition, tangs[1].End);
      s1.Scale(12);
      s2.Scale(12);
      s1 = new Segment(tangs[0].End, s1.End);
      s2 = new Segment(tangs[1].End, s2.End);
      var i1 = Rectangle.Intersection(bounds, s1);
      var i2 = Rectangle.Intersection(bounds, s2);

      if (Equals(i1.X, i2.X) || Equals(i1.Y, i2.Y))
      {
        return new Polygon {tangs[0].End, i1, i2, tangs[1].End, tangs[0].End};
      }

      // Add border edge if necessary

      var minX = bounds.Min.X;
      var minY = bounds.Min.Y;
      var maxX = bounds.Max.X;
      var maxY = bounds.Max.Y;

      var x = Equals(i1.X, minX) || Equals(i2.X, minX) ? minX : maxX;
      var y = Equals(i1.Y, minY) || Equals(i2.Y, minY) ? minY : maxY;

      return new Polygon
      {
        tangs[0].End, i1, new Vector2(x, y), i2, tangs[1].End, tangs[0].End
      };
    }


    /// <summary>
    /// Calculate the area that can be overseen by one sensor.
    /// </summary>
    /// <param name="sensor"></param>
    /// <param name="obstacles"></param>
    /// <param name="bounds"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static List<Polygon> Shadows(
      Arc sensor, List<Circle> obstacles, Rectangle bounds)
    {
      if (!bounds.IsInside(sensor.Position))
        throw new ArgumentException("Sensor is outside bounds.");

      var polygons = new List<Polygon>();
      foreach (var obstacle in obstacles)
      {
        if (!bounds.IsInside(obstacle.Position))
          throw new ArgumentException("Obstacle is outside bounds.");

        polygons.Add(HiddenArea(sensor.Position, obstacle, bounds));
      }

      polygons.Add(UnseenArea(sensor, bounds));

      return Polygon.Union(polygons);
    }


    /// <summary>
    /// Calculate the area that can be overseen by multiple sensors.
    /// </summary>
    /// <param name="sensors"></param>
    /// <param name="obstacles"></param>
    /// <param name="bounds"></param>
    /// <returns></returns>
    public static List<List<Polygon>> Shadows(
      List<Arc> sensors, List<Circle> obstacles, Rectangle bounds)
    {
      var baseShadows = new List<List<Polygon>>();

      sensors.ForEach(light => baseShadows.Add(
        Shadows(light, obstacles, bounds)));

      return baseShadows;
    }


    /// <summary>
    /// Calculate the core shadow polygons for two shadow polygons.
    /// </summary>
    /// <param name="shadows1"></param>
    /// <param name="shadows2"></param>
    /// <returns></returns>
    public static List<Polygon> CoreShadows(
      List<Polygon> shadows1, List<Polygon> shadows2)
    {
      return Polygon.Intersection(shadows1, shadows2);
    }


    /// <summary>
    /// Calculate the core shadow for a list of shadow polygons.
    /// </summary>
    /// <param name="shadows"></param>
    /// <returns></returns>
    public static List<Polygon> CoreShadows(
      List<List<Polygon>> shadows)
    {
      var coreShadows = shadows[0];

      for (var i = 1; i < shadows.Count; i++)
      {
        coreShadows = CoreShadows(coreShadows, shadows[i]);
      }

      return coreShadows;
    }


    /// <summary>
    /// Calculate the the core shadows for a list of sensors. Core shadows
    /// means the area which is hidden for all sensors.
    /// </summary>
    /// <param name="sensors"></param>
    /// <param name="obstacles"></param>
    /// <param name="bounds"></param>
    /// <returns></returns>
    public static List<Polygon> CoreShadows(
      List<Arc> sensors, List<Circle> obstacles,
      Rectangle bounds)
    {
      return CoreShadows(Shadows(sensors, obstacles, bounds));
    }


    /// <summary>
    /// Calculate the area of shadow for a list of sensors.
    /// </summary>
    /// <param name="sensors"></param>
    /// <param name="obstacles"></param>
    /// <param name="bounds"></param>
    /// <returns></returns>
    public static double CoreShadowArea(
      List<Arc> sensors, List<Circle> obstacles, Rectangle bounds)
    {
      return Polygon.Area(CoreShadows(sensors, obstacles, bounds));
    }
  }
}