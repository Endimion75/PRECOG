using System.Collections.Generic;
using System.Globalization;

namespace DataModels
{
    public class MergedCulture : Culture
    {
        public MergedCulture(string container) : base(container)
        {
            Ini();
        }

        private void Ini()
        {
            IniLocals();
            IsSample = true;
            //GrowthMeasurementsList = new List<GrowthMeasurements>();
            MergedGrowthMeasurements = new Dictionary<string, GrowthMeasurements>();
        }

        private void IniLocals()
        {
            _gtCount = 0;
            _lagCount = 0;
            _sumGt = 0;
            _sumLag = 0;
            _sumYield = 0;
            _yieldCount = 0;
            _cultureCount = 0;
            _mergedContent = string.Empty;
        }

        public MergedCulture(int containerIndex, string wellName, List<GrowthMeasurement> rawGrowthMeasurements) : base(containerIndex, wellName, rawGrowthMeasurements)
        {
            Ini();
        }

        public Dictionary<string, GrowthMeasurements> MergedGrowthMeasurements { get; set; }

        private int _cultureCount;
        public int CulturesCount
        {
            get { return _cultureCount; }
        }

        public void CalculateValues()
        {
            Lag = _sumLag / _lagCount;
            Rate = _sumGt / _gtCount;
            Yield =_sumYield / _yieldCount; 
        }

        public void AddCulture(Culture culture)
        {
            string faulty = string.Empty;
            if (!culture.IsFaulty)
            {
                AddLag(culture.Lag);
                AddGT(culture.Rate);
                AddYield(culture.Yield);
            }
            else
            {
                faulty = "*";
                this.IsFaulty = true;
            }

            IncreaseSampleCount();
            _mergedContent = string.Format(CultureInfo.InvariantCulture, "{0}{1}{2},", _mergedContent, culture.ContainerIndex.ToString(), faulty);
        }
         
        private string _mergedContent;
        public string MergedContent
        {
            get
            {
                return _mergedContent.EndsWith(",") ? _mergedContent.Remove(_mergedContent.Length -1, 1) : _mergedContent;
            }
        }

        private int _lagCount;
        private int _gtCount;
        private int _yieldCount;

        private double _sumLag;
        private double _sumGt;
        private double _sumYield;

        private void AddLag(double lag)
        {
            if(!double.IsNaN(lag))
            {
                _lagCount += 1;
                _sumLag += lag;    
            }
        }

        private void AddGT(double gt)
        {
            if(!double.IsNaN(gt))
            {
                _gtCount += 1;
                _sumGt += gt;    
            }
        }

        private void AddYield(double yield)
        {
            if(!double.IsNaN(yield))
            {
                _yieldCount += 1;
                _sumYield += yield;    
            }
        }

        private void IncreaseSampleCount()
        {
            _cultureCount += 1;
        }

       
    }
}
