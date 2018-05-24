using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenCLTemplate.MachineLearning
{
    /// <summary>
    /// This class encapsulates all training data.
    /// </summary>
    public class TrainingSet
    {
        /// <summary>
        /// List of training units
        /// </summary>
        public List<TrainingUnit> trainingArray = new List<TrainingUnit>();
        int p = 0; // Number of dimensions in each training unit (must be the same in them all)

        /// <summary>Auxiliar variable used during training, kernels values</summary>
        public float[][] kernels;
        /// <summary>Is kernel K[i][p], p=0..n, calculated?</summary>
        public bool[] IsKernelCalculated;

        /// <summary>
        /// Auxiliar variable used during training
        /// </summary>
        public float[] errors;

        /// <summary>Adds a new training unit to the set</summary>
        /// <param name="newTrainingUnit">New training unit to add</param>
        public void addTrainingUnit(TrainingUnit newTrainingUnit)
        {
            if (p != 0)
            {
                if (p == newTrainingUnit.getDimension())
                {
                    trainingArray.Add(newTrainingUnit);
                }
                else
                {
                    // Invalid entry, not equal in size to the others training units
                    // Do nothing
                }
            }
            else // The first training set is being added
            {
                p = newTrainingUnit.getDimension();
                trainingArray.Add(newTrainingUnit);
            }
        }

        /// <summary>
        /// Random number generator
        /// </summary>
        private static Random rnd = new Random();
        
        /// <summary>Removes a random training unit from the set</summary>
        public void removeRandomTrainingUnit()
        {
            trainingArray.RemoveAt(rnd.Next(getN - 1));
        }

        /// <summary>
        /// Gets the number of elements in the training set (equivalent to trainingArray.Count)
        /// </summary>
        public int getN
        {
            get
            {
                return trainingArray.Count;
            }
        }
    }
}
