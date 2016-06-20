using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModels
{
    public class Peak
    {
        public string Experiment { get; set; }
        public string Well { get; set; }
        public float MaxPeak { get; set; }
        public int Number { get; set; }
    }

    public static class SessionVariables
    {
        public static List<Peak> Peaks { get; set; }

        static SessionVariables()
        {
            Peaks = new List<Peak>();
        }
    }
}
