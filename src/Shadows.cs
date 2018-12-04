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
    public static List<Polygon> UnseenArea(Arc sensor, Rectangle bounds)
    {
      return Polygon.Difference(bounds.ToPolygon(), sensor.ToPolygon());
    }

    /// <summary>
    /// Calculate the area that is hidden trough an obstacle.
    /// The area will be a quadrilateral.
    /// </summary>
    /// <param name="sensorPosition"></param>
    /// <param name="obstacle"></param>
    /// <param name="bounds"></param>
    /// <returns>
    /// The bounds if the sensor is positioned inside the obstacle or the
    /// sensor is outside the bounds.
    /// A quadrilateral representing the shadow of the obstacle. The shadow has
    /// the same length as the bounds diagonal.
    /// </returns>
    public static Polygon HiddenArea(
      Vector2 sensorPosition, Circle obstacle, Rectangle bounds)
    {
      if (bounds.Contains(sensorPosition) == false) return bounds.ToPolygon(); 
      if (obstacle.OnBorder(sensorPosition))
      {
        throw new NotImplementedException(
          "Case sensor on obstacle border not implemented");
      }

      var d = bounds.Diagonal();
      var tangs = Circle.ExternalTangents(sensorPosition, obstacle);
      if (tangs.Length == 0) return bounds.ToPolygon();
      
      var s1 = new Segment(sensorPosition, tangs[0].End);
      var s2 = new Segment(sensorPosition, tangs[1].End);
      s1.Scale(d);
      s2.Scale(d);
      s1 = new Segment(tangs[0].End, s1.End);
      s2 = new Segment(tangs[1].End, s2.End);

      var angle = Vector2.Gradient(sensorPosition, obstacle.Position);
      var result = new Polygon {
        s1.Start, s1.End,
        s1.End.Move(angle, d), s2.End.Move(angle, d),
        s2.End, s2.Start};
      result = Polygon.Intersection(bounds.ToPolygon(), result)[0];
      return result;
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
      if (!bounds.Contains(sensor.Position))
        throw new ArgumentException("Sensor is outside bounds.");

      var polygons = new List<Polygon>();
      foreach (var obstacle in obstacles)
      {
        if (!bounds.Contains(obstacle.Position))
          throw new ArgumentException("Obstacle is outside bounds.");

        polygons.Add(HiddenArea(sensor.Position, obstacle, bounds));
      }

      polygons.AddRange(UnseenArea(sensor, bounds));

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