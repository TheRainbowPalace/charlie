using System;
using System.Linq;
using Geometry;


public static class CircleTest
{
    public static void TestToPolygon()
    {
        var polygon = new Circle(9, 6, 3).ToPolygon();
        
        Console.WriteLine(polygon.Count == 5);
        Console.WriteLine(polygon[0] == new Vector2(12, 6));
        Console.WriteLine(polygon[1] == new Vector2(9, 9));
        Console.WriteLine(polygon[2] == new Vector2(6, 6));
        Console.WriteLine(polygon[3] == new Vector2(9, 3));

        polygon = new Circle(9, 6, 3).ToPolygon(2);
        
        polygon.ForEach(vertex => Console.WriteLine(vertex));
        
        Console.WriteLine(new Vector2(12, 6) == polygon[0]);
        Console.WriteLine(new Vector2(11.12132, 8.121321) == polygon[1]);
        Console.WriteLine(new Vector2(9, 9) == polygon[2]);
        Console.WriteLine(new Vector2(6.87868, 8.121321) == polygon[3]);
        Console.WriteLine(new Vector2(6, 6) == polygon[4]);
        Console.WriteLine(new Vector2(6.87868, 3.87868) == polygon[5]);
        Console.WriteLine(new Vector2(9, 3) == polygon[6]);
        Console.WriteLine(new Vector2(11.12132, 3.87868) == polygon[7]);
    }
    
//    public static Tuple<Vector2, Vector2> Intersection(Circle circle, 
//        Segment segment)
//    {
// 
//        ∂ = asin(
//        var ∂ = Math.Asin()
//        return null;
//    }
}


public static class ArcTest
{
    public static void TestToPolygon()
    {
        var x = new Arc(9, 6, 3, 45).ToPolygon();
        var y = new Arc(9, 6, 3, 360).ToPolygon(10);
        var z = new Arc(9, 6, 3, 187).ToPolygon(10);
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
    
    public static bool TestIntersection()
    {
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
            || !double.IsInfinity(intersection.X)) return false;

        intersection = Segment.Intersection(
            new[] {new Vector2(0, 0), new Vector2(1, 1)},
            new[] {new Vector2(0, 0), new Vector2(1, 1)});

        if (!double.IsInfinity(intersection.X)
            || !double.IsInfinity(intersection.X)) return false;

        return true;
    }
}


public static class BoundsTest
{
    public static bool TestIntersection()
    {
        var bounds = new Rectangle(Vector2.One * 2, Vector2.One);

        return Rectangle.OnBounds(bounds,
                   new Vector2(bounds.Min.X, 10)) &&
               !Rectangle.OnBounds(bounds,
                   new Vector2(bounds.Min.X + 0.1, 10));
    }
}


public static class VectorTest
{
    private static bool TestAdd()
    {
        var v1 = new[] {0.0, 0, 0};
        var v2 = new[] {1, 1.5, -10.1};

        return Vector.Add(v1, v2).SequenceEqual(
            new[] {1, 1.5, -10.1});
    }


    private static bool TestSubtract()
    {
        var v1 = new[] {0.0, 0, 0};
        var v2 = new[] {1, 1.5, -10.1};

        return Vector.Subtract(v1, v2).SequenceEqual(
            new[] {-1, -1.5, 10.1});
    }


    private static bool TestMultiply()
    {
        var v1 = new[] {-10, 0, 1.5};

        return Vector.Multiply(v1, 2).SequenceEqual(
            new[] {-20.0, 0, 3});
    }


    private static bool TestDivide()
    {
        var v1 = new[] {.75, -0.375, 1.5};

        return Vector.Divide(v1, 1.5).SequenceEqual(
            new[] {0.5, -0.25, 1});
    }

    public static void TestAll()
    {
        Console.WriteLine(
            TestAdd() + " " +
            TestSubtract() + " " +
            TestMultiply() + " " +
            TestDivide());
    }
}