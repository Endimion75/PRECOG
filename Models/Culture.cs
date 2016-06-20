using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows.Media;

namespace DataModels
{
    [DataContract]
    public class Culture : IComparable , INotifyPropertyChanged
    {
        [DataMember]
        public GrowthMeasurements GrowthMeasurements { get; set; }

        public ImageSource GraphImageSource { get; set; }

        [DataMember]
        public string Container { get; set; }

        [DataMember]
        public int ContainerIndex { get; set; }

        private double _lag;
        [DataMember]
        public double Lag
        {
            get { return _lag; }
            set
            {
                _lag = value;
                NotifyPropertyChanged("Lag");
            }
        }

        private double _rate;
        [DataMember]
        public double Rate
        {
            get { return _rate; }
            set
            {
                _rate = value;
                NotifyPropertyChanged("Rate");
            }
        }

        private double _yield;
        [DataMember]
        public double Yield
        {
            get { return _yield; }
            set
            {
                _yield = value;
                NotifyPropertyChanged("Yield");
            }
        }

        private QualityIndex _qualityIndex;
        [DataMember]
        public QualityIndex QualityIndex
        {
            get { return _qualityIndex; }
            set
            {
                _qualityIndex = value;
                NotifyPropertyChanged("qIdx");
            }
        }

        [DataMember]
        public bool IsFaulty { get; set; }

        [DataMember]
        public bool IsSample { get; set; }

        public Range ODRange { get; set; }
        public Range TimeRange { get; set; }
        

        public Culture()
        {
            GrowthMeasurements = new GrowthMeasurements();
        }
        
        public Culture(string container)
        {
            Container = container;
            GrowthMeasurements = new GrowthMeasurements();
            IsSample = false;
            IsFaulty = false;
        }

        public Culture(int containerIndex, string wellName, List<GrowthMeasurement> rawGrowthMeasurements)
        {
            ContainerIndex = containerIndex;
            Container = "Well " + wellName;
            GrowthMeasurements = new GrowthMeasurements(DataType.Raw,rawGrowthMeasurements);
            
            IsSample = false;
            IsFaulty = false;
        }

        public int CompareTo(object obj)
        {
            var culture = (Culture)obj;

            if (culture == null)
                throw new ArgumentException("Object is not culture");

            int thisIdx = this.ContainerIndex;
            int otherIdx = culture.ContainerIndex; 
            
            return thisIdx.CompareTo(otherIdx);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

    }
}
