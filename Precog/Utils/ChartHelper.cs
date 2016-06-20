using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xaml;
using DataModels;
using Visiblox.Charts;
using Visiblox.Charts.Primitives;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace Precog.Utils
{
    public class ChartHelper
    {
        public class GrowthSeries
        {
            public DataSeries<double, double> Series { get; set; }
            public DataType DataType { get; set; }

            public GrowthSeries()
            {
                Series = new DataSeries<double, double>();
            }
        }
        
        public class GrowthGraph
        {
            public List<GrowthSeries> GrowthSeries { get; set; }

            public string ContainerName { get; set; }
            public IRange ODRange { get; set; }
            public IRange TimeRange { get; set; }

            public GrowthGraph(string container)
            {
                GrowthSeries = new List<GrowthSeries>();
                ContainerName = container;
            }
        }

        public static ImageSource CreateChartImage(ExperimentalRun experimentalRun, int cultureIndex, List<DataType> validTypes, int height, int width, int padding, bool logged)
        {
            GrowthGraph growthGraph;
            var fd = false;
            if (validTypes.Contains(DataType.FirstDerivative))
            {
                var culture = experimentalRun.Run.FirstOrDefault(exp => exp.ContainerIndex == cultureIndex);
                var referenceCulture = experimentalRun.Run.FirstOrDefault(exp => exp.ContainerIndex == experimentalRun.ReferenceCultureIndex);
                growthGraph = AsGrowthGraph(culture, referenceCulture, validTypes);
                logged = false;
                fd = true;
            }
            else
                growthGraph = AsGrowthGraph(experimentalRun, cultureIndex, validTypes);
            
            return CreateChartImage(growthGraph, height, width, padding, logged, fd);
        }

        public static ImageSource CreateChartImage(GrowthGraph growthGraph, int height, int width, int padding, bool logged, bool firstDerivative)
        {
            InvalidationHandler.ForceImmediateInvalidate = true;

            var chartSize = new Size(width , height);
            var chartSize2 = new Size(width, height+5);
            var chart = new Chart();
            chart.Height = chartSize.Height;
            chart.Width = chartSize.Width;
            chart.LegendVisibility = Visibility.Collapsed;

            foreach (var series in growthGraph.GrowthSeries)
            {
                var newSerieRaw = new LineSeries {DataSeries = series.Series, LineStrokeThickness = 2};
                switch (series.DataType)
                {
                    case DataType.FirstDerivative:
                        var blueColorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#088DA5"));
                        newSerieRaw.LineStroke = blueColorBrush;
                        break;
                    case DataType.Raw:
                        var RedColorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff0000"));
                        newSerieRaw.LineStroke = RedColorBrush;
                        break;
                    case DataType.Processed:
                        var blackColorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000"));
                        newSerieRaw.LineStroke = blackColorBrush;
                        break;
                }
                chart.Series.Add(newSerieRaw);
            }

            if (logged)
                chart.YAxis = new LogarithmicAxis();

            chart.XAxis.Range = growthGraph.TimeRange;
            var buffer = (double)growthGraph.ODRange.Maximum*.20;
            chart.YAxis.Range = new DoubleRange((double) growthGraph.ODRange.Minimum, (double) growthGraph.ODRange.Maximum + buffer);

            chart.HorizontalAlignment = HorizontalAlignment.Center;
            chart.VerticalAlignment = VerticalAlignment.Center;
            chart.VerticalContentAlignment =VerticalAlignment.Center;
            chart.HorizontalContentAlignment = HorizontalAlignment.Center;
            chart.Padding = new Thickness(0);
            chart.Margin =new Thickness(0);
            
            chart.Measure(chartSize2);
            chart.Arrange(new Rect(chartSize2));
            chart.UpdateLayout();

            var rtb = new RenderTargetBitmap((int)chart.Width, (int)chart.Height+10, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(chart);

            InvalidationHandler.ForceImmediateInvalidate = false;

            return rtb;
        }

        public static GrowthGraph AsGrowthGraph(ExperimentalRun experimentalRun, int cultureIndex, List<DataType> validTypes)
        {
            Culture culture = experimentalRun.Run.Where(exp => exp.ContainerIndex == cultureIndex).FirstOrDefault();
            Culture referenceCulture = null;
            if (experimentalRun.ReferenceCultureIndex != -1)
            {
                referenceCulture = experimentalRun.Run.Where(exp => exp.ContainerIndex == experimentalRun.ReferenceCultureIndex).FirstOrDefault();
            }

            if (culture != null)
            {
                var graph = new GrowthGraph(culture.Container);
                if (culture.GetType() == typeof(MergedCulture))
                {
                    CreateSeriesFromMergedCulture(((MergedCulture)culture), validTypes, ref graph);
                    if (referenceCulture != null)
                        CreateSeriesFromMergedCulture(((MergedCulture)culture), validTypes, ref graph);
                }
                else if (culture.GetType() == typeof (Culture))
                {
                    string serieName = GetSeriesName(culture.IsFaulty, culture.Container);
                    CreateSeriesFromCulture(culture.GrowthMeasurements, serieName, validTypes, ref graph);
                    if (referenceCulture != null)
                        CreateSeriesFromCulture(referenceCulture.GrowthMeasurements, serieName, validTypes, ref graph);
                }

                graph.ODRange = new DoubleRange(experimentalRun.RunODRange.Min, experimentalRun.RunODRange.Max);
                graph.TimeRange = new DoubleRange(experimentalRun.RunTimeRange.Min, experimentalRun.RunTimeRange.Max); 

                return graph;
            }
            return null;
        }

        private static void CreateSeriesFromMergedCulture(MergedCulture mergedCulture, List<DataType> validTypes, ref GrowthGraph graph)
        {
            foreach (var growthMeasurementsKeyPair in mergedCulture.MergedGrowthMeasurements)
            {
                string serieName = GetSeriesName(growthMeasurementsKeyPair.Value.IsFaulty, growthMeasurementsKeyPair.Key);
                    CreateSeriesFromCulture(growthMeasurementsKeyPair.Value, serieName, validTypes, ref graph);
            }
        }

        private static void CreateSeriesFromCulture(GrowthMeasurements growthMeasurements, string container, List<DataType> validTypes, ref GrowthGraph graph)
        {
            foreach (KeyValuePair<DataType, List<GrowthMeasurement>> pair in growthMeasurements.Measurements)
            {
                if (!validTypes.Contains(pair.Key))
                    continue;
                if (pair.Value.Count > 0)
                {
                    string type = string.Format(CultureInfo.InvariantCulture, "({0})", pair.Key.ToString());
                    var newGraph = CreateNewSeries(pair.Value, container + type);
                    graph.GrowthSeries.Add(new GrowthSeries { DataType = pair.Key, Series = newGraph });
                }
            }
        }

        public static GrowthGraph AsGrowthGraph(Culture culture, Culture referenceCulture, List<DataType> validTypes)
        {
            var graph = new GrowthGraph(culture.Container);

            var timeRange = new Range(GrowthRangeType.Time, float.MinValue, 0);
            var odRange = new Range(GrowthRangeType.OD, float.MinValue, float.MaxValue);

            if (culture.GetType() == typeof(MergedCulture))
            {
                CreateSeriesFromMergedCulture((MergedCulture)culture, validTypes, ref graph, ref odRange, ref timeRange);
                if (referenceCulture != null)
                    CreateSeriesFromMergedCulture((MergedCulture)referenceCulture, validTypes, ref graph, ref odRange, ref timeRange);
            }
            else if (culture.GetType() == typeof (Culture))
            {
                string serieName = GetSeriesName(culture.IsFaulty, culture.Container);
                CreateSeriesFromCulture(culture.GrowthMeasurements, serieName, validTypes, ref graph, ref odRange, ref timeRange);
                if (referenceCulture != null)
                    CreateSeriesFromCulture(referenceCulture.GrowthMeasurements, serieName, validTypes, ref graph, ref odRange, ref timeRange);
            }
            graph.ODRange = new DoubleRange(odRange.Min, odRange.Max);
            graph.TimeRange = new DoubleRange(timeRange.Min, timeRange.Max);

            return graph;
        }

        private static void CreateSeriesFromMergedCulture(MergedCulture mergedCulture, List<DataType> validTypes, ref GrowthGraph graph, ref Range odRange, ref Range timeRange)
        {
            foreach (var growthMeasurementsKeyPair in mergedCulture.MergedGrowthMeasurements)
            {
                    string serieName = GetSeriesName(growthMeasurementsKeyPair.Value.IsFaulty, growthMeasurementsKeyPair.Key);
                    CreateSeriesFromCulture(growthMeasurementsKeyPair.Value, serieName, validTypes, ref graph, ref odRange, ref timeRange);
            }
        }

        private static string GetSeriesName(bool isFaulty, string name)
        {
            string serieName = name;
            if(isFaulty)
                serieName += "*";
            return serieName;
        }

        private static void CreateSeriesFromCulture(GrowthMeasurements growthMeasurements, string container, List<DataType> validTypes, ref GrowthGraph graph, ref Range odRange, ref Range timeRange)
        {
            foreach (KeyValuePair<DataType, List<GrowthMeasurement>> pair in growthMeasurements.Measurements)
            {
                if(!validTypes.Contains(pair.Key))
                    continue;
                if (pair.Value.Count > 0)
                {
                    string type = string.Format(CultureInfo.InvariantCulture, "({0})", pair.Key.ToString());
                    var newGraph = CreateNewSeries(pair.Value, container + type + " [" + pair.Value.Count + "]", ref odRange, ref timeRange);
                    graph.GrowthSeries.Add(new GrowthSeries { DataType = pair.Key, Series = newGraph });
                }
            }
    }

        private static DataSeries<double, double> CreateNewSeries(List<GrowthMeasurement> rawGrowthMeasurements, string title, ref Range odRange, ref Range timeRange)
        {
            var newRawGraph = new DataSeries<double, double>(title);
            foreach (var growthMeasurement in rawGrowthMeasurements)
            {
                odRange.SetMaxOnlyIfGreater(growthMeasurement.OD);
                odRange.SetMinOnlyIfSmaller(growthMeasurement.OD);
                timeRange.SetMaxOnlyIfGreater(growthMeasurement.Time);
                newRawGraph.Add(growthMeasurement.Time, growthMeasurement.OD);
            }
            return newRawGraph;
        }

        private static DataSeries<double, double> CreateNewSeries(List<GrowthMeasurement> rawGrowthMeasurements, string title)
        {
            var newRawGraph = new DataSeries<double, double>(title);
            foreach (var growthMeasurement in rawGrowthMeasurements)
            {
                newRawGraph.Add(growthMeasurement.Time, growthMeasurement.OD);
            }
            return newRawGraph;
        }
    }
}
