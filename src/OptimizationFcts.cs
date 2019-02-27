using System;
using System.Collections.Generic;
using System.Linq;
using M = System.Math;

namespace Optimization
{
    public static class OptimizationFct
    {
        public const double SPHERE_FCT_OPT = 0; 
        public const double MC_CORMICK_FCT_OPT = -1.9133; 
        public const double HOELDER_TABLE_FCT_OPT = -19.2085;
        public const double THREE_HUMP_CAMEL_FCT_OPT = 0;
        public const double HIMMELBLAU_FCT_OPT = 0;
        
        public static double SphereFct(IEnumerable<double> x)
        {
            return x.Sum(t => M.Pow(t, 2));
        }


        public static double StyblinskiTangFct(IEnumerable<double> x)
        {
            return 0.5 * x.Sum(t =>
                       M.Pow(t, 4) - 16 * M.Pow(t, 2) + 5 * t);
        }
        
        
        public static double StyblinskiTangOpt(double dimension)
        {
            return -39.16599 * dimension;
        }

        
        public static double McCormickFct(double[] x)
        {
            if (x.Length != 2) throw new ArgumentException(
                "McCormick function is two dimensional");
            
            return M.Sin(x[0] + x[1]) + M.Pow(x[0] - x[1], 2)
                   - 1.5 * x[0] + 2.5 * x[1] + 1;
        }

        public static double HoelderTableFct(double[] x)
        {
            if (x.Length != 2) throw new ArgumentException(
                "Hoelder-Table function is two dimensional");
            
            return -M.Abs(M.Sin(x[0]) * M.Cos(x[1]) 
                   * M.Exp(M.Abs(1 - M.Sqrt(
                     M.Pow(x[0], 2) + M.Pow(x[1], 2)) / M.PI)));
        }

        public static double ThreeHumpCamelFct(double[] x)
        {
            if (x.Length != 2) throw new ArgumentException(
                "Three-Hump-Camel function is two dimensional");
            
            return 2 * M.Pow(x[0], 2) - 1.05 * M.Pow(x[0], 4) 
                   + M.Pow(x[0], 6) / 6 + x[0] * x[1] + M.Pow(x[1], 2);
        }

        public static double HimmelblauFct(double[] x)
        {
            if (x.Length != 2) throw new ArgumentException(
                "Himmelblau function is two dimensional");
            
            return M.Pow(M.Pow(x[0], 2) + x[1] - 11, 2)
                   + M.Pow(x[0] + M.Pow(x[1], 2) - 7, 2);
        }
    }
}