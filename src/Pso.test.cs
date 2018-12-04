// Author: Jakob Rieke

using System;
using System.Collections.Generic;
using System.Linq;
using Opt = ParticleSwarmOptimization.OptimizationFct;

namespace ParticleSwarmOptimization
{
    public static class PsoTest
    {
        public static void Passed(string title, bool condition)
        {
            if (condition) Console.WriteLine(title + " passed");
            else Console.WriteLine(title + " failed");
        }

        
        public static bool TestArgMin()
        {
            var updateValue = new Action<Particle>(p =>
                p.PositionValue = OptimizationFct.SphereFct(p.Position));

            var p1 = new Particle {Position = new[] {1.5, 1.5, 1.5}};
            var p2 = new Particle {Position = new[] {0.0, 0, 0}};
            var p3 = new Particle {Position = new[] {-1.5, -1.5, -1.5}};

            updateValue(p1);
            updateValue(p2);
            updateValue(p3);

            var particles = new List<Particle> {p1, p2, p3};

            var r1 = Pso.ArgMin(particles).Position
                .SequenceEqual(new []{0.0, 0, 0});

            p2.Position[2] = 5f;
            updateValue(p2);
        
            var r2 = Pso.ArgMin(particles).Position
                .SequenceEqual(new []{1.5, 1.5, 1.5});

            return r1 && r2;
        }
        
        
        public static bool TestInitialize()
        {
            var swarm = Pso.SwarmSpso2006(
                new SearchSpace(2, 100f),
                OptimizationFct.SphereFct);
            swarm.Initialize();

            var dims = swarm.SearchSpace.Dimensions;

            var result = true;
            foreach (var particle in swarm.Particles)
            {
                if (particle.Position.Length == dims &&
                    particle.PreviousBest.Length.Equals(dims) &&
                    particle.Velocity.Length == dims &&
                    particle.NeighboursBest.Length == dims &&
                    particle.Neighbours.Count >= 1 &&
                    particle.Neighbours.Count == 3) continue;

                result = false;
                break;
            }

            result = result &&
                     swarm.Particles.Count == 40 &&
                     swarm.Iteration == 0 &&
                     swarm.EvalsDone == 0;

            return result;
        }

        
        private static void TestPso(string title, Swarm swarm, 
            double expectation, int iterations = 100)
        {
            swarm.Initialize();
            swarm.IterateMaxIterations(iterations);
            
            Console.Write("Best: " + swarm.GlobalBestValue);
            Console.Write(", Expected: " + expectation + " +- 1, ");
            
            var result = Math.Abs(swarm.GlobalBestValue - expectation) < 1;
            
            if (result) Console.Write(title + " passed\n");
            else Console.Write(title + " failed\n"); 
        }

        
        private static void TestWithSphereFct()
        {
            var sp = new SearchSpace(16, 100);
            var swarm2006 = Pso.SwarmSpso2006(sp, Opt.SphereFct);
            var swarm2007 = Pso.SwarmSpso2007(sp, Opt.SphereFct);
            var swarm2011 = Pso.SwarmSpso2011(sp, Opt.SphereFct);

            Console.WriteLine("Sphere function:");
            TestPso("SPSO 2006", swarm2006, Opt.SPHERE_FCT_OPT);
            TestPso("SPSO 2007", swarm2007, Opt.SPHERE_FCT_OPT);
            TestPso("SPSO 2011", swarm2011, Opt.SPHERE_FCT_OPT);
        }

        private static void TestWithStyblinskiTangFct()
        {
            var sp = new SearchSpace(2, 10);
            var swarm2006 = Pso.SwarmSpso2006(sp, Opt.StyblinskiTangFct);
            var swarm2007 = Pso.SwarmSpso2007(sp, Opt.StyblinskiTangFct);
            var swarm2011 = Pso.SwarmSpso2011(sp, Opt.StyblinskiTangFct);

            Console.WriteLine("Styblinski-Tang function:");
            TestPso("SPSO 2006", swarm2006, Opt.StyblinskiTangOpt(2), 200);
            TestPso("SPSO 2007", swarm2007, Opt.StyblinskiTangOpt(2), 200);
            TestPso("SPSO 2011", swarm2011, Opt.StyblinskiTangOpt(2), 200);
        }

        private static void TestWithMcCormickFct()
        {
            var sp = new SearchSpace(2, 20);
            var swarm2006 = Pso.SwarmSpso2006(sp, Opt.McCormickFct);
            var swarm2007 = Pso.SwarmSpso2007(sp, Opt.McCormickFct);
            var swarm2011 = Pso.SwarmSpso2011(sp, Opt.McCormickFct);
            
            Console.WriteLine("McCormick function:");
            TestPso("SPSO 2006", swarm2006, Opt.MC_CORMICK_FCT_OPT);
            TestPso("SPSO 2007", swarm2007, Opt.MC_CORMICK_FCT_OPT);
            TestPso("SPSO 2011", swarm2011, Opt.MC_CORMICK_FCT_OPT);
        }
       
        private static void TestWithHoelderTableFct()
        {
            var sp = new SearchSpace(2, 10);
            var swarm2006 = Pso.SwarmSpso2006(sp, Opt.HoelderTableFct);
            var swarm2007 = Pso.SwarmSpso2007(sp, Opt.HoelderTableFct);
            var swarm2011 = Pso.SwarmSpso2011(sp, Opt.HoelderTableFct);
            
            Console.WriteLine("Hoelder-Table function:");
            TestPso("SPSO 2006", swarm2006, Opt.HOELDER_TABLE_FCT_OPT);
            TestPso("SPSO 2007", swarm2007, Opt.HOELDER_TABLE_FCT_OPT);
            TestPso("SPSO 2011", swarm2011, Opt.HOELDER_TABLE_FCT_OPT, 200);
        }

        private static void TestWithThreeCamelHumpFct()
        {
            var sp = new SearchSpace(2, 100);
            var swarm2006 = Pso.SwarmSpso2006(sp, Opt.ThreeHumpCamelFct);
            var swarm2007 = Pso.SwarmSpso2007(sp, Opt.ThreeHumpCamelFct);
            var swarm2011 = Pso.SwarmSpso2011(sp, Opt.ThreeHumpCamelFct);
            
            Console.WriteLine("Three-Hump-Camel function:");
            TestPso("SPSO 2006", swarm2006, Opt.THREE_HUMP_CAMEL_FCT_OPT);
            TestPso("SPSO 2007", swarm2007, Opt.THREE_HUMP_CAMEL_FCT_OPT);
            TestPso("SPSO 2011", swarm2011, Opt.THREE_HUMP_CAMEL_FCT_OPT);
        }

        private static void TestWithHimmelblauFct()
        {
            var sp = new SearchSpace(2, 100);
            var swarm2006 = Pso.SwarmSpso2006(sp, Opt.HimmelblauFct);
            var swarm2007 = Pso.SwarmSpso2007(sp, Opt.HimmelblauFct);
            var swarm2011 = Pso.SwarmSpso2011(sp, Opt.HimmelblauFct);
            
            Console.WriteLine("Himmelblau function:");
            TestPso("SPSO 2006", swarm2006, Opt.HIMMELBLAU_FCT_OPT);
            TestPso("SPSO 2007", swarm2007, Opt.HIMMELBLAU_FCT_OPT);
            TestPso("SPSO 2011", swarm2011, Opt.HIMMELBLAU_FCT_OPT);
        }

        private static void TestFindsMultipleRoots()
        {
            var sp = new SearchSpace(2, 100);
            for (var i = 0; i < 10000; i++)
            {
                var swarm = Pso.SwarmSpso2011(sp, Opt.HimmelblauFct);
                swarm.Initialize();
                swarm.IterateMaxIterations(100);
                
                foreach (var d in swarm.GlobalBest)
                {
                    Console.Write(d + ", ");
                }
                Console.WriteLine();
            }
        }

        private static void DebugPso()
        {
            var sp = new SearchSpace(10, 100);
            var swarm = Pso.SwarmSpso2011(sp, Opt.StyblinskiTangFct);
            swarm.Initialize();
            
            Console.WriteLine("Optimum: " + Opt.StyblinskiTangOpt(10));
            
            for (var i = 0; i < 10000; i++)
            {
                Console.Write(swarm.GlobalBestValue);
                swarm.IterateOnce();
                Console.Read();
            } 
        }
        
        public static void TestAll()
        {
//            Passed("Pso.ArgMin", TestArgMin());
//            Passed("Swarm.Initialize", TestInitialize());

//            TestFindsMultipleRoots();

//            DebugPso();
            
            Console.WriteLine();
            TestWithHimmelblauFct();

            Console.WriteLine();
            TestWithSphereFct();
            
            Console.WriteLine();
            TestWithStyblinskiTangFct();
            
            Console.WriteLine();
            TestWithMcCormickFct();
            
            Console.WriteLine();
            TestWithHoelderTableFct();
            
            Console.WriteLine();
            TestWithThreeCamelHumpFct();
        }
    }
}