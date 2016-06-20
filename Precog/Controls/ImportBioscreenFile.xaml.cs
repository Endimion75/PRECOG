using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Common;
using DataModels;
using Precog.Utils;
using Microsoft.Win32;
using Visiblox.Charts;

namespace Precog.Controls
{
    /// <summary>
    /// Interaction logic for ImportBioscreenFile.xaml
    /// </summary>
    public partial class ImportBioscreenFile : UserControl
    {

        #region Routed Events

        #region ImportStep
        public static readonly RoutedEvent ImportStepEvent = EventManager.RegisterRoutedEvent(
        "ImportStep", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ImportBioscreenFile));

        public event RoutedEventHandler ImportStep
        {
            add { AddHandler(ImportStepEvent, value); }
            remove { RemoveHandler(ImportStepEvent, value); }
        }

        void RaiseImportStepEvent()
        {
            var newEventArgs = new RoutedEventArgs(ImportStepEvent);
            RaiseEvent(newEventArgs);
        }
        #endregion

        #region ImportMessage
        public static readonly RoutedEvent ImportMessageEvent = EventManager.RegisterRoutedEvent(
        "ImportMessage", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ImportBioscreenFile));

        public event RoutedEventHandler ImportMessage
        {
            add { AddHandler(ImportMessageEvent, value); }
            remove { RemoveHandler(ImportMessageEvent, value); }
        }

        void RaiseImportMessageEvent(object source)
        {
            var newEventArgs = new RoutedEventArgs(ImportMessageEvent, source);
            RaiseEvent(newEventArgs);
        }
        #endregion

        #region ImportStart
        public static readonly RoutedEvent ImportStartedEvent = EventManager.RegisterRoutedEvent(
        "ImportStarted", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ImportBioscreenFile));

        public event RoutedEventHandler ImportStarted
        {
            add { AddHandler(ImportStartedEvent, value); }
            remove { RemoveHandler(ImportStartedEvent, value); }
        }

        void RaiseImportStartedEvent(object source)
        {
            var newEventArgs = new RoutedEventArgs(ImportStartedEvent, source);
            RaiseEvent(newEventArgs);
        }
        #endregion

        #region ImportEnd
        public static readonly RoutedEvent ImportEndedEvent = EventManager.RegisterRoutedEvent(
        "ImportEnded", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ImportBioscreenFile));

        public event RoutedEventHandler ImportEnded
        {
            add { AddHandler(ImportEndedEvent, value); }
            remove { RemoveHandler(ImportEndedEvent, value); }
        }

        void RaiseImportEndedEvent(object source)
        {
            var newEventArgs = new RoutedEventArgs(ImportEndedEvent, source);
            RaiseEvent(newEventArgs);
        }
        #endregion

        #endregion

        #region BlankValue
        public float BlankValue
        {
            get { return (float)GetValue(BlankValueProperty); }
            set { SetValue(BlankValueProperty, value); }
        }

        public static readonly DependencyProperty BlankValueProperty =
            DependencyProperty.Register("BlankValue", typeof(float), typeof(ImportBioscreenFile), new PropertyMetadata(float.NaN, OnBlankValuePropertyChanged));

        private static void OnBlankValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as ImportBioscreenFile;
            myObj.OnBlankValuePropertyChanged(e);
        }

        private void OnBlankValuePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!float.IsNaN(BlankValue))
            {
            }
        }

        #endregion

        #region RateTraitExtractionMethod
        public RateTraitExtractionMethod RateTraitExtractionMethod
        {
            get { return (RateTraitExtractionMethod)GetValue(RateTraitExtractionMethodProperty); }
            set { SetValue(RateTraitExtractionMethodProperty, value); }
        }

        public static readonly DependencyProperty RateTraitExtractionMethodProperty =
            DependencyProperty.Register("RateTraitExtractionMethod", typeof(RateTraitExtractionMethod), typeof(ImportBioscreenFile), new PropertyMetadata(RateTraitExtractionMethod.Default, OnRateTraitExtractionMethodPropertyChanged));

        private static void OnRateTraitExtractionMethodPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as ImportBioscreenFile;
            myObj.OnRateTraitExtractionMethodPropertyChanged(e);
        }

        private void OnRateTraitExtractionMethodPropertyChanged(DependencyPropertyChangedEventArgs e)
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
            DependencyProperty.Register("TrueODCalibarationFunction", typeof(CalibrationFunction), typeof(ImportBioscreenFile), new PropertyMetadata(null, OnTrueODCalibarationFunctionPropertyChanged));

        private static void OnTrueODCalibarationFunctionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as ImportBioscreenFile;
            myObj.OnBlankValuePropertyChanged(e);
        }

        private void OnTrueODCalibarationFunctionPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (TrueODCalibarationFunction != null)
            {
            }
        }

        #endregion

        public ImportBioscreenFile()
        {
            InitializeComponent();
            cbRateExtractionMethod.Items.Add("Default");
            cbRateExtractionMethod.Items.Add("Linear Regression");
            cbRateExtractionMethod.SelectedIndex = 0;
        }

        private void DlgOpen_Click(object sender, RoutedEventArgs e)
        {
            var dlgBox = OpenBioScreenFileDialog();

            var result = dlgBox.ShowDialog();

            string[] filenames = dlgBox.FileNames;

            if (result == true)
            {

                foreach (string fileName in filenames)
                {
                    var BSfile = new BioScreenFile();
                    BSfile.FileName = System.IO.Path.GetFileName(fileName);
                    BSfile.FullPath = fileName;

                    var itemExist = from BioScreenFile BSf in BSFilesList.Items where BSf.FileName == BSfile.FileName select BSf.FileName;

                    if (!itemExist.Any())
                        BSFilesList.Items.Add(BSfile);
                }
            }
            else
            {
                MessageBox.Show("No files were selcted.", "Information", MessageBoxButton.OK,
                                MessageBoxImage.Information);
            }
        }

        private OpenFileDialog OpenBioScreenFileDialog()
        {
            var dlgBox = new OpenFileDialog();
            dlgBox.DefaultExt = "*.~xl";
            dlgBox.Filter = "XL~ BioScreen Files|*.xl~|CSV BioScreen Export|*.csv|Tab Delimited|*.txt";
            dlgBox.Multiselect = true;
            return dlgBox;
        }

        private void Upload_Click(object sender, RoutedEventArgs e)
        {
            if(BSFilesList.Items.Count<1)
            {
                MessageBox.Show("There is nothing to import.", "Information", MessageBoxButton.OK,
                                MessageBoxImage.Information);
                return;
            }

            if(TrueODCalibarationFunction == null)
            {
                MessageBox.Show("OD calibration function values arenot set, please select one", "True OD Calibration function", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            RateTraitExtractionMethod = cbRateExtractionMethod.SelectedIndex == 0 ? RateTraitExtractionMethod.Default : RateTraitExtractionMethod.LinearRegression;

            var experimentRuns = new List<ExperimentalRun>();
            
            RaiseImportStartedEvent( BSFilesList.Items.Count);
            RaiseImportMessageEvent("Preparing for Import.");
            bool skipMonotonicFilter = cbSkipMonotonicFilter.IsChecked != null && (bool)cbSkipMonotonicFilter.IsChecked;
            foreach (BioScreenFile file in BSFilesList.Items)
            {
                RaiseImportMessageEvent("Importing File: " + file.FileName);
                ExperimentalRun expRun;
                var error = GetExperimentalRun(file.FullPath, out expRun);

                if (error)
                    continue;

                var maxOD = float.MinValue;
                var minOD = float.MaxValue;
                var maxTime = float.MinValue;
                var functionTerms = TrueODCalibarationFunction.GetTerms();
                foreach (var culture in expRun.Run)
                {
                    RaiseImportMessageEvent("Processing Well " + culture.Container + " from " + file.FileName);
                    Macro.ProcessCulture.Process(culture, ref maxTime, ref minOD, ref maxOD, BlankValue, false, functionTerms.ElementAt(0), functionTerms.ElementAt(1), functionTerms.ElementAt(2), RateTraitExtractionMethod, skipMonotonicFilter);
                }
                expRun.RunODRange = new Range(GrowthRangeType.OD, maxOD, minOD); 
                expRun.RunTimeRange = new Range(GrowthRangeType.Time,maxTime,0); 

                expRun.ReplicateBehaviour = ReplicateSelection.None;
                experimentRuns.Add(expRun);
                RaiseImportStepEvent();
            }
            //MessageBox.Show("peaks" + SessionVariables.Peaks.Count());
            RaiseImportEndedEvent(experimentRuns);
            BSFilesList.Items.Clear();
        }

        private void BSListItemDelete_Click(object sender, RoutedEventArgs e)
        {
            BSFilesList.Items.Remove(BSFilesList.SelectedItem);
        }

        private static bool GetExperimentalRun(string fileFullPath, out ExperimentalRun expRun)
        {
            try
            {
                expRun = BioScreenHelper.ImportBioscreenFile(fileFullPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not import the file: " + fileFullPath + "\r\nReason: " + ex.Message, "Import Errror", MessageBoxButton.OK, MessageBoxImage.Error);
                expRun = null;
                return true;
            }
            return false;
        }

    }
}
