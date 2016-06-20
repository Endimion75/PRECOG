using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DataModels
{
    public class CalibrationFunction
    {
        private readonly Dictionary<short, float> _termDictionary;

        public void AddTerm(short order, float coefficient)
        {
            _termDictionary.Add(order, coefficient);
        }

        public List<float> GetTerms()
        {
            return _termDictionary.Select(valuePair => valuePair.Value).ToList();
        }

        public short CountTerms()
        {
            return (short) _termDictionary.Count();
        }

        public CalibrationFunction()
        {
            _termDictionary = new Dictionary<short, float>();
        }
    }
}
