using System;
using System.Collections.Generic;
using System.Linq;
using Geometry;
using V = Geometry.Vector;


namespace Optimization
{
  interface IOptimization {}

  
  interface IObjective {}
  
  
  /// <summary>
  /// A Particle in terms of Particle Swarm Optimization is a point inside a
  /// n-dimensional space with a velocity applied to it which indicates where
  /// and with what speed the particle is currently moving inside the space.
  /// A particle is used to evaluate the search space at a given position
  /// with a given fitness function which is to be optimized.
  /// </summary>
  public class Particle
  {
    public double[] Position;
    public double[] PreviousBest;
    public double[] Velocity;
    public double[] LocalBest;

    public double PositionValue;
    public double PreviousBestValue;
    public double LocalBestValue;

    public List<Particle> Neighbours;
  }


  /// <summary>
  /// A SearchSpace in terms of Particle Swarm Optimization is a n-dimensional
  /// space with a left and right border for each dimension.
  /// </summary>
  public struct SearchSpace
  {
    public readonly int Dimensions;
    public readonly double[][] Intervals;

    public SearchSpace(int dimensions, double size) : this()
    {
      Dimensions = dimensions;
      Intervals = new double[dimensions][];

      for (var i = 0; i < dimensions; i++)
      {
        Intervals[i] = new[] {-size, size};
      }
    }

    public SearchSpace(int dimensions, double left, double right)
    {
      if (left > right)
      {
        throw new ArgumentException(
          "Left border must be greater than right border.");
      }

      Dimensions = dimensions;
      Intervals = new double[dimensions][];

      for (var i = 0; i < dimensions; i++)
      {
        Intervals[i] = new[] {left, right};
      }
    }

    public SearchSpace(double[][] intervals)
    {
      Intervals = intervals;
      Dimensions = intervals.Length;
    }

    /// <summary>
    /// Get a random position inside the SearchSpace.
    /// </summary>
    /// <returns>
    /// An array with the same dimension as the SearchSpace.
    /// </returns>
    public double[] RandPos()
    {
      return Intervals.Select(i => Pso.UniformRand(i[0], i[1])).ToArray();
    }
  }

  /// <summary>
  /// A swarm in terms of Particle Swarm Optimization is a set of Particles which
  /// move inside a SearchSpace to find the the minimum of a given fitness
  /// function. Also there is a topology defined between the particles which
  /// indicates which particle informs another particle.
  /// </summary>
  public class Swarm : IOptimization
  {
    /// <summary>
    /// An n dimensional space to move the particles in and update
    /// the fitness function on. Note that the fitness function must have the
    /// same number of parameters as the search space has dimensions.
    /// </summary>
    public SearchSpace SearchSpace;
    
    /// <summary>
    /// A function that takes an array of n numbers and returns a single number.
    /// </summary>
    public Func<double[], double> Fitness;

    /// <summary>
    /// An array of particles which makes up the swarm.
    /// </summary>
    public List<Particle> Particles;

    /// <summary>
    /// A function that defines the topology of the swarm. This is also known
    /// as the neighbourhood of the particles.
    /// </summary>
    public Action<List<Particle>> Topology;
    
    /// <summary>
    /// A function that indicates if the topology should be updated or not.
    /// </summary>
    public Func<Swarm, bool> ShouldTopoUpdate;
    
    /// <summary>
    /// A function that defines how the veloctiy and the position of a
    /// particle changes at each timestep. The update! method is called on each
    /// particle when running an iteration e.g. iterateOnce(swarm).
    /// </summary>
    public Action<Particle> Update;
    
    /// <summary>
    /// A function that defines in what ways a particle is
    /// restricted. If a particle has no restrictions it is also known as
    /// 'let them fly' method. The confinement! Method is called before the
    /// Update method.
    /// </summary>
    public Action<Particle, SearchSpace> Confinement;

    /// <summary>
    /// Indicates how many iterations where run so far on the swarm.
    /// An iterations marks a complete update of each particle inside the swarm.
    /// Note that this value is only strict forward if the swarm size is constant
    /// over all iterations. If not so, the interpretation of this values depends
    /// on the method used to change the swarm size.
    /// </summary>
    public int Iteration;
    
    /// <summary>
    /// Indicates how many evaluations are done on the swarm in total.
    /// </summary>
    public int EvalsDone;

    /// <summary>
    /// The best known position in the search space.
    /// </summary>
    public double[] GlobalBest;
    
    /// <summary>
    /// The value at the best known position.
    /// </summary>
    public double GlobalBestValue;
    
    /// <summary>
    /// Indicates if the global best was updated in the last iteration.
    /// </summary>
    public bool GlobalBestChanged;

    /// <summary>
    /// Create a new particle swarm.
    /// </summary>
    /// <param name="searchSpace"></param>
    /// <param name="fitness"></param>
    /// <param name="topology"></param>
    /// <param name="shouldTopoUpdate"></param>
    /// <param name="update"></param>
    /// <param name="confinement"></param>
    public Swarm(SearchSpace searchSpace, Func<double[], double> fitness,
      Action<List<Particle>> topology, Func<Swarm, bool> shouldTopoUpdate,
      Action<Particle> update,
      Action<Particle, SearchSpace> confinement)
    {
      SearchSpace = searchSpace;
      Fitness = fitness;
      Topology = topology;
      ShouldTopoUpdate = shouldTopoUpdate;
      Update = update;
      Confinement = confinement;
      Iteration = -1;
      EvalsDone = 0;
    }

    /// <summary>
    /// Initialize a swarm from a list of start positions and velocities.
    /// </summary>
    /// <param name="startPosition">
    /// An array of initial positions for each particle.
    /// </param>
    /// <param name="startVelocity">
    /// An array of initial velocities for each particle.
    /// </param>
    public void Initialize(double[][] startPosition, double[][] startVelocity)
    {
      Particles = new List<Particle>();

      for (var i = 0; i < startPosition.Length; i++)
      {
        var particle = new Particle
        {
          Position = startPosition[i],
          Velocity = startVelocity[i],
          PreviousBest = new double[SearchSpace.Dimensions]
        };

        particle.Position.CopyTo(particle.PreviousBest, 0);
        particle.PositionValue = Fitness(particle.Position);
        particle.PreviousBestValue = particle.PositionValue;

        Particles.Add(particle);
      }

      Topology(Particles);

      var best = Pso.GetGlobalBest(this);
      GlobalBest = best.Item1;
      GlobalBestValue = best.Item2;
      GlobalBestChanged = true;

      Iteration = 0;
      EvalsDone = 0;
    }
    
    /// <summary>
    /// Initialize a swarm with n particles.
    /// This creates n particles and initializes them with random values 
    /// inside the search space.
    /// A swarm has to be initialized before calling iterateX! on it.
    /// </summary>
    /// <param name="numberOfParticles"></param>
    public void Initialize(int numberOfParticles = 40)
    {
      Particles = new List<Particle>();

      for (var i = 0; i < numberOfParticles; i++)
      {
        var particle = new Particle
        {
          Position = new double[SearchSpace.Dimensions],
          Velocity = new double[SearchSpace.Dimensions],
          PreviousBest = new double[SearchSpace.Dimensions],
          LocalBest = new double[SearchSpace.Dimensions]
        };

        for (var j = 0; j < SearchSpace.Dimensions; j++)
        {
          particle.Position[j] = Pso.UniformRand(
            SearchSpace.Intervals[j][0], SearchSpace.Intervals[j][1]);

          particle.Velocity[j] = Pso.UniformRand(
            SearchSpace.Intervals[j][0] - particle.Position[j],
            SearchSpace.Intervals[j][1] - particle.Position[j]);
        }

        particle.PositionValue = Fitness(particle.Position);
        particle.Position.CopyTo(particle.PreviousBest, 0);
        particle.PreviousBestValue = particle.PositionValue;

        Particles.Add(particle);
      }

      Topology(Particles);

      var best = Pso.GetGlobalBest(this);
      GlobalBest = best.Item1;
      GlobalBestValue = best.Item2;
      GlobalBestChanged = true;

      Iteration = 0;
      EvalsDone = 0;
    }

    /// <summary>
    /// Run one iteration on an initialized swarm.
    /// The iteration will update the particle positions, their previousBest,
    /// apply a confinement on the particles, update their
    /// localBests and increase the the number of evaluations, done
    /// for each particle in the swarm.
    /// </summary>
    public void IterateOnce()
    {
      var best = Particles[0];
      foreach (var particle in Particles)
      {
        Update(particle);
        Confinement(particle, SearchSpace);
        particle.PositionValue = Fitness(particle.Position);

        if (particle.PositionValue < particle.PreviousBestValue)
        {
          particle.Position.CopyTo(particle.PreviousBest, 0);
          particle.PreviousBestValue = particle.PositionValue;
        }

        if (particle.PositionValue < particle.LocalBestValue)
        {
          foreach (var neighbour in particle.Neighbours)
          {
            particle.Position.CopyTo(neighbour.LocalBest, 0);
            neighbour.LocalBestValue = particle.PositionValue;
          }
        }

        if (particle.PositionValue < best.PositionValue) best = particle;
        
        EvalsDone++;
      }
      
      if (best.PositionValue < GlobalBestValue)
      {
        best.Position.CopyTo(GlobalBest, 0);
        GlobalBestValue = best.PositionValue;
        GlobalBestChanged = true;
//        Console.WriteLine("Global Best: " + GlobalBestValue);
      }
      else GlobalBestChanged = false;

      // Error: Local best is not updated
      if (ShouldTopoUpdate(this)) Topology(Particles);

      Iteration++;
    }

    /// <summary>
    ///  Run n iterations on an initialized swarm.
    /// </summary>
    /// <param name="maxIterations"></param>
    public void IterateMaxIterations(int maxIterations)
    {
      for (var i = 0; i < maxIterations; i++) IterateOnce();
    }

    /// <summary>
    /// Update the swarm for a maximum number of fitness evaluations. An
    /// evaluation overhead of swarm-size - 1 arises if evals mod
    /// swarm-size > 0.
    /// </summary>
    /// <param name="evals"></param>
    public void IterateMaxEvals(int evals)
    {
      while (EvalsDone < evals) IterateOnce();
    }

    /// <summary>
    /// Iterate until a certain minimum is reached.
    /// </summary>
    /// <param name="value">A minimum that has to be reached.</param>
    /// <param name="maxIterations">
    /// Ensures that function terminates even if value could not be reached.
    /// If set to -1 this 'value' is ignored and the optimization only terminates
    /// after it reached 'value'.
    /// Defaults to 10000.</param>
    public void IterateUntilValue(double value, int maxIterations = 10000)
    {
      if (maxIterations == -1)
      {
        while (GlobalBestValue < value) IterateOnce();
        return;
      }
      
      var i = 0;
      while (GlobalBestValue < value && i < maxIterations)
      {
        IterateOnce();
        i++;
      }
    }
  }


  public static class Pso
  {
    /// <summary>
    /// Create a uniform random number inside the interval [left, right].
    /// The random number generation is based on PCG.
    /// If the left border is greater than the right border they are
    /// automatically switched.
    /// </summary>
    /// <param name="left">The left interval border. Default to 0.0</param>
    /// <param name="right">The right interval border. Default to 1.0</param>
    /// <returns></returns>
    public static double UniformRand(double left = 0.0, double right = 1.0)
    {
      return left > right ? 
        PcgRandom.Double(right, left) : PcgRandom.Double(left, right);
    }

    /// <summary>
    /// Find the particle with the smallest value at its position.
    /// </summary>
    /// <param name="particles">A list of particles.</param>
    /// <returns>
    /// The particle with the smallest position value.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Throws an argument exception if the list of particles is empty.
    /// </exception>
    public static Particle ArgMin(List<Particle> particles)
    {
      if (particles.Count == 0)
      {
        throw new ArgumentException("List of particles is empty.");
      }
      if (particles.Count == 1) return particles[0];

      var result = particles[0];

      for (var i = 1; i < particles.Count; i++)
      {
        if (result.PositionValue > particles[i].PositionValue)
        {
          result = particles[i];
        }
      }

      return result;
    }

    /// <summary>
    /// Get the best position and it's corresponding value that has been 
    /// found so far by the particle swarm. Globally means in reference to 
    /// all particles contained in the swarm.
    /// Note that the swarm has to be initialized first.
    /// </summary>
    /// <param name="swarm">
    /// The swarm to get the so far globally found value from.
    /// </param>
    /// <returns>
    /// The position and the corresponding value.
    /// </returns>
    public static Tuple<double[], double> GetGlobalBest(Swarm swarm)
    {
      var min = swarm.Particles[0];

      for (var i = 1; i < swarm.Particles.Count; i++)
      {
        if (swarm.Particles[i].PreviousBestValue < min.PreviousBestValue)
        {
          min = swarm.Particles[i];
        }
      }

      return new Tuple<double[], double>(
        min.PreviousBest,
        min.PreviousBestValue);
    }


    //
    //   Topology Functions
    //

    /// <summary>
    /// Apply a ring topology to an array of particles.
    /// </summary>
    /// <param name="particles">
    /// The array of particles to apply the topology to.
    /// </param>
    public static void RingTopology(List<Particle> particles)
    {
      for (var i = 0; i < particles.Count; i++)
      {
        particles[i].Neighbours = new List<Particle>
        {
          particles[i == 0 ? particles.Count - 1 : i - 1], 
          particles[i], 
          particles[i == particles.Count - 1 ? 0 : i + 1]
        };
        
        var min = ArgMin(particles[i].Neighbours);
        min.PreviousBest.CopyTo(particles[i].LocalBest, 0);
        particles[i].LocalBestValue = min.PreviousBestValue;
      }
    }

    /// <summary>
    /// Apply a random topology to an array of particles.
    /// For more information please refers to "Back to random topology" - 
    /// Method 2 by Maurice Clerc, 27th March 2007.
    /// </summary>
    /// <param name="particles">
    /// The array of particles to apply the topology to.
    /// </param>
    /// <param name="k">
    /// The maximum possible number of neighbours > 0. If k is set to -1
    /// k will equal the number of particles.
    /// Defaults to 3.
    /// </param>
    public static void AdaptiveRandomTopology(List<Particle> particles,
      int k = 3)
    {
      var amount = particles.Count;

      if (amount == 0) return;

      k = k == -1 ? amount : k;
      var result = new bool[amount, amount];

      for (var i = 0; i < amount; i++)
      {
        result[i, i] = true;
        for (var j = 0; j < k; j++)
        {
          var rand = new Random(Guid.NewGuid().GetHashCode());
          var column = rand.Next(0, amount - 1);

          result[i, column] = true;
        }
      }

      for (var j = 0; j < amount; j++)
      {
        var particle = particles[j];
        particle.Neighbours = new List<Particle>();

        for (var i = 0; i < amount; i++)
        {
          if (result[i, j])
          {
            particle.Neighbours.Add(particles[i]);
          }
        }

        particles[j] = particle;
      }

      foreach (var particle in particles)
      {
        var min = ArgMin(particle.Neighbours);
        min.PreviousBest.CopyTo(particle.LocalBest, 0);
        particle.LocalBestValue = min.PreviousBestValue;
      }
    }


    //
    // Update Functions
    //

    public static double W = 1 / (2 * Math.Log(2));
    public static double C1 = 1 / 2f + Math.Log(2);
    public static double C2 = C1;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="p"></param>
    public static void UpdateSpso2006(Particle p)
    {
      for (var i = 0; i < p.Velocity.Length; i++)
      {
        p.Velocity[i] = 
          W * p.Velocity[i] + 
          UniformRand(0, C1) * (p.PreviousBest[i] - p.Position[i]) + 
          UniformRand(0, C1) * (p.LocalBest[i] - p.Position[i]);
        p.Position[i] = 
          W * p.Velocity[i] +
          UniformRand(
            p.Position[i], 
            p.Position[i] + C1 * (p.PreviousBest[i] - p.Position[i])) 
          - p.Position[i] + 
          UniformRand(
            p.Position[i], 
            p.Position[i] + C1 * (p.LocalBest[i] - p.Position[i])) 
          - p.Position[i];
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="p"></param>
    public static void UpdateSpso2007(Particle p)
    {
      if (p.LocalBestValue.Equals(p.PreviousBestValue))
      {
        for (var i = 0; i < p.Velocity.Length; i++)
        {
          p.Velocity[i] = 
            p.Velocity[i]
            + UniformRand(0, C1) * (p.PreviousBest[i] - p.Position[i]);
          p.Position[i] += p.Velocity[i];
        }
      }
      else UpdateSpso2006(p);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="particle"></param>
    public static void UpdateSpso2011(Particle particle)
    {
      var value = V.Add(particle.PreviousBest, particle.LocalBest);
      value = V.Subtract(value, V.Multiply(particle.Position, 2f));

      if (Math.Abs(particle.LocalBestValue - particle.PreviousBestValue) 
          < Defs.Precision)
      {
        value = V.Subtract(particle.PreviousBest, particle.Position);
      }

      var g = V.Add(particle.Position, V.Multiply(V.Divide(value, 3f), C1));
      var x = new double[particle.Position.Length];

      for (var i = 0; i < particle.Position.Length; i++)
      {
        x[i] = UniformRand(g[i], Math.Abs(g[i] - particle.Position[i]));
      }

      var lastPosition = new double[particle.Position.Length];
      particle.Position.CopyTo(lastPosition, 0);

      particle.Position = V.Add(V.Multiply(particle.Velocity, W), x);
      particle.Velocity = V.Subtract(
        V.Add(V.Multiply(particle.Velocity, W), x), lastPosition);
    }


    //
    // Confinement Functions
    //

    /// <summary>
    /// Apply no confinements to the particle.
    /// </summary>
    /// <param name="p"></param>
    /// <param name="sp"></param>
    public static void LetThemFly(Particle p, SearchSpace sp) {}

    /// <summary>
    /// Set the particles velocity to 0 and their position to the search 
    /// space interval border for each dimension where the particles 
    ///  position is outside of the interval border.
    /// </summary>
    /// <param name="p"></param>
    /// <param name="sp"></param>
    public static void ConfinementSpso2006(Particle p, SearchSpace sp)
    {
      var i = 0;
      foreach (var interval in sp.Intervals)
      {
        if (p.Position[i] < interval[0])
        {
          p.Position[i] = interval[0];
          p.Velocity[i] = 0;
        }
        else if (p.Position[i] > interval[1])
        {
          p.Position[i] = interval[1];
          p.Velocity[i] = 0;
        }
        i++;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="p"></param>
    /// <param name="sp"></param>
    public static void DeterministicBack(Particle p, SearchSpace sp)
    {
      var i = 0;
      foreach (var interval in sp.Intervals)
      {
        if (p.Position[i] < interval[0])
        {
          p.Position[i] = interval[0];
          p.Velocity[i] *= -0.5f;
        }
        else if (p.Position[i] > interval[1])
        {
          p.Position[i] = interval[1];
          p.Velocity[i] *= -0.5f;
        }
        i++;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="p"></param>
    /// <param name="sp"></param>
    public static void RandomBack(Particle p, SearchSpace sp)
    {
      var i = 0;
      foreach (var interval in sp.Intervals)
      {
        if (p.Position[i] < interval[0])
        {
          p.Position[i] = interval[0];
          p.Velocity[i] *= -UniformRand();
        }
        else if (p.Position[i] > interval[1])
        {
          p.Position[i] = interval[1];
          p.Velocity[i] *= -UniformRand();
        }
        i++;
      }
    }


    //
    // Standard Particle Swarm Definitions
    //

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sp"></param>
    /// <param name="fitness"></param>
    /// <returns></returns>
    public static Swarm SwarmSpso2006(SearchSpace sp, 
      Func<double[], double> fitness)
    {
      return new Swarm(sp, fitness,
        RingTopology, swarm => false,
        UpdateSpso2006, ConfinementSpso2006);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sp"></param>
    /// <param name="fitness"></param>
    /// <returns></returns>
    public static Swarm SwarmSpso2007(SearchSpace sp,
      Func<double[], double> fitness)
    {
      return new Swarm(sp, fitness,
        RingTopology, swarm => false,
        UpdateSpso2007, ConfinementSpso2006);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sp"></param>
    /// <param name="fitness"></param>
    /// <returns></returns>
    public static Swarm SwarmSpso2011(SearchSpace sp,
      Func<double[], double> fitness)
    {
      return new Swarm(sp, fitness,
        swarm => AdaptiveRandomTopology(swarm),
        swarm => swarm.GlobalBestChanged,
        UpdateSpso2011, DeterministicBack);
    }
  }
}