using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using AForge.Neuro;
using Accord.Neuro;
using Accord.Neuro.Learning;
using DataModels;

namespace Macro
{
    public class Neural
    {
        private double _learningRate = 100;
        private double _sigmoidAlphaValue = 2.0;
        private int _neuronsInFirstLayer = 10;
        private int _iterations = 500;
        private bool _useRegularization= false;
        private bool _useNguyenWidrow = false;

        private Thread workerThread = null;

        //private Culture _culture;
        private bool needToStop = false;
        private double[,] _soultion;

        public Neural(NeuralParameters parameters)
        {
            _learningRate = parameters.LearningRate;
            _sigmoidAlphaValue = parameters.SigmoidAlphaValue;
            _neuronsInFirstLayer = parameters.NeuronsInFirstLayer;
            _iterations = parameters.Iterations;
            _useNguyenWidrow = parameters.UseNguyenWidrow;
            _useRegularization = parameters.UseRegularization;
        }
        public Neural()
        {
        }

        public List<GrowthMeasurement> GetFittedData(List<GrowthMeasurement> data)
        {
            Range xRange = new Range(GrowthRangeType.Time,data.Max(m => m.Time),data.Min(m => m.Time));
            Range yRange = new Range(GrowthRangeType.OD, data.Max(m => m.OD), data.Min(m => m.OD)); ;
            SearchSolution(xRange, yRange,data);
            var temp = new List<GrowthMeasurement>();
            for (int i = 0; i < _soultion.GetLength(0); i++)
            {
                var fittedMeasurement = new GrowthMeasurement((float) _soultion[i, 0], (float) _soultion[i, 1]);
                temp.Add(fittedMeasurement);
            }
            return temp;
        }

        void SearchSolution(Range xRange, Range yRange, List<GrowthMeasurement> data)
        {
            // number of learning samples
            int samples = data.Count();
            // data transformation factor
            double yFactor = 1.7 / yRange.Max;
            double yMin = yRange.Min;
            double xFactor = 2.0 / xRange.Max;
            double xMin = xRange.Min;

            // prepare learning data
            double[][] input = new double[samples][];
            double[][] output = new double[samples][];

            for (int i = 0; i < samples; i++)
            {
                input[i] = new double[1];
                output[i] = new double[1];

                // set input
                input[i][0] = (data[i].Time - xMin) * xFactor - 1.0;
                // set output
                output[i][0] = (data[i].OD - yMin) * yFactor - 0.85;
            }

            // create multi-layer neural network
            ActivationNetwork network = new ActivationNetwork(new BipolarSigmoidFunction(_sigmoidAlphaValue),1, _neuronsInFirstLayer, 1);

            if (_useNguyenWidrow)
            {
                //NguyenWidrowInitializer initializer = new NguyenWidrowInitializer(network);
                //initializer.Randomize();
            }

            // create teacher
            LevenbergMarquardtLearning teacher = new LevenbergMarquardtLearning(network, _useRegularization);

            // set learning rate and momentum
            teacher.LearningRate = _learningRate;

            // iterations
            int iteration = 1;

            // solution array
            double[,] solution = new double[samples, 2];
            double[] networkInput = new double[1];

            // calculate X values to be used with solution function
            for (int j = 0; j < samples; j++)
            {
                solution[j, 0] = xRange.Min + (double)j * xRange.Max / (samples - 1);
            }

            // loop
            while (!needToStop)
            {
                // if (useRegularization)
                // teacher.UseRegularization = ((iteration % 3) == 0);
                // run epoch of learning procedure
                double error = teacher.RunEpoch(input, output) / samples;

                // calculate solution
                for (int j = 0; j < samples; j++)
                {
                    networkInput[0] = (solution[j, 0] - xMin) * xFactor - 1.0;
                    solution[j, 1] = (network.Compute(networkInput)[0] + 0.85) / yFactor + yMin;
                }
                //chart.UpdateDataSeries("solution", solution);
                // calculate error
                double learningError = 0.0;
                for (int j = 0, k = data.Count; j < k; j++)
                {
                    networkInput[0] = input[j][0];
                    learningError += Math.Abs(data[j].OD - ((network.Compute(networkInput)[0] + 0.85) / yFactor + yMin));
                }

                // set current iteration's info
                //SetText(currentIterationBox, iteration.ToString());
                //SetText(currentErrorBox, learningError.ToString("F3"));

                // increase current iteration
                iteration++;

                // check if we need to stop
                if ((_iterations != 0) && (iteration > _iterations))
                {
                    _soultion = solution;
                    break;
                }

            }


            // enable settings controls
            //EnableControls(true);
        }
    }
}
