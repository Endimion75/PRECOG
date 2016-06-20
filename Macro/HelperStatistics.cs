using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Macro
{
    public class HelperStatistics
    {
        private static double SumXY(float[] x, float[] y)
        {
            if (x.Length != y.Length)
                throw new Exception("Different sizes!!!");

            int i;
            double sum = 0;
            for (i = 0; i <= x.Length - 1; i++)
                sum += x[i]*y[i];
            return sum;
        }

        private static float SumX2(float[] x)
        {
            int i;
            float sum = 0;
            for (i = 0; i <= x.Length - 1; i++)
                sum += (float)Math.Pow(x[i],2);
            return sum;
        }

        private static float Sum(float[] x)
        {
            return x.Sum();
        }

        public static double ForeCastv2(float x, float[] knownYs, float[] knownXs)
        {
            if (knownXs.Length != knownYs.Length)
                throw new Exception("Different sizes!!!");

            int n = knownYs.Length;

            double slope = (n * SumXY(knownXs, knownYs) - Sum(knownXs) * Sum(knownYs)) / (n * SumX2(knownXs) - Math.Pow(Sum(knownXs), 2));

            double a = knownYs.Average() - slope * knownXs.Average();

            return a + slope * x;
        }


        public static double Forecast(double x, float[] knownYs, float[] knownXs)
        {
            // X
            var xAvg = (float) knownXs.Sum();
            xAvg /= knownXs.Length;

            // Y
            var yAvg = (float) knownYs.Sum();
            yAvg /= knownYs.Length;

            double tempTop = 0f;
            double tempBottom = 0f;
            for (int i = 0; i < knownYs.Length; i++)
            {
                tempTop += (knownXs[i] - xAvg) * (knownYs[i] - yAvg);
                tempBottom += Math.Pow(((knownXs[i] - xAvg)), 2f);
            }

            double b = Math.Round(tempTop,5) / Math.Round(tempBottom,5);
            double a = yAvg - b * xAvg;

            return a + b * x;
        }

        public static double CalculateStdDev(IEnumerable<float> values)
        {
            double ret = 0;
            if (values.Count() > 0)
            {
                //Compute the Average
                float avg = values.Average();
                //Perform the Sum of (value-avg)_2_2 
                double sum = values.Sum(d => Math.Pow(d - avg, 2));
                //Put it all together 
                ret = Math.Sqrt((sum) / (values.Count() - 1));
            }
            return ret;
        }


        /// <summary>
        /// Fits a line to a collection of (x,y) points.
        /// </summary>
        /// <param name="xVals">The x-axis values.</param>
        /// <param name="yVals">The y-axis values.</param>
        /// <param name="inclusiveStart">The inclusive inclusiveStart index.</param>
        /// <param name="exclusiveEnd">The exclusive exclusiveEnd index.</param>
        /// <param name="rsquared">The r^2 value of the line.</param>
        /// <param name="yintercept">The y-intercept value of the line (i.e. y = ax + b, yintercept is b).</param>
        /// <param name="slope">The slop of the line (i.e. y = ax + b, slope is a).</param>
        public static void LinearRegression(double[] xVals, double[] yVals, int inclusiveStart, int exclusiveEnd, out double rsquared, out double yintercept, out double slope)
        {

            if(xVals.Length != yVals.Length)
                throw new Exception("Input values error!");

            double sumOfX = 0;
            double sumOfY = 0;
            double sumOfXSq = 0;
            double sumOfYSq = 0;
            double ssX = 0;
            double ssY = 0;
            double sumCodeviates = 0;
            double sCo = 0;
            double count = exclusiveEnd - inclusiveStart;

            for (int ctr = inclusiveStart; ctr < exclusiveEnd; ctr++)
            {
                double x = xVals[ctr];
                double y = yVals[ctr];
                sumCodeviates += x * y;
                sumOfX += x;
                sumOfY += y;
                sumOfXSq += x * x;
                sumOfYSq += y * y;
            }

            ssX = sumOfXSq - ((sumOfX * sumOfX) / count);
            ssY = sumOfYSq - ((sumOfY * sumOfY) / count);

            double RNumerator = (count * sumCodeviates) - (sumOfX * sumOfY);
            double RDenom = (count * sumOfXSq - (sumOfX * sumOfX)) * (count * sumOfYSq - (sumOfY * sumOfY));
            sCo = sumCodeviates - ((sumOfX * sumOfY) / count);

            double meanX = sumOfX / count;
            double meanY = sumOfY / count;
            double dblR = RNumerator / Math.Sqrt(RDenom);

            rsquared = dblR * dblR;
            yintercept = meanY - ((sCo / ssX) * meanX);
            slope = sCo / ssX;
        }


    }
}
