using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DataModels;
using Macro;
using Precog.Utils;
using Visiblox.Charts;
using Visiblox.Charts.Primitives;

namespace Precog.Controls
{
    /// <summary>
    /// Interaction logic for zoomGraph.xaml
    /// </summary>
    public partial class zoomGraph : UserControl
    {
        private const string MetaData = "Meta";
        private const int LeyendFontSize = 50;
        private const bool GlobalRange = false;

        #region NeuralParameters
        public NeuralParameters NeuralParameters
        {
            get { return (NeuralParameters)GetValue(NeuralParametershProperty); }
            set { SetValue(NeuralParametershProperty, value); }
        }

        public static readonly DependencyProperty NeuralParametershProperty =
            DependencyProperty.Register("NeuralParameters", typeof(NeuralParameters), typeof(zoomGraph), new PropertyMetadata(null, OnNeuralParametersPropertyChanged));


        private static void OnNeuralParametersPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as zoomGraph;
            myObj.OnNeuralParametersPropertyChanged(e);
        }

        private void OnNeuralParametersPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (NeuralParameters != null)
            {
            }
        }

        #endregion

        #region ExperimentalRun
        public ExperimentalRun ExperimentalRun
        {
            get { return (ExperimentalRun)GetValue(ExperimentalRunProperty); }
            set { SetValue(ExperimentalRunProperty, value); }
        }

        public static readonly DependencyProperty ExperimentalRunProperty =
            DependencyProperty.Register("ExperimentalRun", typeof(ExperimentalRun), typeof(zoomGraph), new PropertyMetadata(null, OnExperimentalRunPropertyPropertyChanged));


        private static void OnExperimentalRunPropertyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as zoomGraph;
            //myObj.OnExperimentalRunPropertyPropertyChanged(e);
        }

        private void OnExperimentalRunPropertyPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (ExperimentalRun != null)
            {
            }
        }

        #endregion

        #region DisplayItems
        public List<int> DisplayItems
        {
            get { return (List<int>)GetValue(DisplayItemsProperty); }
            set { SetValue(DisplayItemsProperty, value); }
        }

        public static readonly DependencyProperty DisplayItemsProperty =
            DependencyProperty.Register("DisplayItems", typeof(List<int>), typeof(zoomGraph), new PropertyMetadata(null, OnDisplayItemsPropertyPropertyChanged));


        private static void OnDisplayItemsPropertyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as zoomGraph;
            myObj.OnDisplayItemsPropertyPropertyChanged(e);
        }

        private void OnDisplayItemsPropertyPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (ExperimentalRun != null && DisplayItems != null)
            {
                (chart.SecondaryYAxis as NumericAxisBase).Visibility = Visibility.Collapsed;
                
                var series = new SeriesCollection<IChartSeries>();
                var generator = new ColourGenerator();
                var title = new StringBuilder();
                if (DisplayItems.Count == 1)
                {
                    Culture culture = ExperimentalRun.Run[DisplayItems[0]];
                    AddSeriesFromCulture(culture, ref series, generator, false);
                    if (ExperimentalRun.ReferenceCultureIndex != -1)
                    {
                        var refCulture = ExperimentalRun.Run.Where(exp => exp.ContainerIndex == ExperimentalRun.ReferenceCultureIndex).FirstOrDefault();
                        if(refCulture != null)
                            AddSeriesFromCulture(refCulture, ref series, generator, true);
                    }
                    RaiseNameChangedEvent(ExperimentalRun.Run[DisplayItems[0]].Container);
                    string format = "00.00";
                    Lag.Text = culture.Lag.ToString(format, CultureInfo.InvariantCulture);
                    Rate.Text = culture.Rate.ToString(format, CultureInfo.InvariantCulture);
                    Yield.Text = culture.Yield.ToString(format, CultureInfo.InvariantCulture);
                    title.Append(culture.Container);
                    NavPanel.Visibility = Visibility.Visible;
                }
                else if(DisplayItems.Count > 1)
                {
                    var cultures = new List<Culture>();
                    
                    foreach (var item in DisplayItems)
                        cultures.Add(ExperimentalRun.Run[item]);

                    title.Append(cultures.First().Container.Split(' ')[0]).Append(" ");
                    foreach (var culture in cultures)
                    {
                        AddSeriesFromCulture(culture, ref series, generator, false);
                        title.Append(culture.ContainerIndex.ToString()).Append(", ");
                    }
                    title.Remove(title.Length - 2, 2);
                    ZoomGraphControlValues.EnableFit = false;
                    NavPanel.Visibility = Visibility.Hidden;
                }
                
                chart.Series = series;
                
                UpdateYAxisType();
                
                ((BehaviourManager)chart.Behaviour).Behaviours[0]= new TrackballBehaviour();

                SetChartBehabiour();

                lbTitle.Content = title.ToString();

                CreateSeriesDetailsUI();
            }
        }

        private void AddSeriesFromCulture(Culture culture, ref SeriesCollection<IChartSeries> addToSeries, ColourGenerator colourGenerator, bool isReference)
        {
            var validTypes = GetValidTypes();

            if (culture.GetType() == typeof(MergedCulture))
                ControlEnableFitData = false;
            
            AddPrimaryAxisSeries(culture, addToSeries, colourGenerator, isReference, validTypes);

            if (ZoomGraphControlValues.DisplayFD) 
                AddSecundaryAxisSeries(culture, addToSeries);
        }

        private void AddSecundaryAxisSeries(Culture culture, SeriesCollection<IChartSeries> addToSeries)
        {
            var growthGraphSecondary = ChartHelper.AsGrowthGraph(culture, null, new List<DataType> {DataType.FirstDerivative});
            foreach (var series in growthGraphSecondary.GrowthSeries)
            {
                var fDcolorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#088DA5"));
                var newSerieRaw = new LineSeries
                {
                    YAxis = chart.SecondaryYAxis,
                    DataSeries = series.Series,
                    LineStroke = fDcolorBrush,
                    LineStrokeThickness = 2,
                    ShowPoints = false,
                    Visibility = Visibility.Visible,
                };
                (chart.SecondaryYAxis as NumericAxisBase).Visibility = Visibility.Visible;
                (chart.SecondaryYAxis as NumericAxisBase).TitleStyle = Resources["AxixLabelsStyle"] as Style;
                (chart.SecondaryYAxis as NumericAxisBase).ShowGridlines = false;
                (chart.SecondaryYAxis as NumericAxisBase).ShowMajorTicks = true;
                (chart.SecondaryYAxis as NumericAxisBase).ShowMinorTicks = true;
                addToSeries.Add(newSerieRaw);
            }
        }

        private void AddPrimaryAxisSeries(Culture culture, SeriesCollection<IChartSeries> addToSeries, ColourGenerator colourGenerator, bool isReference, List<DataType> validTypes)
        {
            var growthGraphPrimary = ChartHelper.AsGrowthGraph(culture, null, validTypes);

            if (isReference)
                growthGraphPrimary.ContainerName = "Ref: " + growthGraphPrimary.ContainerName;
            
            foreach (var series in growthGraphPrimary.GrowthSeries)
            {
                if (isReference)
                    series.Series.Title = "Ref: " + series.Series.Title;

                var newSerieRaw = new LineSeries
                {
                    DataSeries = series.Series,
                    LegendItemTemplate = (ControlTemplate) this.FindResource("LegendItemTemplate")
                };
                
                var colorHexValue = GetColorHexValue(colourGenerator, series);
                var colorBrush = new SolidColorBrush((Color) ColorConverter.ConvertFromString(colorHexValue));
                newSerieRaw.LineStroke = colorBrush;
                newSerieRaw.LineStrokeThickness = 1.5;
                newSerieRaw.ShowPoints = false;
                addToSeries.Add(newSerieRaw);
            }

            if (DisplayFirstDervivative)
            {
                if (!ControlDisplayMetaRateData) return;
                var dataType = culture.GrowthMeasurements.VariableMetaDatas.ContainsKey(DataType.ProcessedThinned)
                    ? DataType.ProcessedThinned
                    : DataType.Processed;
                var metaSerieRateRawFD = MetaSerieRate(dataType, culture.GrowthMeasurements, true);
                addToSeries.Add(metaSerieRateRawFD);
                return;
            }

            if (culture.GetType() == typeof (MergedCulture))
            {
                foreach (var growthMeasurements in ((MergedCulture) culture).MergedGrowthMeasurements.Values)
                {
                    if (validTypes.Contains(DataType.MetaDataLag))
                    {
                        var metaSerieLag = MetaSerieLag(DataType.Processed, growthMeasurements);
                        addToSeries.Add(metaSerieLag);
                    }
                    if (validTypes.Contains(DataType.MetaDataRate))
                    {
                        var metaSerieRate = MetaSerieRate(DataType.Processed, growthMeasurements);
                        addToSeries.Add(metaSerieRate);
                    }
                    if (validTypes.Contains(DataType.MetaDataYield))
                    {
                        var metaSerieYield = MetaSerieYield(DataType.Processed, growthMeasurements);
                        addToSeries.Add(metaSerieYield);
                    }
                }
            }
            else
            {
                if (validTypes.Contains(DataType.MetaDataLag))
                {
                    var metaSerieLag = MetaSerieLag(DataType.Processed, culture.GrowthMeasurements);
                    addToSeries.Add(metaSerieLag);
                    if (culture.GrowthMeasurements.VariableMetaDatas.ContainsKey(DataType.ProcessedThinned))
                    {
                        var metaSerieLagThinned = MetaSerieLag(DataType.ProcessedThinned, culture.GrowthMeasurements);
                        addToSeries.Add(metaSerieLagThinned);
                    }
                    if (culture.GrowthMeasurements.VariableMetaDatas.ContainsKey(DataType.Raw))
                    {
                        var metaSerieLagRaw = MetaSerieLag(DataType.Raw, culture.GrowthMeasurements);
                        addToSeries.Add(metaSerieLagRaw);
                    }
                }
                if (validTypes.Contains(DataType.MetaDataRate))
                {
                    var metaSerieRate = MetaSerieRate(DataType.Processed, culture.GrowthMeasurements);
                    addToSeries.Add(metaSerieRate);
                    if (culture.GrowthMeasurements.VariableMetaDatas.ContainsKey(DataType.ProcessedThinned))
                    {
                        var metaSerieRateThinned = MetaSerieRate(DataType.ProcessedThinned, culture.GrowthMeasurements);
                        addToSeries.Add(metaSerieRateThinned);
                    }
                    if (culture.GrowthMeasurements.VariableMetaDatas.ContainsKey(DataType.Raw))
                    {
                        var metaSerieRateRaw = MetaSerieRate(DataType.Raw, culture.GrowthMeasurements);
                        addToSeries.Add(metaSerieRateRaw);
                    }
                }
                if (validTypes.Contains(DataType.MetaDataYield))
                {
                    var metaSerieYield = MetaSerieYield(DataType.Processed, culture.GrowthMeasurements);
                    addToSeries.Add(metaSerieYield);
                    if (culture.GrowthMeasurements.VariableMetaDatas.ContainsKey(DataType.ProcessedThinned))
                    {
                        var metaSerieYieldThinned = MetaSerieYield(DataType.ProcessedThinned, culture.GrowthMeasurements);
                        addToSeries.Add(metaSerieYieldThinned);
                    }
                    if (culture.GrowthMeasurements.VariableMetaDatas.ContainsKey(DataType.Raw))
                    {
                        var metaSerieYieldRaw = MetaSerieYield(DataType.Raw, culture.GrowthMeasurements);
                        addToSeries.Add(metaSerieYieldRaw);
                    }
                }
            }
        }

        private string GetColorHexValue(ColourGenerator colourGenerator, ChartHelper.GrowthSeries series)
        {
            string colorHexValue;

            if (DisplayItems.Count > 1)
                colorHexValue = GetColorHexValueFromGenerator(colourGenerator);
            else
            {
                switch (series.DataType)
                {
                    case DataType.Processed:
                        colorHexValue = "#000000";
                        break;
                    case DataType.Raw:
                        colorHexValue = "#ff0000";
                        break;
                    case DataType.FirstDerivative:
                        colorHexValue = "#088DA5";
                        break;
                    case DataType.ProcessedUnfiltered:
                        colorHexValue = "#00FF00";
                        break;
                    default:
                        colorHexValue = GetColorHexValueFromGenerator(colourGenerator);
                        break;
                }
            }
            return colorHexValue;
        }

        private static string GetColorHexValueFromGenerator(ColourGenerator colourGenerator)
        {
            string colorHexValue;
            colorHexValue = "#" + colourGenerator.NextColour();
            if (colorHexValue == "#FFFFFF" || colorHexValue == "#FFFF00")
                colorHexValue = "#" + colourGenerator.NextColour();
            return colorHexValue;
        }

        private List<DataType> GetValidTypes()
        {
            var validTypes = new List<DataType>();
            if (DisplayFirstDervivative)
                validTypes.Add(DataType.FirstDerivative);
            else
            {
                validTypes.Add(DataType.Processed);
                validTypes.Add(DataType.Unfiltered);
                validTypes.Add(DataType.ProcessedUnfiltered);

                validTypes.Add(DataType.ProcessedThinned);
                if (ZoomGraphControlValues.DisplayRaw)
                {
                    validTypes.Add(DataType.Raw);
                    validTypes.Add(DataType.RawThinned);
                }
                if (ZoomGraphControlValues.DisplayMetaLagData)
                    validTypes.Add(DataType.MetaDataLag);
                if (ZoomGraphControlValues.DisplayMetaRateData)
                    validTypes.Add(DataType.MetaDataRate);
                if (ZoomGraphControlValues.DisplayMetaYieldData)
                    validTypes.Add(DataType.MetaDataYield);
                if (ZoomGraphControlValues.FitData)
                    validTypes.Add(DataType.ProcessedFitted);
            }
            return validTypes;
        }

        #endregion

        #region ControlDisplayRaw
        public bool ControlDisplayRaw
        {
            get { return (bool)GetValue(ControlDisplayRawProperty); }
            set { SetValue(ControlDisplayRawProperty, value); }
        }

        public static readonly DependencyProperty ControlDisplayRawProperty =
            DependencyProperty.Register("ControlDisplayRaw", typeof(bool), typeof(zoomGraph), new PropertyMetadata(default(bool), OnControlDisplayRawPropertyChanged));


        private static void OnControlDisplayRawPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as zoomGraph;
            myObj.OnControlDisplayRawPropertyChanged(e);
        }

        private void OnControlDisplayRawPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!HasFocus)
                return;
            ZoomGraphControlValues.DisplayRaw = ControlDisplayRaw;
            RefreshGraph();
        }
        #endregion

        #region ControlDisplayFD
        public bool ControlDisplayFD
        {
            get { return (bool)GetValue(ControlDisplayFDProperty); }
            set { SetValue(ControlDisplayFDProperty, value); }
        }

        public static readonly DependencyProperty ControlDisplayFDProperty =
            DependencyProperty.Register("ControlDisplayFD", typeof(bool), typeof(zoomGraph), new PropertyMetadata(default(bool), OnControlDisplayFDPropertyChanged));


        private static void OnControlDisplayFDPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as zoomGraph;
            myObj.OnControlDisplayFDPropertyChanged(e);
        }

        private void OnControlDisplayFDPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!HasFocus)
                return;
            ZoomGraphControlValues.DisplayFD = ControlDisplayFD;
            RefreshGraph();
        }

        #endregion

        #region ControlDisplayMetaLagData
        public bool ControlDisplayMetaLagData
        {
            get { return (bool)GetValue(ControlDisplayMetaLagDataProperty); }
            set { SetValue(ControlDisplayMetaLagDataProperty, value); }
        }

        public static readonly DependencyProperty ControlDisplayMetaLagDataProperty =
            DependencyProperty.Register("ControlDisplayMetaLagData", typeof(bool), typeof(zoomGraph), new PropertyMetadata(default(bool), OnControlDisplayMetaLagDataPropertyChanged));


        private static void OnControlDisplayMetaLagDataPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as zoomGraph;
            myObj.OnControlDisplayMetaLagDataPropertyChanged(e);
        }

        private void OnControlDisplayMetaLagDataPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!HasFocus)
                return;
            ZoomGraphControlValues.DisplayMetaLagData = ControlDisplayMetaLagData;
            RefreshGraph();
        }
        #endregion

        #region ControlDisplayMetaRateData
        public bool ControlDisplayMetaRateData
        {
            get { return (bool)GetValue(ControlDisplayMetaRateDataProperty); }
            set { SetValue(ControlDisplayMetaRateDataProperty, value); }
        }

        public static readonly DependencyProperty ControlDisplayMetaRateDataProperty =
            DependencyProperty.Register("ControlDisplayMetaRateData", typeof(bool), typeof(zoomGraph), new PropertyMetadata(default(bool), OnControlDisplayMetaRateDataPropertyChanged));


        private static void OnControlDisplayMetaRateDataPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as zoomGraph;
            myObj.OnControlDisplayMetaRateDataPropertyChanged(e);
        }

        private void OnControlDisplayMetaRateDataPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!HasFocus)
                return;
            ZoomGraphControlValues.DisplayMetaRateData = ControlDisplayMetaRateData;
            RefreshGraph();
        }
        #endregion

        #region ControlDisplayMetaYieldData
        public bool ControlDisplayMetaYieldData
        {
            get { return (bool)GetValue(ControlDisplayMetaYieldDataProperty); }
            set { SetValue(ControlDisplayMetaYieldDataProperty, value); }
        }

        public static readonly DependencyProperty ControlDisplayMetaYieldDataProperty =
            DependencyProperty.Register("ControlDisplayMetaYieldData", typeof(bool), typeof(zoomGraph), new PropertyMetadata(default(bool), OnControlDisplayMetaYieldDataPropertyChanged));


        private static void OnControlDisplayMetaYieldDataPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as zoomGraph;
            myObj.OnControlDisplayMetaYieldDataPropertyChanged(e);
        }

        private void OnControlDisplayMetaYieldDataPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!HasFocus)
                return;
            ZoomGraphControlValues.DisplayMetaYieldData = ControlDisplayMetaYieldData;
            RefreshGraph();
        }
        #endregion

        #region ControlLogYAxis
        public bool ControlLogYAxis
        {
            get { return (bool)GetValue(ControlLogYAxisProperty); }
            set { SetValue(ControlLogYAxisProperty, value); }
        }

        public static readonly DependencyProperty ControlLogYAxisProperty =
            DependencyProperty.Register("ControlLogYAxis", typeof(bool), typeof(zoomGraph), new PropertyMetadata(default(bool), OnControlLogYAxisPropertyChanged));


        private static void OnControlLogYAxisPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as zoomGraph;
            myObj.OnControlLogYAxisPropertyChanged(e);
        }

        private void OnControlLogYAxisPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!HasFocus)
                return;
            
            ZoomGraphControlValues.LogYAxis = ControlLogYAxis;
            RefreshGraph();
            UpdateYAxisType();
        }

        #endregion

        #region ControlZoom
        public bool ControlZoom
        {
            get { return (bool)GetValue(ControlZoomProperty); }
            set { SetValue(ControlZoomProperty, value); }
        }

        public static readonly DependencyProperty ControlZoomProperty =
            DependencyProperty.Register("ControlZoom", typeof(bool), typeof(zoomGraph), new PropertyMetadata(default(bool), OnControlZoomPropertyChanged));


        private static void OnControlZoomPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as zoomGraph;
            myObj.OnControlZoomPropertyChanged(e);
        }

        private void OnControlZoomPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!HasFocus)
                return;

            ZoomGraphControlValues.ChartBehaviour = ControlZoom ? ChartBehaviour.Zoom : ChartBehaviour.Pan;
            SetChartBehabiour();
        }

        #endregion

        #region ControlPan
        public bool ControlPan
        {
            get { return (bool)GetValue(ControlPanProperty); }
            set { SetValue(ControlPanProperty, value); }
        }

        public static readonly DependencyProperty ControlPanProperty =
            DependencyProperty.Register("ControlPan", typeof(bool), typeof(zoomGraph), new PropertyMetadata(default(bool), OnControlPanPropertyChanged));


        private static void OnControlPanPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as zoomGraph;
            myObj.OnControlPanPropertyChanged(e);
        }

        private void OnControlPanPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!HasFocus)
                return;
            
            ZoomGraphControlValues.ChartBehaviour = ControlPan ? ChartBehaviour.Pan : ChartBehaviour.Zoom;
            SetChartBehabiour();
        }

        #endregion

        #region ControlZoomFit
        public bool ControlZoomFit
        {
            get { return (bool)GetValue(ControlZoomFitProperty); }
            set { SetValue(ControlZoomFitProperty, value); }
        }

        public static readonly DependencyProperty ControlZoomFitProperty =
            DependencyProperty.Register("ControlZoomFit", typeof(bool), typeof(zoomGraph), new PropertyMetadata(default(bool), OnControlZoomFitPropertyChanged));


        private static void OnControlZoomFitPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as zoomGraph;
            myObj.OnControlZoomFitPropertyChanged(e);
        }

        private void OnControlZoomFitPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!HasFocus)
                return;

            chart.XAxis.Range = null;
            chart.YAxis.Range = null;
        }

        #endregion

        #region ControlFitData
        public bool ControlFitData
        {
            get { return (bool)GetValue(ControlFitDataProperty); }
            set { SetValue(ControlFitDataProperty, value); }
        }

        public static readonly DependencyProperty ControlFitDataProperty =
            DependencyProperty.Register("ControlFitData", typeof(bool), typeof(zoomGraph), new PropertyMetadata(default(bool), OnControlFitDataPropertyChanged));


        private static void OnControlFitDataPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as zoomGraph;
            myObj.OnControlFitDataPropertyChanged(e);
        }

        private void OnControlFitDataPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!HasFocus)
                return;

            ZoomGraphControlValues.FitData = true;
            if (ExperimentalRun != null && DisplayItems != null && NeuralParameters != null )
            {
                if (DisplayItems.Count == 1)
                {
                    var culture = ExperimentalRun.Run[DisplayItems[0]];
                    lbInfo.Content = ProcessCulture.GetFittedDataPreview(ref culture, NeuralParameters);
                    FitInfoBorder.Visibility = Visibility.Visible;
                    btnFitReplace.IsEnabled = true;
                    FitGroup.Visibility = Visibility.Visible;
                }
                RefreshGraph();
            }
        }

        #endregion

        #region ControlSkipPointsNumber
        public double ControlSkipPointsNumber
        {
            get { return (double)GetValue(ControlSkipPointsNumberProperty); }
            set { SetValue(ControlSkipPointsNumberProperty, value); }
        }

        public static readonly DependencyProperty ControlSkipPointsNumberProperty =
            DependencyProperty.Register("ControlSkipPointsNumber", typeof(double), typeof(zoomGraph), new PropertyMetadata(default(double), OnControlSkipPointsNumberPropertyChanged));


        private static void OnControlSkipPointsNumberPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as zoomGraph;
            myObj.OnControlSkipPointsNumberPropertyChanged(e);
        }

        private void OnControlSkipPointsNumberPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!HasFocus)
                return;
            ZoomGraphControlValues.SkipPointsNumber = ControlSkipPointsNumber;

            if (ExperimentalRun != null && DisplayItems != null && NeuralParameters != null)
            {
                if (DisplayItems.Count == 1)
                {
                    var culture = ExperimentalRun.Run[DisplayItems[0]];
                    var skipPoint = (int)ControlSkipPointsNumber;
                    ProcessCulture.AddRawThinned(ref culture, skipPoint, DisplayFirstDervivative, TrueODCalibarationFunction);
                    lbInfo.Content = ProcessCulture.GetThinnedDataPreview(ref culture, NeuralParameters);
                    FitInfoBorder.Visibility = Visibility.Visible;
                    btnFitReplace.IsEnabled = false;
                    FitGroup.Visibility = Visibility.Visible;
                    //ProcessCulture.ReProcess(culture);
                }
                RefreshGraph();
            }
        }

        #endregion

        #region ControlEnableFitData
        public bool ControlEnableFitData
        {
            get { return (bool)GetValue(ControlEnableFitDataProperty); }
            set { SetValue(ControlEnableFitDataProperty, value); }
        }

        public static readonly DependencyProperty ControlEnableFitDataProperty =
            DependencyProperty.Register("ControlEnableFitData", typeof(bool), typeof(zoomGraph), new PropertyMetadata(default(bool), null));


        #endregion

        #region ZoomGraphControlValues
        public ZoomGraphControlValues ZoomGraphControlValues
        {
            get { return (ZoomGraphControlValues)GetValue(ZoomGraphControlValuesProperty); }
            set { SetValue(ZoomGraphControlValuesProperty, value); }
        }

        public static readonly DependencyProperty ZoomGraphControlValuesProperty =
            DependencyProperty.Register("ZoomGraphControlValues", typeof(ZoomGraphControlValues), typeof(zoomGraph), new PropertyMetadata(null, OnZoomGraphControlValuesPropertyChanged));


        private static void OnZoomGraphControlValuesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as zoomGraph;
            myObj.OnZoomGraphControlValuesPropertyChanged(e);
        }

        private void OnZoomGraphControlValuesPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        #endregion

        #region TrueODCalibarationFunction
        public CalibrationFunction TrueODCalibarationFunction
        {
            get { return (CalibrationFunction)GetValue(TrueODCalibarationFunctionProperty); }
            set { SetValue(TrueODCalibarationFunctionProperty, value); }
        }

        public static readonly DependencyProperty TrueODCalibarationFunctionProperty =
            DependencyProperty.Register("TrueODCalibarationFunction", typeof(CalibrationFunction), typeof(zoomGraph), new PropertyMetadata(null, OnTrueODCalibarationFunctionPropertyChanged));

        private static void OnTrueODCalibarationFunctionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as zoomGraph;
            myObj.OnTrueODCalibarationFunctionPropertyChanged(e);
        }

        private void OnTrueODCalibarationFunctionPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (TrueODCalibarationFunction != null)
            {
            }
        }

        #endregion

        private bool _displayFirstDervivative;
        public bool DisplayFirstDervivative
        {
            get { return _displayFirstDervivative; }
            set
            {
                _displayFirstDervivative = value;
                ControlEnableFitData = !value;
            }
        }

        public bool HasFocus { get; set; }

        //events

        #region SeriesCreated
        public static readonly RoutedEvent SeriesCreatedEvent = EventManager.RegisterRoutedEvent(
        "SeriesCreated", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(zoomGraph));

        public event RoutedEventHandler SeriesCreated
        {
            add { AddHandler(SeriesCreatedEvent, value); }
            remove { RemoveHandler(SeriesCreatedEvent, value); }
        }

        void RaiseSeriesCreatedEvent(object source)
        {
            var newEventArgs = new RoutedEventArgs(SeriesCreatedEvent, source);
            RaiseEvent(newEventArgs);
        }
        #endregion

        #region NameChanged
        public static readonly RoutedEvent NameChangedEvent = EventManager.RegisterRoutedEvent(
        "NameChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(zoomGraph));

        public event RoutedEventHandler NameChanged
        {
            add { AddHandler(NameChangedEvent, value); }
            remove { RemoveHandler(NameChangedEvent, value); }
        }

        void RaiseNameChangedEvent(object source)
        {
            var newEventArgs = new RoutedEventArgs(NameChangedEvent, source);
            RaiseEvent(newEventArgs);
        }
        #endregion

        public zoomGraph()
        {
            InitializeComponent();
            chart.DataContext = this;
            Focus();
        }

        public void RefreshZoomGraph()
        {
            RefreshGraph();
        }

        private void UpdateYAxisType()
        {
            if (ControlLogYAxis)
                chart.YAxis = new LogarithmicAxis { ShowMinorTicks = true, ShowMajorTicks = true, ShowGridlines = true, LogarithmicBase = 2, Title = "Biomass (log2 OD)", TitleStyle = Resources["AxixLabelsStyle"] as Style,LabelFormatString = "N2"};
            else
            {
                chart.YAxis = new LinearAxis { ShowMinorTicks = true, ShowMajorTicks = true, ShowGridlines = true, Title = "Biomass (OD)", TitleStyle = Resources["AxixLabelsStyle"] as Style };
                if (!_displayFirstDervivative && GlobalRange)
                {
                    chart.XAxis.Range = new DoubleRange(ExperimentalRun.RunTimeRange.Min, ExperimentalRun.RunTimeRange.Max);
                    chart.YAxis.Range = new DoubleRange(ExperimentalRun.RunODRange.Min, ExperimentalRun.RunODRange.Max);
                }
            }
        }

        private LineSeries MetaSerieLag(DataType datatype, GrowthMeasurements growthMeasurements)
        {
            if (!growthMeasurements.VariableMetaDatas.ContainsKey(datatype)) return null;

            var metaSerieLag = CreateMetaSerie(Colors.Violet, 8, ShapeType.Ellipse);
            var dataSeries = new DataSeries<double, double>("Lag Macro Data");

            foreach (var metaData in growthMeasurements.VariableMetaDatas[datatype].Lag)
            {
                foreach (var groundPoint in metaData.GroundPoints)
                {
                    var point = new DataPoint<double, double>(groundPoint.Time, groundPoint.OD);
                    dataSeries.Add(point);
                }
            }
            foreach (var metaData in growthMeasurements.VariableMetaDatas[datatype].Lag)
            {
                foreach (var slopePoint in metaData.SlopePoints)
                {
                    var point = new DataPoint<double, double>(slopePoint.Time, slopePoint.OD);
                    dataSeries.Add(point);
                }
            }
            foreach (var metaData in growthMeasurements.VariableMetaDatas[datatype].Lag)
            {
                var point = new DataPoint<double, double>(metaData.InterceptTime, metaData.InterceptOD);
                dataSeries.Add(point);
            }

            metaSerieLag.DataSeries = dataSeries;
            return metaSerieLag;
        }

        private LineSeries MetaSerieYield(DataType datatype, GrowthMeasurements growthMeasurements)
        {
            if (!growthMeasurements.VariableMetaDatas.ContainsKey(datatype)) return null;
            
            var metaSerieYield = CreateMetaSerie(Colors.Lime, 12, ShapeType.Triangle);
            var dataSeries = new DataSeries<double, double>("Yield Macro Data");

            foreach (var metaData in growthMeasurements.VariableMetaDatas[datatype].Yield.LowestPoints)
            {
                var point = new DataPoint<double, double>(metaData.Time, metaData.OD);
                dataSeries.Add(point);
            }
            foreach (var metaData in growthMeasurements.VariableMetaDatas[datatype].Yield.HighestPoints)
            {
                var point = new DataPoint<double, double>(metaData.Time, metaData.OD);
                dataSeries.Add(point);
            }
            metaSerieYield.DataSeries = dataSeries;
            return metaSerieYield;
        }

        private LineSeries MetaSerieRate(DataType datatype, GrowthMeasurements growthMeasurements, bool isFirstDeriv = false)
        {
            if (!growthMeasurements.VariableMetaDatas.ContainsKey(datatype)) return null;
            
            var metaSerieRate = CreateMetaSerie(Colors.Red, 12, ShapeType.Cross);
            var dataSeries = new DataSeries<double, double>("Rate Macro Data");

            foreach (var metaData in growthMeasurements.VariableMetaDatas[datatype].Rate)
            {
                float od;
                if (isFirstDeriv)
                    od = growthMeasurements.GetMeasurements(DataType.FirstDerivative).First(m => Math.Abs(m.Time - metaData.Time) < double.Epsilon).OD;
                else 
                    od = metaData.OD;
                var point = new DataPoint<double, double>(metaData.Time, od);
                dataSeries.Add(point);
            }
            metaSerieRate.DataSeries = dataSeries;
            return metaSerieRate;
        }

        private LineSeries CreateMetaSerie(Color color, double pointSize, ShapeType shape)
        {
            var metaSerie = new LineSeries();
            metaSerie.LegendItemTemplate = (ControlTemplate)this.FindResource("LegendItemTemplate");
            metaSerie.ShowPoints = true;
            metaSerie.ShowLine = false;
            metaSerie.PointSize = pointSize;
            metaSerie.PointShape = shape;
            metaSerie.IsDisplayedOnLegend = false;
            metaSerie.PointFill = new SolidColorBrush(color);
            metaSerie.Tag = MetaData;
            metaSerie.Visibility = Visibility.Visible;

            return metaSerie;
        }

        private void CreateSeriesDetailsUI()
        {
            SeriesDetails.Children.Clear();
            
            var textTime = new TextBlock();
            var behaviourBindTime = new Binding("TrackCurrentPoint");
            behaviourBindTime.Source = chart;
            behaviourBindTime.Path = new PropertyPath("Behaviour.Behaviours[0].CurrentPoints[0].X");
            behaviourBindTime.StringFormat = "0.00";
            textTime.SetBinding(TextBlock.TextProperty, behaviourBindTime);
            var textTimeLabel = new TextBlock();
            textTimeLabel.Text = "Time: ";

            var wrapStackTime = new StackPanel();
            wrapStackTime.Orientation = Orientation.Horizontal;
            wrapStackTime.Children.Add(textTimeLabel);
            wrapStackTime.Children.Add(textTime);

            SeriesDetails.Children.Add(wrapStackTime);

            for (int i = 0; i < chart.Series.Count(); i++)
            {
                var series = (LineSeries) chart.Series[i];
                
                var rect = new Rectangle();
                rect.Height = 10;
                rect.Width = 10;
                rect.Fill = series.LineStroke;

                var textOD = new TextBlock();
                var behaviourBind = new Binding("TrackCurrentPoint");
                behaviourBind.Source = chart;
                behaviourBind.Path = new PropertyPath(string.Format(CultureInfo.InvariantCulture, "Behaviour.Behaviours[0].CurrentPoints[{0}].Y", i));
                behaviourBind.StringFormat = "0.00";
                textOD.SetBinding(TextBlock.TextProperty, behaviourBind);

                var textSeriesName = new TextBlock();
                var seriesNameBind = new Binding("SeriesName");
                seriesNameBind.Source = chart;
                seriesNameBind.Path = new PropertyPath(string.Format(CultureInfo.InvariantCulture, "Series[{0}].DataSeries.Title", i));
                textSeriesName.SetBinding(TextBlock.TextProperty, seriesNameBind);

                var textColon = new TextBlock();
                textColon.Text = ": ";

                var textODLabel = new TextBlock();
                textODLabel.Text = " OD";

                var wrapStack = new StackPanel();
                wrapStack.Orientation = Orientation.Horizontal;
                wrapStack.Children.Add(rect);
                wrapStack.Children.Add(textSeriesName);
                wrapStack.Children.Add(textColon);
                wrapStack.Children.Add(textOD);
                wrapStack.Children.Add(textODLabel);

                SeriesDetails.Children.Add(wrapStack);

            }
        }

        private void CopyDataToClipBoard()
        {
            var timeList = new List<string>();
            var seriesList = new List<List<string>>();
            int serieCount = 1;

            foreach (var serie in chart.Series)
            {
                var newSerie = new List<string>();
                foreach (IDataPoint point in serie.DataSeries)
                {
                    newSerie.Add(point.Y.ToString());
                    if (serieCount == 1)
                        timeList.Add(point.X.ToString());
                }
                seriesList.Add(newSerie);
                serieCount++;
            }

            var paste = new StringBuilder();

            paste.Append("Time");
            foreach (var serie in chart.Series)
            {
                paste.Append("\t").Append(serie.DataSeries.Title);
            }
            paste.AppendLine();

            foreach (var time in timeList)
            {
                paste.Append(time);
                foreach (var serie in seriesList)
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
            Clipboard.SetText(paste.ToString());
        }

        private void CopyImageToClipBoard()
        {
            var rtb = new RenderTargetBitmap((int)LayoutRoot.ActualWidth, (int)LayoutRoot.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(LayoutRoot);
            Clipboard.SetImage(rtb);
        }

        private void RefreshGraph()
        {
            var currentDisplayItems = DisplayItems.ToList();
            DisplayItems = currentDisplayItems;
        }

        private void Copy_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            CopyDataToClipBoard();
        }

        private void CopyGraph_Executed(object sender, ExecutedRoutedEventArgs e)
       {
           CopyImageToClipBoard();
       }

        private void SetChartBehabiour()
        {
            if (ZoomGraphControlValues != null)
            {
                switch (ZoomGraphControlValues.ChartBehaviour)
                {
                    case ChartBehaviour.Zoom:
                        BehabiourZooming.IsEnabled = true;
                        BehaviourPanning.IsEnabled = false;
                        break;
                    case ChartBehaviour.Pan:
                        BehabiourZooming.IsEnabled = false;
                        BehaviourPanning.IsEnabled = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (ExperimentalRun != null && DisplayItems != null)
            {
                if (DisplayItems.Count == 1)
                {
                    int index = DisplayItems[0];
                    if (index + 1 > ExperimentalRun.Run.Count - 1)
                        index = -1;

                    index ++;
                    DisplayItems = new List<int>{index};
                    ResetFitDataControls(false);
                }
            }
        }

        private void Prev_Click(object sender, RoutedEventArgs e)
        {
            if (ExperimentalRun != null && DisplayItems != null)
            {
                if (DisplayItems.Count == 1)
                {
                    int index = DisplayItems[0];
                    if (index - 1 < 0)
                        index = ExperimentalRun.Run.Count;

                    index--;
                    DisplayItems = new List<int> { index };
                    ResetFitDataControls(false);
                }
            }
        }

        private void btnFitClear_Click(object sender, RoutedEventArgs e)
        {
            ResetFitDataControls(true);
        }

        private void btnFitReplace_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to replace your current data with this fit?","Replace with Fitted Data", MessageBoxButton.YesNo,MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                var culture = ExperimentalRun.Run[DisplayItems[0]];
                ProcessCulture.UpdateFittedData(ref culture);
                ResetFitDataControls(true);
            } 
        }

        private void ResetFitDataControls(bool refresh)
        {
            var culture = ExperimentalRun.Run[DisplayItems[0]];
            culture.GrowthMeasurements.Measurements.Remove(DataType.ProcessedFitted);
            lbInfo.Content = string.Empty;
            FitInfoBorder.Visibility = Visibility.Collapsed;
            btnFitReplace.IsEnabled = false;
            FitGroup.Visibility = Visibility.Collapsed;
            ZoomGraphControlValues.FitData = false;
            if(refresh)
                RefreshGraph();
        }

    }
}
