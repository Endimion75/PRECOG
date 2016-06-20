using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for ProcessData.xaml
    /// </summary>
    public partial class ProcessData : UserControl
    {

        #region Routed Events

        #region ImportStep
        public static readonly RoutedEvent ParamaterSetEvent = EventManager.RegisterRoutedEvent(
        "ParamaterSet", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ProcessData));

        public event RoutedEventHandler ParamaterSet
        {
            add { AddHandler(ParamaterSetEvent, value); }
            remove { RemoveHandler(ParamaterSetEvent, value); }
        }

        void RaiseParamaterSetEvent()
        {
            var newEventArgs = new RoutedEventArgs(ParamaterSetEvent);
            RaiseEvent(newEventArgs);
        }
        #endregion

        #endregion

        #region public List<ExperimentalRun> ExperimentalRuns
        /// <summary>
        /// 
        /// </summary>
        public List<ExperimentalRun> ExperimentalRuns
        {
            get { return GetValue(ExperimentalRunsProperty) as List<ExperimentalRun>; }
            set { SetValue(ExperimentalRunsProperty, value); }
        }

        /// <summary>
        /// Identifies the DataSeries dependency property.
        /// </summary>
        public static readonly DependencyProperty ExperimentalRunsProperty =
            DependencyProperty.Register(
                "ExperimentalRuns",
                typeof (List<ExperimentalRun>),
                typeof (ProcessData),
                new PropertyMetadata(null, null)); //OnExperimentalRunsPropertyChanged));

        private static void OnExperimentalRunsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as ProcessData;
        }

        private void OnExperimentalRunsPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (ExperimentalRuns != null)
            {
               
            }
        }

        #endregion public List<ExperimentalRun> ExperimentalRuns

        #region BlankValue
        public float BlankValue
        {
            get { return (float)GetValue(BlankValueProperty); }
            set { SetValue(BlankValueProperty, value); }
        }

        public static readonly DependencyProperty BlankValueProperty =
            DependencyProperty.Register("BlankValue", typeof(float), typeof(ProcessData), new PropertyMetadata(float.NaN, OnBlankValuePropertyChanged));

        private static void OnBlankValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as ProcessData;
            myObj.OnBlankValuePropertyChanged(e);
        }

        private void OnBlankValuePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!float.IsNaN(BlankValue))
            {
            }
        }

        #endregion

        #region TrueODCalibarationFunction
        public CalibrationFunction TrueODCalibarationFunction
        {
            get { return (CalibrationFunction)GetValue(TrueODCalibarationFunctionProperty); }
            set { SetValue(TrueODCalibarationFunctionProperty, value); }
        }

        public static readonly DependencyProperty TrueODCalibarationFunctionProperty =
            DependencyProperty.Register("TrueODCalibarationFunction", typeof(CalibrationFunction), typeof(ProcessData), new PropertyMetadata(null, OnTrueODCalibarationFunctionPropertyChanged));

        private static void OnTrueODCalibarationFunctionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var myObj = d as ProcessData;
            myObj.OnBlankValuePropertyChanged(e);
        }

        private void OnTrueODCalibarationFunctionPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (TrueODCalibarationFunction != null)
            {
            }
        }

        #endregion

        public double learningRate = 100;
        public double sigmoidAlphaValue = 2.0;
        public int neuronsInFirstLayer = 10;
        public int iterations = 500;

        public ProcessData()
        {
            InitializeComponent();


            this.cbMacro.Items.Add("Default");
            this.cbMacro.SelectedIndex = 0;
            this.cbCalFunctions.Items.Add("None");
            this.cbCalFunctions.Items.Add("S. cerevisiae");
            this.cbCalFunctions.Items.Add("S. pombe");
            this.cbCalFunctions.Items.Add("C. albicans");
            this.cbCalFunctions.Items.Add("P. pastoris");
            this.cbCalFunctions.Items.Add("E. coli");
            this.cbCalFunctions.Items.Add("Custom");
            this.cbCalFunctions.SelectedIndex = 1;
            this.txBlank.Text = "0";
            BlankValue = 0;
            txtCoeffA.Text = "1";
            txtCoeffB.Text = "0";
            txtCoeffC.Text = "0.8324057";
            TrueODCalibarationFunction = new CalibrationFunction();
            TrueODCalibarationFunction.AddTerm(0, 1);
            TrueODCalibarationFunction.AddTerm(1, 0);
            TrueODCalibarationFunction.AddTerm(2, (float)0.8324057);
        }

        private void btnSet_Click(object sender, RoutedEventArgs e)
        {
            SetBlank();
            SetCalibrationFunction();
            RaiseParamaterSetEvent();
        }

        private void SetBlank()
        {
            float value;
            var isValid = ValidateTextBoxEntry(txBlank, out value);
            if(isValid)
                BlankValue = value;
        }

        private bool ValidateTextBoxEntry(TextBox control, out Single sngValue)
        {
            float value;
            var isValid = float.TryParse(control.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            if (isValid)
            {
                sngValue = Convert.ToSingle(control.Text);
                return true;
            }
            
            control.Text = string.Empty;
            MessageBox.Show("Value is invalid!");
            sngValue = Single.NaN;
            return false;
        }

        private void SetCalibrationFunction()
        {
            Single coeffA;
            Single coeffB; 
            Single coeffC;

            if (cbCalFunctions.SelectedIndex != 6)
            {
                TrueODCalibarationFunction = new CalibrationFunction();
                var x = cbCalFunctions.SelectedIndex;
                switch (x)
                {
                    case 0:
                        //None
                        TrueODCalibarationFunction.AddTerm(0, 1);
                        TrueODCalibarationFunction.AddTerm(1, 0);
                        TrueODCalibarationFunction.AddTerm(2, 0);
                        break;
                    case 1:
                        //S. cerevisiae
                        TrueODCalibarationFunction.AddTerm(0, 1);
                        TrueODCalibarationFunction.AddTerm(1, 0);
                        TrueODCalibarationFunction.AddTerm(2, (float)0.8324057);
                        break;
                    case 2:
                        //S. pombe
                        TrueODCalibarationFunction.AddTerm(0, 1);
                        TrueODCalibarationFunction.AddTerm(1, 0);
                        TrueODCalibarationFunction.AddTerm(2, (float)0.64672463774234579);
                        break;
                    case 3:
                        //C. albicans
                        TrueODCalibarationFunction.AddTerm(0, 1);
                        TrueODCalibarationFunction.AddTerm(1, 0);
                        TrueODCalibarationFunction.AddTerm(2, (float)0.5790256635480614);
                        break;
                    case 4:
                        //P. pastoris
                        TrueODCalibarationFunction.AddTerm(0, 1);
                        TrueODCalibarationFunction.AddTerm(1, 0);
                        TrueODCalibarationFunction.AddTerm(2, (float)0.5653284345804932);
                        break;
                    case 5:
                        //E.coli
                        TrueODCalibarationFunction.AddTerm(0, 1);
                        TrueODCalibarationFunction.AddTerm(1, 0);
                        TrueODCalibarationFunction.AddTerm(2, (float)0.75389848795692815);
                        break;
                }
            }
            else
            {
                var isCoeefAValid = ValidateTextBoxEntry(txtCoeffA, out coeffA);
                var isCoeefBValid = ValidateTextBoxEntry(txtCoeffB, out coeffB);
                var isCoeefCValid = ValidateTextBoxEntry(txtCoeffC, out coeffC);

                if (!isCoeefAValid || !isCoeefBValid || !isCoeefCValid) return;

                TrueODCalibarationFunction = new CalibrationFunction();
                TrueODCalibarationFunction.AddTerm(0, coeffA);
                TrueODCalibarationFunction.AddTerm(1, coeffB);
                TrueODCalibarationFunction.AddTerm(2, coeffC);
            }
        }

        private void txtNumericType_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private static bool IsTextAllowed(string text)
        {
            var regex = new Regex(@"^[0-9]*(?:\.[0-9]*)?$");
            return regex.IsMatch(text);
        }

        private void TextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(String)))
            {
                var text = (String)e.DataObject.GetData(typeof(String));
                if (!IsTextAllowed(text))
                    e.CancelCommand();
            }
            else
                e.CancelCommand();
        }

        private void CbCalFunctions_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            stCustomFunction.Visibility = cbCalFunctions.SelectedIndex == 6 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
