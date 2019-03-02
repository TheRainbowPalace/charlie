using System;
using System.Collections.Generic;
using System.Linq;
using Geometry;
using Optimization;
using Shadows;
using Vector2 = Geometry.Vector2;


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
  
  public static void PlaceFromVector(double[] vector, double[] lastState, 
    List<Sensor> sensors, double moveWidth, double rotWidth)
  {
    for (var i = 0; i < vector.Length / 3; i += 3)
    {
      var v1 = new Vector2(lastState[i], lastState[i + 1]);
      var v2 = new Vector2(vector[i], vector[i + 1]);
      
      Console.WriteLine("Distance: " + Vector2.Distance(v1, v2));
      if (Vector2.Distance(v1, v2) > moveWidth)
      {
        var pos = v1.Move(Vector2.Gradient(v1, v2), moveWidth);
        vector[i] = pos.X;
        vector[i + 1] = pos.Y;
      }
      if (vector[i + 2] > rotWidth) vector[i + 2] = rotWidth;
      
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

  public SearchSpace SearchSpace()
  {
    return new SearchSpace(Intervals());
  }
}


public class SSP : SensorPositioningProblem
{
  public SSP(
    int sizeTeamA = 1,
    int sizeTeamB = 1,
    double fieldWidth = 9,
    double fieldHeight = 6,
    double playerSensorRange = 12,
    double playerSensorFov = 56.3,
    double playerSize = 0.1555
  ) : base(sizeTeamA, sizeTeamB, fieldWidth, fieldHeight,
    playerSensorRange, playerSensorFov, playerSize)
  {}


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


public class SSPByStep : SensorPositioningProblem
{
  private double _moveWidth;
  private double _rotWidth;
  
  public SSPByStep(
    double moveWidth,
    int sizeTeamA = 1,
    int sizeTeamB = 1,
    double fieldWidth = 9,
    double fieldHeight = 6,
    double playerSensorRange = 12,
    double playerSensorFov = 56.3,
    double playerSize = 0.1555
  ) : base(sizeTeamA, sizeTeamB, fieldWidth, fieldHeight,
    playerSensorRange, playerSensorFov, playerSize)
  {
    _moveWidth = moveWidth;
  }

  public double FitnessFct(double[] vector, double[] lastState)
  {
    if (lastState == null) return double.PositiveInfinity;
    
    PlaceFromVector(vector, lastState, TeamA, _moveWidth, _rotWidth);
    foreach (var p in TeamA)
    {
      if (CheckCollision(p)) return double.PositiveInfinity;
    }
    return Sensor.ShadowArea(TeamA, Env);
  }
}
