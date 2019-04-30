﻿using Alea;
using Alea.Parallel;
using System.Linq;
using System;

namespace DicomStrictCompare
{
    public class CudaMathematics : IMathematics
    {
        public int CompareAbsolute(double[] source, double[] target, double tolerance, double epsilon)
        {
            if (source.Length != target.Length)
                throw new ArgumentException("The source and target lengths need to match");

            double MaxSource = source.Max();
            double MaxTarget = target.Max();
            double MinDoseEvaluated = MaxSource * epsilon;
            var isCountedArray = new int[source.Length];
            var differenceDoubles = new double[source.Length];
            var absDifferenceDoubles = new double[source.Length];
            var isGTtol = new int[source.Length];

            // filter doses below threshold
            // TODO: should failure be -1?
            Gpu.Default.For(0, source.Length, i => source[i] = (source[i] > epsilon) ? source[i] : 0);
            Gpu.Default.For(0, target.Length, i => target[i] = (target[i] > epsilon) ? target[i] : 0);
            Gpu.Default.For(0, source.Length, i => source[i] = (target[i] > epsilon) ? source[i] : 0);
            Gpu.Default.For(0, target.Length, i => target[i] = (source[i] > epsilon) ? target[i] : 0);

            Gpu.Default.For(0, source.Length, i => isCountedArray[i] = (source[i] > 0) ? 1 : 0);

            // find relative difference 
            Gpu.Default.For(0, differenceDoubles.Length, i => differenceDoubles[i] = ((source[i] - target[i])/source[i]) );

            // absolute value of previous table 
            Gpu.Default.For(0, absDifferenceDoubles.Length,
                i => absDifferenceDoubles[i] = (differenceDoubles[i] < 0) ? -1*differenceDoubles[i] : differenceDoubles[i]);

            //determine if relative difference is greater than minDoseEvaluated 
            // stores 1 as GT minDoseEvaluated is true
            Gpu.Default.For(0, isGTtol.Length,
                i => isGTtol[i] = (absDifferenceDoubles[i] > tolerance) ? 1 : 0);


            int isCounted = 0;
            isCounted = Gpu.Default.Sum(isCountedArray);

            int failed = 0;
            failed = Gpu.Default.Sum(isGTtol);

            /*foreach (var value in isGTtol)
            {
                if (value > 0)
                {
                    failed++;
                }
            }*/

            return failed;
        }
        public int CompareRelative(double[] source, double[] target, double tolerance, double epsilon)
        {
            double MaxSource = source.Max();
            double MaxTarget = target.Max();
            double MinDoseEvaluated = MaxSource * epsilon;
            double sourceVariance = MaxSource * tolerance;
            var sourceLow = new double[source.Length];
            var sourceHigh = new double[source.Length];
            var differenceDoubles = new double[source.Length];
            var absDifferenceDoubles = new double[source.Length];
            var isGTtol = new int[source.Length];

            // filter doses below threshold
            // TODO: should failure be -1?
            Gpu.Default.For(0, source.Length, i => source[i] = (source[i] > epsilon) ? source[i] : 0);
            Gpu.Default.For(0, target.Length, i => target[i] = (target[i] > epsilon) ? target[i] : 0);
            //determine if relative difference is greater than minDoseEvaluated 
            // stores 1 as GT minDoseEvaluated is true
            Gpu.Default.For(0, isGTtol.Length,
                i => isGTtol[i] = (((source[i] - sourceVariance) < target[i]) && ((source[i] + sourceVariance) > target[i])) ? 0 : 1);
            int failed = 0;
            failed = Gpu.Default.Sum(isGTtol);
            return failed;
        }

        public int CompareOpt(double[] source, double[] target, double tolerance, double epsilon)
        {
            double MaxSource = source.Max();
            double MaxTarget = target.Max();
            double MinDoseEvaluated = MaxSource * tolerance;
            var differenceDoubles = new double[source.Length];
            var absDifferenceDoubles = new double[source.Length];
            var isGTtol = new int[source.Length];

            // filter doses below threshold
            // TODO: should failure be -1?
            Gpu.Default.For(0, source.Length, i => source[i] = (source[i] > epsilon) ? source[i] : 0);
            Gpu.Default.For(0, target.Length, i => target[i] = (target[i] > epsilon) ? target[i] : 0);


            // find relative difference 
            Gpu.Default.For(0, differenceDoubles.Length, i => differenceDoubles[i] = (Math.Abs( ((source[i] - target[i]) / source[i])) > tolerance)? 1:0 );


            int failed = 0;
            failed = Gpu.Default.Sum(isGTtol);

            /*foreach (var value in isGTtol)
            {
                if (value > 0)
                {
                    failed++;
                }
            }*/

            return failed;
        }
    }

}
