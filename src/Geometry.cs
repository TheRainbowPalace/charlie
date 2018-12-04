using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;
using ClipperLib;

namespace Geometry
{
  public static class Defs
  {
    public static double Precision = 1E-14;
  }
  
  
  public interface IGeometryObject {}
  
  
  public class Vector : IGeometryObject 
  {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="vector"></param>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double[] Multiply(double[] vector, double x)
    {
      var result = new double[vector.Length];
      for (var i = 0; i < result.Length; i++) result[i] = vector[i] * x;
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="vector"></param>
    /// <param name="x"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static double[] Divide(double[] vector, double x)
    {
      if (x.Equals(0f))
      {
        throw new ArgumentException("Can't divide through zero.");
      }

      var result = new double[vector.Length];
      for (var i = 0; i < result.Length; i++) result[i] = vector[i] / x;
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="vector1"></param>
    /// <param name="vector2"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static double[] Add(double[] vector1, double[] vector2)
    {
      if (vector1.Length != vector2.Length)
      {
        throw new ArgumentException(
          "Length of vector 1 is not equal to length of vector 2");
      }

      var result = new double[vector1.Length];
      for (var i = 0; i < result.Length; i++)
      {
        result[i] = vector1[i] + vector2[i];
      }

      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="vector1"></param>
    /// <param name="vector2"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static double[] Subtract(double[] vector1, double[] vector2)
    {
      if (vector1.Length != vector2.Length)
      {
        throw new ArgumentException(
          "Length of vector 1 is not equal to length of vector 2");
      }

      var result = new double[vector1.Length];
      for (var i = 0; i < result.Length; i++)
      {
        result[i] = vector1[i] - vector2[i];
      }

      return result;
    }
  }
  
  
  public struct Vector2 
  {
    public readonly double X;
    public readonly double Y;
    public static readonly Vector2 Zero = new Vector2(0f, 0f);
    public static readonly Vector2 One = new Vector2(1f, 1f);


    public Vector2(double x, double y)
    {
      X = x;
      Y = y;
    }

    public static Vector2 operator *(Vector2 v, double factor)
    {
      return new Vector2(v.X * factor, v.Y * factor);
    }

    public static Vector2 operator +(Vector2 v1, Vector2 v2)
    {
      return new Vector2(v1.X + v2.X, v1.Y + v2.Y);
    }

    public static bool operator ==(Vector2 v1, Vector2 v2)
    {
      return v1.X - v2.X <= Defs.Precision && v1.Y - v2.Y <= Defs.Precision;
    }

    public static bool operator !=(Vector2 v1, Vector2 v2)
    {
      return !(v1 == v2);
    }

    public override string ToString()
    {
      return X + ", " + Y;
    }

    public override bool Equals(object obj)
    {
      return base.Equals(obj);
    }

    public override int GetHashCode()
    {
      return base.GetHashCode();
    }

    public static double Distance(Vector2 a, Vector2 b)
    {
      return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
    }

    public static double Gradient(Vector2 a, Vector2 b)
    {
      if (b.Y < a.Y)
      {
        return Math.Asin((b.X - a.X) / Distance(a, b)) * 180 / Math.PI + 270;
      }
      return Math.Asin((a.X - b.X) / Distance(a, b)) * 180 / Math.PI + 90;
    }

    public IntPoint ToIntPoint()
    {
      return new IntPoint(
        Math.Round(X / Defs.Precision), 
        Math.Round(Y / Defs.Precision));
    }

    public static Vector2 FromIntPoint(IntPoint point)
    {
      return new Vector2(point.X * Defs.Precision, point.Y * Defs.Precision);
    }

    /// <summary>
    /// Move the vector by a given distance and angle where the angle is
    /// measured anticlockwise from east.
    /// </summary>
    /// <param name="angle"></param>
    /// <param name="distance"></param>
    /// <param name="useDegrees"></param>
    /// <returns></returns>
    public Vector2 Move(double angle, double distance, bool useDegrees = true)
    {
      if (useDegrees) angle *= Math.PI / 180;

      return new Vector2(
        Math.Cos(angle) * distance + X,
        Math.Sin(angle) * distance + Y);
    }
  }

  
  public struct Segment : IGeometryObject 
  {
    public Vector2 Start;
    public Vector2 End;

    public Segment(Vector2 start, Vector2 end)
    {
      Start = start;
      End = end;
    }

    public Segment(double x1, double y1, double x2, double y2)
    {
      Start = new Vector2(x1, y1);
      End = new Vector2(x2, y2);
    }

    public void Scale(double value)
    {
      var d = Length();
      End = new Vector2(
        End.X + (End.X - Start.X) / d * value,
        End.Y + (End.Y - Start.Y) / d * value);
    }

    public void Inverse()
    {
      var buffer = End;
      End = Start;
      Start = buffer;
    }

    public double Length()
    {
      return Vector2.Distance(Start, End);
    }

    public static bool operator ==(Segment s1, Segment s2)
    {
      return s1.Start == s2.Start && s1.End == s2.End;
    }

    public static bool operator !=(Segment s1, Segment s2)
    {
      return !(s1 == s2);
    }

    public override string ToString()
    {
      return Start + "; " + End;
    }

    /**
     * Calculate the intersection point between two line segments.
     * 
     * Returns (Infinity, Infinity) if the lines do not intersect or
     * if the lines intersect in every point.
     */
    public static Vector2 Intersection(Vector2[] line1, Vector2[] line2)
    {
      var x1 = line1[0].X;
      var y1 = line1[0].Y;
      var x2 = line1[1].X;
      var y2 = line1[1].Y;

      var x3 = line2[0].X;
      var y3 = line2[0].Y;
      var x4 = line2[1].X;
      var y4 = line2[1].Y;

      var c = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);

      if (Math.Abs(c) < 0.01)
      {
        return new Vector2(double.PositiveInfinity,
          double.PositiveInfinity);
      }

      var a = x1 * y2 - y1 * x2;
      var b = x3 * y4 - y3 * x4;

      var x = (a * (x3 - x4) - b * (x1 - x2)) / c;
      var y = (a * (y3 - y4) - b * (y1 - y2)) / c;

      return new Vector2(x, y);
    }
  }
  
  
  public class Polygon : List<Vector2>, IGeometryObject 
  {
    public Polygon() {}

    public Polygon(IEnumerable<Vector2> collection) : base(collection) {}

    public Polygon(int capacity) : base(capacity) {}

    
    public static bool operator ==(Polygon p1, Polygon p2)
    {
      if (p1 is null && p2 is null) return true;
      if (p1 is null || p2 is null) return false;
      if (p1.Count != p2.Count) return false;
      return !p1.Where((t, i) => t != p2[i]).Any();
    }

    public static bool operator !=(Polygon p1, Polygon p2)
    {
      return !(p1 == p2);
    }

    public override string ToString()
    {
      return string.Join("; ", this);
    }

    public override bool Equals(object obj)
    {
      return base.Equals(obj);
    }

    public override int GetHashCode()
    {
      return base.GetHashCode();
    }
  
    /// <summary>
    /// Calculate the area of any polygon (Convex, Concave, Self-Intersecting).
    /// </summary>
    /// <param name="polygon"></param>
    /// <returns>
    /// 0 if the polygon is invalid (consists of lesser than 3 points).
    /// </returns>
    public static double Area(Polygon polygon)
    {
      if (polygon.Count < 3) return 0;

      if (polygon[0] != polygon[polygon.Count - 1])
      {
        polygon.Add(polygon[0]);
      }

      var result = 0.0;

      for (var i = 0; i < polygon.Count - 1; i++)
      {
        result += polygon[i].X * polygon[i + 1].Y -
                  polygon[i + 1].X * polygon[i].Y;
      }

      return Math.Abs(result / 2);
    }

    /// <summary>
    /// Calculate the union of a list of any type of polygon.
    /// </summary>
    /// <param name="polygons"></param>
    /// <param name="closed"></param>
    /// <returns></returns>
    public static List<Polygon> Union(
      List<Polygon> polygons, bool closed = true)
    {
      var clipper = new Clipper();

      foreach (var polygon in polygons)
      {
        var shadowPath = ToIntPoint(polygon);

        clipper.AddPath(shadowPath, PolyType.ptSubject, closed);
      }

      var solution = new List<List<IntPoint>>();

      clipper.Execute(ClipType.ctUnion, solution,
        PolyFillType.pftPositive, PolyFillType.pftPositive);

      var result = new List<Polygon>();

      solution.ForEach(polygon =>
      {
        var shadowPolygon = FromIntPoint(polygon);
        shadowPolygon.Add(shadowPolygon[0]);
        result.Add(shadowPolygon);
      });

      return result;
    }

    /// <summary>
    /// Calculate the intersection between two polygons of any type.
    /// </summary>
    /// <param name="polygon1"></param>
    /// <param name="polygon2"></param>
    /// <param name="closed"></param>
    /// <returns></returns>
    public static List<Polygon> Intersection(
      Polygon polygon1,
      Polygon polygon2, bool closed = true)
    {
      var clipper = new Clipper();

      clipper.AddPath(ToIntPoint(polygon1),
        PolyType.ptSubject, closed);

      clipper.AddPath(ToIntPoint(polygon2),
        PolyType.ptClip, closed);

      var solution = new List<List<IntPoint>>();

      clipper.Execute(ClipType.ctIntersection, solution,
        PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);

      var result = new List<Polygon>();
      foreach (var path in solution)
      {
        var polygon = FromIntPoint(path);
        polygon.Add(polygon[0]);
        result.Add(polygon);
      }

      return result;
    }

    /// <summary>
    /// Calculate the intersection of two lists of polygons.
    /// </summary>
    /// <param name="polygons1"></param>
    /// <param name="polygons2"></param>
    /// <param name="closed"></param>
    /// <returns></returns>
    public static List<Polygon> Intersection(
      List<Polygon> polygons1,
      List<Polygon> polygons2, bool closed = true)
    {
      var clipper = new Clipper();

      foreach (var shadow in polygons1)
      {
        clipper.AddPath(ToIntPoint(shadow),
          PolyType.ptSubject, closed);
      }

      foreach (var shadow in polygons2)
      {
        clipper.AddPath(ToIntPoint(shadow),
          PolyType.ptClip, closed);
      }

      var solution = new List<List<IntPoint>>();

      clipper.Execute(ClipType.ctIntersection, solution,
        PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);

      var result = new List<Polygon>();
      foreach (var path in solution)
      {
        var polygon = FromIntPoint(path);
        polygon.Add(polygon[0]);
        result.Add(polygon);
      }

      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="polygon1"></param>
    /// <param name="polygon2"></param>
    /// <returns></returns>
    public static List<Polygon> Difference(Polygon polygon1,
      Polygon polygon2)
    {
      var clipper = new Clipper();
      clipper.AddPath(ToIntPoint(polygon1),
        PolyType.ptSubject, true);
      clipper.AddPath(ToIntPoint(polygon2),
        PolyType.ptClip, true);

      var solution = new List<List<IntPoint>>();
      clipper.Execute(ClipType.ctDifference, solution,
        PolyFillType.pftNonZero);

      return solution.Select(FromIntPoint).ToList();
    }


    /// <summary>
    /// Calculate the area of a list of polygons.
    /// </summary>
    /// <param name="polygons"></param>
    /// <returns></returns>
    public static double Area(List<Polygon> polygons)
    {
      return polygons.Sum(polygon => Area(polygon));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="polygon"></param>
    /// <returns></returns>
    public static List<IntPoint> ToIntPoint(Polygon polygon)
    {
      return polygon.Select(vertex => vertex.ToIntPoint()).ToList();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="polygon"></param>
    /// <returns></returns>
    public static Polygon FromIntPoint(List<IntPoint> polygon)
    {
      return new Polygon(polygon.Select(vertex => 
        Vector2.FromIntPoint(vertex)).ToList());
    }
  }
  
  
  public struct Rectangle : IGeometryObject 
  {
    public Vector2 Min;
    public Vector2 Max;

    public Rectangle(Vector2 min, Vector2 max)
    {
      Min = min;
      Max = max;
    }

    public Rectangle(double minX, double minY, double maxX, double maxY)
    {
      Min = new Vector2(minX, minY);
      Max = new Vector2(maxX, maxY);
    }

    public override string ToString()
    {
      return Min + "; " + Max;
    }

    public double Height()
    {
      return Max.Y - Min.Y;
    }

    public double Width()
    {
      return Max.X - Min.X;
    }

    public double Area()
    {
      return Height() * Width();
    }

    public double Diagonal()
    {
      return Vector2.Distance(Min, Max);
    }
    
    /**
     * Test if a point is inside the bounds.
     */
    public bool Contains(Vector2 point, bool excludeBorders = false)
    {
      if (excludeBorders)
      {
        return point.X > Min.X && point.Y > Min.Y &&
               point.X < Max.X && point.Y < Max.Y;
      }

      return point.X >= Min.X && point.Y >= Min.Y &&
             point.X <= Max.X && point.Y <= Max.Y;
    }

    /**
     * Calculate the intersection point between the rectangle
     * and a line segment.
     */
    public Vector2[] Intersection(Segment segment)
    {
      var x1 = segment.Start.X;
      var y1 = segment.Start.Y;

      var x2 = segment.End.X;
      var y2 = segment.End.Y;

      var values = new[]
      {
        (Min.X - x1) / (x2 - x1),
        (Min.Y - y1) / (y2 - y1),
        (Max.X - x1) / (x2 - x1),
        (Max.Y - y1) / (y2 - y1)
      };

      var λ = values[0];
      foreach (var value in values)
      {
        λ = λ > value && value >= 0 ? value : λ;

        if (λ < 0) λ = value;
      }

      if (λ < 0) return new Vector2[0];

      var x = x1 + λ * (x2 - x1);
      var y = y1 + λ * (y2 - y1);

      return new []{new Vector2(x, y)};
    }

    /**
     * Test if a point lays on an rectangle.
     */
    public static bool OnBounds(Rectangle bounds, Vector2 point)
    {
      return bounds.Min.X.Equals(point.X) ||
             bounds.Min.Y.Equals(point.X) ||
             bounds.Max.X.Equals(point.X) ||
             bounds.Max.Y.Equals(point.X);
    }

    public Polygon ToPolygon()
    {
      return new Polygon
      {
        Min, new Vector2(Max.X, Min.Y), Max, new Vector2(Min.X, Max.Y)
      };
    }
  }


  public struct Circle : IGeometryObject
  {
    public Vector2 Position;
    public readonly double Radius;

    public Circle(double x, double y, double radius) : 
      this(new Vector2(x, y), radius) {}

    public Circle(Vector2 position, double radius)
    {
      if (radius <= 0) throw new ArgumentException("Radius must be greater 0");
      Position = position;
      Radius = radius;
    }

    public override string ToString()
    {
      return Position + ", " + Radius;
    }

    /// <summary>
    /// Check if a point lies on the border of the circle. 
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public bool OnBorder(Vector2 p)
    {
      return Math.Abs(Vector2.Distance(p, Position) - Radius) < Defs.Precision;
    }

    /// <summary>
    /// Check if a point lies inside the circle. This does not include the
    /// border of the circle.
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public bool Contains(Vector2 p)
    {
      return Vector2.Distance(p, Position) < Radius - Defs.Precision;
    }

    /// <summary>
    /// Calculate the tangents of a point and a circle.
    /// </summary>
    /// <remarks>
    /// For more information see:
    /// https://en.wikipedia.org/wiki/Tangent_lines_to_circles
    /// </remarks>
    /// <param name="p"></param>
    /// <param name="c"></param>
    /// <returns>
    /// An array of two segments or an empty array if the point lies inside
    /// the circle.
    /// </returns>
    public static Segment[] ExternalTangents(Vector2 p, Circle c)
    {
      if (c.OnBorder(p))
      {
        return new[] {new Segment(p, p), new Segment(p, p)};
      }
      if (c.Contains(p)) return new Segment[]{};

      var tangent1 = ExternalTangent(c.Position.X, c.Position.Y, 
        c.Radius, p.X, p.Y, 0);
      var tangent2 = ExternalTangent(p.X, p.Y, 0, 
        c.Position.X, c.Position.Y, c.Radius);
      tangent1.Inverse();
      
      return new[] {tangent1, tangent2};
    }

    /// <summary>
    /// Calculate one outer tangent of two circles given by their position and
    /// radius.
    /// </summary>
    /// <param name="x1"></param>
    /// <param name="y1"></param>
    /// <param name="r1"></param>
    /// <param name="x2"></param>
    /// <param name="y2"></param>
    /// <param name="r2"></param>
    /// <returns></returns>
    private static Segment ExternalTangent(double x1, double y1, double r1,
      double x2, double y2, double r2)
    {
      var d = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
      var α = Math.PI / 2  
              + Math.Atan2(y2 - y1, x2 - x1) - Math.Asin((r2 - r1) / d);

      return new Segment(
        new Vector2(x1 - r1 * Math.Cos(α), y1 - r1 * Math.Sin(α)), 
        new Vector2(x2 - r2 * Math.Cos(α), y2 - r2 * Math.Sin(α)));
    }


    /// <summary>
    /// Convert the circle to a polygon.
    /// </summary>
    /// <param name="precision"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public Polygon ToPolygon(int precision = 1, double offset = 0)
    {
      if (precision < 1)
        throw new ArgumentException(
          "Precision can't be smaller than 1");

      offset %= 360;

      var result = new Polygon();
      for (var i = 0; i < precision * 4; i++)
      {
        var angle = i * 360f / (precision * 4) + offset;
        result.Add(Position.Move(angle, Radius));
      }

      result.Add(result[0]);

      return result;
    }
  }


  public class Arc : IGeometryObject 
  {
    public Vector2 Position;
    public readonly double Radius;
    public readonly double Angle;
    public readonly double Rotation;


    public Arc(Vector2 position, double radius, double angle,
      double rotation = 0)
    {
      Position = position;
      Radius = radius;
      Angle = angle % 360;
      Rotation = rotation;
    }

    public Arc(double x, double y, double radius,
      double angle, double rotation = 0)
    {
      Position = new Vector2(x, y);
      Radius = radius;
      Angle = angle % 360;
      Rotation = rotation % 360;
    }


    public override string ToString()
    {
      return Position + "; " + Angle + "; " + Rotation + "; " + Radius;
    }

    /// <summary>
    /// Return a polygon representation of the arc.
    /// </summary>
    /// <param name="precision">
    /// The precision for the polygon representation, needs to be greater than
    /// 1. A higher number means a higher precision. Defaults to 1.
    /// </param>
    /// <param name="noCenter">
    /// Defines if the center point is included. If the arc has an angle of
    /// 360 degrees the center is always excluded. Defaults to false.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public Polygon ToPolygon(int precision = 2, bool noCenter = false)
    {
      if (precision < 0)
        throw new ArgumentException(
          "Precision can't be smaller than 0");
      precision += 1;

      var result = new Polygon();
      if (!noCenter && Angle < 360) result.Add(Position);

      for (var i = 0; i < precision + 1; i++)
      {
        var angle = i * Angle / precision + Rotation;
        result.Add(Position.Move(angle, Radius));
      }

      return result;
    }
  }
}