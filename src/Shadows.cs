using System;
using System.Collections.Generic;
using System.Linq;
using Geometry;
using Vector2 = Geometry.Vector2;


namespace Shadows
{
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
        return new List<Polygon> {bounds.ToPolygon()};

      var polygons = new List<Polygon>();
      foreach (var obstacle in obstacles)
      {
        if (!bounds.Contains(obstacle.Position)) continue;

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