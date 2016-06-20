using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Macro
{
    public class NeuralParameters
    {
        public double LearningRate { get; set; }
        public double SigmoidAlphaValue { get; set; }
        public int NeuronsInFirstLayer { get; set; }
        public int Iterations { get; set; }
        public bool UseRegularization { get; set; }
        public bool UseNguyenWidrow { get; set; }
    }
}
