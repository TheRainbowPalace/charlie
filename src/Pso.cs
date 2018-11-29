using System;
using System.Collections.Generic;
using V = Geometry.Vector;


namespace ParticleSwarmOptimization {
    
/**
    A Particle in terms of Particle Swarm Optimization is a point inside a
    n-dimensional space with a velocity applied to it which indicates where
    and with what speed the particle is currently moving inside the space.
    A particle is used to evaluate the search space at a given position
    with a given fitness function which is to be optimized.
*/
public class Particle
{
    public double[] Position;
    public double[] PreviousBest;
    public double[] Velocity;
    public double[] NeighboursBest;

    public double PositionValue;
    public double PreviousBestValue;
    public double NeighboursBestValue;

    public List<Particle> Neighbours;
}


/**
    A SearchSpace in terms of Particle Swarm Optimization is a n-dimensional
    space with borders for each dimension.
*/
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

    public SearchSpace(int dimensions, double left, double right) : this()
    {
        if (left > right)
        {
            // Todo: Throw exception
        }
        
        Dimensions = dimensions;
        Intervals = new double[dimensions][];
        
        for (var i = 0; i < dimensions; i++)
        {
            Intervals[i] = new[] {left, right};
        }
    }

    public SearchSpace(double[][] intervals) : this()
    {
        Intervals = intervals;
        Dimensions = intervals.Length;
    }
}

/// <summary>
/// A swarm in terms of Particle Swarm Optimization is a set of Particles which
/// move inside a SearchSpace to find the the minimum of a given fitness
/// function. Also there is a topology defined between the particles which
/// indicates which particle informs another particle.
/// 
/// ATTRIBUTES:
/// - searchSpace: An n dimensional space to move the particles in and update
///   the fitness function on. Note that the fitness function must have the
///   same number of parameters as the search space has dimensions.
/// - fitness: A function that takes an array of n numbers and returns a single
///   number.
/// - particles: An array of particles who make up the swarm.
/// - topology!: A function that defines the topology of the swarm. This is 
///   also known as the neighbourhood of the particles. 
///   Default is ringTopology!
/// - shouldTopoUpdate (swarm:Swarm) -> Bool: A function that indicates if the
///   topology should be updated or not. Default is always false.
/// - update!: A function that defines how the veloctiy and the position of a
///   particle changes at each timestep. The update! method is called on each
///   particle when running an iteration e.g. iterateOnce(swarm).
///   Default is updateSPSO2011!
/// - confinement!: A function that defines in what ways a particle is
///   restricted. If a particle has no restrictions it is also known as
///   'let them fly' method. The confinement! method is called before the
///   update! method.
/// - iteration: Indicates how many iterations where run so far on the swarm.
///   An iterations marks a complete update of each particle inside the swarm.
///   Note that this value is only strict forward if the swarm size is constant
///   over all iterations. If not so, the interpretation of this values depends
///   on the method used to change the swarm size.
/// - evalsDone: Indicates how many evaluations are done on the swarm in total.
/// - globalBest: The so far best known position in the search space .
/// - globalBestValue: The value for the so far best known position.
/// - lastGlobalBestValue: The globalBestValue which was found before the
///   current globalBestValue. Note that this value is only updated if a new
///   global best is found and not every iteration.
/// - globalBestChanged: Indicates if the global best was updated in the last
///   iteration.
/// </summary>
public class Swarm
{
    public SearchSpace SearchSpace;
    public Func<double[], double> Fitness;

    public List<Particle> Particles;

    public Action<List<Particle>> Topology;
    public Func<Swarm, bool> ShouldTopoUpdate;
    public Action<Particle> Update;
    public Action<Particle, SearchSpace> Confinement;

    public int Iteration;
    public int EvalsDone;
    public int TopoUpdatesDone;

    public double[] GlobalBest;
    public double GlobalBestValue;
    public double LastGlobalBestValue;
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
        TopoUpdatesDone = 0;
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
        var sp = SearchSpace;
        Particles = new List<Particle>();

        for (var i = 0; i < numberOfParticles; i++)
        {
            var particle = new Particle();

            var position = new double[sp.Dimensions];
            var velocity = new double[sp.Dimensions];
    
            for (var j = 0; j < sp.Dimensions; j++)
            {
                position[j] = Pso.UniformRand(
                    sp.Intervals[j][0], sp.Intervals[j][1]);
                
                velocity[j] = Pso.UniformRand(
                    sp.Intervals[j][0] - position[j],
                    sp.Intervals[j][1] - position[j]);
            }

            particle.Position = position;
            particle.PositionValue = Fitness(position);
            particle.Velocity = velocity;
            particle.PreviousBest = position;
            particle.PreviousBestValue = particle.PositionValue;

            Particles.Add(particle);
        }
        
        Topology(Particles);
        
        for (var i = 0; i < Particles.Count; i++)
        {
            var particle = Particles[i];
            
            var min = Pso.ArgMin(particle.Neighbours);
            particle.NeighboursBest = min.Position;
            particle.NeighboursBestValue = min.PositionValue;

            Particles[i] = particle;
        }
    
        var best = Pso.GetGlobalBest(this);
        GlobalBest = best.Item1;
        GlobalBestValue = best.Item2;
        
        GlobalBestChanged = true;
        Iteration = 0;
        EvalsDone = 0;
        TopoUpdatesDone = 0;
    }


    /// <summary>
    /// Run n iterations/updates on an initialized swarm.
    /// An iteration will update a particles position, it's previousBest 
    /// value, apply a confinement on the particle, update the particles
    /// neighboursBestValue and increase the the number of evaluations done
    /// for each updated particle in the swarm.
    /// Also the globalBest and it's value as well as the iteration count 
    /// and the topology update count are updated.
    /// </summary>
    public void IterateOnce()
    {
        foreach (var particle in Particles)
        {
//                Console.WriteLine("Before: " + particle.PositionValue);
            
            Update(particle);
            Confinement(particle, SearchSpace);
            particle.PositionValue = Fitness(particle.Position);
            
//                Console.WriteLine("After: " + particle.PositionValue);

//                var value = Fitness(particle.Position);

            if (particle.PositionValue < particle.PreviousBestValue)
            {
                particle.PreviousBest = particle.Position;
                particle.PreviousBestValue = particle.PositionValue;
            }

            if (particle.PositionValue < particle.NeighboursBestValue)
            {
                for (var j = 0; j < particle.Neighbours.Count; j++) 
                {
                    var neighbour = particle.Neighbours[j];
                    neighbour.NeighboursBest = particle.Position;
                    neighbour.NeighboursBestValue = particle.PositionValue;

                    particle.Neighbours[j] = neighbour;
                }
            }

            EvalsDone += 1;
        }
    
        var best = Pso.GetGlobalBest(this);
        var globalBest = best.Item1;
        var globalBestValue = best.Item2;
        
            
        if (globalBestValue < GlobalBestValue)
        {
            LastGlobalBestValue = GlobalBestValue;
            GlobalBest = globalBest;
            GlobalBestValue = globalBestValue;
            GlobalBestChanged = true;
        }
        else GlobalBestChanged = false;
    
        if (ShouldTopoUpdate(this))
        {
            Topology(Particles);
            TopoUpdatesDone += 1;
        }
        
        Iteration += 1;
    }
    
    
    /// <summary>
    ///  Run n iterations/updates on an initialized swarm.
    /// </summary>
    /// <param name="maxIterations"></param>
    public void IterateMaxIterations(int maxIterations)
    {
        var iterationsDone = 0;
    
        while (iterationsDone < maxIterations)
        {
            IterateOnce();
            iterationsDone += 1;
        }
    }
    
    
    /**
        Run n iterations/updates on an initialized swarm.
    */
    public void IterateMaxEvals(int maxEvals)
    {
        while (EvalsDone < maxEvals) IterateOnce();
    }
}


public static class Pso
{
    /**
        Create a uniform random number inside the interval [left, right].
    
        PARAMETERS:
        - left: The left interval border. Default to 0.0
        - right: The right interval border. Default to 1.0
        - switchIfNecessary: Indicates if the left and right border should 
          be switched if the right border is smaller then the left. 
          Defaults to false
    
        RETURNS: The created uniform random value.
    */
    public static double UniformRand(double left = 0.0f, double right = 1.0f,
        bool switchIfNecessary = false)
    {
        if (left > right)
        {
            if (switchIfNecessary)
            {
                var newLeft = right;
                right = left;
                left = newLeft;
            }
            else
            {
                throw new ArgumentException(
                    "Left border should be smaller than right border");
            }
        }

        var r = new Random(Guid.NewGuid().GetHashCode());
        return r.NextDouble() * Math.Abs(right - left) + left;
    }

    
    /// Find the particle with the smallest value at its position.
    ///
    /// PARAMETERS:
    /// - particles: The particles to search in.
    /// - fitness: A function which returns a value for a position of a 
    ///   particle.
    ///
    /// RETURNS: Particle The particle with the smallest position value.
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
    
    
    /// Get the best position and it's corresponding value that has been 
    /// found so far by the particle swarm. Globally means in reference to 
    /// all particles contained in the swarm.
    /// Note that the swarm has to be initialized first.
    ///
    /// PARAMETERS:
    /// - swarm: The swarm to get the so far globally found value from.
    ///
    /// RETURNS: Tuple{Array{Float64}, Float64} The position and the 
    /// corresponding value.
    public static Tuple<double[], double> GetGlobalBest(Swarm swarm)
    {
        var min = swarm.Particles[0];
    
        for (var i = 1; i < swarm.Particles.Count; i++)
        {
            if (swarm.Particles[i].PreviousBestValue < 
                min.PreviousBestValue)
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
    
    
    /// Apply a ring topology to an array of particles.
    ///
    /// PARAMETERS:
    /// - particles: The array of particles to apply the topology to.
    public static void RingTopology(List<Particle> particles)
    {
        for (var i = 0; i < particles.Count; i++)
        {
            var left = i - 1;
            if (i == 0) left = particles.Count - 1;

            var right = i + 1;
            if (i == particles.Count - 1) right = 0;

            var particle = particles[i];
            particle.Neighbours = new List<Particle>
            {
                particles[left], particles[i], particles[right]
            };
        }
    }
        
    
    /// <summary>
    ///   Apply a random topology to an array of particles.
    ///   For more information please refers to "Back to random topology" - 
    ///   Method 2 by Maurice Clerc, 27th March 2007.
    /// </summary>
    /// <param name="particles">
    ///   The array of particles to apply the topology to.
    /// </param>
    /// <param name="k">
    ///   The maximum possible number of neighbours > 0. If k is set to -1
    ///   k will equal the number of particles.
    /// </param>
    public static void AdaptiveRandomTopology(List<Particle> particles,
        int k = -1)
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
                var column = rand.Next(0, amount -1);
                
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
    }


    //
    // Update Functions
    //
    
    
    public static double W = 1 / (2 * Math.Log(2));
    public static double C1 = 1 / 2f + Math.Log(2);
    public static double C2 = C1;
    
    
//    v[] = v[] + c1 * rand() * (pbest[] - present[]) + c2
//          * rand() * (gbest[] - present[])
//    present[] = present[] + v[]

//    v[] is the particle velocity,
//    present[] is the current particle (solution).
//    pbest[] and gbest[] are defined as stated before.
//    rand () is a random number between (0,1).
//    c1, c2 are learning factors.
//    usually c1 = c2 = 2. 
    public static void UpdateSpso2006(Particle particle)
    {
        for (var i = 0; i < particle.Velocity.Length; i++)
        {
            particle.Velocity[i] = W * particle.Velocity[i]
                + UniformRand(0f, C1) 
                * (particle.PreviousBest[i] - particle.Position[i])
                + UniformRand(0f, C1) 
                * (particle.NeighboursBest[i] - particle.Position[i]);
            particle.Position[i] += particle.Velocity[i];
        }
    }


    public static void UpdateSpso2007(Particle particle)
    {
        if (particle.NeighboursBestValue.Equals(particle.PreviousBestValue))
        {
            for (var i = 0; i < particle.Velocity.Length; i++)
            {
                particle.Velocity[i] = particle.Velocity[i]
                    + UniformRand(0.0f, C1) * (particle.PreviousBest[i]
                    - particle.Position[i]);
                particle.Position[i] += particle.Velocity[i];
            }
        }
        else UpdateSpso2006(particle);
    }
    
    
    public static void UpdateSpso2011(Particle particle)
    {
        var value = V.Add(particle.PreviousBest, particle.NeighboursBest);
        value = V.Subtract(value, V.Multiply(particle.Position, 2f));
    
        if (particle.NeighboursBestValue.Equals(particle.PreviousBestValue))
        {
            value = V.Subtract(particle.PreviousBest, particle.Position);
        }

        var g = V.Add(particle.Position, V.Multiply(V.Divide(value, 3f), C1));
        var x = new double[particle.Position.Length];
    
        for (var i = 0; i < particle.Position.Length; i++)
        {
            x[i] = UniformRand(g[i], 
                Math.Abs(g[i] - particle.Position[i]), true);
        }
        
        var lastPosition = particle.Position;
        
        particle.Position = V.Add(V.Multiply(particle.Velocity, W), x);
        particle.Velocity = V.Subtract(
            V.Add(V.Multiply(particle.Velocity, W), x), 
            lastPosition);
    }
    
    
    //
    // Confinement Functions
    //
    
    
    /**
     * Apply no confinements to the particle.
     */
    public static void LetThemFly(Particle particle, 
        SearchSpace searchSpace) {}
    
    
    /**
        Set the particles velocity to 0 and their position to the search 
        space interval border for each dimension where the particles 
        position is outside of the interval border.
     */
    public static void ConfinementSpso2006(Particle particle, 
        SearchSpace searchSpace)
    {
        var i = 0;
        foreach (var interval in searchSpace.Intervals)
        {
            if (particle.Position[i] < interval[0])
            {
                particle.Position[i] = interval[0];
                particle.Velocity[i] = 0;
            }
            else if (particle.Position[i] > interval[1])
            {
                particle.Position[i] = interval[1];
                particle.Velocity[i] = 0;
            }
            i++;
        }
    }
    
    
    public static void DeterministicBack(Particle particle, 
        SearchSpace searchSpace)
    {
        var i = 0;
        foreach (var interval in searchSpace.Intervals)
        {
            if (particle.Position[i] < interval[0])
            {
                particle.Position[i] = interval[0];
                particle.Velocity[i] *= -0.5f;
            }
            else if (particle.Position[i] > interval[1])
            {
                particle.Position[i] = interval[1];
                particle.Velocity[i] *= -0.5f;
            }
            i++;
        }
    }


    public static void RandomBack(Particle particle, 
        SearchSpace searchSpace)
    {
        var i = 0;
        foreach (var interval in searchSpace.Intervals)
        {
            if (particle.Position[i] < interval[0])
            {
                particle.Position[i] = interval[0];
                particle.Velocity[i] *= -UniformRand(0, 1);
            }
            else if (particle.Position[i] > interval[1])
            {
                particle.Position[i] = interval[1];
                particle.Velocity[i] *= -UniformRand(0, 1);
            }
            i++;
        }
    }
 
    
    //
    // Standard Particle Swarm Definitions
    //
    
    
    public static Swarm SwarmSpso2006(SearchSpace searchSpace, 
        Func<double[], double> fitness)
    {            
        return new Swarm(searchSpace, fitness, 
            RingTopology, swarm => false, 
            UpdateSpso2006, ConfinementSpso2006);
    }
    
    
    public static Swarm SwarmSpso2007(SearchSpace searchSpace, 
        Func<double[], double> fitness)
    {
        return new Swarm(searchSpace, fitness, 
            RingTopology, swarm => false, 
            UpdateSpso2007, ConfinementSpso2006);
    }
    
    
    public static Swarm SwarmSpso2011(SearchSpace searchSpace, 
        Func<double[], double> fitness)
    {
        return new Swarm(searchSpace, fitness, 
            swarm => AdaptiveRandomTopology(swarm), 
            swarm => swarm.GlobalBestChanged, 
            UpdateSpso2011, DeterministicBack);
    }
}
    
}