using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

/* SVM References
 * 
 * 
[1] VAPNIK, V. Statistical Learning Theory. New York: Wiley, 1998.

[2] EL-NAQA, Issam, YANG, Yongyi, WERNICK, Miles N., GALATSANOS, Nikolas P. and NISHIKAWA, Robert M., A Support Vector Machine Approach for Detection of Microcalcifications. IEEE Transactions on medical imaging Vol 21, No 12, December 2002.

[3] HUSSAIN, Syed Fawad, BISSON, Gilles. Text Categorization Using Word Similarities Based on Higher Co-occurrences. SIAM

[4] ANDREW, Y. Ng, Stanford Machine Learning Course, http://www.stanford.edu/class/cs229/

[5] LECUN, Y., BOTTOU, L., BENGIO, Y., and HAFFNER, P. "Gradient-based learning applied to document recognition." Proceedings of the IEEE, 86(11):2278-2324, November 1998.

 * 
 * 
 */
namespace OpenCLTemplate.MachineLearning
{
    /// <summary>
    /// This class stores the variables of a SMO problem solution
    /// </summary>
    public partial class SVM
    {
        /// <summary>Training set for this solution</summary>
        public TrainingSet TrainingSet = null;

        /// <summary>Problem configuration</summary>
        public ProblemConfig ProblemCfg = new ProblemConfig(0.1f, 50, 1e-3f, 1);

        /// <summary>
        /// Lagrange multipliers for solution
        /// </summary>
        private List<float> alphaList;

        /// <summary>
        /// Threshold for solution
        /// </summary>
        private float b;

        /// <summary>
        /// The dimension of the training set for which this is a solution
        /// </summary>
        private int dimension;

        /// <summary>
        ///  Constructor that initializes with 0s
        /// </summary>
        public SVM()
        {
            initializeWithZeros();

            CLSVMInit();
        }

        /*
        /// <summary>
        ///  Constructor that initializes with a copy of another solution's values
        /// </summary>
        /// <param name="sourceSolution">The source to copy from</param>
        public SVM(SVM sourceSolution)
        {
            Load(sourceSolution);
        }
        */

        /// <summary>
        /// Set all values of the solution to 0
        /// </summary>
        public void initializeWithZeros()
        {
            if (TrainingSet != null)
            {
                dimension = TrainingSet.getN;
                alphaList = new List<float>();
                for (int i = 0; i < dimension; i++)
                {
                    alphaList.Add(0);
                }
                b = 0;
            }
        }


        #region Trains, cross-validates, and resets SVM

        /// <summary>Cross validation parameters: [0] - maximum crossValidation value found so far, [1] - lambda, [2] - C.  Returns best performance so far</summary>
        private float[] CrossValParams;

        /// <summary>Attempts to pre-calibrate configuration parameters.
        /// Finds an alpha that enhances similarities between positive examples
        /// and reduces similarities between positive and negative examples.
        /// Assumes that decreasing lambda increases kernel match.
        /// </summary>
        /// <param name="tolPositive">Positive kernels average should be greater than tolPositive</param>
        /// <param name="tolNegative">Negative kernels average should be lesser than tolNegative</param>
        public void PreCalibrateCfg(float tolPositive, float tolNegative)
        {
            #region Checks if there are positive and negative examples
            bool posSamples = false; bool negSamples = false;
            for (int i = 0; i < TrainingSet.trainingArray.Count; i++)
            {
                if (TrainingSet.trainingArray[i].y > 0) posSamples = true;
                if (TrainingSet.trainingArray[i].y < 0) negSamples = true;
                if (posSamples && negSamples) i = TrainingSet.trainingArray.Count;
            }
            if ((!posSamples) || (!negSamples)) throw new Exception("Training set must contain positive and negative samples");
            #endregion

            Random rnd = new Random();
            int nSet = (int)(20 * Math.Log(TrainingSet.getN, 2));

            TrainingSet PositiveExamples1 = new TrainingSet();
            TrainingSet PositiveExamples2 = new TrainingSet();
            TrainingSet NegativeExamples = new TrainingSet();

            //Kernel average for positive and negative samples
            float positiveAvg = 0, negativeAvg = 0;
            float invN = 1 / (float)nSet;
            int count = 0;

            float bestLambda = ProblemCfg.lambda;
            float maxPosNegAvg = -1.0f;

            while ((positiveAvg <= tolPositive || negativeAvg >= tolNegative) && count < nSet)
            {
                //Populates training sets
                PositiveExamples1.trainingArray.Clear();
                PositiveExamples2.trainingArray.Clear();
                NegativeExamples.trainingArray.Clear();
                while (PositiveExamples1.getN < nSet || PositiveExamples2.getN < nSet || NegativeExamples.getN < nSet)
                {
                    TrainingUnit tu = TrainingSet.trainingArray[rnd.Next(TrainingSet.trainingArray.Count - 1)];
                    if (tu.y > 0 && PositiveExamples1.getN < nSet)
                        PositiveExamples1.addTrainingUnit(tu);
                    else if (tu.y > 0 && PositiveExamples2.getN < nSet)
                        PositiveExamples2.addTrainingUnit(tu);

                    if (tu.y < 0 && NegativeExamples.getN < nSet) NegativeExamples.addTrainingUnit(tu);
                }

                count++;

                positiveAvg = 0;
                negativeAvg = 0;
                for (int i = 0; i < nSet; i++)
                {
                    positiveAvg += ProblemSolver.calculateSingleKernel(PositiveExamples1.trainingArray[i], PositiveExamples2.trainingArray[i], this);
                    negativeAvg += ProblemSolver.calculateSingleKernel(PositiveExamples1.trainingArray[i], NegativeExamples.trainingArray[i], this);
                }
                positiveAvg *= invN;
                negativeAvg *= invN;

                if (maxPosNegAvg < positiveAvg - negativeAvg)
                {
                    bestLambda = ProblemCfg.lambda;
                    maxPosNegAvg = positiveAvg - negativeAvg;
                }

                //Desired: positiveAvg=1, negativeAvg = 0
                if (positiveAvg <= tolPositive) this.ProblemCfg.lambda *= 0.15f;
                else if (negativeAvg >= tolNegative) this.ProblemCfg.lambda *= 1.2f;
            }
            ProblemCfg.lambda = bestLambda;

        }

        /// <summary>Trains current SVM with cross-validation, adjusting kernel parameter lambda and box parameter C</summary>
        public float TrainWithCrossValidation()
        {
            Random rnd = new Random();

            float[] lambdaSet = new float[10];
            lambdaSet[0] = 3E-7f * ((float)rnd.NextDouble() + 1);
            for (int i = 1; i < lambdaSet.Length; i++) lambdaSet[i] = 4.5f * lambdaSet[i - 1];

            float[] cSet = new float[12];
            cSet[0] = 1E-4f * ((float)rnd.NextDouble() + 1);
            for (int i = 1; i < cSet.Length; i++) cSet[i] = 2.0f * cSet[i - 1];

            return TrainWithCrossValidation(0.15f, lambdaSet, cSet);
        }

        /// <summary>Trains current SVM with cross-validation, adjusting kernel parameter lambda and box parameter C. Returns best performance so far</summary>
        /// <param name="CrossValidationSetPercent">Percentage of training examples that should be used as cross validation set</param>
        /// <param name="lambdaSet">Values of lambda to try</param>
        /// <param name="CSet">Values of c to try</param>
        public float TrainWithCrossValidation(float CrossValidationSetPercent, float[] lambdaSet, float[] CSet)
        {
            if (alphaList == null || alphaList.Count != TrainingSet.getN)
            {
                //Problem changed, previous values dont make sense
                initializeWithZeros();
                CrossValParams = null;
            }

            #region Constructs cross validation set

            TrainingSet CrossValidationSet = new TrainingSet();
            int nCrossSet = (int)(CrossValidationSetPercent * (float)this.TrainingSet.getN);
            Random rnd = new Random();
            for (int i = 0; i < nCrossSet; i++)
            {
                int ind = rnd.Next(0, this.TrainingSet.trainingArray.Count - 1);
                TrainingUnit u = this.TrainingSet.trainingArray[ind];
                this.TrainingSet.trainingArray.Remove(u);
                CrossValidationSet.addTrainingUnit(u);
            }

            #endregion

            #region Loops through lambdas and Cs and finds maximum crossvalidation

            foreach (float _lambda in lambdaSet)
            {
                this.ProblemCfg.lambda = _lambda;

                this.initializeWithZeros();
                PreComputeKernels();

                foreach (float _c in CSet)
                {
                    this.ProblemCfg.c = _c;


                    //ProblemSolver.solveSMOStartingFromPreviousSolution(this);
                    ProblemSolver.solveSMOStartingFromZero(this);

                    float performance = this.GetHitRate(CrossValidationSet);

                    if (CrossValParams == null) CrossValParams = new float[3];

                    if (performance > CrossValParams[0])
                    {
                        CrossValParams[0] = performance;
                        CrossValParams[1] = _lambda;
                        CrossValParams[2] = _c;
                    }
                }
            }

            #endregion

            #region Trains with best parameters so far

            this.ProblemCfg.lambda = CrossValParams[1];
            this.ProblemCfg.c = CrossValParams[2];
            this.Train();

            #endregion

            return CrossValParams[0];
        }

        /// <summary>Trains current SVM</summary>
        public void Train()
        {
            //if (alphaList == null || alphaList.Count != TrainingSet.getN) initializeWithZeros();
            initializeWithZeros();

            PreComputeKernels();

            //Solves SMO
            ProblemSolver.solveSMOStartingFromPreviousSolution(this);
        }

        /// <summary>Precomputes kernels of training set</summary>
        private void PreComputeKernels()
        {
            // Cache all kernels and errors
            if (OpenCLTemplate.CLCalc.CLAcceleration == OpenCLTemplate.CLCalc.CLAccelerationType.UsingCL)
            {
                this.WriteToDevice();
                CLcalculateAllKernels(this);
            }
            else
            {
                ProblemSolver.calculateAllKernels(this);
            }
        }

        /// <summary>Resets current SVM solution</summary>
        public void ResetSolution()
        {
            this.initializeWithZeros();
        }

        #endregion


        #region Perform classification and self-diagnosis

        /// <summary>Removes all vectors that are not SVMs, ie, so that alpha[i]=0</summary>
        public void RemoveNonSupportVectors()
        {
            for (int i = 0; i < this.alphaList.Count; i++)
            {
                if (alphaList[i] == 0)
                {
                    alphaList.RemoveAt(i);
                    TrainingSet.trainingArray.RemoveAt(i);

                    i--;
                }
            }
            this.dimension = TrainingSet.trainingArray.Count;
            this.WriteToDevice();

            GC.Collect();
        }

        /// <summary>Classifies a training unit as positive or negative (true or false)</summary>
        /// <param name="Sample">Sample to be classified</param>
        public bool Classify(TrainingUnit Sample)
        {
            if (OpenCLTemplate.CLCalc.CLAcceleration == OpenCLTemplate.CLCalc.CLAccelerationType.UsingCL)
            {
                return (CLpredictOutput(this, Sample)>=0);
            }
            else return (ProblemSolver.predictOutput(this, Sample)>=0);
        }

        /// <summary>Classifies a training unit with a float. The bigger, the more positive the sample. Values greater than zero
        /// are assumed to be positive samples</summary>
        /// <param name="Sample">Sample to be classified</param>
        public float ClassificationValue(TrainingUnit Sample)
        {
            if (OpenCLTemplate.CLCalc.CLAcceleration == OpenCLTemplate.CLCalc.CLAccelerationType.UsingCL)
            {
                return (CLpredictOutput(this, Sample));
            }
            else return (ProblemSolver.predictOutput(this, Sample));
        }

        /// <summary>Gets the percentage of training examples classified correctly in the training set</summary>
        public float GetTrainingSetHitRate()
        {
            return GetTrainingSetHitRate(0);
        }

        /// <summary>Gets the percentage of training examples classified correctly starting from the ind-th training sample</summary>
        /// <param name="ind">Index to start reading training set samples</param>
        public float GetTrainingSetHitRate(int ind)
        {
            if (TrainingSet.kernels == null) ProblemSolver.calculateAllKernels(this);

            float rate = 0;

            for (int i = ind; i < TrainingSet.trainingArray.Count; i++)
            {
                bool c = ((TrainingSet.errors[i] + TrainingSet.trainingArray[i].y) >= 0);

                if ((c && TrainingSet.trainingArray[i].y == 1) || ((!c) && TrainingSet.trainingArray[i].y == -1)) rate++;
            }

            return rate / (float)TrainingSet.trainingArray.Count;
        }

        /// <summary>Computes hit rates for a given test set</summary>
        /// <param name="samples">Test set to be used</param>
        public float GetHitRate(TrainingSet samples)
        {
            float rate = 0;

            foreach (TrainingUnit tu in samples.trainingArray)
            {
                bool c = Classify(tu);
                if ((c && tu.y == 1) || ((!c) && tu.y == -1)) rate++;
            }

            return rate / (float)samples.getN;
        }

        #endregion



        #region Read/Write files

        /*
        /// <summary>
        /// Copy all values from another solution
        /// </summary>
        /// <param name="sourceSolution">The source to copy from</param>
        public void Load(SVM sourceSolution)
        {
            dimension = sourceSolution.dimension;
            alphaList = new float[dimension];
            for (int i = 0; i < dimension; i++)
            {
                alphaList[i] = sourceSolution.alphaList[i];
            }
            b = sourceSolution.b;
        }
        */

        /// <summary>
        /// Copy all values from another solution
        /// </summary>
        /// <param name="FileName">File containing alpha's data</param>
        public void Load(string FileName)
        {

            DataSet d = new DataSet();
            d.ReadXml(FileName);
            DataTable t = d.Tables["Solution"];
            dimension = t.Rows.Count;


            //Configuration
            DataTable TblCfg = d.Tables["Config"];

            float valC, valTol; int valKernel, valMaxP;

            valC = (float)((double)TblCfg.Rows[0]["dblValues"]);
            valKernel = (int)((double)TblCfg.Rows[1]["dblValues"]);
            valTol = (float)((double)TblCfg.Rows[2]["dblValues"]);
            valMaxP = (int)((double)TblCfg.Rows[3]["dblValues"]);
            this.b = (float)((double)TblCfg.Rows[4]["dblValues"]);
            float Lambda = (float)((double)TblCfg.Rows[5]["dblValues"]);
            int xDim = (int)((double)TblCfg.Rows[6]["dblValues"]);


            //Reads classifications
            DataTable TblClassif = d.Tables["Classifications"];

            alphaList = new List<float>();
            TrainingSet = new TrainingSet();

            for (int i = 0; i < dimension; i++)
            {
                TrainingSet.addTrainingUnit(new TrainingUnit(new float[xDim], -1));
            }

            for (int i = 0; i < dimension; i++)
            {
                alphaList.Add((float)((double)t.Rows[i]["dblValues"]));
                TrainingSet.trainingArray[i].y = (float)((double)TblClassif.Rows[i]["dblValues"]) > 0 ? 1 : -1;
            }

            //Reads training set
            //Creates datatables for training examples
            DataTable Tbl = d.Tables["Examples"];
            for (int i = 0; i < dimension; i ++)
            {
                for (int j = 0; j < xDim; j++)
                {
                    TrainingSet.trainingArray[i].xVector[j] = (float)((double)Tbl.Rows[j + i*xDim]["dblValues"]);
                }
            }

            this.ProblemCfg = new ProblemConfig(Lambda, valC, valTol, valMaxP, (ProblemConfig.KernelType)valKernel);

            if (OpenCLTemplate.CLCalc.CLAcceleration == OpenCLTemplate.CLCalc.CLAccelerationType.UsingCL)
            {
                this.WriteToDevice();
            }

        }

        /// <summary>Writes current solution to a file</summary>
        public void Save(string FileName)
        {
            DataSet d = new DataSet();

            //Creates datatables for training examples
            DataTable Tbl = XMLFuncs.CreateNewTable("Examples", new string[] { "dblValues" });
            d.Tables.Add(Tbl);
            for (int i = 0; i < dimension; i++)
            {
                for (int j = 0; j < TrainingSet.trainingArray[i].xVector.Length; j++)
                {
                    DataRow r = Tbl.NewRow();
                    r["dblValues"] = TrainingSet.trainingArray[i].xVector[j];
                    Tbl.Rows.Add(r);
                }
            }

            //Classifications
            DataTable TblClassif = XMLFuncs.CreateNewTable("Classifications", new string[] { "dblValues" });
            d.Tables.Add(TblClassif);

            DataTable t = XMLFuncs.CreateNewTable("Solution", new string[] { "dblValues" });
            d.Tables.Add(t);

            for (int i = 0; i < dimension; i++)
            {
                DataRow r = t.NewRow();
                r["dblValues"] = (double)alphaList[i];
                t.Rows.Add(r);

                DataRow rr = TblClassif.NewRow();
                rr["dblValues"] = (double)TrainingSet.trainingArray[i].y;
                TblClassif.Rows.Add(rr);
            }

            //Configuration
            DataTable TblCfg = XMLFuncs.CreateNewTable("Config", new string[] { "dblValues" });
            d.Tables.Add(TblCfg);

            DataRow rCfg1 = TblCfg.NewRow(); rCfg1["dblValues"] = ProblemCfg.c; TblCfg.Rows.Add(rCfg1);
            DataRow rCfg2 = TblCfg.NewRow(); rCfg2["dblValues"] = ProblemCfg.kernelType; TblCfg.Rows.Add(rCfg2);
            DataRow rCfg3 = TblCfg.NewRow(); rCfg3["dblValues"] = ProblemCfg.tol; TblCfg.Rows.Add(rCfg3);
            DataRow rCfg4 = TblCfg.NewRow(); rCfg4["dblValues"] = ProblemCfg.maxPasses; TblCfg.Rows.Add(rCfg4);
            DataRow rCfg5 = TblCfg.NewRow(); rCfg5["dblValues"] = this.b; TblCfg.Rows.Add(rCfg5);
            DataRow rCfg6 = TblCfg.NewRow(); rCfg6["dblValues"] = this.ProblemCfg.lambda; TblCfg.Rows.Add(rCfg6);
            DataRow rCfg7 = TblCfg.NewRow(); rCfg7["dblValues"] = TrainingSet.trainingArray[0].xVector.Length; TblCfg.Rows.Add(rCfg7);

            d.WriteXml(FileName, XmlWriteMode.WriteSchema);
        }

        #endregion
    }
}
