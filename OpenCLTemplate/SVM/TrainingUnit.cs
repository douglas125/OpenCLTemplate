using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenCLTemplate.MachineLearning
{
    /// <summary>
    /// This class corresponds to a training unit of the training data with all its inputs and the desired output.
    /// </summary>
    public class TrainingUnit
    {
        /// <summary>Features</summary>
        public float[] xVector;

        /// <summary>Desired output (-1 or 1)</summary>
        public float y;


        /// <summary>Creates a new training unit</summary>
        /// <param name="newXVector">New unit</param>
        /// <param name="Classification">Classification, should ONLY be 1 or -1 for pure SVMs, write desired value for MultiClassSVMs</param>
        public TrainingUnit(float[] newXVector, float Classification)
        {
            xVector = newXVector;
            y = Classification;
        }
        /// <summary>Gets dimension of feature vector</summary>
        public int getDimension()
        {
            return xVector.Length;
        }

        /// <summary>Retuurns a new trainingUnit which is the clone of this one</summary>
        public TrainingUnit Clone()
        {
            float[] x = new float[xVector.Length];
            for (int i = 0; i < x.Length; i++) x[i] = xVector[i];

            return new TrainingUnit(x, this.y);
        }
    }
}
