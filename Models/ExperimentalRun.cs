using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace DataModels
{
    [DataContract]
    public class ExperimentalRun
    {
        [DataMember]
        public string ImportFileName { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public DateTime CreationDate { get; set; }
        [DataMember]
        public ExperimentType ExperimentType { get; set; }
        [DataMember]
        public BioscreenFileType FileType { get; set; }
        [DataMember]
        public ObservableCollection<Culture> Run { get; set; }

        public List<Culture> GetRun()
        {
            return new List<Culture>(Run);
        } 
        
        public Culture GetCulture(string container)
        {
            return Run.FirstOrDefault(x => x.Container == container);
        }

        [DataMember]
        public Range RunODRange { get; set; }
        [DataMember]
        public Range RunTimeRange { get; set; }

        [DataMember]
        public ReplicateSelection ReplicateBehaviour { get; set; }
        [DataMember]
        public int ReferenceCultureIndex { get; set; }

        public ExperimentalRun(string importFileName)
        {
            ImportFileName = importFileName;
            CreationDate = DateTime.Now;
            ExperimentType = ExperimentType.Cultures;

            Run = new ObservableCollection<Culture>();
        }

        public string GetTabularDelimitedOutput()
        {
            var paste = new StringBuilder();

            if (ExperimentType == ExperimentType.Cultures)
                paste.Append("Container Name").Append("\t").Append("Lag").Append("\t").Append("GT").Append("\t").Append("Yield").Append("\t")
                .Append("Problem found").Append("\t").Append("Details").Append("\t")
                .AppendLine();
            else
                paste.Append("Container Name").Append("\t").Append("AVG Lag").Append("\t").Append("AVG GT").Append("\t").Append("AVG Yield").Append("\t").Append("Elements in Sample").Append("\t")
                .Append("# Quality Warnings").Append("\t").Append("Details").Append("\t")
                .AppendLine();

            foreach (var culture in Run.Where(culture => !culture.IsFaulty))
            {
                paste.Append(culture.Container).Append("\t");
                paste.Append(culture.Lag.ToString()).Append("\t");
                paste.Append(culture.Rate.ToString()).Append("\t");
                paste.Append(culture.Yield.ToString()).Append("\t");
                if (culture.GetType() == typeof(MergedCulture))
                    paste.Append(((MergedCulture)culture).MergedContent).Append("\t");
                paste.Append(culture.QualityIndex.Flags).Append("\t");
                paste.Append(culture.QualityIndex.FlagDetails).Append("\t");
                paste.AppendLine();
            }
            return paste.ToString();
        }

        public string GetTabularDelimitedOutputQIdxNumerical()
        {
            var paste = new StringBuilder();

            if (ExperimentType == ExperimentType.Cultures)
                paste.Append("Container Name").Append("\t").Append("Lag").Append("\t").Append("GT").Append("\t").Append("Yield").Append("\t")
                .Append("QIdx R2").Append("\t").Append("QIdx Worst R2").Append("\t").Append("QIdx Peaks").Append("\t").Append("Point Difference").Append("\t")
                .AppendLine();
            else
                paste.Append("Container Name").Append("\t").Append("AVG Lag").Append("\t").Append("AVG GT").Append("\t").Append("AVG Yield").Append("\t").Append("Elements in Sample").Append("\t")
                .Append("QIdx R2").Append("\t").Append("QIdx Worst R2").Append("\t").Append("QIdx Peaks").Append("\t").Append("Point Difference").Append("\t")
                .AppendLine();

            foreach (var culture in Run.Where(culture => !culture.IsFaulty))
            {
                paste.Append(culture.Container).Append("\t");
                paste.Append(culture.Lag.ToString()).Append("\t");
                paste.Append(culture.Rate.ToString()).Append("\t");
                paste.Append(culture.Yield.ToString()).Append("\t");
                if (culture.GetType() == typeof(MergedCulture))
                    paste.Append(((MergedCulture)culture).MergedContent).Append("\t");
                paste.Append(culture.QualityIndex.R2).Append("\t");
                paste.Append(culture.QualityIndex.R2Worst).Append("\t");
                paste.Append(culture.QualityIndex.R2Peaks).Append("\t");
                paste.Append(culture.QualityIndex.PointDifference).Append("\t");
                paste.AppendLine();
            }
            return paste.ToString();
        }

        public string GetTabularDelimitedOutputExperiment(bool includeHeaders)
        {
            var paste = new StringBuilder();

            if (includeHeaders)
            {
                if (ExperimentType == ExperimentType.Cultures)
                    paste.Append("Experiment").Append("\t").Append("Container Name").Append("\t").Append("Lag").Append("\t").Append("GT").Append("\t").Append("Yield").Append("\t")
                    .Append("# Quality Warnings").Append("\t").Append("Details").Append("\t")
                    .AppendLine();
                else
                    paste.Append("Experiment").Append("\t").Append("Container Name").Append("\t").Append("AVG Lag").Append("\t").Append("AVG GT").Append("\t").Append("AVG Yield").Append("\t").Append("Elements in Sample").Append("\t")
                    .Append("# Quality Warnings").Append("\t").Append("Details").Append("\t")
                    .AppendLine();    
            }
            foreach (var culture in Run.Where(culture => !culture.IsFaulty))
            {
                paste.Append(System.IO.Path.GetFileNameWithoutExtension(ImportFileName)).Append("\t");
                paste.Append(culture.Container).Append("\t");
                paste.Append(culture.Lag.ToString()).Append("\t");
                paste.Append(culture.Rate.ToString()).Append("\t");
                paste.Append(culture.Yield.ToString()).Append("\t");
                if (culture.GetType() == typeof(MergedCulture))
                    paste.Append(((MergedCulture)culture).MergedContent).Append("\t");
                paste.Append(culture.QualityIndex.Flags).Append("\t");
                paste.Append(culture.QualityIndex.FlagDetails).Append("\t");
                paste.AppendLine();
            }
            return paste.ToString();
        }

        public string GetTabularDelimitedOutputExperimentQIdxNumeric(bool includeHeaders)
        {
            var paste = new StringBuilder();

            if (includeHeaders)
            {
                if (ExperimentType == ExperimentType.Cultures)
                    paste.Append("Experiment").Append("\t").Append("Container Name").Append("\t").Append("Lag").Append("\t").Append("GT").Append("\t").Append("Yield").Append("\t")
                    .Append("QIdx R2").Append("\t").Append("QIdx Worst R2").Append("\t").Append("QIdx Peaks").Append("\t").Append("Point Difference").Append("\t")
                    .AppendLine();
                else
                    paste.Append("Experiment").Append("\t").Append("Container Name").Append("\t").Append("AVG Lag").Append("\t").Append("AVG GT").Append("\t").Append("AVG Yield").Append("\t").Append("Elements in Sample").Append("\t")
                    .Append("QIdx R2").Append("\t").Append("QIdx Worst R2").Append("\t").Append("QIdx Peaks").Append("\t").Append("Point Difference").Append("\t")
                    .AppendLine();
            }
            foreach (var culture in Run.Where(culture => !culture.IsFaulty))
            {
                paste.Append(System.IO.Path.GetFileNameWithoutExtension(ImportFileName)).Append("\t");
                paste.Append(culture.Container).Append("\t");
                paste.Append(culture.Lag.ToString()).Append("\t");
                paste.Append(culture.Rate.ToString()).Append("\t");
                paste.Append(culture.Yield.ToString()).Append("\t");
                if (culture.GetType() == typeof(MergedCulture))
                    paste.Append(((MergedCulture)culture).MergedContent).Append("\t");
                paste.Append(culture.QualityIndex.R2).Append("\t");
                paste.Append(culture.QualityIndex.R2Worst).Append("\t");
                paste.Append(culture.QualityIndex.R2Peaks).Append("\t");
                paste.Append(culture.QualityIndex.PointDifference).Append("\t");
                paste.AppendLine();
            }
            return paste.ToString();
        }

        public string GetTabularDelimitedCurveOutput(DataType dataType)
        {
            var paste = new StringBuilder();

            var curveList = new Dictionary<string,List<GrowthMeasurement>>();
            
            foreach (var culture in Run)
            {
                foreach (var measurement in culture.GrowthMeasurements.Measurements.Where(measurement => measurement.Key == dataType).Where(measurement => !culture.IsFaulty))
                    curveList.Add(culture.Container, measurement.Value);
            }
            
            var timeList = curveList.OrderByDescending(m => m.Value.Count).First().Value.Select(longestList => longestList.Time.ToString()).ToList();
            var serieList = curveList.Select(value => value.Value.Select(measurement => measurement.OD.ToString()).ToList()).ToList();

            paste.Append("Time");
            foreach (KeyValuePair<string, List<GrowthMeasurement>> keyValuePair in curveList)
            {
                paste.Append("\t").Append(keyValuePair.Key);
            }
            paste.AppendLine();
            foreach (var time in timeList)
            {
                paste.Append(time);
                foreach (var serie in serieList)
                {
                    if (serie.Count > 0)
                    {
                        var firstElement = serie.First();
                        paste.Append("\t").Append(firstElement);
                        serie.RemoveAt(0);
                    }
                    else
                    {
                        paste.Append('\t');
                    }
                }
                paste.AppendLine();
            }
            return paste.ToString();
        }

    }
}
