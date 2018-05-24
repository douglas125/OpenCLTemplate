using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenCLTemplate.MachineLearning
{
    /// <summary>Multiple training SVM</summary>
    public class MultiClassSVM
    {
        /// <summary>List of possible classifications</summary>
        public List<float> Classifications;
        /// <summary>SVMs to perform each classification</summary>
        public List<SVM> SVMs;

        /// <summary>Creates a new multiclass SVM using desired outputs from training set. Classifications -1.0f are negative for all sets</summary>
        /// <param name="TSet">Training set</param>
        public MultiClassSVM(TrainingSet TSet)
        {
            ProblemConfig cfg = new ProblemConfig(2.529822E-8f * (float)Math.Sqrt(TSet.getN), 127.922182f, 1e-3f, 1, ProblemConfig.KernelType.RBF);
            initMultiSVM(TSet, cfg);
        }

        /// <summary>Creates a new multiclass SVM using desired outputs from training set. Classifications -1.0f are negative for all sets</summary>
        /// <param name="TSet">Training set</param>
        /// <param name="SVMCfg">Configuration parameters</param>
        public MultiClassSVM(TrainingSet TSet, ProblemConfig SVMCfg)
        {
            initMultiSVM(TSet, SVMCfg);
        }

        /// <summary>Creates a new multiclass SVM using desired outputs from training set. Classifications -1.0f are negative for all sets</summary>
        /// <param name="TSet">Training set</param>
        /// <param name="SVMCfg">Configuration parameters</param>
        private void initMultiSVM(TrainingSet TSet, ProblemConfig SVMCfg)
        {
            //Determines how many different classifications are there
            Classifications = new List<float>();
            foreach (TrainingUnit tu in TSet.trainingArray)
            {
                if (Classifications.IndexOf(tu.y) < 0 && tu.y != -1.0f) Classifications.Add(tu.y);
            }

            //For each different possible classification, create a different SVM
            SVMs = new List<SVM>();
            foreach (float c in Classifications)
            {
                SVM svm = new SVM();
                svm.TrainingSet = new TrainingSet();
                svm.ProblemCfg = SVMCfg.Clone();
                SVMs.Add(svm);

                foreach (TrainingUnit tu in TSet.trainingArray)
                {
                    TrainingUnit newTu = tu.Clone();
                    newTu.y = tu.y == c ? 1 : -1;
                    svm.TrainingSet.addTrainingUnit(newTu);
                }

                //Train svm
                svm.PreCalibrateCfg(0.8f / (float)Math.Sqrt(svm.TrainingSet.getN), 0.3f / (float)Math.Sqrt(svm.TrainingSet.getN));
                svm.Train();
                svm.RemoveNonSupportVectors();
            }
        }

        #region Training
        /// <summary>Trains all SVMs in this multiclass SVM</summary>
        public void Train()
        {
            Train(0.8f / (float)Math.Sqrt(SVMs[0].TrainingSet.getN), 0.3f / (float)Math.Sqrt(SVMs[0].TrainingSet.getN));
        }

        /// <summary>Trains all SVMs in this multiclass SVM precalibrating kernels</summary>
        /// <param name="tolPositive">Positive kernels average should be greater than tolPositive</param>
        /// <param name="tolNegative">Negative kernels average should be lesser than tolNegative</param>
        public void Train(float tolPositive, float tolNegative)
        {
            foreach (SVM svm in SVMs)
            {
                svm.PreCalibrateCfg(tolPositive, tolNegative);
                svm.Train();
                svm.RemoveNonSupportVectors();
            }
        }

        /// <summary>Cross validation parameters: [0] - maximum crossValidation value found so far, [1] - lambda, [2] - C.  Returns best performance so far</summary>
        private float[] CrossValParams;


        /// <summary>Trains current SVM with cross-validation, adjusting kernel parameter lambda and box parameter C.
        /// Returns best achieved efficiency.</summary>
        /// <param name="CrossValidationSet">Cross validation set</param>
        public float TrainWithCrossValidation(TrainingSet CrossValidationSet)
        {
            Random rnd = new Random();

            float[] lambdaSet = new float[12];
            //lambdaSet[0] = 3E-9f * ((float)rnd.NextDouble() + 1);
            lambdaSet[0] = 3E-3f * ((float)rnd.NextDouble() + 1);
            for (int i = 1; i < lambdaSet.Length; i++) lambdaSet[i] = 4.5f * lambdaSet[i - 1];

            float[] cSet = new float[13];
            cSet[0] = 1E-5f * ((float)rnd.NextDouble() + 1);
            for (int i = 1; i < cSet.Length; i++) cSet[i] = 2.0f * cSet[i - 1];

            return TrainWithCrossValidation(CrossValidationSet, lambdaSet, cSet);
        }

        /// <summary>Trains current SVM with cross-validation, adjusting kernel parameter lambda and box parameter C.
        /// Returns best achieved efficiency.</summary>
        /// <param name="CrossValidationSet">Cross validation set</param>
        /// <param name="LambdaSet">Lambda set</param>
        /// <param name="CSet">C values set</param>
        public float TrainWithCrossValidation(TrainingSet CrossValidationSet, float[] LambdaSet, float[] CSet)
        {
            foreach (float _lambda in LambdaSet)
            {
                for (int i = 0; i < SVMs.Count; i++) SVMs[i].ProblemCfg.lambda = _lambda;

                foreach (float _c in CSet)
                {
                    for (int i = 0; i < SVMs.Count; i++)
                    {
                        SVMs[i].ProblemCfg.c = _c;
                        SVMs[i].Train();
                    }

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

            //Train with best parameters
            for (int i = 0; i < SVMs.Count; i++)
            {
                SVMs[i].ProblemCfg.lambda = CrossValParams[1];
                SVMs[i].ProblemCfg.c = CrossValParams[2];
                SVMs[i].Train();
            }

            return CrossValParams[0];
        }

        /// <summary>Extracts a cross validation set from a given set</summary>
        /// <param name="Set">Set to extract cross validation from</param>
        /// <param name="CrossValidationSetPercent">Percentage of elements to extract</param>
        public static TrainingSet GetCrossValidationSet(TrainingSet Set, float CrossValidationSetPercent)
        {
            TrainingSet CrossValidationSet = new TrainingSet();
            int nCrossSet = (int)(CrossValidationSetPercent * (float)Set.getN);
            Random rnd = new Random();
            for (int i = 0; i < nCrossSet; i++)
            {
                int ind = rnd.Next(0, Set.trainingArray.Count - 1);
                TrainingUnit u = Set.trainingArray[ind];
                Set.trainingArray.Remove(u);
                CrossValidationSet.addTrainingUnit(u);
            }
            return CrossValidationSet;
        }


        #endregion

        #region Classification and hit rate
        /// <summary>Sample to classify</summary>
        private TrainingUnit sample;
        /// <summary>Returned classification values</summary>
        private float[] ClassificationValues;

        /// <summary>Attempts to classify a sample within a given category. Returns -1 if no classification was achieved.</summary>
        public float ClassifyWithRejection(TrainingUnit Sample)
        {
            float maxVal;
            float resp = Classify(Sample, out maxVal);
            if (maxVal >= 0) return resp;
            else return -1.0f;
        }

        /// <summary>Classifies a sample within a given category even if all SVMs predict it doesn`t belong to any.</summary>
        /// <param name="Sample">Sample to classify</param>
        /// <param name="maxVal">Maximum classification value found</param>
        public float Classify(TrainingUnit Sample, out float maxVal)
        {
            sample = Sample;
            if (ClassificationValues == null) ClassificationValues = new float[SVMs.Count];

            for (int i = 0; i < SVMs.Count; i++) Classify(i);

            //Finds maximum value
            maxVal = ClassificationValues[0];
            float classification = Classifications[0];
            for (int i = 1; i < ClassificationValues.Length; i++)
            {
                if (ClassificationValues[i] > maxVal)
                {
                    maxVal = ClassificationValues[i];
                    classification = Classifications[i];
                }
            }

            return classification;
        }

        /// <summary>Classifies a given set of Samples (image2d of floats) each one in a category. Each row of the image is a sample
        /// to be classified and the features should be stored in the columns. The number of columns Ncol = Nfeatures/4 since 
        /// each pixel holds 4 floats</summary>
        /// <param name="Samples">Image2D containing samples to be classified</param>
        /// <param name="maxVals">Maximum values found</param>
        /// <returns></returns>
        public float[] Classify(CLCalc.Program.Image2D Samples, out float[] maxVals)
        {
            maxVals = SVM.MultiClassify(SVMs[0], Samples);
            float[] classif = new float[maxVals.Length];
            for (int j = 0; j < maxVals.Length; j++) classif[j] = Classifications[0];

            for (int i = 1; i < SVMs.Count; i++)
            {
                float[] m = SVM.MultiClassify(SVMs[i], Samples);
                for (int j = 0; j < maxVals.Length; j++)
                {
                    if (m[j] > maxVals[j])
                    {
                        maxVals[j] = m[j];
                        classif[j] = Classifications[i];
                    }
                }
            }

            return classif;
        }

        /// <summary>Classifies a sample using i-th svm</summary>
        /// <param name="SVMInd">(int) Index of svm to use</param>
        private void Classify(object SVMInd)
        {
            int i = (int)SVMInd;
            ClassificationValues[i] = SVMs[i].ClassificationValue(this.sample);
        }

        /// <summary>Gets SVM hit rate</summary>
        /// <param name="TestSet">Test set</param>
        public float GetHitRate(TrainingSet TestSet)
        {
            float rate = 0, val;
            foreach (TrainingUnit tu in TestSet.trainingArray)
            {
                float resp = Classify(tu, out val);
                if (resp == tu.y) rate++;
            }

            return rate / (float)TestSet.trainingArray.Count;
        }

        /// <summary>Gets average internal hit rate</summary>
        public float GetInternalHitRate()
        {
            float r = 0;
            foreach (SVM svm in SVMs)
                r += svm.GetTrainingSetHitRate();

            return r / (float)SVMs.Count;
        }

        #endregion
    }
}
