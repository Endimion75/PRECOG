using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DataModels;
using Precog.Utils;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;
using System.IO;

namespace Precog.Controls
{
    /// <summary>
    /// Interaction logic for SaveData.xaml
    /// </summary>
    public partial class SaveData : UserControl
    {
        #region public List<ExperimentalRun> ExperimentalRuns
        public List<ExperimentalRun> ExperimentalRuns
        {
            get { return GetValue(ExperimentalRunsProperty) as List<ExperimentalRun>; }
            set { SetValue(ExperimentalRunsProperty, value); }
        }

        public static readonly DependencyProperty ExperimentalRunsProperty =
            DependencyProperty.Register("ExperimentalRuns",typeof(List<ExperimentalRun>),typeof(SaveData),new PropertyMetadata(null, OnExperimentalRunsPropertyChanged));

        private static void OnExperimentalRunsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as SaveData;
            myObj.OnExperimentalRunsPropertyChanged(e);
        }

        private void OnExperimentalRunsPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            RootLayout.IsEnabled = ExperimentalRuns != null;
        }

        #endregion public List<ExperimentalRun> ExperimentalRuns

        #region public ReplicateSelection ReplicateSelection
        public ReplicateSelection ReplicateSelection
        {
            get { return (ReplicateSelection)GetValue(ReplicateSelectionProperty); }
            set { SetValue(ReplicateSelectionProperty, value); }
        }

        public static readonly DependencyProperty ReplicateSelectionProperty =
            DependencyProperty.Register("ReplicateSelection", typeof(ReplicateSelection), typeof(SaveData), new PropertyMetadata(ReplicateSelection.None, OnReplicateSelectionPropertyChanged));

        private static void OnReplicateSelectionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as SaveData;
            myObj.OnReplicateSelectionPropertyChanged(e);
        }

        private void OnReplicateSelectionPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            StackGrowthData.IsEnabled = ReplicateSelection == ReplicateSelection.None;
        }

        #endregion public List<ExperimentalRun> ExperimentalRuns
        
        public SaveData()
        {
            InitializeComponent();
            RootLayout.IsEnabled = ExperimentalRuns != null;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            var hasErrors = false;

            foreach (var experimentalRun in ExperimentalRuns)
            {
                try
                {
                    string name = System.IO.Path.GetFileNameWithoutExtension(experimentalRun.ImportFileName);
                    using (var sw = new StreamWriter(string.Format(CultureInfo.InvariantCulture, "{0}\\{1}.tsv", txtOutputDirectory.Text, name)))
                        sw.Write(experimentalRun.GetTabularDelimitedOutput());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    hasErrors = true;
                }
                
            }
            if(!hasErrors)
                MessageBox.Show(string.Format(CultureInfo.InvariantCulture, "{0} Experiment(s) was(were) successfully saved.", ExperimentalRuns.Count()));
        }

        private void btnSave_Click_MegaFile(object sender, RoutedEventArgs e)
        {
            var hasErrors = false;
            bool firstIteration = true;
            try
            {
                string name = "MasterJonas";
                using (var sw = new StreamWriter(string.Format(CultureInfo.InvariantCulture, "{0}\\{1}.tsv", txtOutputDirectory.Text, name)))
                {
                    foreach (var experimentalRun in ExperimentalRuns)
                    {
                        sw.Write(experimentalRun.GetTabularDelimitedOutputExperiment(firstIteration));
                        if (firstIteration) firstIteration = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                hasErrors = true;
            }

            if (!hasErrors)
                MessageBox.Show(string.Format(CultureInfo.InvariantCulture, "{0} Experiment(s) was(were) successfully saved.", ExperimentalRuns.Count()));
        }

        private void btnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new FolderBrowserDialog();
            DialogResult result = dlg.ShowDialog(this.GetIWin32Window());
            switch (result)
            {
                case DialogResult.OK:
                    txtOutputDirectory.Text = dlg.SelectedPath;
                    Operations.IsEnabled = dlg.SelectedPath != string.Empty;
                    break;
                case DialogResult.Cancel:
                    txtOutputDirectory.Text = string.Empty;
                    break;
            }
            
        }

        private void btnSaveCurves_Click(object sender, RoutedEventArgs e)
        {
            var include = GetDataTypesToInclude();
            var hasErrors = false;

            if(include.Count > 0)
            {
                foreach (var experimentalRun in ExperimentalRuns)
                {
                    foreach (var dataType in include)
                    {
                        try
                        {
                            var name = System.IO.Path.GetFileNameWithoutExtension(experimentalRun.ImportFileName);
                            using (var sw = new StreamWriter(string.Format(CultureInfo.InvariantCulture, "{0}\\{1}_curves_{2}.tsv", txtOutputDirectory.Text, name, dataType)))
                                sw.Write(experimentalRun.GetTabularDelimitedCurveOutput(dataType));
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                            hasErrors = true;
                        }
                    }
                    
                }
                if(!hasErrors)
                    MessageBox.Show(string.Format(CultureInfo.InvariantCulture, "{0} Experiment(s) was(were) successfully saved, for {1} selected type(s) of data.", ExperimentalRuns.Count(), include.Count));
            }
            else
            {
                MessageBox.Show("With the current selection there is nothing to save!");
            }
        }

        private List<DataType> GetDataTypesToInclude()
        {
            var include = new List<DataType>();
            if ((bool) ckRaw.IsChecked)
                include.Add(DataType.Raw);
            if ((bool) ckProcessed.IsChecked)
                include.Add(DataType.Processed);
            if ((bool) ckFirstDeriv.IsChecked)
                include.Add(DataType.FirstDerivative);
            return include;
        }

       
    }
}
