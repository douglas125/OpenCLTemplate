using System;
using System.Collections.Generic;
using System.Text;

namespace OpenCLTemplate.MachineLearning
{

    public partial class SVM
    {

        /// <summary>
        /// This class aggregates all methods for solving SMO problems
        /// </summary>
        private static class ProblemSolver
        {
            /// <summary>
            /// Minimal alpha variation to optimize
            /// </summary>
            private const float MIN_ALPHA_CHANGE = 10e-5f;

            /// <summary>
            /// Solves the SMO considering no previous knowledge about the problem
            /// </summary>
            /// <param name="problemSolution">Solution of the problem</param>
            /// <returns>Solution of the problem with alphas and threshold</returns>
            public static SVM solveSMOStartingFromZero(SVM problemSolution)
            {
                problemSolution.initializeWithZeros();

                // Solve it
                return solveSMOStartingFromPreviousSolution(problemSolution);
            }

            /// <summary>
            /// Solves the SMO considering no previous knowledge about the problem
            /// </summary>
            /// <param name="problemSolution">Known solution</param>
            /// <returns>Solution of the problem with alphas and threshold</returns>
            public static SVM solveSMOStartingFromPreviousSolution(SVM problemSolution)
            {
                System.Diagnostics.Stopwatch swTotalTime = new System.Diagnostics.Stopwatch();
                System.Diagnostics.Stopwatch swHeuristica = new System.Diagnostics.Stopwatch();
                System.Diagnostics.Stopwatch swComputeKernel = new System.Diagnostics.Stopwatch();
                System.Diagnostics.Stopwatch swUpdateError = new System.Diagnostics.Stopwatch();
                swTotalTime.Start();
                
                
                ProblemConfig problemConfig = problemSolution.ProblemCfg;
                if (problemSolution.alphaList == null) problemSolution.initializeWithZeros();
                ProblemSolver.calculateErrors(problemSolution);

                //Initializes GPU error vector
                if (OpenCLTemplate.CLCalc.CLAcceleration == OpenCLTemplate.CLCalc.CLAccelerationType.UsingCL)
                    WriteCLErr(problemSolution);

                TrainingSet trainingSet = problemSolution.TrainingSet;

                int passes = 0;
                int m = trainingSet.getN;

                while (passes < problemConfig.maxPasses)
                {

                    int changedAlphas = 0;
                    for (int i = 0; i < m; i++)
                    {


                        float yi = trainingSet.trainingArray[i].y;
                        float alpha_i = problemSolution.alphaList[i];
                        // Error between the SVM output on the ith training unit and the true ith output
                        float ei = trainingSet.errors[i];

                        // KKT conditions for ith element
                        if (
                            ((yi * ei < -problemConfig.tol && alpha_i < problemConfig.c) || (yi * ei > problemConfig.tol && alpha_i > 0))
                            )
                        {
                            swHeuristica.Start();

                            #region Computes J using maximum variation heuristics
                            // Get a number from 0 to m - 1 not equal to i
                            int j = 0;
                            if (trainingSet.errors[i] >= 0)
                            {
                                if (OpenCLTemplate.CLCalc.CLAcceleration == OpenCLTemplate.CLCalc.CLAccelerationType.UsingCL)
                                {
                                    j = CLFindMinError(problemSolution);
                                }
                                else
                                {
                                    float minError = trainingSet.errors[0];
                                    for (int k = 1; k < trainingSet.getN; k++)
                                    {
                                        if (minError > trainingSet.errors[k])
                                        {
                                            minError = trainingSet.errors[k];
                                            j = k;
                                        }
                                    }
                                }

                            }
                            else
                            {
                                if (OpenCLTemplate.CLCalc.CLAcceleration == OpenCLTemplate.CLCalc.CLAccelerationType.UsingCL)
                                {
                                    j = CLFindMaxError(problemSolution);
                                }
                                else
                                {
                                    float maxError = trainingSet.errors[0];
                                    for (int k = 1; k < trainingSet.getN; k++)
                                    {
                                        if (maxError < trainingSet.errors[k])
                                        {
                                            maxError = trainingSet.errors[k];
                                            j = k;
                                        }
                                    }
                                }
                            }
                            #endregion

                            swHeuristica.Stop();

                            float yj = trainingSet.trainingArray[j].y;
                            float alpha_j = problemSolution.alphaList[j];
                            // Error between the SVM output on the jth training unit and the true jth output
                            float ej = trainingSet.errors[j];

                            // Save old alphas
                            float oldAlpha_i = problemSolution.alphaList[i];
                            float oldAlpha_j = problemSolution.alphaList[j];

                            #region Compute lower and higher bounds of alpha_j
                            float lowerBound;
                            float higherBound;
                            if (yi != yj)
                            {
                                lowerBound = Math.Max(0, alpha_j - alpha_i);
                                higherBound = Math.Min(problemConfig.c, problemConfig.c + alpha_j - alpha_i);
                            }
                            else
                            {
                                lowerBound = Math.Max(0, alpha_j + alpha_i - problemConfig.c);
                                higherBound = Math.Min(problemConfig.c, alpha_j + alpha_i);
                            }
                            #endregion

                            // Nothing to adjust if we can't set any value between those bounds
                            if (lowerBound == higherBound) continue;


                            #region Compute eta
                            float kernel_xi_xj;
                            float kernel_xi_xi;
                            float kernel_xj_xj;

                            if (trainingSet.IsKernelCalculated[i])
                                kernel_xi_xj = trainingSet.kernels[i][j];
                            else if (trainingSet.IsKernelCalculated[j])
                                kernel_xi_xj = trainingSet.kernels[j][i];
                            else kernel_xi_xj = calculateSingleKernel(trainingSet.trainingArray[i], trainingSet.trainingArray[j], problemSolution);//trainingSet.kernels[i][j];

                            if (trainingSet.IsKernelCalculated[i])
                                kernel_xi_xi = trainingSet.kernels[i][i];
                            else kernel_xi_xi = calculateSingleKernel(trainingSet.trainingArray[i], trainingSet.trainingArray[i], problemSolution);//trainingSet.kernels[i][i];

                            if (trainingSet.IsKernelCalculated[j])
                                kernel_xj_xj = trainingSet.kernels[j][j];
                            else kernel_xj_xj = calculateSingleKernel(trainingSet.trainingArray[j], trainingSet.trainingArray[j], problemSolution);//trainingSet.kernels[j][j];


                            float eta = 2 * kernel_xi_xj - kernel_xi_xi - kernel_xj_xj;
                            #endregion
                            if (eta >= 0) continue;

                            // Compute new alpha_j
                            alpha_j = alpha_j - yj * (ei - ej) / eta;
                            // Clip alpha_j if necessary
                            if (alpha_j > higherBound) alpha_j = higherBound;
                            else if (alpha_j < lowerBound) alpha_j = lowerBound;

                            // If the changes are not big enough, just continue
                            if (Math.Abs(oldAlpha_j - alpha_j) < MIN_ALPHA_CHANGE) continue;

                            swComputeKernel.Start();
                            //Needs to compute lines K[i][] and K[j][] since the alphas will change
                            if (OpenCLTemplate.CLCalc.CLAcceleration == OpenCLTemplate.CLCalc.CLAccelerationType.UsingCL)
                            {
                                CLComputeKernels(problemSolution, i);
                                CLComputeKernels(problemSolution, j);
                            }
                            else
                            {
                                ComputeKernels(problemSolution, i);
                                ComputeKernels(problemSolution, j);
                            }
                            swComputeKernel.Stop();


                            // Compute value for alpha_i
                            alpha_i = alpha_i + yi * yj * (oldAlpha_j - alpha_j);

                            // Compute b1, b2 and new b (threshold)
                            float oldB = problemSolution.b;
                            if (0 < alpha_i && alpha_i < problemConfig.c)
                            {
                                // b1 is enough in this case
                                float b1 = problemSolution.b - ei - yi * (alpha_i - oldAlpha_i) * kernel_xi_xi - yj * (alpha_j - oldAlpha_j) * kernel_xi_xj;
                                problemSolution.b = b1;
                            }
                            else if (0 < alpha_j && alpha_j < problemConfig.c)
                            {
                                // b2 is enough in this case
                                float b2 = problemSolution.b - ej - yi * (alpha_i - oldAlpha_i) * kernel_xi_xj - yj * (alpha_j - oldAlpha_j) * kernel_xj_xj;
                                problemSolution.b = b2;
                            }
                            else
                            {
                                // b is the average between b1 and b2
                                float b1 = problemSolution.b - ei - yi * (alpha_i - oldAlpha_i) * kernel_xi_xi - yj * (alpha_j - oldAlpha_j) * kernel_xi_xj;
                                float b2 = problemSolution.b - ej - yi * (alpha_i - oldAlpha_i) * kernel_xi_xj - yj * (alpha_j - oldAlpha_j) * kernel_xj_xj;
                                problemSolution.b = (b1 + b2) * 0.5f;
                            }

                            // Update the changed alphas in the solution
                            problemSolution.alphaList[i] = alpha_i;
                            problemSolution.alphaList[j] = alpha_j;

                            // Update errors cache
                            swUpdateError.Start();
                            if (OpenCLTemplate.CLCalc.CLAcceleration == OpenCLTemplate.CLCalc.CLAccelerationType.UsingCL)
                                CLupdateErrorsCache(trainingSet, problemSolution, oldAlpha_i, alpha_i, i, oldAlpha_j, alpha_j, j, oldB, problemSolution.b);
                            else
                                updateErrorsCache(trainingSet, problemSolution, oldAlpha_i, alpha_i, i, oldAlpha_j, alpha_j, j, oldB, problemSolution.b);

                            swUpdateError.Stop();

                            changedAlphas++;


                        }
                    }
                    if (changedAlphas == 0) 
                    { passes++; }
                    else 
                    { passes = 0; }
                }

                return problemSolution;
            }

            /// <summary>
            /// Predicts the output of a single entry, given a previous problem, solution and correspondent training set
            /// </summary>
            /// <param name="problemSolution">Correspondent problem solution</param>
            /// <param name="untrainedUnit">Input features from which the output will be predicted</param>
            /// <returns>The y classification (true/false = positive/negative)</returns>
            public static float predictOutput(SVM problemSolution, TrainingUnit untrainedUnit)
            {
                TrainingSet trainingSet = problemSolution.TrainingSet;
                ProblemConfig problemConfig = problemSolution.ProblemCfg;

                // F(x) = sum + b
                // sum = summation of alpha_i * y_i * kernel(untrained unit, i) for all i in the training set
                float sum = 0;
                for (int i = 0; i < trainingSet.getN; i++)
                {
                    if (trainingSet.trainingArray[i].y > 0)
                        sum += problemSolution.alphaList[i] * calculateSingleKernel(trainingSet.trainingArray[i], untrainedUnit, problemSolution);
                    else
                        sum -= problemSolution.alphaList[i] * calculateSingleKernel(trainingSet.trainingArray[i], untrainedUnit, problemSolution);
                }

                return sum + problemSolution.b;
            }

            private static float calculateFx(int indexX, SVM currentSolution)
            {
                TrainingSet trainingSet = currentSolution.TrainingSet;
                ProblemConfig problemConfig = currentSolution.ProblemCfg;

                float sum = 0;
                for (int i = 0; i < trainingSet.getN; i++)
                {
                    if (trainingSet.trainingArray[i].y > 0)
                        sum += currentSolution.alphaList[i] * trainingSet.kernels[i][indexX];
                    else
                        sum -= currentSolution.alphaList[i] * trainingSet.kernels[i][indexX];
                }
                return sum + currentSolution.b;
            }

            public static void calculateAllKernels(SVM problemSolution)
            {
                TrainingSet trainingSet = problemSolution.TrainingSet;
                ProblemConfig problemConfig = problemSolution.ProblemCfg;


                trainingSet.errors = new float[trainingSet.getN];
                trainingSet.kernels = new float[trainingSet.getN][];
                for (int i = 0; i < trainingSet.getN; i++)
                    trainingSet.kernels[i] = new float[trainingSet.getN];

                trainingSet.IsKernelCalculated = new bool[trainingSet.getN];

                // Caching kernels
                for (int i = 0; i < trainingSet.getN; i++)
                {
                    if (problemSolution.alphaList[i] != 0)
                    {
                        trainingSet.IsKernelCalculated[i] = true;

                        for (int j = i; j < trainingSet.getN; j++)
                        {
                            trainingSet.kernels[i][j] = calculateSingleKernel(trainingSet.trainingArray[i], trainingSet.trainingArray[j], problemSolution);

                            if (j != i)
                                trainingSet.kernels[j][i] = trainingSet.kernels[i][j];
                        }
                    }
                }
            }

            public static void calculateErrors(SVM problemSolution)
            {
                // Caching errors
                for (int i = 0; i < problemSolution.TrainingSet.getN; i++)
                {
                    //problemSolution.TrainingSet.errors[i] = calculateFx(i, problemSolution) - problemSolution.TrainingSet.trainingArray[i].y;
                    problemSolution.TrainingSet.errors[i] = - problemSolution.TrainingSet.trainingArray[i].y;

                    if (problemSolution.alphaList[i] != 0)
                    {
                        updateSingleError(problemSolution.TrainingSet, problemSolution, problemSolution.alphaList[i], i);
                    }
                }
            }

            /// <summary>Computes the i-th line of matrix K[i][j]</summary>
            /// <param name="problemSolution">SVM to solve</param>
            /// <param name="i">Kernel line number to compute</param>
            private static void ComputeKernels(SVM problemSolution, int i)
            {
                if (problemSolution.TrainingSet.IsKernelCalculated[i]) return;
                TrainingSet trainingSet = problemSolution.TrainingSet;
                problemSolution.TrainingSet.kernels[i] = new float[problemSolution.TrainingSet.getN];

                trainingSet.kernels[i] = new float[trainingSet.getN];
                problemSolution.TrainingSet.IsKernelCalculated[i] = true;

                for (int j = 0; j < trainingSet.getN; j++)
                {
                    trainingSet.kernels[i][j] = calculateSingleKernel(trainingSet.trainingArray[i], trainingSet.trainingArray[j], problemSolution);
                    //trainingSet.kernels[j][i] = trainingSet.kernels[i][j];
                }
            }

            public static float calculateSingleKernel(TrainingUnit xi, TrainingUnit xj,SVM ProblemSolution)
            {
                ProblemConfig problemConfig = ProblemSolution.ProblemCfg;

                // Vectors size check
                //if (xi.getDimension() != xj.getDimension()) return 0;
                // Linear: u'*v (inner product)
                if (problemConfig.kernelType == ProblemConfig.KernelType.Linear)
                {
                    float sum = 0;
                    for (int i = 0; i < xi.getDimension(); i++)
                    {
                        sum += xi.xVector[i] * xj.xVector[i];
                    }
                    return sum;
                }
                // Radial basis function: exp(-gamma*|u-v|^2)
                if (problemConfig.kernelType == ProblemConfig.KernelType.RBF)
                {
                    // Gamma is, by choice, 1 / (number of features).
                    float sum = 0, temp;
                    for (int i = 0; i < xi.getDimension(); i++)
                    {
                        temp = xi.xVector[i] - xj.xVector[i];
                        sum += temp * temp;
                    }
                    return  (float)Math.Exp(-ProblemSolution.ProblemCfg.lambda * sum);
                }
                return 0;
            }

            private static void updateErrorsCache(TrainingSet trainingSet, SVM currentSolution,
                float oldAlphai, float newAlphai, int iIndex,
                float oldAlphaj, float newAlphaj, int jIndex,
                float oldB, float newB)
            {
                float alphaiDif = newAlphai - oldAlphai;
                float alphajDif = newAlphaj - oldAlphaj;
                float BDif = newB - oldB;

                if (trainingSet.trainingArray[iIndex].y < 0) alphaiDif = -alphaiDif;
                if (trainingSet.trainingArray[jIndex].y < 0) alphajDif = -alphajDif;

                for (int t = 0; t < trainingSet.getN; t++)
                {
                    float variation = alphaiDif * trainingSet.kernels[iIndex][t];
                    variation += alphajDif * trainingSet.kernels[jIndex][t];
                    variation += BDif;

                    trainingSet.errors[t] += variation;
                }
            }

            private static void updateSingleError(TrainingSet trainingSet, SVM currentSolution,
                    float newAlphai, int iIndex)
            {
                for (int t = 0; t < trainingSet.getN; t++)
                {
                    float variation = 0;
                    if (trainingSet.trainingArray[iIndex].y > 0)
                    {
                        variation += newAlphai * trainingSet.kernels[iIndex][t];
                    }
                    else
                    {
                        variation -= newAlphai * trainingSet.kernels[iIndex][t];
                    }
                    trainingSet.errors[t] += variation;
                }
            }
        }
    }
}
