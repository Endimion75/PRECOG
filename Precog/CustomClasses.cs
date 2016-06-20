using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Visiblox.Charts;

namespace Precog
{
    class CustomClasses
    {
    }

    public class BioScreenFile 
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }

        public override string ToString()
        {
            return FileName;
        }
    }

    public enum ChartBehaviour
    {
        Zoom,
        Pan
    }

    public class ZoomGraphControlValues : INotifyPropertyChanged
    {
        private bool _displayRaw;
        public bool DisplayRaw
        {
            get { return _displayRaw; }
            set 
            {
                _displayRaw = value;
                OnPropertyChanged(new PropertyChangedEventArgs("DisplayRaw"));
            }
        }

        private bool _displayFD;
        public bool DisplayFD
        {
            get { return _displayFD; }
            set
            {
                _displayFD = value;
                OnPropertyChanged(new PropertyChangedEventArgs("DisplayFD"));
            }
        }

        private bool _displayMetaLagData;
        public bool DisplayMetaLagData
        {
            get { return _displayMetaLagData; }
            set
            {
                _displayMetaLagData = value;
                OnPropertyChanged(new PropertyChangedEventArgs("DisplayMetaLagData"));
            }
        }

        private bool _displayMetaRateData;
        public bool DisplayMetaRateData
        {
            get { return _displayMetaRateData; }
            set
            {
                _displayMetaRateData = value;
                OnPropertyChanged(new PropertyChangedEventArgs("DisplayMetaRateData"));
            }
        }

        private bool _displayMetaYieldData;
        public bool DisplayMetaYieldData
        {
            get { return _displayMetaYieldData; }
            set
            {
                _displayMetaYieldData = value;
                OnPropertyChanged(new PropertyChangedEventArgs("DisplayMetaYieldData"));
            }
        }

        private bool _logYAxis;
        public bool LogYAxis
        {
            get { return _logYAxis; }
            set
            {
                _logYAxis = value;
                OnPropertyChanged(new PropertyChangedEventArgs("LogYAxis"));
            }
        }

        private ChartBehaviour _chartBehaviour;
        public ChartBehaviour ChartBehaviour
        {
            get { return _chartBehaviour; }
            set
            {
                _chartBehaviour = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ChartBehaviour"));
            }
        }

        private bool _fitData;
        public bool FitData
        {
            get { return _fitData; }
            set
            {
                _fitData = value;
                OnPropertyChanged(new PropertyChangedEventArgs("FitData"));
            }
        }

        private bool _enableFit;
        public bool EnableFit
        {
            get { return _enableFit; }
            set
            {
                _enableFit = value;
                OnPropertyChanged(new PropertyChangedEventArgs("EnableFit"));
            }
        }

        private bool _zoomFit;
        public bool ZoomFit
        {
            get { return _zoomFit; }
            set
            {
                _zoomFit = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ZoomFit"));
            }
        }

        private double _skipPointsNumber;
        public double SkipPointsNumber
        {
            get { return _skipPointsNumber; }
            set
            {
                _skipPointsNumber = value;
                OnPropertyChanged(new PropertyChangedEventArgs("SkipPointsNumber"));
            }
        }

        public ZoomGraphControlValues()
        {
            //OnPropertyChanged("Ini");
        }

        public event PropertyChangedEventHandler PropertyChanged; 

        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, e);
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

    }
}
