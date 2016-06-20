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

namespace Precog.Controls
{
    /// <summary>
    /// Interaction logic for ZoomGraphControls.xaml
    /// </summary>
    public partial class ZoomGraphControls : UserControl
    {
        #region ControlValues
        public ZoomGraphControlValues ControlValues
        {
            get { return (ZoomGraphControlValues)GetValue(ControlValuesProperty); }
            set { SetValue(ControlValuesProperty, value); }
        }

        public static readonly DependencyProperty ControlValuesProperty =
            DependencyProperty.Register("ControlValues", typeof(ZoomGraphControlValues), typeof(ZoomGraphControls), new PropertyMetadata(null, OnControlValuesPropertyChanged));


        private static void OnControlValuesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as ZoomGraphControls;
            myObj.OnControlValuesPropertyChanged(e);
        }

        private void OnControlValuesPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (ControlValues != null)
            {
                IniValues();
            }
        }

        #endregion

        public ZoomGraphControls()
        {
            InitializeComponent();
            IniValues();
        }

        private void btnFitData_Click(object sender, RoutedEventArgs e)
        {
            btnFitData.Tag = btnFitData.Tag.ToString() == false.ToString() ? true.ToString() : false.ToString();
        }

        private void btnZoomFit_Click(object sender, RoutedEventArgs e)
        {
            btnZoomFit.Tag = btnZoomFit.Tag.ToString() == false.ToString() ? true.ToString() : false.ToString();
        }

        private void IniValues()
        {
            if (ControlValues == null)
            {
                RootStack.IsEnabled = false;
                return;
            }

            RootStack.IsEnabled = true;
            if (ckDisplayMetaDataLag != null)
                ckDisplayMetaDataLag.IsChecked = ControlValues.DisplayMetaLagData;
            if (ckDisplayMetaDataRate != null)
                ckDisplayMetaDataRate.IsChecked = ControlValues.DisplayMetaRateData;
            if (ckDisplayMetaDataYield != null)
                ckDisplayMetaDataYield.IsChecked = ControlValues.DisplayMetaYieldData;
            if (ckDisplayRaw != null) ckDisplayRaw.IsChecked = ControlValues.DisplayRaw;
            if (ckDisplayFD != null) ckDisplayFD.IsChecked = ControlValues.DisplayFD;
            if (ckLogYAxis != null) ckLogYAxis.IsChecked = ControlValues.LogYAxis;
            if (rbPan != null && rbZoom != null)
            {
                switch (ControlValues.ChartBehaviour)
                {
                    case ChartBehaviour.Zoom:
                        rbPan.IsChecked = false;
                        rbZoom.IsChecked = true;
                        break;
                    case ChartBehaviour.Pan:
                        rbPan.IsChecked = true;
                        rbZoom.IsChecked = false;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            btnFitData.Tag = false;
            btnZoomFit.Tag = false;
        }

        private void ckDisplayFD_Checked(object sender, RoutedEventArgs e)
        {
            ckDisplayRaw.IsChecked = false;
        }

        private void ckDisplayRaw_Checked(object sender, RoutedEventArgs e)
        {
            ckDisplayFD.IsChecked = false;
        }

        
    }
}
