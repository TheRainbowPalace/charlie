using System;
using System.Collections.Generic;
using System.Linq;
using Geometry;


public static class Vector2Test
{
  public static void TestGradient()
  {
    Console.WriteLine("Test Vector2.Gradient()");

    var v1 = new Vector2(0, 0);
    var tests = new Dictionary<Vector2, double>
    {
      {new Vector2(0, 0), double.NaN},
      {new Vector2(1, 0), 0},
      {new Vector2(0, 1), 90},
      {new Vector2(-1, 0), 180},
      {new Vector2(0, -1), 270},
      {new Vector2(1, 1), 45},
      {new Vector2(-1, 1), 135},
      {new Vector2(-1, -1), 225},
      {new Vector2(1, -1), 315},
      {new Vector2(.5, Math.Sqrt(3) / 2), 60},
      {new Vector2(-.5, -Math.Sqrt(3) / 2), 240},
      {new Vector2(Math.Sqrt(3) / 2, -.5), 330},
    };

    foreach (var test in tests.Keys)
    {
      var r = Vector2.Gradient(v1, test);
      Console.WriteLine("Got: " + r + ", Expected: " + tests[test]);
    }
  }
}


public static class CircleTest
{
  public static void TestToPolygon()
  {
    Console.WriteLine("\nTest Circle.ToPolygon(): ");

    var p1 = new Circle(9, 6, 3).ToPolygon();
    var tests = new List<bool>
    {
      p1.Count == 5,
      p1[0] == new Vector2(12, 6),
      p1[1] == new Vector2(9, 9),
      p1[2] == new Vector2(6, 6),
      p1[3] == new Vector2(9, 3)
    };

    var p2 = new Circle(9, 6, 3).ToPolygon(2);
//        polygon.ForEach(vertex => Console.WriteLine(vertex));

    tests.Add(new Vector2(12, 6) == p2[0]);
    tests.Add(new Vector2(11.12132, 8.121321) == p2[1]);
    tests.Add(new Vector2(9, 9) == p2[2]);
    tests.Add(new Vector2(6.87868, 8.121321) == p2[3]);
    tests.Add(new Vector2(6, 6) == p2[4]);
    tests.Add(new Vector2(6.87868, 3.87868) == p2[5]);
    tests.Add(new Vector2(9, 3) == p2[6]);
    tests.Add(new Vector2(11.12132, 3.87868) == p2[7]);

    for (var i = 0; i < tests.Count; i++)
    {
      Console.WriteLine(i + ": " + tests[0]);
    }
  }

  public static void TestExternalTangents()
  {
    Console.WriteLine("\nTest Circle.ExternalTangents()");

    var s1 = new Vector2(0, 0);
    var s2 = new Vector2(.5, .6);
    var s3 = new Vector2(.5, .51);
    var o1 = new Circle(.5, .5, .1);
    var o2 = new Circle(-.3, -.3, .1);
    var o3 = new Circle(.5, 0, .1);
    var o4 = new Circle(-.15, 0, .1);

    var t1 = Circle.ExternalTangents(s1, o1);
    var t2 = Circle.ExternalTangents(s1, o2);
    var t3 = Circle.ExternalTangents(s2, o1);
    var t4 = Circle.ExternalTangents(s3, o1);
    var t5 = Circle.ExternalTangents(s1, o3);
    var t6 = Circle.ExternalTangents(s1, o4);

    var tests = new List<bool>
    {
      string.Join("; ", t1) == "0, 0; 0.42, 0.56; 0, 0; 0.56, 0.42",
      string.Join("; ", t2) == "0, 0; -0.214614906239706, " +
      "-0.352051760426961; 0, 0; -0.352051760426961, -0.214614906239706",
      string.Join("; ", t3) == "0.5, 0.6; 0.5, 0.6; 0.5, 0.6; 0.5, 0.6",
      string.Join("; ", t4) == "",
      string.Join("; ", t5) == "0, 0; 0.48, 0.0979795897113271; 0, 0; " +
      "0.48, -0.0979795897113271",
      string.Join("; ", t6) == "0, 0; -0.0833333333333333, " +
      "-0.074535599249993; 0, 0; -0.0833333333333333, 0.074535599249993"
    };

    for (var i = 0; i < tests.Count; i++)
    {
      Console.WriteLine(i + ": " + tests[i]);
    }

//        var plotter = new Plotter();
//        plotter.Plot(new Circle(s1, 0.02));
//        plotter.Plot(new Circle(s2, 0.02));
//        plotter.Plot(new Circle(s3, 0.02));
//        plotter.Plot(o1);
//        plotter.Plot(o2);
//        plotter.Plot(o3);
//        plotter.Plot(o4);
//        foreach (var s in t1) plotter.Plot(s);
//        foreach (var s in t2) plotter.Plot(s);
//        foreach (var s in t3) plotter.Plot(s);
//        foreach (var s in t4) plotter.Plot(s);
//        foreach (var s in t5) plotter.Plot(s);
//        foreach (var s in t6) plotter.Plot(s);
//        plotter.Plot();
  }
}


public static class ArcTest
{
  public static void TestToPolygon()
  {
    Console.WriteLine("\nTest Arc.ToPolygon(..)");
    var r1 = new Arc(0, 0, 1, 90).ToPolygon() == new Polygon
    {
      new Vector2(0, 0), new Vector2(1, 0),
      new Vector2(0.866025403784439, 0.5),
      new Vector2(0.5, 0.866025403784439),
      new Vector2(6.12303176911189E-17, 1)
    };
    Console.WriteLine("01: " + r1);

    // Todo: Fix bug when arc radius is >= 180 degrees
    var arc2 = new Arc(0, 0, 1, 190).ToPolygon(0);
    Console.WriteLine("02: " + false);

    var r3 = new Arc(-1, -1, 1, 90, 180).ToPolygon(4) == new Polygon
    {
      new Vector2(-1, -1), new Vector2(-2, -1),
      new Vector2(-1.95105651629515, -1.30901699437495),
      new Vector2(-1.80901699437495, -1.58778525229247),
      new Vector2(-1.58778525229247, -1.80901699437495),
      new Vector2(-1.30901699437495, -1.95105651629515),
      new Vector2(-1, -2)
    };
    Console.WriteLine("03: " + r3);

//      var plotter = new Plotter();
//      plotter.Plot(arc2);
//      plotter.Plot();
  }
}


public static class SegmentTest
{
  /**
   * Calculate the angle between two segments.
   */
//    public double Angle(Segment segment)
//    {
//        var m1 = (Start.Y - End.Y) / (Start.X - End.Y); 
//        var m2 = (Start.Y - End.Y) / (Start.X - End.Y);
//        
//        Console.WriteLine(m1 + " " + m2);
//        
//        return Math.Atan(Math.Abs((m1 - m2) / (1 + m1 * m2)));
//    }
//
//
//    public static void Main()
//    {
//        var angle = new Segment(0, 0, 1, 1).Angle(new Segment(0, 0, 1, 1));
//        Console.WriteLine(angle);
//        
//        angle = new Segment(0, 0, 1, 1).Angle(new Segment(0, 1, 1, 0));
//        Console.WriteLine(angle);
//        
//        angle = new Segment(0, 0, 1, 1).Angle(new Segment(0, 0, 1, 0));
//        Console.WriteLine(angle);
//    }

  public static void TestIntersection()
  {
    Console.WriteLine("\nTest Segment.Intersection()");

    var intersection = Segment.Intersection(
      new[] {new Vector2(0, 0), new Vector2(1, 1)},
      new[] {new Vector2(1, 0), new Vector2(0, 1)});

    Console.WriteLine("1" + intersection);

    intersection = Segment.Intersection(
      new[] {new Vector2(-3.3, 2.0), new Vector2(-8.5, 1.1)},
      new[] {new Vector2(-3.4, 2.3), new Vector2(-8.5, 1.2)});

    Console.WriteLine("1" + intersection);

    intersection = Segment.Intersection(
      new[] {new Vector2(-8.5, 1.1), new Vector2(-3.3, 2.0)},
      new[] {new Vector2(-8.5, 1.2), new Vector2(-3.4, 2.3)});

    Console.WriteLine("1" + intersection);

    intersection = Segment.Intersection(
      new[] {new Vector2(-3.3, 2.0), new Vector2(-8.5, 1.1)},
      new[] {new Vector2(-8.5, 1.2), new Vector2(-3.4, 2.3)});

    Console.WriteLine("1" + intersection);

    intersection = Segment.Intersection(
      new[] {new Vector2(0, 1.5), new Vector2(1.5, 0)},
      new[] {new Vector2(0, 0), new Vector2(1.5, 1.5)});

    Console.WriteLine("1" + intersection);

    intersection = Segment.Intersection(
      new[] {new Vector2(0, 1.5), new Vector2(1.5, 0)},
      new[] {new Vector2(0, 0), new Vector2(1.5, 1.5)});

    Console.WriteLine("1" + intersection);

    intersection = Segment.Intersection(
      new[] {new Vector2(1, 1), new Vector2(1.5, 1.5)},
      new[] {new Vector2(1, 1.5), new Vector2(1.5, 1)});

    Console.WriteLine("1" + intersection);

    intersection = Segment.Intersection(
      new[] {new Vector2(0, 0), new Vector2(1, 1)},
      new[] {new Vector2(1, 0), new Vector2(2, 1)});

    if (!double.IsInfinity(intersection.X)
        || !double.IsInfinity(intersection.X))
      return;

    intersection = Segment.Intersection(
      new[] {new Vector2(0, 0), new Vector2(1, 1)},
      new[] {new Vector2(0, 0), new Vector2(1, 1)});

    if (!double.IsInfinity(intersection.X)
        || !double.IsInfinity(intersection.X))
      return;

    return;
  }

  public static void TestScale()
  {
    Console.WriteLine("\nTest segment.Scale(): " + "No tests");
  }
}


public static class BoundsTest
{
  public static void TestOnBounds()
  {
    Console.WriteLine("\nTest Bounds.OnBounds()");

    var bounds = new Rectangle(Vector2.One * 2, Vector2.One);

    var onBounds = Rectangle.OnBounds(bounds,
                     new Vector2(bounds.Min.X, 10)) &&
                   !Rectangle.OnBounds(bounds,
                     new Vector2(bounds.Min.X + 0.1, 10));
    Console.WriteLine("01: " + onBounds);
  }

  public static void TestSegmentIntersection()
  {
    var tests = new List<bool>();

    Console.WriteLine("\nTest Bounds.Intersection()");
    var r1 = new Rectangle(-1, -1, 1, 1);
    var v1 = new Segment(0, 0, 2, 2);
    tests.Add(r1.Intersection(v1)[0].ToString() == "1, 1");

    var v2 = new Segment(0, 0, 0, 1);
    tests.Add(r1.Intersection(v2)[0].ToString() == "0, 1");

    var v3 = new Segment(0, 0, 0, 1.1);
    tests.Add(r1.Intersection(v3)[0].ToString() == "0, 1");

    var v4 = new Segment(0, 0.1, 0, -1.1);
    tests.Add(r1.Intersection(v4)[0].ToString() == "0, -1");

    var v5 = new Segment(.1, 0.1, 0, -1.1);
    tests.Add(r1.Intersection(v5)[0].ToString() == "0.00833333333333333, -1");

    var v6 = new Segment(.1, 0.1, 0, -.9);
    Console.WriteLine("Error: " + r1.Intersection(v6)[0]);

    var v7 = new Segment(-1.1, 0, -2.1, 0);
    Console.WriteLine("Error: " + r1.Intersection(v7)[0]);

    var v8 = new Segment(0, -1, 0, 1);
    Console.WriteLine("Error: " + r1.Intersection(v8)[0]);

    Console.WriteLine(string.Join("\n", tests));

//        var plotter = new Plotter();
//        plotter.Plot(r1);
//        plotter.Plot(v6);
//        plotter.Plot(new Circle(r1.Intersection(v6)[0], .02));
//        plotter.Plot();
  }
}


public static class VectorTest
{
  public static bool TestAdd()
  {
    var v1 = new[] {0.0, 0, 0};
    var v2 = new[] {1, 1.5, -10.1};

    return Vector.Add(v1, v2).SequenceEqual(
      new[] {1, 1.5, -10.1});
  }


  public static bool TestSubtract()
  {
    var v1 = new[] {0.0, 0, 0};
    var v2 = new[] {1, 1.5, -10.1};

    return Vector.Subtract(v1, v2).SequenceEqual(
      new[] {-1, -1.5, 10.1});
  }


  public static bool TestMultiply()
  {
    var v1 = new[] {-10, 0, 1.5};

    return Vector.Multiply(v1, 2).SequenceEqual(
      new[] {-20.0, 0, 3});
  }


  public static bool TestDivide()
  {
    var v1 = new[] {.75, -0.375, 1.5};

    return Vector.Divide(v1, 1.5).SequenceEqual(
      new[] {0.5, -0.25, 1});
  }


  public static void TestAll()
  {
    Console.WriteLine("\nTest Vector");
    Console.WriteLine("Add: " + TestAdd());
    Console.WriteLine("Subtract: " + TestSubtract());
    Console.WriteLine("Multiply: " + TestMultiply());
    Console.WriteLine("Divide: " + TestDivide());
  }
}