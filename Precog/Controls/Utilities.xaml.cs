using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using DataModels;

namespace Precog.Controls
{
    /// <summary>
    /// Interaction logic for Utilities.xaml
    /// </summary>
    public partial class Utilities : UserControl
    {

        #region Run
        public ExperimentalRun SelectedExperimentalRun
        {
            get { return GetValue(SelectedExperimentalRunProperty) as ExperimentalRun; }
            set { SetValue(SelectedExperimentalRunProperty, value); }
        }

        // Using a DependencyProperty as the backing store for XRange.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedExperimentalRunProperty =
            DependencyProperty.Register("SelectedExperimentalRun", typeof(ExperimentalRun), typeof(Utilities), new PropertyMetadata(null, OnSelectedExperimentalRunPropertyChanged));

        private static void OnSelectedExperimentalRunPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as Utilities;
            myObj.OnSelectedExperimentalRunPropertyChanged(e);
        }

        private void OnSelectedExperimentalRunPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (SelectedExperimentalRun != null)
            {
                SetContolEnableBehaviour();
            }
        }

        #endregion

        #region ImportStart
        public static readonly RoutedEvent CreateHeatMapEvent = EventManager.RegisterRoutedEvent(
        "CreateHeatMap", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Utilities));

        public event RoutedEventHandler CreateHeatMap
        {
            add { AddHandler(CreateHeatMapEvent, value); }
            remove { RemoveHandler(CreateHeatMapEvent, value); }
        }

        void RaiseCreateHeatMapEvent()
        {
            var newEventArgs = new RoutedEventArgs(CreateHeatMapEvent);
            RaiseEvent(newEventArgs);
        }
        #endregion

        public Utilities()
        {
            InitializeComponent();
            SetContolEnableBehaviour();
        }

        private void SetContolEnableBehaviour()
        {
            RootLayout.IsEnabled = SelectedExperimentalRun != null;
            if (SelectedExperimentalRun != null)
                switch (SelectedExperimentalRun.ReplicateBehaviour)
                {
                    case ReplicateSelection.None:
                        grpHeatMap.IsEnabled = true;
                        break;
                    case ReplicateSelection.PlateWise:
                        grpHeatMap.IsEnabled = false;
                        break;
                    case ReplicateSelection.Pattern:
                        grpHeatMap.IsEnabled = false;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
        }

        private void btnCreateHeatMap_Click(object sender, RoutedEventArgs e)
        {
            RaiseCreateHeatMapEvent();
        }
    }
}
