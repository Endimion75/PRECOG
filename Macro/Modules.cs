using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DataModels;
using ExpressionEvaluator;
using Macro.Classes;
using MathNet.SignalProcessing.Filter.Utils;

namespace Macro
{
    public class Modules
    {
        private const int BioScreenTimetoHours = 3600;

        private class Scope
        {
            public double X { get; set; }
            public double Y { get; set; }
        }

        private enum LinearWindowPosition
        {
            P0 = -3,
            P1 = -2,
            P2 = -1,
            P3 = 0,
            P4 = 1,
            P5 = 2,
            P6 = 3
        }

        public enum Order
        {
            Asc,
            Desc
        }

        public enum MeasurmentField
        {
            OD,
            Time
        }

        public static bool ValidGrowth()
        {
            return false;
        }

        public static bool NoGrowth(List<GrowthMeasurement> dataPoints)
        {
            // get the highest OD value
            double highestOD = dataPoints.Max(a => a.OD);
            // get the First OD value
            double firstOD = dataPoints.ElementAt(0).OD;
            // empiric filter to avoid getting slopes from small bumps on the curve when there is no real growth
            if (highestOD > firstOD * 2)
                return false;

            return true;
        }

        public static bool NoGrowth(List<GrowthMeasurement> dataPoints, double blank)
        {
            // get the highest OD value
            double highestOD = dataPoints.Max(a => a.OD);

            // empiric filter to avoid getting slopes from small bumps on the curve when there is no real growth
            if (highestOD > blank * 2)
                return false;

            return true;
        }

        public static List<Intercept> GetIntercepts(float ground, List<GrowthMeasurement> dataPoints, List<GrowthMeasurement> groundPoints, float unloggedGround)
        {
            var intercepts = new List<Intercept>();
            var gapODs = new List<float>();
            var gapTimes = new List<float>();
            const int gap = 7;

            int i;
            for (i = 0; i <= dataPoints.Count - 1; i++)
            {
                if ((i + gap) > (dataPoints.Count - 1))
                    break;

                gapODs.Clear();
                gapTimes.Clear();
                var intercept = new Intercept();
                intercept.GroundPoints = groundPoints;
                intercept.InterceptOD = unloggedGround;
                int j;
                for (j = i; j <= i + gap; j++)
                {
                    gapODs.Add(dataPoints[j].OD);
                    gapTimes.Add(dataPoints[j].Time);
                    intercept.SlopePoints.Add(new GrowthMeasurement(dataPoints[j].Time, dataPoints[j].OD));
                }
                intercept.InterceptTime = HelperStatistics.Forecast(ground, gapTimes.ToArray(), gapODs.ToArray());
                if (!(double.IsNaN(intercept.InterceptTime) | double.IsInfinity(intercept.InterceptTime) | intercept.InterceptTime < 0))
                    intercepts.Add(intercept);
            }
            return intercepts;
        }

        public static List<Intercept> GetIntercept(float ground, List<GrowthMeasurement> dataPoints, List<GrowthMeasurement> groundPoints, float unloggedGround)
        {
            var intercepts = new List<Intercept>();
            var gapODs = new List<float>();
            var gapTimes = new List<float>();

            gapODs.Clear();
            gapTimes.Clear();
            var intercept = new Intercept();
            intercept.GroundPoints = groundPoints;
            intercept.InterceptOD = unloggedGround;
            foreach (var dataPoint in dataPoints)
            {
                gapODs.Add(dataPoint.OD);
                gapTimes.Add(dataPoint.Time);
                intercept.SlopePoints.Add(new GrowthMeasurement(dataPoint.Time, dataPoint.OD));
            }
            //Todo:Sure about this??????? time on y and OD on x
            intercept.InterceptTime = HelperStatistics.Forecast(ground, gapTimes.ToArray(), gapODs.ToArray());
            //intercept.InterceptTime = HelperStatistics.Forecast(ground, gapODs.ToArray(), gapTimes.ToArray());
            if (!(double.IsNaN(intercept.InterceptTime) | double.IsInfinity(intercept.InterceptTime) | intercept.InterceptTime < 0))
                intercepts.Add(intercept);
            return intercepts;
        }

        public static List<GrowthMeasurement> ConvertToLog10(List<GrowthMeasurement> datapoints)
        {
            int i;
            for (i = 0; i <= datapoints.Count - 1; i++)
            {
                datapoints[i].OD = (float) Math.Log10(datapoints[i].OD);
            }
            return datapoints;
        }

        public static List<GrowthMeasurement> ConvertToLog2(List<GrowthMeasurement> datapoints)
        {
            int i;
            for (i = 0; i <= datapoints.Count - 1; i++)
            {
                datapoints[i].OD = (float) Math.Log(datapoints[i].OD, 2);
            }
            return datapoints;
        }

        public static List<GrowthMeasurement> NormalizeAndConvertToLog10(List<GrowthMeasurement> datapoints, double startValue)
        {
            int i;
            double normal = datapoints.ElementAt(0).OD - startValue;
            for (i = 0; i <= datapoints.Count - 1; i++)
            {
                datapoints[i].OD = (float) Math.Log10(datapoints[i].OD - normal);
            }
            return datapoints;
        }

        public static List<GrowthMeasurement> Normalize(List<GrowthMeasurement> datapoints, float startValue)
        {
            int i;
            float normal = datapoints.ElementAt(0).OD - startValue;
            for (i = 0; i <= datapoints.Count - 1; i++)
            {
                datapoints[i].OD = datapoints[i].OD - normal;
            }
            return datapoints;
        }

        public static List<GrowthMeasurement> ConvertToLog10Plus1(List<GrowthMeasurement> datapoints)
        {
            int i;
            for (i = 0; i <= datapoints.Count - 1; i++)
            {
                datapoints[i].OD = (float) (Math.Log10(datapoints[i].OD) + 1);
            }
            return datapoints;
        }

        public static List<GrowthMeasurement> CathastrophicSmoothening(List<GrowthMeasurement> datapoints)
        {
            return SmootheningCore(datapoints);
        }

        public static List<GrowthMeasurement> CathastrophicSmootheningTillHighestPoint(List<GrowthMeasurement> datapoints)
        {
            GrowthMeasurement highestPoint = FindHighestODElement(datapoints);
            return SmootheningCore(datapoints, true, highestPoint);
        }

        public static List<GrowthMeasurement> BoxingSmoothening(List<GrowthMeasurement> datapoints)
        {
            return BoxingSmootheningCore(datapoints);
        }

        public static List<GrowthMeasurement> BoxingSmootheningTillHighestPoint(List<GrowthMeasurement> datapoints)
        {
            GrowthMeasurement highestPoint = FindHighestODElement(datapoints);
            return BoxingSmootheningCore(datapoints, true, highestPoint);
        }

        public static List<GrowthMeasurement> T0Smoothening(List<GrowthMeasurement> datapoints)
        {
            if (datapoints.ElementAt(0).OD > datapoints.ElementAt(1).OD)
            {
                datapoints.ElementAt(0).OD = datapoints.ElementAt(1).OD;
            }
            return datapoints;
        }

        //-----------------------------------------------------------------------------------------

        public static bool ValidGrowth(List<GrowthMeasurement> dataPoints)
        {
            // mean of the 5 highest ods
            double meanHigh = dataPoints.OrderByDescending(gm => gm.OD).Take(5).Average(num => num.OD);

            // mean of the lowest 5 ods
            double Mean_Low = dataPoints.OrderBy(gm => gm.OD).Take(5).Average(num => num.OD);

            // get the fist OD value
            double FirstOD = dataPoints[0].OD;

            // To avoid high OD's resulting from dirt in wells
            if (FirstOD > 0.4)
            {
                return false;
            }

            // empiric filter to avoid getting slopes from small bumps on the curve when there is no real growth
            if ((Mean_Low * 1.1) >= meanHigh)
            {
                return false;
            }

            return true;
        }

        public static List<GrowthMeasurement> ConvertBioScreenTimeToHrs(List<GrowthMeasurement> datapoints)
        {
            int i;
            for (i = 0; i <= datapoints.Count - 1; i++)
            {
                datapoints.ElementAt(i).Time = datapoints.ElementAt(i).Time / BioScreenTimetoHours;
            }
            return datapoints;
        }
        
        public static List<GrowthMeasurement> CorrectCalibrateOD_ConvertBioScreenTimeToHrs(List<GrowthMeasurement> datapoints, float blank, string trueODFunction)
        {
            
            var function = FormatFunction(trueODFunction);
            var func = GetCompiledFunction(function);
            for (var i = 0; i <= datapoints.Count - 1; i++)
            {
                datapoints.ElementAt(i).Time = datapoints.ElementAt(i).Time / BioScreenTimetoHours;
                var correctedOD = datapoints.ElementAt(i).OD - blank;
                var scope = new Scope { X = correctedOD };
                func(scope);
                datapoints.ElementAt(i).OD = Convert.ToSingle(scope.Y);
            }
            System.Threading.Thread.Sleep(50);
            return datapoints;
        }

        public static List<GrowthMeasurement> CorrectCalibrateOD_ConvertBioScreenTimeToHrs(List<GrowthMeasurement> datapoints, float blank, CalibrationFunction calibrationFunction)
        {
            if(calibrationFunction.CountTerms()<3)
                throw new Exception("CalibrationFunction is invalid");

            int i;
            for (i = 0; i <= datapoints.Count - 1; i++)
            {
                datapoints.ElementAt(i).Time = datapoints.ElementAt(i).Time / BioScreenTimetoHours;
                double correctedOD = datapoints.ElementAt(i).OD - blank;
                var terms = calibrationFunction.GetTerms();
                var x = (float) correctedOD;
                var a = terms.ElementAt(0);
                var b = terms.ElementAt(1);
                var c = terms.ElementAt(2);
                datapoints.ElementAt(i).OD = (float) ((a*x) + (b*(Math.Pow(x,2))) + (c*(Math.Pow(x,3))));
            }
            return datapoints;
        }

        public static bool IsODFunctionValid(string trueODFunction)
        {
            try
            {
                var reg = new TypeRegistry();
                reg.RegisterType("Math", typeof (Math));
                var function = string.Format(CultureInfo.InvariantCulture, trueODFunction, 1);
                var expression = new CompiledExpression(function) {TypeRegistry = reg};
                var trueOD = expression.Eval();
                var result = Convert.ToSingle(trueOD);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public static float EvaluateCalibrationFunction(string trueODFunction, float value)
        {
            try
            {
                var reg = new TypeRegistry();
                reg.RegisterType("Math", typeof(Math));
                var function = string.Format(CultureInfo.InvariantCulture, trueODFunction, value);
                var expression = new CompiledExpression(function) { TypeRegistry = reg };
                var trueOD = expression.Eval();
                var result = Convert.ToSingle(trueOD);
                return result;
            }
            catch (Exception)
            {
                return float.NaN;
            }
        }

        public static List<GrowthMeasurement> CorrectAndCalibrateOD(List<GrowthMeasurement> datapoints, float blank, CalibrationFunction trueODFunction)
        {
            if(trueODFunction.CountTerms() < 3)
                throw new Exception("CalibrationFunction is invalid");

            int i;
            var reg = new TypeRegistry();
            reg.RegisterType("Math", typeof(Math));
            for (i = 0; i <= datapoints.Count - 1; i++)
            {
                datapoints.ElementAt(i).Time = datapoints.ElementAt(i).Time;
                double correctedOD = datapoints.ElementAt(i).OD - blank;

                var terms = trueODFunction.GetTerms();
                var x = (float)correctedOD;
                var a = terms.ElementAt(0);
                var b = terms.ElementAt(1);
                var c = terms.ElementAt(2);
                datapoints.ElementAt(i).OD = (float)((a * x) + (b * (Math.Pow(x, 2))) + (c * (Math.Pow(x, 3))));
            }
            return datapoints;
        }

        public static List<GrowthMeasurement> CorrectAndCalibrateOD(List<GrowthMeasurement> datapoints, float blank, string trueODFunction)
        {
            int i;
            var reg = new TypeRegistry();
            reg.RegisterType("Math", typeof(Math));
            for (i = 0; i <= datapoints.Count - 1; i++)
            {
                datapoints.ElementAt(i).Time = datapoints.ElementAt(i).Time;
                double correctedOD = datapoints.ElementAt(i).OD - blank;
                var function = string.Format(CultureInfo.InvariantCulture, trueODFunction, correctedOD);
                var expression = new CompiledExpression(function) { TypeRegistry = reg };
                var trueOD = expression.Eval();
                datapoints.ElementAt(i).OD = Convert.ToSingle(trueOD);
                //datapoints.ElementAt(i).OD = (float)(correctedOD + 0.8324057 * (Math.Pow(correctedOD, 3)));
            }
            return datapoints;
        }

        public static MacroYieldData GetYield(List<GrowthMeasurement> datapoints)
        {
            var macroData = new MacroYieldData();
            macroData.Yield = double.NaN;

            var clonedDatapoints = Clone(datapoints);
            Report("clonedDatapoints", clonedDatapoints);
            // -1 take the mean of the 2 lowest values
            var lowestTowDatapoints = (List<GrowthMeasurement>)clonedDatapoints.OrderBy(gm => gm.OD).Take(2).ToList();
            double meanLow = lowestTowDatapoints.Average(gm => gm.OD);
            Report("meanLow", clonedDatapoints);
            macroData.YieldAnchors.LowestPoints =lowestTowDatapoints;
            // 1.5 take the 6 highest values
            var highestSix = clonedDatapoints.OrderByDescending(gm => gm.OD).Take(6);
            // -2 take teh mean of the 6 highest
            double meanHigh = highestSix.Average(gm => gm.OD);
            // std dev of the 6 highest
            double stdDevHigh = HelperStatistics.CalculateStdDev(highestSix.Select(gm => gm.OD));
            // highest value
            double highestOD = clonedDatapoints.Max(gm => gm.OD);
            macroData.YieldAnchors.HighestPoints.AddRange(highestSix);
            // ---
            if (Math.Abs(stdDevHigh - 0) < double.Epsilon)
                stdDevHigh = 0.0001;
            if ((stdDevHigh / meanHigh) < 0.02)
            {
               macroData.Yield = highestOD - meanLow;
               return macroData;
            }
            return macroData;
        }

        public static MacroRateData GetGT(List<GrowthMeasurement> datapoints)
        {
            var macroData = new MacroRateData();
            macroData.GT = double.NaN;
            
            var clonedDatapoints = Clone(datapoints);
            // Convert the curve to log base 10
            var dataPointsLoged = ConvertToLog10(clonedDatapoints);
            // Remove the first 3 hrs of the curve
            dataPointsLoged = dataPointsLoged.Where(p => p.Time >= 3).ToList();
            // Caltulate Slopes
            var slopeList = GetSlopes(dataPointsLoged);
            if (slopeList.Count > 0)
            {
                // Order the slopes Descending
                slopeList.Sort((x, y) => y.Value.CompareTo(x.Value));
                // remove the 2 highest values
                slopeList.RemoveRange(0, 2);
                int count = slopeList.Count < 5 ? slopeList.Count : 5;
                var topFiveSlopeList = slopeList.Take(count);
                double mean = topFiveSlopeList.Average(x => x.Value);
                if (Math.Abs(mean - 0) > double.Epsilon)
                {
                    macroData.GT = Math.Log10(2) / mean;
                    macroData.RateSlopeAnchors = topFiveSlopeList.Select(slopes => slopes.Anchor).ToList();
                    AntiLog10Datapoints(macroData.RateSlopeAnchors);
                }
            }
            return macroData;
            
        }

        public static MacroRateData GetGTLinearRegression(List<GrowthMeasurement> datapoints)
        {
            var macroData = new MacroRateData {GT = double.NaN};

            var clonedDatapoints = Clone(datapoints);
            var dataPointsLoged = ConvertToLog10(clonedDatapoints);
            dataPointsLoged = dataPointsLoged.Where(p => p.Time >= 3).ToList();
            var slopeList = GetLinnearregression(dataPointsLoged);
            if (slopeList.Count > 0)
            {
                slopeList.Sort((x, y) => y.Value.CompareTo(x.Value));
                var highestSlope = slopeList.First();
                macroData.GT = Math.Log10(2) / highestSlope.Value;
                macroData.RateSlopeAnchors = new List<GrowthMeasurement> { highestSlope.Anchor };
                AntiLog10Datapoints(macroData.RateSlopeAnchors);
            }
            return macroData;
        }

        public static QualityIndex GetQualityIndex(Culture culture, DataType type)
        {
            const float slopeThreshold = 0.007f;
            const float peakThreshold = 0.5f;
            var qIdx = new QualityIndex();
            var datapoints = culture.GrowthMeasurements.GetMeasurements(type);

            var clonedDatapoints = Clone(datapoints);
            var dataPointsLoged = ConvertToLog10(clonedDatapoints);
            List<float> r2List;
            var slopeList = GetLinnearregression(dataPointsLoged,  slopeThreshold, out r2List);
            //slopeList = GetLinnearregression(dataPointsLoged, slopeThreshold, out r2List);
            if (r2List.Any())
            {
                qIdx.R2 = 1 - r2List.Average();
                qIdx.R2Peaks = PeakFinder(r2List, peakThreshold);
                r2List.Sort();
                qIdx.R2Worst = 1 - r2List.Take(3).Average();
            }
            qIdx.PointDifference = GetPointDifference(culture);

            SetFlags(qIdx);
            return qIdx;
        }

        private static void SetFlags(QualityIndex qIdx)
        {
            short flagCount = 0;
            var details = string.Empty;
            if (qIdx.R2 >= 0.125)
            {
                flagCount++;
                details += "Overall noisiness,";
            }
            if (qIdx.R2Worst >= 0.77)
            {
                flagCount++;
                details += "Local noisiness,";
            }
            if (qIdx.R2Peaks >= 6)
            {
                flagCount++;
                details += "Number of spikes,";
            }
            if (qIdx.PointDifference >= 0.016333333)
            {
                flagCount++;
                details += "Curve collapses,";
            }
            qIdx.Flags = flagCount;
            qIdx.HasFlags = qIdx.Flags > 0;
            if(details.EndsWith(","))
                qIdx.FlagDetails = details.Remove(details.Count() - 1, 1);
        }

        public static float GetPointDifference(Culture culture)
        {

            var raw = culture.GrowthMeasurements.GetMeasurements(DataType.Raw);
            var processedUncalibrated = culture.GrowthMeasurements.GetMeasurements(DataType.ProcessedUncalibrated);

            float valueDiff = 0;
            float rawValueArea = 0;
            for (int i = 0; i < processedUncalibrated.Count - 1; i++)
            {
                valueDiff += Math.Abs(raw[i].OD - processedUncalibrated[i].OD);
                rawValueArea += raw[i].OD;
            }

            var proportiationalPointDifference = Math.Round((valueDiff / rawValueArea), 4);

            return (float) proportiationalPointDifference;
        }

        private static int PeakFinder(List<float> r2List, float threshold)
        {
            var invertedList = r2List.Select(item => 1 - item).ToList();
            var peakList = invertedList.Where(item => item >= threshold).ToList();
            return peakList.Count();
        }

        public static MacroRateData GetGtSim(List<GrowthMeasurement> datapoints)
        {
            var macroData = new MacroRateData();
            macroData.GT = double.NaN;

            var clonedDatapoints = Clone(datapoints);
            // Convert the curve to log base 10
            var dataPointsLoged = ConvertToLog10(clonedDatapoints);
            dataPointsLoged = dataPointsLoged.Where(p => p.Time >= 3).ToList();
            // Caltulate Slopes
            var slopeList = GetSlopes(dataPointsLoged);
            if (slopeList.Count > 0)
            {
                // Order the slopes Descending
                slopeList.Sort((x, y) => y.Value.CompareTo(x.Value));
                int count = slopeList.Count < 5 ? slopeList.Count : 5;
                var topFiveSlopeList = slopeList.Take(count);
                double mean = topFiveSlopeList.Average(x => x.Value);
                if (Math.Abs(mean - 0) > double.Epsilon)
                {
                    macroData.GT = Math.Log10(2) / mean;
                    macroData.RateSlopeAnchors = topFiveSlopeList.Select(slopes => slopes.Anchor).ToList();
                    AntiLog10Datapoints(macroData.RateSlopeAnchors);
                }
            }
            return macroData;

        }
        
        public static MacroLagData GetLag(List<GrowthMeasurement> datapoints, MacroRateData macroRateData)
        {
            var macroData = new MacroLagData();
            macroData.Lag = double.NaN;
            //1. Get Ground points (base line to find intercept with steepest slope)
            var clonedDatapoints = Clone(datapoints);
            //1.1 Convert the curve to log base 10
            var dataPointsLoged = ConvertToLog10Plus1(clonedDatapoints);
            Report("ConvertToLog10Plus1", clonedDatapoints);
            //1.1 take the mean of the 5 first lowest OD values
            var sortedByODdatatPointsLoged = dataPointsLoged.OrderBy(gm => gm.OD).ThenBy(gm => gm.Time).ToList();
            var lowestFiveODPointList = sortedByODdatatPointsLoged.Take(5).ToList();
            float ground = lowestFiveODPointList.Average(gm => gm.OD);
            Report("After Sort", sortedByODdatatPointsLoged);
            //2 Caltulate Intercepts between Baseline and Slopes
            //Antilog (restore) for Medadata storing
            AntiLog10Minus1Datapoints(lowestFiveODPointList);
            var unloggedGround = AntiLog10Minus1Datapoints(ground);

            var window = GetRateCenterSlopePoints(macroRateData, dataPointsLoged);

            var rateSlopes = Clone(window);
            var intercepts = GetIntercept(ground, rateSlopes, lowestFiveODPointList, unloggedGround);
            if (intercepts.Count > 0)
            {
                //3. Take the mean of the two highest Intercepts (== to steepest slopes)
                // --- Order the slopes Descending
                intercepts.Sort((x, y) => y.InterceptTime.CompareTo(x.InterceptTime));
                // Mean of the 2 highes intercepts
                int elements = intercepts.Count >= 2 ? 2 : intercepts.Count;
                var highestInterceptList = intercepts.Take(elements).ToList();
                double lag = highestInterceptList.Average(i => i.InterceptTime);
                foreach (var intercept in highestInterceptList)
                    AntiLog10Minus1Datapoints(intercept.SlopePoints);
                macroData.InterceptStretchs = highestInterceptList;
                if (Math.Abs(lag - 0) > double.Epsilon)
                    macroData.Lag= lag;
            }
            return macroData;
        }

        private static List<GrowthMeasurement> GetRateCenterSlopePoints(MacroRateData macroRateData, List<GrowthMeasurement> dataPointsLoged)
        {
            var middle = macroRateData.RateSlopeAnchors.Count/2;
            var sortedslopes = macroRateData.RateSlopeAnchors.OrderBy(gm => gm.Time).ToList();
            var centerRatePoint = sortedslopes.Skip(middle).Take(1).First();
            var idx = dataPointsLoged.FindIndex(gm=>Math.Abs(gm.Time - centerRatePoint.Time) < float.Epsilon);
            var window = new List<GrowthMeasurement>
            {
                dataPointsLoged.ElementAt(idx - 2),
                dataPointsLoged.ElementAt(idx - 1),
                dataPointsLoged.ElementAt(idx),
                dataPointsLoged.ElementAt(idx + 1),
                dataPointsLoged.ElementAt(idx + 2)
            };
            return window;
        }

        public static MacroLagData GetLag(List<GrowthMeasurement> datapoints)
        {
            var macroData = new MacroLagData();
            macroData.Lag = double.NaN;
            //1. Get Ground points (base line to find intercept with steepest slope)
            var clonedDatapoints = Clone(datapoints);
            //1.1 Convert the curve to log base 10
            var dataPointsLoged = ConvertToLog10(clonedDatapoints);
            Report("ConvertToLog10Plus1", clonedDatapoints);
            //1.1 take the mean of the 5 first lowest OD values
            var sortedByODdatatPointsLoged = dataPointsLoged.OrderBy(gm => gm.OD).ThenBy(gm => gm.Time).ToList();
            var lowestFiveODPointList = sortedByODdatatPointsLoged.Take(5).ToList();
            float ground = lowestFiveODPointList.Average(gm => gm.OD);
            Report("After Sort", sortedByODdatatPointsLoged);
            //2 Caltulate Intercepts between Baseline and Slopes
            //Antilog (restore) for Medadata storing
            var unloggedLowestFivePoints = AnitLog10DatapointsDeep(lowestFiveODPointList);
            var unloggedGround = AntiLog10(ground);
            var intercepts = GetIntercepts(ground, sortedByODdatatPointsLoged, unloggedLowestFivePoints, unloggedGround);
            if (intercepts.Count > 0)
            {
                //3. Take the mean of the two highest Intercepts (== to steepest slopes)
                // --- Order the slopes Descending
                intercepts.Sort((x, y) => y.InterceptTime.CompareTo(x.InterceptTime));
                // Mean of the 2 highes intercepts
                int elements = intercepts.Count >= 2 ? 2 : intercepts.Count;
                var highestInterceptList = intercepts.Take(elements).ToList();
                double lag = highestInterceptList.Average(i => i.InterceptTime);
                foreach (var intercept in highestInterceptList)
                    AntiLog10Datapoints(intercept.SlopePoints);
                macroData.InterceptStretchs = highestInterceptList;
                if (Math.Abs(lag - 0) > double.Epsilon)
                    macroData.Lag = lag;
            }
            return macroData;
        }
        public static List<GrowthMeasurement> PreProccessedDatapoints(List<GrowthMeasurement> datapoints, float blank, string trueODFunction)
        {
            var clonedDatapoints = Clone(datapoints);
            Report("Raw", clonedDatapoints);
            var temp = CorrectCalibrateOD_ConvertBioScreenTimeToHrs(clonedDatapoints, blank, trueODFunction);
            Report("CorrectAndCalibrateOD", temp);
            temp = BoxingSmoothening(temp);
            Report("BoxingSmoothening", temp);
            temp = CathastrophicSmoothening(temp);
            Report("CathastrophicSmoothening", temp);

            return temp;
        }

        public static List<GrowthMeasurement> PreProccessedDatapoints(List<GrowthMeasurement> datapoints, float blank, CalibrationFunction trueODFunction)
        {
            var clonedDatapoints = Clone(datapoints);
            Report("Raw", clonedDatapoints);
            var temp = CorrectCalibrateOD_ConvertBioScreenTimeToHrs(clonedDatapoints, blank, trueODFunction);
            Report("CorrectAndCalibrateOD", temp);
            temp = BoxingSmoothening(temp);
            Report("BoxingSmoothening", temp);
            temp = CathastrophicSmoothening(temp);
            Report("CathastrophicSmoothening", temp);

            return temp;
        }

        public static List<GrowthMeasurement> ProccessedDatapoints(List<GrowthMeasurement> datapoints, float blank, string trueODFunction)
        {
            int count;
            float maxPeaks;
            return ProcessAlgorithm(datapoints, blank, trueODFunction, out count, out maxPeaks);
        }

        public static List<GrowthMeasurement> ProccessedDatapoints(List<GrowthMeasurement> datapoints, float blank, CalibrationFunction trueODFunction)
        {
            int count;
            float maxPeaks;
            return ProcessAlgorithm(datapoints, blank, trueODFunction, out count, out maxPeaks);
        }

        public static List<GrowthMeasurement> ProccessedDatapoints(List<GrowthMeasurement> datapoints, float blank, CalibrationFunction trueODFunction, out int count, out float maxPeak, bool skipClean = false, bool skipCalibration = false, bool skipMonotonicFilter = false)
        {
            return ProcessAlgorithm(datapoints, blank, trueODFunction, out count, out maxPeak, skipClean, skipCalibration, skipMonotonicFilter);
        }

        public static List<GrowthMeasurement> ProccessedDatapoints(List<GrowthMeasurement> datapoints, float blank, string trueODFunction, out int count, out float maxPeak, bool skipClean = false, bool skipCalibration = false, bool skipMonotonicFilter = false)
        {
            return ProcessAlgorithm(datapoints, blank, trueODFunction, out count, out maxPeak, skipClean, skipCalibration, skipMonotonicFilter);
        }

        public static List<GrowthMeasurement> ProccessedDatapointsNoFileter(List<GrowthMeasurement> datapoints, float blank, string trueODFunction)
        {
            int count;
            float maxPeaks;
            return ProcessAlgorithm(datapoints, blank, trueODFunction, out count, out maxPeaks,true);
        }

        private static List<GrowthMeasurement> ProcessAlgorithm(List<GrowthMeasurement> datapoints, float blank, object trueODFunction,out int count, out float maxPeak, bool skipClean = false, bool skipCalibration = false, bool skipMonotonicFilter = false)
        {
            count = 0;
            maxPeak = 0;
            var clonedDatapoints = Clone(datapoints);
            Report("Raw", clonedDatapoints);

            var clean = clonedDatapoints;
            if (!skipClean)
                clean = ProcessClean(clonedDatapoints, out count, out maxPeak);

            var noColapsed = clean;
            if (!skipMonotonicFilter)
                noColapsed = CathastrophicSmoothening(clean);
            Report("CathastrophicSmoothening", noColapsed);

            var calibarated = noColapsed;

            calibarated = !skipCalibration ? ProcessCalibrate(blank, trueODFunction, noColapsed) : ConvertBioScreenTimeToHrs(calibarated);

            Report("CorrectAndCalibrateOD", calibarated);

            return calibarated;
        }

        private static List<GrowthMeasurement> ProcessCalibrate(float blank, object trueODFunction, List<GrowthMeasurement> dataPointsList)
        {
            var calibarated = new List<GrowthMeasurement>();
            if (trueODFunction is string)
                calibarated = CorrectCalibrateOD_ConvertBioScreenTimeToHrs(dataPointsList, blank, (string)trueODFunction);
            else if (trueODFunction is CalibrationFunction)
                calibarated = CorrectCalibrateOD_ConvertBioScreenTimeToHrs(dataPointsList, blank, (CalibrationFunction)trueODFunction);
            return calibarated;
        }

        private static List<GrowthMeasurement> ProcessClean(List<GrowthMeasurement> datapoints, out int count, out float maxPeak)
        {
            var peakfilter1 = ApplyMedianFiter(datapoints, out count, out maxPeak);
            Report("filter", peakfilter1);


            var peakfilter2 = ApplyMeanFiter(peakfilter1);
            Report("BoxingSmoothening", peakfilter2);

            return peakfilter2;
        }

        public static List<GrowthMeasurement> ReProccessedDatapoints(List<GrowthMeasurement> datapoints, float blank, CalibrationFunction trueODFunction)
        {
            var clonedDatapoints = Clone(datapoints);
            Report("Raw", clonedDatapoints);
            var count = 0;
            float maxPeak = 0;
            var temp = ApplyMedianFiter(clonedDatapoints, out count, out maxPeak);
            Report("filter", clonedDatapoints);

            var temp2 = CorrectAndCalibrateOD(temp, blank, trueODFunction);
            Report("CorrectAndCalibrateOD", temp2);
            temp2 = ApplyMeanFiter(temp2);
            Report("BoxingSmoothening", temp2);
            temp2 = CathastrophicSmoothening(temp2);
            Report("CathastrophicSmoothening", temp2);

            return temp2;
        }

        public static List<GrowthMeasurement> CalculateFirstDerivative(List<GrowthMeasurement> datapoints, int window)
        {
            var clonedDatapoints = Clone(datapoints);
            var logged = ConvertToLog2(clonedDatapoints);
            var dervList = new List<GrowthMeasurement>();
            for (var i = 0; i < logged.Count-1; i++)
            {
                if(i+window > logged.Count-1)
                    break;

                var p1 = logged[i];
                var p2 = logged[i + window];

                var newDerivedPoint = new GrowthMeasurement();
                float derivedOD = (p2.OD - p1.OD)/(p2.Time - p1.Time);
                newDerivedPoint.Time = p1.Time;
                newDerivedPoint.OD = derivedOD;
                dervList.Add(newDerivedPoint);
            }
            return dervList;
        }

        public static List<GrowthMeasurement> ProccessedAndFitDatapoints(List<GrowthMeasurement> datapoints, float blank, NeuralParameters parameters, string trueODFunction)
        {
            var clonedDatapoints = Clone(datapoints);
            var count = 0;
            float maxPeak = 0;
            var temp = ApplyMedianFiter(clonedDatapoints, out count, out maxPeak);
            var neuro = new Neural(parameters);
            temp = neuro.GetFittedData(temp);
            temp = CathastrophicSmoothening(temp);
            temp = CorrectAndCalibrateOD(temp, blank, trueODFunction);

            return temp;
        }


        public static List<GrowthMeasurement> ApplyMedianFiter(List<GrowthMeasurement> data, out int count, out float maxPeak)
        {
            var filteredList = new List<GrowthMeasurement>(data.Count());
            AddFirstMedianElement(data, ref filteredList);
            var window = new OrderedShiftBuffer(3);
            count = 0;
            maxPeak = float.MinValue;
            for (int i = 1; i < data.Count - 1; i ++)
            {
                window.Clear();
                window.Append(data[i - 1].OD);
                window.Append(data[i].OD);
                window.Append(data[i + 1].OD);
                var median = (float) window.Median;
                if (data[i].OD != median )
                {
                    var peak = Math.Abs(data[i].OD - median);
                    if (peak > 0.1)
                    {
                        count++;
                        var diff = Math.Abs(data[i].OD - median);
                        if (diff > maxPeak)
                            maxPeak = diff;
                    }
                }
                var medianGrowthMeasurement = new GrowthMeasurement(data[i].Time, median);
                filteredList.Add(medianGrowthMeasurement);
            }
            AddLastMedianElement(data, ref filteredList);
            return filteredList;
        }

        public static List<GrowthMeasurement> ApplyMedianFiter(List<GrowthMeasurement> data)
        {
            var filteredList = new List<GrowthMeasurement>(data.Count());
            AddFirstMedianElement(data, ref filteredList);
            var window = new OrderedShiftBuffer(3);
            for (int i = 1; i < data.Count - 1; i++)
            {
                window.Clear();
                window.Append(data[i - 1].OD);
                window.Append(data[i].OD);
                window.Append(data[i + 1].OD);
                var median = (float)window.Median;
                var medianGrowthMeasurement = new GrowthMeasurement(data[i].Time, median);
                filteredList.Add(medianGrowthMeasurement);
            }
            AddLastMedianElement(data, ref filteredList);
            return filteredList;
        }

        public static List<GrowthMeasurement> ApplyMeanFiter(List<GrowthMeasurement> data)
        {
            var filteredList = new List<GrowthMeasurement>(data.Count());
            AddFirstMeanElement(data, ref filteredList);
            var window = new List<float>(3);
            for (int i = 1; i < data.Count() - 1; i++)
            {
                window.Clear();
                window.Add(data[i - 1].OD);
                window.Add(data[i].OD);
                window.Add(data[i + 1].OD);
                var mean = window.Average();
                var medianGrowthMeasurement = new GrowthMeasurement(data[i].Time, mean);
                filteredList.Add(medianGrowthMeasurement);
            }
            AddLastMeanElement(data, ref filteredList);
            return filteredList;
        }

        public static List<GrowthMeasurement> Clone(List<GrowthMeasurement> original)
        {
            return original.Select(originalGrowthMeasurement => new GrowthMeasurement(originalGrowthMeasurement.Time, originalGrowthMeasurement.OD)).ToList();
        }

        private static void AddFirstMedianElement(List<GrowthMeasurement> data, ref List<GrowthMeasurement> filteredList)
        {
            var window = new OrderedShiftBuffer(3);
            window.Append(data[0].OD);
            window.Append(data[0].OD);
            window.Append(data[1].OD);
            var median = (float)window.Median;
            var medianGrowthMeasurement = new GrowthMeasurement(data[0].Time, median);
            filteredList.Add(medianGrowthMeasurement);
        }

        private static void AddFirstMeanElement(List<GrowthMeasurement> data, ref List<GrowthMeasurement> filteredList)
        {
            var window = new List<float>(3);
            window.Add(data[0].OD);
            window.Add(data[0].OD);
            window.Add(data[1].OD);
            var mean = window.Average();
            var medianGrowthMeasurement = new GrowthMeasurement(data[0].Time, mean);
            filteredList.Add(medianGrowthMeasurement);
        }

        private static void AddLastMedianElement(List<GrowthMeasurement> data, ref List<GrowthMeasurement> filteredList)
        {
            var window = new OrderedShiftBuffer(3);
            int index = data.Count - 1;
            window.Append(data[index - 1].OD);
            window.Append(data[index].OD);
            window.Append(data[index].OD);
            var median = (float)window.Median;
            var medianGrowthMeasurement = new GrowthMeasurement(data[index].Time, median);
            filteredList.Add(medianGrowthMeasurement);
        }

        private static void AddLastMeanElement(List<GrowthMeasurement> data, ref List<GrowthMeasurement> filteredList)
        {
            var window = new List<float>(3);
            int index = data.Count - 1;
            window.Add(data[index - 1].OD);
            window.Add(data[index].OD);
            window.Add(data[index].OD);
            var mean = window.Average();
            var medianGrowthMeasurement = new GrowthMeasurement(data[index].Time, mean);
            filteredList.Add(medianGrowthMeasurement);
        }

        private static List<GrowthMeasurement> BoxingSmootheningCore(List<GrowthMeasurement> datapoints, bool haltOnHighestPoint = false, GrowthMeasurement highestPoint = null)
        {
            if (haltOnHighestPoint)
                if (highestPoint == null)
                    throw new Exception("HighestPoint is Null");

            var temp = new List<GrowthMeasurement>();
            // Jonas asked me to keep the first value
            var firstMesurement = new GrowthMeasurement(datapoints[0].Time, datapoints[0].OD);
            temp.Add(firstMesurement);
            // Start from 1 so I can compare with a previous value
            var previousIndex = 0;
            int currentIndex;
            for (currentIndex = 1; currentIndex <= datapoints.Count - 2; currentIndex++)
            {
                GrowthMeasurement mesurement;
                if (haltOnHighestPoint && (datapoints.ElementAt(currentIndex).Time < highestPoint.Time))
                {
                    mesurement = new GrowthMeasurement(datapoints.ElementAt(currentIndex).Time, datapoints.ElementAt(currentIndex).OD);
                    temp.Add(mesurement);
                    break;
                }
                var afterIndex = currentIndex + 1;
                double avg = (datapoints.ElementAt(previousIndex).OD + datapoints.ElementAt(currentIndex).OD + datapoints.ElementAt(afterIndex).OD) / 3;
                mesurement = new GrowthMeasurement(datapoints.ElementAt(currentIndex).Time, (float)avg);
                temp.Add(mesurement);
                previousIndex = currentIndex;
            }
            return temp;
        }

        private static List<GrowthMeasurement> SmootheningCore(List<GrowthMeasurement> datapoints, bool haltOnHighestPoint = false, GrowthMeasurement highestPoint = null)
        {
            if (haltOnHighestPoint)
                if (highestPoint == null)
                    throw new Exception("HighestPoint is Null");

            var previous = 0;
            var currentIndex = 0;
            var noDrop = new List<GrowthMeasurement> {new GrowthMeasurement(datapoints[currentIndex].Time, datapoints[currentIndex].OD)};
            for (currentIndex = 1; currentIndex <= datapoints.Count - 1; currentIndex++)
            {
                if (haltOnHighestPoint && (datapoints.ElementAt(currentIndex).Time < highestPoint.Time))
                    break;
                var od = datapoints[currentIndex].OD;
                if (od < noDrop[previous].OD)
                    od = noDrop[previous].OD;
                noDrop.Add(new GrowthMeasurement(datapoints[currentIndex].Time, od));
                previous = currentIndex;
            }
            return noDrop;
        }

        private static GrowthMeasurement FindHighestODElement(List<GrowthMeasurement> datapoints)
        {
            int I;
            float highestOD = 0;
            float highestODTime = 0;
            for (I = 0; I <= datapoints.Count - 1; I++)
            {
                if (datapoints.ElementAt(I).OD > highestOD)
                {
                    highestOD = datapoints.ElementAt(I).OD;
                    highestODTime = datapoints.ElementAt(I).Time;
                }
            }
            return new GrowthMeasurement(highestODTime, highestOD);
        }

        private static void ListSort(ref List<GrowthMeasurement> dataPoints, Order order, MeasurmentField field)
        {
            switch (field)
            {
                case MeasurmentField.OD:
                    if (order == Order.Asc)
                        dataPoints.Sort((a, b) => a.OD.CompareTo(b.OD));
                    else
                        dataPoints.Sort((a, b) => b.OD.CompareTo(a.OD));
                    break;
                case MeasurmentField.Time:
                    if (order == Order.Asc)
                        dataPoints.Sort((a, b) => a.Time.CompareTo(b.Time));
                    else
                        dataPoints.Sort((a, b) => b.Time.CompareTo(a.Time));
                    break;
            }

        }

        private static void Report(string method, List<GrowthMeasurement> datapoints)
        {
            //Console.WriteLine("");
            //Console.WriteLine(method);
            //foreach (var measurement in datapoints)
            //{
            //    Console.WriteLine(measurement.Time + " | " + measurement.OD);
            //} 
        }

        private static void Report(string method, List<double> datapoints)
        {
            //Console.WriteLine("");
            //Console.WriteLine(method);
            //foreach (var measurement in datapoints)
            //{
            //    Console.WriteLine(measurement);
            //} 
        }

        private static void AntiLog10Datapoints(List<GrowthMeasurement> dataPoints)
        {
            foreach (var anchor in dataPoints)
                anchor.OD = (float)Math.Pow(10, anchor.OD);
        }

        private static List<GrowthMeasurement> AnitLog10DatapointsDeep(List<GrowthMeasurement> dataPoints)
        {
            var newPoints = new List<GrowthMeasurement>();
            foreach (var anchor in dataPoints)
            {
                var newPoint = new GrowthMeasurement();
                newPoint.Time = anchor.Time;
                newPoint.OD = (float)Math.Pow(10, anchor.OD);
                newPoints.Add(newPoint);
            }
            return newPoints;
        } 

        private static void AntiLog10Minus1Datapoints(List<GrowthMeasurement> dataPoints)
        {
            foreach (var anchor in dataPoints)
                anchor.OD = (float)Math.Pow(10, anchor.OD - 1);
        }

        private static List<GrowthMeasurement> AnitLog10Minus1DatapointsDeep(List<GrowthMeasurement> dataPoints)
        {
            var newPoints = new List<GrowthMeasurement>();
            foreach (var anchor in dataPoints)
            {
                var newPoint = new GrowthMeasurement();
                newPoint.Time = anchor.Time;
                newPoint.OD = (float)Math.Pow(10, anchor.OD -1);
                newPoints.Add(newPoint);
            }
            return newPoints;
        } 

        private static float AntiLog10Minus1Datapoints(float ground)
        {
            return (float)Math.Pow(10, ground - 1);
        }

        private static float AntiLog10(float ground)
        {
            return (float)Math.Pow(10, ground);
        }

        private static Func<Scope, object> GetCompiledFunction(string function)
        {
            var reg = new TypeRegistry();
            reg.RegisterType("Math", typeof(Math));
            var expression = new CompiledExpression(function) { TypeRegistry = reg };
            var func = expression.ScopeCompile<Scope>();
            return func;
        }

        private static string FormatFunction(string trueODFunction)
        {
            var function = string.Format(trueODFunction, "X");
            function = string.Format(CultureInfo.InvariantCulture, "Y = {0}", function);
            return function;
        }

        private static List<Slopes> GetSlopes(List<GrowthMeasurement> datapoints)
        {
            int i;
            var slopes = new List<Slopes>();
            for (i = 0; i <= datapoints.Count - 1; i++)
            {
                if ((i + 2) > (datapoints.Count - 1))
                    break;

                double previousOD = datapoints.ElementAt(i).OD;
                double previousTime = datapoints.ElementAt(i).Time;
                double nextOD = datapoints.ElementAt(i + 2).OD;
                double nextTime = datapoints.ElementAt(i + 2).Time;
                var slope = new Slopes();
                slope.Value = (nextOD - previousOD) / (nextTime - previousTime);
                slope.Anchor = datapoints.ElementAt(i + 1);

                if (slope.Value < 0)
                {
                    continue;
                    throw new Exception("Tell Jonas!");
                }



                slopes.Add(slope);
            }
            return slopes;
        }

        private static List<Slopes> GetLinnearregression(List<GrowthMeasurement> datapoints)
        {
            List<float> r2List;
            var slopeList = GetLinnearregression(datapoints, 0.01f, out r2List);
            return slopeList;
        }

        private static List<Slopes> GetLinnearregression(List<GrowthMeasurement> datapoints, float threshold, out List<float> fitList)
        {
            int i;
            var slopes = new List<Slopes>();
            var r2List = new List<float>();
            for (i = 0; i <= datapoints.Count - 1; i++)
            {
                var xVals = CreateTimeValArray(datapoints, i);
                var yVals = CreateODValArray(datapoints, i);
                
                double rsquared;
                double yintercept;
                double slope;
                HelperStatistics.LinearRegression(xVals, yVals, 0, 5, out rsquared, out yintercept, out slope);
                var t = Math.Abs(slope) >= threshold;
                AddR2ToList(threshold, rsquared, slope, r2List);
                AddSlopeToList(datapoints.ElementAt(i), slope, slopes);
            }
            fitList = r2List;
            return slopes;
        }

        private static List<Slopes> GetLinnearregressionAdvanced(List<GrowthMeasurement> datapoints, float threshold, out List<float> fitList)
        {
            int i;
            var slopes = new List<Slopes>();
            var r2List = new List<float>();
            for (i = 0; i <= datapoints.Count - 1; i++)
            {
                var xVals = CreateTimeValArray(datapoints, i);
                var yVals = CreateODValArray(datapoints, i);

                var ds = new Regression(xVals,yVals);

                var r2 = ds.ComputeRSquared();
                AddR2ToList(threshold, r2, ds.Slope, r2List);
                AddSlopeToList(datapoints.ElementAt(i), ds.Slope, slopes);
            }
            fitList = r2List;
            return slopes;
        }

        private static void AddSlopeToList(GrowthMeasurement anchor, double slope, List<Slopes> slopes)
        {
            var newSlope = new Slopes
            {
                Value = slope,
                Anchor = anchor
            };

            if (newSlope.Value < 0) return;

            slopes.Add(newSlope);
        }

        private static void AddR2ToList(float threshold, double rsquared, double slope, List<float> r2List)
        {
            if (double.IsNaN(rsquared) || double.IsInfinity(rsquared)) return;
            if (Math.Abs(slope) >= threshold)
                r2List.Add((float)rsquared);
        }

        private static double[] CreateTimeValArray(List<GrowthMeasurement> datapoints, int i)
        {
            var vals = new double[]
            {
                GetValidElementFromList(datapoints,i,LinearWindowPosition.P0).Time,
                GetValidElementFromList(datapoints,i,LinearWindowPosition.P1).Time,
                GetValidElementFromList(datapoints,i,LinearWindowPosition.P2).Time,
                GetValidElementFromList(datapoints,i,LinearWindowPosition.P3).Time,
                GetValidElementFromList(datapoints,i,LinearWindowPosition.P4).Time
            };
            return vals;
        }

        private static double[] CreateODValArray(List<GrowthMeasurement> datapoints, int i)
        {
            var vals = new double[]
            {
                GetValidElementFromList(datapoints,i,LinearWindowPosition.P0).OD,
                GetValidElementFromList(datapoints,i,LinearWindowPosition.P1).OD,
                GetValidElementFromList(datapoints,i,LinearWindowPosition.P2).OD,
                GetValidElementFromList(datapoints,i,LinearWindowPosition.P3).OD,
                GetValidElementFromList(datapoints,i,LinearWindowPosition.P4).OD
            };
            return vals;
        }

        private static double[] CreateTimeValArray6(List<GrowthMeasurement> datapoints, int i)
        {
            var vals = new double[]
            {
                GetValidElementFromList(datapoints,i,LinearWindowPosition.P0).Time,
                GetValidElementFromList(datapoints,i,LinearWindowPosition.P1).Time,
                GetValidElementFromList(datapoints,i,LinearWindowPosition.P2).Time,
                GetValidElementFromList(datapoints,i,LinearWindowPosition.P3).Time,
                GetValidElementFromList(datapoints,i,LinearWindowPosition.P4).Time,
                GetValidElementFromList(datapoints,i,LinearWindowPosition.P5).Time,
                GetValidElementFromList(datapoints,i,LinearWindowPosition.P6).Time
            };
            return vals;
        }

        private static double[] CreateODValArray6(List<GrowthMeasurement> datapoints, int i)
        {
            var vals = new double[]
            {
                GetValidElementFromList(datapoints,i,LinearWindowPosition.P0).OD,
                GetValidElementFromList(datapoints,i,LinearWindowPosition.P1).OD,
                GetValidElementFromList(datapoints,i,LinearWindowPosition.P2).OD,
                GetValidElementFromList(datapoints,i,LinearWindowPosition.P3).OD,
                GetValidElementFromList(datapoints,i,LinearWindowPosition.P4).OD,
                GetValidElementFromList(datapoints,i,LinearWindowPosition.P5).OD,
                GetValidElementFromList(datapoints,i,LinearWindowPosition.P6).OD
            };
            return vals;
        }
        
        private static GrowthMeasurement GetValidElementFromList(List<GrowthMeasurement> datapoints, int i, LinearWindowPosition position)
        {
            var pos = (int) position;
            var idx = i + pos;
            var lenght = datapoints.Count();
            GrowthMeasurement item;
            if (idx < 0)
                item = datapoints.ElementAtOrDefault(0);
            else if (idx > lenght - pos)
                item = datapoints.ElementAtOrDefault(lenght - 1);
            else
                item = datapoints.ElementAtOrDefault(idx);
            
            return item;
        }
    }
}
