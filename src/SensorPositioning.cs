using System;
using System.Collections.Generic;
using Geometry;
using ParticleSwarmOptimization;
using Shadows;
using Vector2 = Geometry.Vector2;
using Environment = Shadows.Environment;


public class SensorPositioningProblem
{
    /// <summary>The problem environment.</summary>
    public Environment Env;
    /// <summary>A list of sensors.</summary>
    public List<Sensor> TeamA;
    /// <summary>A list of obstacles</summary>
    public List<Sensor> TeamB;

    
    public SensorPositioningProblem(
        int sizeTeamA = 1,
        int sizeTeamB = 1,
        double fieldWidth = 9f,
        double fieldHeight = 6f,
        double playerSensorRange = 12,
        double playerSensorFov = 56.3f,
        double playerSize = 0.1555f
        )
    {
        var field = new Rectangle(0, 0, fieldWidth, fieldHeight);
        Env = new Environment(field);
        
        TeamA = new List<Sensor>();
        TeamB = new List<Sensor>();

        for (var i = 0; i < sizeTeamA; i++)
        {
            var s = new Sensor(0, 0, 0, playerSensorRange, 
                playerSensorFov, playerSize);
            
            PlaceWithoutCollision(s);
            TeamA.Add(s);
            Env.Obstacles.Add(s);
        }
        
        for (var i = 0; i < sizeTeamB; i++)
        {
            var s = new Sensor(0, 0, 0, playerSensorRange, 
                playerSensorFov, playerSize);

            PlaceWithoutCollision(s);
            TeamB.Add(s);
            Env.Obstacles.Add(s);
        }
    }

    /// <summary>
    /// Place an object randomly without collision inside the problem
    /// environment.
    /// </summary>
    /// <remarks>
    /// Warning, this function is not deterministic and might not
    /// terminate if Env.Field is to small.
    /// The object can also be placed on the edges of the field.
    /// </remarks>
    /// <param name="o">The object to place.</param>
    public void PlaceWithoutCollision(Obstacle o)
    {
        var collided = true;
        while (collided)
        {
            PlaceRandom(o);
            collided = CheckCollision(o);
        }
    }

    /// <summary>
    /// Place an object randomly inside the problem environment. This might
    /// Cause a collision.
    /// </summary>
    /// <remarks>
    /// The object can also be placed on the edges of the field.
    /// </remarks>
    /// <param name="o"></param>
    public void PlaceRandom(Obstacle o)
    {
        var x = Pso.UniformRand(Env.Bounds.Min.X,
            Env.Bounds.Max.X);
        var y = Pso.UniformRand(Env.Bounds.Min.Y,
            Env.Bounds.Max.Y);
        o.Position = new Vector2(x, y);
    }
    
    /// <summary>
    /// Check if a given object collides with any object inside the
    /// problem environment. If the object already exists inside the environment
    /// it won't collide with itself.
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    public bool CheckCollision(Obstacle o)
    {
        foreach (var o2 in Env.Obstacles)
        {
            if (o == o2) continue;

            var d = Vector2.Distance(o2.Position, o.Position);
            if (d < o2.Size + o.Size) return true;
        }

        return false;
    }

    /// <summary>
    /// Place a list of sensors according 
    /// </summary>
    /// <remarks>
    /// This will only change the sensors position and rotation, sensors
    /// won't be added to the environment.
    /// </remarks>
    /// <param name="vector"></param>
    /// <param name="sensors"></param>
    public static void PlaceFromVector(double[] vector, List<Sensor> sensors)
    {
        for (var i = 0; i < vector.Length; i += 3)
        {
            sensors[i / 3].Position = new Vector2(vector[i], vector[i + 1]);
            sensors[i / 3].Rotation = vector[i + 2];
        }
    }
    
    /// <summary></summary>
    /// <param name="value"></param>
    /// <param name="round"></param>
    /// <returns></returns>
    public double Normalize(double value, bool round = true)
    {
        if (round) return Math.Round(value / Env.Area() * 100, 2);
        return value / Env.Area() * 100;
    }
    
    /// <summary>
    /// Get the search space of the problem instance.
    /// </summary>
    /// <returns></returns>
    public double[][] Intervals()
    {
        var region = Env.Bounds;
        var intervals = new double[TeamA.Count * 3][];
        for (var i = 0; i < intervals.Length; i += 3)
        {
            intervals[i] = new[] {region.Min.X, region.Max.X};
            intervals[i + 1] = new[] {region.Min.Y, region.Max.Y};
            intervals[i + 2] = new[] {0, 360.0};
        }
        return intervals;
    }
}


/*
 Tests
 =====
 
 ## 01
 Team 01 - 1 player
 Team 02 - 0 players 
 
 Team 01 is updated to find the maximum area of sight.
 
 ## 02
 Team 01 - 1 player
 Team 02 - 1 player
 
 Team 01 is updated to find the maximum area of sight.
 Team 02 is randomly initialized on the field and does not move.
 
 ## 03 - 08
 Team 01 - 1, 2, ..., 6 players
 Team 02 - 5 players
 
 Team 01 is updated to find the maximum area of sight.
 Team 02 is randomly initialized on the field and does not move.

 */

public class StaticSensorPositioning : SensorPositioningProblem
{
    public StaticSensorPositioning(
        int sizeTeamA = 1, 
        int sizeTeamB = 1, 
        double fieldWidth = 9, 
        double fieldHeight = 6, 
        double playerSensorRange = 12, 
        double playerSensorFov = 56.3, 
        double playerSize = 0.1555
    ) : base(sizeTeamA, sizeTeamB, fieldWidth, fieldHeight, 
        playerSensorRange, playerSensorFov, playerSize) {}

    
    public double FitnessFct(double[] vector)
    {
        PlaceFromVector(vector, TeamA);
        foreach (var p in TeamA)
        {
            if (!CheckCollision(p)) continue;
            Collisions++;
            return double.PositiveInfinity;
        }
        return Sensor.ShadowArea(TeamA, Env);
    }

    public int Collisions { get; private set; }
}


public class DynamicSensorPositioning : SensorPositioningProblem
{
    public DynamicSensorPositioning(
        int sizeTeamA = 1, 
        int sizeTeamB = 1, 
        double fieldWidth = 9, 
        double fieldHeight = 6, 
        double playerSensorRange = 12, 
        double playerSensorFov = 56.3, 
        double playerSize = 0.1555
    ) : base(sizeTeamA, sizeTeamB, fieldWidth, fieldHeight, 
        playerSensorRange, playerSensorFov, playerSize) {}
}


//    public static void Main()
//    {
//        Console.WriteLine("Test 01");
//        Console.WriteLine("=======");
//        Console.WriteLine();
//
//        var sizesTeamA = new[] {1, 2, 3, 4, 5, 6};
//        const int testIterations = 100;
//        const int iterations = 500;
//        
//        Console.WriteLine("Iterations");
//
//        foreach (var size in sizesTeamA)
//        {
//            Console.WriteLine("## " + size + "vs5");
//            for (var j = 0; j < testIterations; j++)
//            {
//                var test = new Test01(size, 5);
//                for (var i = 0; i < iterations; i++) test.Update();
//                
//                Console.WriteLine("i: " + (j+1) + "; " + test.Result());
//            }
//            Console.WriteLine();
//        }

//        Console.WriteLine("## 1vs5");
//        var test = new Test01(sizeTeamB:5);
//        for (var i = 0; i < iterations; i++) test.Update();
//        Console.WriteLine();
//        
//        Console.WriteLine("## 2vs5");
//        test = new Test01(sizeTeamA:2);
//        for (var i = 0; i < iterations; i++) test.Update();
//        Console.WriteLine();
//        
//        Console.WriteLine("## 3vs5");
//        test = new Test01(sizeTeamA:3);
//        for (var i = 0; i < iterations; i++) test.Update();
//        Console.WriteLine();
//        
//        Console.WriteLine("## 4vs5");
//        test = new Test01(sizeTeamA:4);
//        for (var i = 0; i < iterations; i++) test.Update();
//        Console.WriteLine();
//        
//        Console.WriteLine("## 5vs5");
//        test = new Test01(sizeTeamA:5);
//        for (var i = 0; i < iterations; i++) test.Update();
//        Console.WriteLine();
//        
//        Console.WriteLine("## 5vs5");
//        test = new Test01(sizeTeamA:6);
//        for (var i = 0; i < iterations; i++) test.Update();
//        Console.WriteLine();
    
//    }
    
