using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DataModels;

namespace Macro
{
    public static class ProcessCulture
    {
        private const int FirstDerivWindow = 3;

        public static string TrueODCalibartionFunction { get; set; }
        public static float BlankValue { get; set; }

        public static void AddRawThinned(ref Culture culture, int skip = 0, bool isFirstDeriv = false, CalibrationFunction calibrationFunction = null)
        {
            var raw = culture.GrowthMeasurements.GetMeasurements(DataType.Raw);
            var thinRaw = new List<GrowthMeasurement>();
            for (int i = 0; i < raw.Count; i += skip + 1)
            {
                var measurement = raw[i];
                thinRaw.Add(measurement);
            }
            culture.GrowthMeasurements.SetMeasurements(thinRaw, DataType.RawThinned);
            var processesThinned = Modules.ReProccessedDatapoints(thinRaw, BlankValue, calibrationFunction);
            if (isFirstDeriv)
            {
                var firstDeriv = Modules.CalculateFirstDerivative(processesThinned, FirstDerivWindow);
                culture.GrowthMeasurements.SetMeasurements(firstDeriv, DataType.FirstDerivative);
            }
            culture.GrowthMeasurements.SetMeasurements(processesThinned, DataType.ProcessedThinned);
            
            if (Modules.NoGrowth(processesThinned)) return;
            var metaDataThinned = new GrowthVariableMetaData();
            var macroLagData = Modules.GetLag(processesThinned);
            metaDataThinned.Lag = macroLagData.InterceptStretchs;
            var macroYieldData = Modules.GetYield(processesThinned);
            metaDataThinned.Yield = macroYieldData.YieldAnchors;
            var macroRateData = Modules.GetGT(processesThinned);
            metaDataThinned.Rate = macroRateData.RateSlopeAnchors;
            culture.GrowthMeasurements.SetMetaData(DataType.ProcessedThinned, metaDataThinned);
        }
        
        public static void Process(Culture culture, ref float maxTime, ref float minOD, ref float maxOD, float blankValue, bool doFit, string trueODCalibartionFunction, RateTraitExtractionMethod rateTraitExtractionMethod, bool skipMonotonicFilter)
        {
            ProcessData(culture, blankValue, doFit, trueODCalibartionFunction, skipMonotonicFilter);

            UpdateMaximums(culture, ref maxTime, ref maxOD, ref minOD);
            ExtractTraits(culture, rateTraitExtractionMethod);
        }

        public static void Process(Culture culture, ref float maxTime, ref float minOD, ref float maxOD, float blankValue, bool doFit, float calibrationCoefA, float calibrationCoefB, float calibrationCoefC, RateTraitExtractionMethod rateTraitExtractionMethod, bool skipMonotonicFilter = false)
        {
            var trueODCalibartionFunction = SetCalibrationFunction(calibrationCoefA, calibrationCoefB, calibrationCoefC);

            ProcessData(culture, blankValue, doFit, trueODCalibartionFunction,skipMonotonicFilter);

            UpdateMaximums(culture, ref maxTime, ref maxOD, ref minOD);
            ExtractTraits(culture, rateTraitExtractionMethod);
        }

        public static void ReProcess(Culture culture)
        {
            var raw = culture.GrowthMeasurements.GetMeasurements(DataType.Raw);

            var thinRaw = culture.GrowthMeasurements.GetMeasurements(DataType.RawThinned);
            if (thinRaw != null)
            {
                var processesThinned = Modules.ProccessedDatapoints(thinRaw, BlankValue, TrueODCalibartionFunction);
                culture.GrowthMeasurements.SetMeasurements(processesThinned, DataType.ProcessedThinned);

                if (!Modules.NoGrowth(processesThinned))
                {
                    var metaDataThinned = new GrowthVariableMetaData();
                    var macroLagData = Modules.GetLag(processesThinned);
                    metaDataThinned.Lag = macroLagData.InterceptStretchs;
                    var macroYieldData = Modules.GetYield(processesThinned);
                    metaDataThinned.Yield = macroYieldData.YieldAnchors;
                    var macroRateData = Modules.GetGT(processesThinned);
                    metaDataThinned.Rate = macroRateData.RateSlopeAnchors;
                    culture.GrowthMeasurements.SetMetaData(DataType.ProcessedThinned, metaDataThinned);
                }
            }
            var smooth = Modules.ProccessedDatapoints(raw, BlankValue, TrueODCalibartionFunction);
            culture.GrowthMeasurements.SetMeasurements(smooth, DataType.Processed);
            var firstDeriv = Modules.CalculateFirstDerivative(smooth, FirstDerivWindow);
            culture.GrowthMeasurements.SetMeasurements(firstDeriv, DataType.FirstDerivative);
            culture.GrowthMeasurements.SetMeasurements(Modules.ConvertBioScreenTimeToHrs(raw), DataType.Raw);

            double lag;
            double yield;
            double gt;
            var metaData = new GrowthVariableMetaData();
            var processed = culture.GrowthMeasurements.GetMeasurements(DataType.Processed);
            if (Modules.NoGrowth(processed))
            {
                lag = 48;
                yield = double.NaN;
                gt = double.NaN;
            }
            else
            {
                var macroLagData = Modules.GetLag(processed);
                lag = macroLagData.Lag;
                metaData.Lag = macroLagData.InterceptStretchs;
                var macroYieldData = Modules.GetYield(processed);
                yield = macroYieldData.Yield;
                metaData.Yield = macroYieldData.YieldAnchors;
                var macroRateData = Modules.GetGT(processed);
                gt = macroRateData.GT;
                metaData.Rate = macroRateData.RateSlopeAnchors;
            }

            culture.Lag = lag;
            culture.Rate = gt;
            culture.Yield = yield;
            culture.GrowthMeasurements.SetMetaData(DataType.Processed, metaData);
        }

        public static string GetThinnedDataPreview(ref Culture culture, NeuralParameters parameters)
        {

            var thinned = Modules.Clone(culture.GrowthMeasurements.GetMeasurements(DataType.ProcessedThinned));

            var lag = new MacroLagData();
            var yield = new MacroYieldData();
            var gt = new MacroRateData();

            if (Modules.NoGrowth(thinned))
            {
                lag.Lag = 48;
                yield.Yield = double.NaN;
                gt.GT = double.NaN;
            }
            else
            {
                lag = Modules.GetLag(thinned);
                yield = Modules.GetYield(thinned);
                gt = Modules.GetGT(thinned);
            }
            string format = "00.0000";
            return string.Format(CultureInfo.InvariantCulture, "Prev Lag = {0}, Fitted Lag = {1} \r\nPrev GT = {2}, Fitted Rate = {3} \r\nPrev Yield = {4}, Fitted Yield = {5}", culture.Lag.ToString(format), lag.Lag.ToString(format), culture.Rate.ToString(format), gt.GT.ToString(format), culture.Yield.ToString(format), yield.Yield.ToString(format));
        }

        public static string GetFittedDataPreview(ref Culture culture, NeuralParameters parameters)
        {

            var raw = Modules.Clone(culture.GrowthMeasurements.GetMeasurements(DataType.Raw));
            var fitted = Modules.ProccessedAndFitDatapoints(raw, 0, parameters, TrueODCalibartionFunction);
            
            culture.GrowthMeasurements.SetMeasurements(fitted,DataType.ProcessedFitted);

            var lag = new MacroLagData();
            var yield =  new MacroYieldData();
            var gt = new MacroRateData();

            if (Modules.NoGrowth(fitted))
            {
                lag.Lag = 48;
                yield.Yield = double.NaN;
                gt.GT = double.NaN;
            }
            else
            {
                lag = Modules.GetLag(fitted);
                yield = Modules.GetYield(fitted);
                gt = Modules.GetGT(fitted);
            }
            string format = "00.0000";
            return string.Format(CultureInfo.InvariantCulture, "Prev Lag = {0}, Fitted Lag = {1} \r\nPrev GT = {2}, Fitted Rate = {3} \r\nPrev Yield = {4}, Fitted Yield = {5}", culture.Lag.ToString(format), lag.Lag.ToString(format), culture.Rate.ToString(format), gt.GT.ToString(format), culture.Yield.ToString(format), yield.Yield.ToString(format));
        }

        public static void UpdateFittedData(ref Culture culture)
        {

            var fitted = culture.GrowthMeasurements.GetMeasurements(DataType.ProcessedFitted);

            culture.GrowthMeasurements.SetMeasurements(fitted, DataType.Processed);
            culture.GrowthMeasurements.Measurements.Remove(DataType.ProcessedFitted);
            
            var lag = new MacroLagData();
            var yield = new MacroYieldData();
            var gt = new MacroRateData();

            if (Modules.NoGrowth(fitted))
            {
                lag.Lag = 48;
                yield.Yield = double.NaN;
                gt.GT = double.NaN;
            }
            else
            {
                lag = Modules.GetLag(fitted);
                yield = Modules.GetYield(fitted);
                gt = Modules.GetGT(fitted);
            }
            
            culture.Lag = lag.Lag;
            culture.GrowthMeasurements.VariableMetaDatas[DataType.Processed].Lag = lag.InterceptStretchs;
            culture.Rate = gt.GT;
            culture.GrowthMeasurements.VariableMetaDatas[DataType.Processed].Rate = gt.RateSlopeAnchors;
            culture.Yield = yield.Yield;
            culture.GrowthMeasurements.VariableMetaDatas[DataType.Processed].Yield = yield.YieldAnchors;

        }

        private static void ProcessData(Culture culture, float blankValue, bool doFit, CalibrationFunction trueODCalibartionFunction, bool skipMonotonicFilter = false)
        {
            var raw = culture.GrowthMeasurements.GetMeasurements(DataType.Raw);

            var count = 0;
            float maxPeak = 0;
            var treated = Modules.ProccessedDatapoints(raw, blankValue, trueODCalibartionFunction, out count, out maxPeak, false, false,skipMonotonicFilter);
            treated = NNSmoothProcessedData(false, raw, treated, blankValue, trueODCalibartionFunction, count, maxPeak);

            SetRawFilteredData(false, raw, ref culture);
            SetRawUnFilteredData(false, raw, ref culture, blankValue);

            var uncalibrated = Modules.ProccessedDatapoints(raw, blankValue, trueODCalibartionFunction, out count, out maxPeak, false,true);
            culture.GrowthMeasurements.SetMeasurements(uncalibrated, DataType.ProcessedUncalibrated);

            if (count > 0)
                SessionVariables.Peaks.Add(new Peak { Well = culture.Container, MaxPeak = maxPeak, Number = count });

            culture.GrowthMeasurements.SetMeasurements(treated, DataType.Processed);

            var firstDeriv = Modules.CalculateFirstDerivative(treated, FirstDerivWindow);
            culture.GrowthMeasurements.SetMeasurements(firstDeriv, DataType.FirstDerivative);
            SetNNSmoothFirstDeriv(false, firstDeriv, ref culture);

            culture.GrowthMeasurements.SetMeasurements(Modules.ConvertBioScreenTimeToHrs(raw), DataType.Raw);

            if (doFit)
                SetNNSmoothProcessed(culture, treated);
        }

        private static void ProcessData(Culture culture, float blankValue, bool doFit, string trueODCalibartionFunction, bool skipMonotonicFilter)
        {
            var raw = culture.GrowthMeasurements.GetMeasurements(DataType.Raw);

            raw = NNsmoothRawData(false, raw);
            SetThinRawData(false, ref culture, blankValue, trueODCalibartionFunction);

            var count = 0;
            float maxPeak = 0;
            var treated = Modules.ProccessedDatapoints(raw, blankValue, trueODCalibartionFunction, out count, out maxPeak, false, false, skipMonotonicFilter);
            treated = NNSmoothProcessedData(false, raw, treated, blankValue, trueODCalibartionFunction, count, maxPeak);

            SetRawFilteredData(false, raw, ref culture);
            SetRawUnFilteredData(false, raw, ref culture, blankValue);

            var uncalibrated = Modules.ProccessedDatapoints(raw, blankValue, trueODCalibartionFunction, out count, out maxPeak, false, true);
            culture.GrowthMeasurements.SetMeasurements(Modules.ConvertBioScreenTimeToHrs(uncalibrated), DataType.ProcessedUncalibrated);
            
            if (count > 0)
                SessionVariables.Peaks.Add(new Peak { Well = culture.Container, MaxPeak = maxPeak, Number = count });

            culture.GrowthMeasurements.SetMeasurements(treated, DataType.Processed);

            var firstDeriv = Modules.CalculateFirstDerivative(treated, FirstDerivWindow);
            culture.GrowthMeasurements.SetMeasurements(firstDeriv, DataType.FirstDerivative);
            SetNNSmoothFirstDeriv(false, firstDeriv, ref culture);

            culture.GrowthMeasurements.SetMeasurements(Modules.ConvertBioScreenTimeToHrs(raw), DataType.Raw);

            if (doFit)
                SetNNSmoothProcessed(culture, treated);
        }

        private static void SetRawUnFilteredData(bool apply, List<GrowthMeasurement> raw, ref Culture culture, float blankValue)
        {
            if (!apply) return;
            var processedUnfiltered = Modules.ProccessedDatapointsNoFileter(raw, blankValue, TrueODCalibartionFunction);
            culture.GrowthMeasurements.SetMeasurements(processedUnfiltered, DataType.Unfiltered);
        }

        private static List<GrowthMeasurement> NNSmoothProcessedData(bool apply, List<GrowthMeasurement> raw, List<GrowthMeasurement> smooth, float blankValue, string trueODCalibartionFunction, int count, float maxPeak)
        {
            if (!apply) return smooth;
            var neuroD = new Neural();
            var fittedRaw = neuroD.GetFittedData(raw);
            return Modules.ProccessedDatapoints(fittedRaw, blankValue, trueODCalibartionFunction, out count, out maxPeak, true);
        }

        private static List<GrowthMeasurement> NNSmoothProcessedData(bool apply, List<GrowthMeasurement> raw, List<GrowthMeasurement> smooth, float blankValue, CalibrationFunction trueODCalibartionFunction, int count, float maxPeak)
        {
            if (!apply) return smooth;
            var neuroD = new Neural();
            var fittedRaw = neuroD.GetFittedData(raw);
            return Modules.ProccessedDatapoints(fittedRaw, blankValue, trueODCalibartionFunction, out count, out maxPeak, true);
        } 
        
        private static void UpdateMaximums(Culture culture, ref float maxTime, ref float maxOD, ref float minOD)
        {
            var processed = culture.GrowthMeasurements.GetMeasurements(DataType.Processed);
            var raw = culture.GrowthMeasurements.GetMeasurements(DataType.Raw);

            var maxProcTime = (double)processed.Max(m => m.Time);
            var maxRawTime = (double)raw.Max(m => m.Time);
            var cultureMaxTime = maxProcTime > maxRawTime ? maxProcTime : maxRawTime;

            var maxProcOD = (double)processed.Max(m => m.OD);
            var maxRawOD = (double)raw.Max(m => m.OD);
            var cultureMaxOD = maxProcOD > maxRawOD ? maxProcOD : maxRawOD;

            var minProcOD = (double)processed.Min(m => m.OD);
            var minRawOD = (double)raw.Min(m => m.OD);
            var cultureMinOD = minProcOD > minRawOD ? minProcOD : minRawOD;

            if (cultureMaxTime > maxTime)
                maxTime = (float)cultureMaxTime;

            if (cultureMaxOD > maxOD)
                maxOD = (float)cultureMaxOD;

            if (cultureMinOD < minOD)
                minOD = (float)cultureMinOD;
        }

        private static void ExtractTraits(Culture culture, RateTraitExtractionMethod rateTraitExtractionMethod)
        {
            const bool getRawMetada = false;
            double lag;
            double yield;
            double gt;
            var qIdx = new QualityIndex();

            var processed = culture.GrowthMeasurements.GetMeasurements(DataType.Processed);
            if (Modules.NoGrowth(processed))
            {
                lag = 48;
                yield = double.NaN;
                gt = double.NaN;
            }
            else
            {
                var metaData = new GrowthVariableMetaData();
                var macroLagData = Modules.GetLag(processed);
                lag = macroLagData.Lag;
                metaData.Lag = macroLagData.InterceptStretchs;
                var macroYieldData = Modules.GetYield(processed);
                yield = macroYieldData.Yield;
                metaData.Yield = macroYieldData.YieldAnchors;
                //var macroRateData = Modules.GetGtSim(processed);
                MacroRateData macroRateData;
                switch (rateTraitExtractionMethod)
                {
                    case RateTraitExtractionMethod.Default:
                        macroRateData = Modules.GetGT(processed);
                        break;
                    case RateTraitExtractionMethod.LinearRegression:
                        macroRateData = Modules.GetGTLinearRegression(processed);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(rateTraitExtractionMethod), rateTraitExtractionMethod, null);
                }
                gt = macroRateData.GT;
                metaData.Rate = macroRateData.RateSlopeAnchors;
                qIdx = Modules.GetQualityIndex(culture, DataType.Raw);

                culture.GrowthMeasurements.SetMetaData(DataType.Processed, metaData);

                if (getRawMetada)
                {
                    var raw = culture.GrowthMeasurements.GetMeasurements(DataType.Raw);
                    var rawMetaData = new GrowthVariableMetaData();
                    var rawMacroLagData = Modules.GetLag(raw);
                    rawMetaData.Lag = rawMacroLagData.InterceptStretchs;
                    var rawMacroYieldData = Modules.GetYield(raw);
                    rawMetaData.Yield = rawMacroYieldData.YieldAnchors;
                    var rawMacroRateData = Modules.GetGT(raw);
                    rawMetaData.Rate = rawMacroRateData.RateSlopeAnchors;
                    culture.GrowthMeasurements.SetMetaData(DataType.Raw, rawMetaData);
                }
            }
            culture.Lag = lag;
            culture.Rate = gt;
            culture.Yield = yield;
            culture.QualityIndex = qIdx;
        }

        private static CalibrationFunction SetCalibrationFunction(float calibrationCoefA, float calibrationCoefB, float calibrationCoefC)
        {
            var trueODCalibartionFunction = new CalibrationFunction();
            trueODCalibartionFunction.AddTerm(0, calibrationCoefA);
            trueODCalibartionFunction.AddTerm(1, calibrationCoefB);
            trueODCalibartionFunction.AddTerm(2, calibrationCoefC);
            return trueODCalibartionFunction;
        }

        private static void SetNNSmoothProcessed(Culture culture, List<GrowthMeasurement> smooth)
        {
            var neuro = new Neural();
            var fitted = Modules.Clone(smooth);
            fitted = neuro.GetFittedData(fitted);
            culture.GrowthMeasurements.SetMeasurements(fitted, DataType.Fitted);
        }

        private static List<GrowthMeasurement> NNsmoothRawData(bool apply, List<GrowthMeasurement> raw)
        {
            if (!apply) return raw;
            var neuroD = new Neural();
            var fittedRaw = neuroD.GetFittedData(raw);
            return fittedRaw;
        }

        private static void SetNNSmoothFirstDeriv(bool apply, List<GrowthMeasurement> firstDeriv, ref Culture culture)
        {
            if (!apply) return;
            var neuroD = new Neural();
            var filteredDer = neuroD.GetFittedData(firstDeriv);
            culture.GrowthMeasurements.SetMeasurements(filteredDer, DataType.FirstDerivative);
        }

        private static void SetThinRawData(bool apply, ref Culture culture, float blankValue, string trueODCalibartionFunction)
        {
            if (!apply) return;
            AddRawThinned(ref culture, 8);
            var thinRaw = culture.GrowthMeasurements.GetMeasurements(DataType.RawThinned);
            var processesThinned = Modules.ProccessedDatapoints(thinRaw, blankValue, trueODCalibartionFunction);
            culture.GrowthMeasurements.SetMeasurements(processesThinned, DataType.ProcessedThinned);
        }

        private static void SetThinRawData(bool apply, ref Culture culture, float blankValue, CalibrationFunction trueODCalibartionFunction, int pointsToSkip = 0)
        {
            if (!apply) return;
            AddRawThinned(ref culture, pointsToSkip, false, trueODCalibartionFunction);
            var thinRaw = culture.GrowthMeasurements.GetMeasurements(DataType.RawThinned);
            var processesThinned = Modules.ProccessedDatapoints(thinRaw, blankValue, trueODCalibartionFunction);
            culture.GrowthMeasurements.SetMeasurements(processesThinned, DataType.ProcessedThinned);
        }

        private static void SetRawFilteredData(bool apply, List<GrowthMeasurement> raw, ref Culture culture)
        {
            if (!apply) return;
            var filteredRaw = Modules.Clone(raw);
            filteredRaw = Modules.ApplyMedianFiter(filteredRaw);
            culture.GrowthMeasurements.SetMeasurements(filteredRaw, DataType.RawFiltered);
        }
    }
}
