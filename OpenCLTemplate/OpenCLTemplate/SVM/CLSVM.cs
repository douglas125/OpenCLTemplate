using System;
using System.Collections.Generic;
using OpenCLTemplate;
using System.Text;

namespace OpenCLTemplate.MachineLearning
{
    //Acceleration of the algorithm using OpenCL functions



    public partial class SVM
    {

        #region OpenCL kernels, static, OpenCL source

        /// <summary>Semaphor to ensure thread safety within OpenCL</summary>
        private static int[] CLResource;

        /// <summary>Compiles code and initializes kernel for this svm stance</summary>
        private void CLSVMInit()
        {
            if (CLResource == null) CLResource = new int[0];

            lock (CLResource)
            {
                if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.Unknown) CLCalc.InitCL();
                if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.UsingCL)
                {
                    if (kernelComputeKernelRBF == null)
                    {
                        CLSVMSrc s = new CLSVMSrc();
                        CLCalc.Program.Compile(new string[] { s.srcKernels, s.srcFindMaxMinErr, s.srcMultClass });

                        //Kernel computation
                        kernelComputeKernelRBF = new CLCalc.Program.Kernel("ComputeKernelRBF");



                        kernelMaxErr = new CLCalc.Program.Kernel("maxErr");
                        kernelComputeMax = new CLCalc.Program.Kernel("computeMax");
                        kernelMinErr = new CLCalc.Program.Kernel("minErr");
                        kernelComputeMin = new CLCalc.Program.Kernel("computeMin");
                        kernelGetResp = new CLCalc.Program.Kernel("getResp");

                        //Update error
                        kernelUpdateErr = new CLCalc.Program.Kernel("UpdateErr");

                        //Multiple classification
                        kernelComputeMultiKernelRBF = new CLCalc.Program.Kernel("ComputeMultiKernelRBF");
                        kernelSumKernels=new CLCalc.Program.Kernel("SumKernels");
                    }

                    //Memory obbjects

                    //Find max/min
                    CLErrLen = new CLCalc.Program.Variable(new int[1]);
                    HostResp = new int[1];
                    CLResp = new CLCalc.Program.Variable(HostResp);
                    CLMaxMinErrs = new CLCalc.Program.Variable(new float[MAXMINWORKSIZE]);
                    CLMaxMinInds = new CLCalc.Program.Variable(new int[MAXMINWORKSIZE]);
                    //Update error
                    CLUpdtErrParams = new CLCalc.Program.Variable(new float[3]);
                }
            }
        }

        #region OpenCL Source

        private class CLSVMSrc
        {
            #region Compute kernels
            public string srcKernels = @"
//GlobalWorkSize = TrainingSet.getN

__kernel void ComputeKernelRBF(__global read_only  float *   TrainingF,
                               __global read_only  int   *   XLength,
                               __read_only         image2d_t Sample,
                               __global write_only float *   KernelValues,
                               __global read_only  float *   lambda)

{
  const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
    CLK_ADDRESS_CLAMP | //Clamp to zeros
    CLK_FILTER_NEAREST; //Don't interpolate
    
   int i = get_global_id(0);
   int XLen = XLength[0];
   int XLenBy4 = XLen >> 2;
   
   float4 sum = (float4)(0,0,0,0);
   float4 temp;
   int2 coords = (int2)(0,0);
   int j4 = XLen*i;
   
   for (int j=0; j<XLenBy4; j++)
   {
      coords.x = j;
      
      temp = read_imagef(Sample, smp, coords) - (float4)(TrainingF[j4], TrainingF[j4+1], TrainingF[j4+2], TrainingF[j4+3]);
      sum = mad(temp,temp,sum);

      j4 += 4;
   }
   
   KernelValues[i] = native_exp(-lambda[0]*(sum.x+sum.y+sum.z+sum.w));
}

__kernel void ComputeKernelRBFMcEd(    __global read_only  float *   TrainingF,
                                       __global read_only  int   *   XLength,
                                       __read_only         image2d_t Sample,
                                       __global write_only float *   KernelValues,
                                       __global read_only  float *   lambda)

{
  const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
    CLK_ADDRESS_CLAMP | //Clamp to zeros
    CLK_FILTER_NEAREST; //Don't interpolate
    
   int i = get_global_id(0);
   int XLen = XLength[0];
   int XLenBy4 = XLen >> 2;
   
   float4 sum = (float4)(0,0,0,0);
   float4 temp;
   int2 coords = (int2)(0,0);
   int j4 = XLen*i;
   
   for (int j=0; j<XLenBy4; j++)
   {
      coords.x = j;
      
      temp = read_imagef(Sample, smp, coords) - (float4)(TrainingF[j4], TrainingF[j4+1], TrainingF[j4+2], TrainingF[j4+3]);
      sum += native_exp(-lambda[0]*temp*temp);

      j4 += 4;
   }
   
   KernelValues[i] = sum.x+sum.y+sum.z+sum.w;
}
";
            #endregion

            #region Find max/min errors, update error
            public string srcFindMaxMinErr = @"
__kernel void maxErr (__global read_only float * Errors,
                      __global read_only int   * ErrVecLen,
                      __global float *           maxErrs,
                      __global int *             indMaxErr)
{
  //Vector length divided by 4 (plus 1)
  //Gets initial and final 'pixel' of image to read
  int id = get_global_id(0);
  int n = get_global_size(0);

  int vLen = ErrVecLen[0];
  int ivLen = vLen*id;
  
  //Each worker has to compute vLen/n maximums
  int ind0 = (ivLen)/n;
  int indf = (ivLen+vLen)/n;
  
  float err;
  float maxVal = Errors[ind0];
  int indMax = ind0;
  for (int i = ind0+1; i < indf; i++)
  {
     err = Errors[i];
     if (maxVal < err)
     {
        maxVal = err;
        indMax = i;
     }
  }  
  
  maxErrs[id] = maxVal;
  indMaxErr[id] = indMax;
  
}

//Needs log(n) calls
__kernel void computeMax(__global float * maxErrs,
                         __global int   * indMaxErr)
{
  int i = get_global_id(0);
  int n = get_global_size(0);
  float val = maxErrs[i+n];
  if (maxErrs[i] < val)
  {
     maxErrs[i] = val;
     indMaxErr[i] = indMaxErr[i+n];
  }
}

__kernel void minErr (__global read_only float * Errors,
                      __global read_only int   * ErrVecLen,
                      __global float *           minErrs,
                      __global int *             indMinErr)
{
  //Vector length divided by 4 (plus 1)
  //Gets initial and final 'pixel' of image to read
  int id = get_global_id(0);
  int n = get_global_size(0);

  int vLen = ErrVecLen[0];
  int ivLen = vLen*id;
  
  //Each worker has to compute vLen/n maximums
  int ind0 = (ivLen)/n;
  int indf = (ivLen+vLen)/n;
  
  float err;
  float minVal = Errors[ind0];
  int indMin = ind0;
  for (int i = ind0+1; i < indf; i++)
  {
     err = Errors[i];
     if (minVal > err)
     {
        minVal = err;
        indMin = i;
     }
  }  
  
  minErrs[id] = minVal;
  indMinErr[id] = indMin;
  
}

//Needs log(n) calls
__kernel void computeMin(__global float * minErrs,
                         __global int   * indMinErr)
{
  int i = get_global_id(0);
  int n = get_global_size(0);
  float val = minErrs[i+n];
  if (minErrs[i] > val)
  {
     minErrs[i] = val;
     indMinErr[i] = indMinErr[i+n];
  }
}

__kernel void getResp(__global int * indErr,
                      __global int * resp)

{
   resp[0]=indErr[0];
}

__kernel void getErr(__global float * Errors,
                     __global int   * ind,
                     __global float * ei)
{
   ei[0]=Errors[ind[0]];
}

//GlobalWorkSize = trainingSet.getN
__kernel void UpdateErr (__global           float * Errors,
                         __global read_only float * Ki,
                         __global read_only float * Kj,
                         __global read_only float * params)
{
  //params = float[3] {alphaiDif, BDif, alphajDif}

  //Vector length divided by 4 (plus 1)
  //Gets initial and final 'pixel' of image to read
  int t = get_global_id(0);

  float variation = mad(params[0], Ki[t], params[1]);
  variation = mad(params[2], Kj[t], variation);

  Errors[t] += variation;

}


";
            #endregion

            #region Multiple classification

            public string srcMultClass = @"

__kernel void ComputeMultiKernelRBF(__global read_only  float4 *   TrainingF,
                                    __global read_only  int    * qtdSupVecs,
                                    __global read_only  int    *   XLength,
                                    __read_only         image2d_t Sample,
                                    __global write_only float  *   KernelValues, 
                                    __global read_only  float  *   lambda)

{
  const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
    CLK_ADDRESS_CLAMP | //Clamp to zeros
    CLK_FILTER_NEAREST; //Don't interpolate
    
   int nSample = get_global_id(1); 
    
   int i = get_global_id(0);
   int XLen = XLength[0];
   int XLenBy4 = XLen >> 2;
   
   float4 sum = (float4)(0,0,0,0);
   float4 temp;
   int2 coords = (int2)(0,nSample);
   
   int jOff = XLenBy4*i;

   while(coords.x <= XLenBy4-5)
   {
      temp = read_imagef(Sample, smp, coords) - TrainingF[coords.x+jOff];
      sum = mad(temp, temp, sum);
      coords.x++;

      temp = read_imagef(Sample, smp, coords) - TrainingF[coords.x+jOff];
      sum = mad(temp, temp, sum);
      coords.x++;

      temp = read_imagef(Sample, smp, coords) - TrainingF[coords.x+jOff];
      sum = mad(temp, temp, sum);
      coords.x++;

      temp = read_imagef(Sample, smp, coords) - TrainingF[coords.x+jOff];
      sum = mad(temp, temp, sum);
      coords.x++;

      temp = read_imagef(Sample, smp, coords) - TrainingF[coords.x+jOff];
      sum = mad(temp, temp, sum);
      coords.x++;
   }

   int cMod = XLenBy4-coords.x;

      if (cMod <=2)
      {
         if (cMod==1)
         {
            temp = read_imagef(Sample, smp, coords) - TrainingF[coords.x+jOff];
            sum = mad(temp, temp, sum);
         }
         else
         {
            temp = read_imagef(Sample, smp, coords) - TrainingF[coords.x+jOff];
            sum = mad(temp, temp, sum);
            coords.x++;
            temp = read_imagef(Sample, smp, coords) - TrainingF[coords.x+jOff];
            sum = mad(temp, temp, sum);
         }
      }
      else
      {
         if (cMod==3)
         {
            temp = read_imagef(Sample, smp, coords) - TrainingF[coords.x+jOff];
            sum = mad(temp, temp, sum);
            coords.x++;
            temp = read_imagef(Sample, smp, coords) - TrainingF[coords.x+jOff];
            sum = mad(temp, temp, sum);
            coords.x++;
            temp = read_imagef(Sample, smp, coords) - TrainingF[coords.x+jOff];
            sum = mad(temp, temp, sum);
         }
         else
         {
            temp = read_imagef(Sample, smp, coords) - TrainingF[coords.x+jOff];
            sum = mad(temp, temp, sum);
            coords.x++;
            temp = read_imagef(Sample, smp, coords) - TrainingF[coords.x+jOff];
            sum = mad(temp, temp, sum);
            coords.x++;
            temp = read_imagef(Sample, smp, coords) - TrainingF[coords.x+jOff];
            sum = mad(temp, temp, sum);
            coords.x++;
            temp = read_imagef(Sample, smp, coords) - TrainingF[coords.x+jOff];
            sum = mad(temp, temp, sum);
         }
      }


//   if (cMod==1)
//   {
//      temp = read_imagef(Sample, smp, coords) - TrainingF[coords.x+jOff];
//      sum = mad(temp, temp, sum);
//   }
//   else if (cMod==2)
//   {
//      temp = read_imagef(Sample, smp, coords) - TrainingF[coords.x+jOff];
//      sum = mad(temp, temp, sum);
//      coords.x++;
//      temp = read_imagef(Sample, smp, coords) - TrainingF[coords.x+jOff];
//      sum = mad(temp, temp, sum);
//   }
//   else if (cMod==3)
//   {
//      temp = read_imagef(Sample, smp, coords) - TrainingF[coords.x+jOff];
//      sum = mad(temp, temp, sum);
//      coords.x++;
//      temp = read_imagef(Sample, smp, coords) - TrainingF[coords.x+jOff];
//      sum = mad(temp, temp, sum);
//      coords.x++;
//      temp = read_imagef(Sample, smp, coords) - TrainingF[coords.x+jOff];
//      sum = mad(temp, temp, sum);
//   }
   
//   for (int j=0; j<XLenBy4; j++)
//   {
//      coords.x = j;
//      
//      temp = read_imagef(Sample, smp, coords) - TrainingF[j+jOff];
//      //sum += temp*temp;
//      sum = mad(temp, temp, sum);
//   }


   
   KernelValues[i+nSample*qtdSupVecs[0]] = native_exp(-lambda[0]*(sum.x+sum.y+sum.z+sum.w));
}

__kernel void SumKernels(__global read_only  float * alpha,
                         __global read_only  int   * qtdSupVecs,
                         __global read_only  int   * XLength,                         
                         __global read_only  float * y,
                         __global read_only  float * KernelValues,
                         __global read_only  float * bb,
                         __global write_only float * Sums)
{
   int nVecs = qtdSupVecs[0];
   int XLen = XLength[0];
   int nSample = get_global_id(0);
   float sum = 0;
   for (int j = 0; j < nVecs; j++)
   {
//      if (y[j] > 0) sum = mad(KernelValues[j+nSample*nVecs], alpha[j], sum);
//      else          sum = mad(KernelValues[j+nSample*nVecs],-alpha[j], sum);

      sum = y[j] > 0 ? mad(KernelValues[j+nSample*nVecs], alpha[j], sum) : mad(KernelValues[j+nSample*nVecs],-alpha[j], sum);
   }
   Sums[nSample] = sum + bb[0];
}
";

            #endregion
        }

        #endregion

        #endregion

        #region Kernel and variable errors computation

        #region Kernel computation variables and opencl kernels
        /// <summary>Computes RBF kernel</summary>
        private static CLCalc.Program.Kernel kernelComputeKernelRBF;

        /// <summary>OpenCL training features matrix</summary>
        private CLCalc.Program.Variable CLTrainingFeatures;
        /// <summary>Host memory training features</summary>
        float[] TrainingFeatures;

        /// <summary>Length of each training feature</summary>
        private CLCalc.Program.Variable CLXVecLen;
        /// <summary>Length of each training feature in host memory</summary>
        private int[] HostVLen;

        /// <summary>OpenCL feature sample</summary>
        private CLCalc.Program.Image2D CLSample;
        /// <summary>OpenCL feature sample in Host Memory</summary>
        private float[] HostSample;
        /// <summary>Computed kernel values</summary>
        private CLCalc.Program.Variable CLKernelValues;
        /// <summary>Kernel lambda configuration parameter</summary>
        private CLCalc.Program.Variable CLLambda;

        /// <summary>Writes Training Set into device memory</summary>
        private void WriteToDevice()
        {
            //Vector length
            if (HostVLen == null)
            {
                HostVLen = new int[1];
                HostVLen[0] = (1 + ((TrainingSet.trainingArray[0].xVector.Length - 1) >> 2)) << 2;
            }

            //Populates OpenCL buffer
            if (TrainingFeatures == null) // || TrainingFeatures.Length != TrainingSet.getN * HostVLen[0])
            {
                TrainingFeatures = new float[TrainingSet.getN * HostVLen[0]];
            }

            for (int i = 0; i < TrainingSet.getN; i++)
            {
                for (int j = 0; j < TrainingSet.trainingArray[0].xVector.Length; j++)
                {
                    TrainingFeatures[j + i * HostVLen[0]] = TrainingSet.trainingArray[i].xVector[j];
                }
            }

            if (CLCalc.CLAcceleration != CLCalc.CLAccelerationType.UsingCL) return;
            lock (CLResource)
            {
                //Writes to OpenCL memory
                //if (CLTrainingFeatures != null) CLTrainingFeatures.Dispose();
                if (CLTrainingFeatures == null || CLTrainingFeatures.OriginalVarLength != TrainingFeatures.Length)
                {
                    if (CLTrainingFeatures != null) 
                        CLTrainingFeatures.Dispose();
                    CLTrainingFeatures = new CLCalc.Program.Variable(TrainingFeatures);
                }
                else CLTrainingFeatures.WriteToDevice(TrainingFeatures);

                //if (CLXVecLen != null) CLXVecLen.Dispose();
                if (CLXVecLen == null) CLXVecLen = new CLCalc.Program.Variable(HostVLen);
                else CLXVecLen.WriteToDevice(HostVLen);

                HostSample = new float[HostVLen[0]];
                //if (CLSample != null) CLSample.Dispose();
                if (CLSample == null) CLSample = new CLCalc.Program.Image2D(HostSample, HostVLen[0] >> 2, 1);
                else CLSample.WriteToDevice(HostSample);

                //if (CLKernelValues != null) CLKernelValues.Dispose();
                if (CLKernelValues == null || CLKernelValues.OriginalVarLength != TrainingSet.getN) CLKernelValues = new CLCalc.Program.Variable(new float[TrainingSet.getN]);
                else CLKernelValues.WriteToDevice(new float[TrainingSet.getN]);

                //if (CLLambda != null) CLLambda.Dispose();
                if (CLLambda == null)
                    CLLambda = new CLCalc.Program.Variable(new float[] { this.ProblemCfg.lambda });
                else CLLambda.WriteToDevice(new float[] { this.ProblemCfg.lambda });
            }
        }
        #endregion


        /// <summary>Computes All kernels and errors accelerating with OpenCL</summary>
        /// <param name="problemSolution">Problem solution SVM</param>
        public static void CLcalculateAllKernels(SVM problemSolution)
        {
            TrainingSet trainingSet = problemSolution.TrainingSet;
            ProblemConfig problemConfig = problemSolution.ProblemCfg;


            trainingSet.errors = new float[trainingSet.getN];
            trainingSet.kernels = new float[trainingSet.getN][];
            trainingSet.IsKernelCalculated = new bool[trainingSet.getN];

            // Caching kernels
            for (int i = 0; i < trainingSet.getN; i++)
            {
                if (problemSolution.alphaList[i] != 0)
                {
                    CLComputeKernels(problemSolution, i);
                }
            }
        }

        /// <summary>Computes the i-th line of matrix K[i][j]</summary>
        /// <param name="problemSolution">SVM to solve</param>
        /// <param name="i">Kernel line number to compute</param>
        private static void CLComputeKernels(SVM problemSolution, int i)
        {
            if (problemSolution.TrainingSet.IsKernelCalculated[i]) return;
            problemSolution.TrainingSet.kernels[i] = new float[problemSolution.TrainingSet.getN];

            TrainingSet trainingSet = problemSolution.TrainingSet;

            trainingSet.IsKernelCalculated[i] = true;

            for (int j = 0; j < trainingSet.trainingArray[i].xVector.Length; j++)
                problemSolution.HostSample[j] = trainingSet.trainingArray[i].xVector[j];
            problemSolution.CLSample.WriteToDevice(problemSolution.HostSample);

            //OpenCL Kernel execution
            CLCalc.Program.MemoryObject[] args = new CLCalc.Program.MemoryObject[]
            {
                problemSolution.CLTrainingFeatures, 
                problemSolution.CLXVecLen,
                problemSolution.CLSample,
                problemSolution.CLKernelValues,
                problemSolution.CLLambda
            };

            lock (CLResource)
            {
                kernelComputeKernelRBF.Execute(args, trainingSet.getN);
                problemSolution.CLKernelValues.ReadFromDeviceTo(trainingSet.kernels[i]);
            }

        }

        private static float CLcalculateFx(int indexX, SVM currentSolution)
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

        #endregion

        #region Predict output

        /// <summary>
        /// Predicts the output of a single entry, given a previous problem, solution and correspondent training set
        /// </summary>
        /// <param name="problemSolution">Correspondent problem solution</param>
        /// <param name="untrainedUnit">Input features from which the output will be predicted</param>
        /// <returns>The y classification (true/false = positive/negative)</returns>
        public static float CLpredictOutput(SVM problemSolution, TrainingUnit untrainedUnit)
        {
            TrainingSet trainingSet = problemSolution.TrainingSet;
            ProblemConfig problemConfig = problemSolution.ProblemCfg;

            #region Compute kernel
            float[] K = new float[problemSolution.TrainingSet.getN];

            CLCalc.Program.MemoryObject[] args = new CLCalc.Program.MemoryObject[] 
            {
                problemSolution.CLTrainingFeatures, 
                problemSolution.CLXVecLen,
                problemSolution.CLSample,
                problemSolution.CLKernelValues,
                problemSolution.CLLambda
            };

            for (int j = 0; j < untrainedUnit.xVector.Length; j++)
                problemSolution.HostSample[j] = untrainedUnit.xVector[j];

            problemSolution.CLSample.WriteToDevice(problemSolution.HostSample);
            
            lock (CLResource)
            {
                kernelComputeKernelRBF.Execute(args, problemSolution.TrainingSet.getN);
                problemSolution.CLKernelValues.ReadFromDeviceTo(K);
            }
            #endregion

            // F(x) = sum + b
            // sum = summation of alpha_i * y_i * kernel(untrained unit, i) for all i in the training set
            float sum = 0;
            for (int i = 0; i < trainingSet.getN; i++)
            {
                if (trainingSet.trainingArray[i].y > 0)
                    sum += problemSolution.alphaList[i] * K[i];
                else
                    sum -= problemSolution.alphaList[i] * K[i];
            }

            return sum + problemSolution.b;
        }

        /// <summary>CLKernel to compute multiple kernels</summary>
        private static CLCalc.Program.Kernel kernelComputeMultiKernelRBF;
        /// <summary>CLKernel to sum kernels to a final value</summary>
        private static CLCalc.Program.Kernel kernelSumKernels;

        /// <summary>Kernel values for multi classification</summary>
        private CLCalc.Program.Variable CLKernelValuesMultiClassify;
        /// <summary>Store svm`s alphas</summary>
        private CLCalc.Program.Variable CLAlphas;
        /// <summary>Store svm`s y</summary>
        private CLCalc.Program.Variable CLys;
        /// <summary>Store svm`s b</summary>
        private CLCalc.Program.Variable CLb;
        /// <summary>Number of support vectors</summary>
        private CLCalc.Program.Variable CLQtdSupVecs;
        /// <summary>Final multiclassification sums</summary>
        private static CLCalc.Program.Variable CLMultiClassifSums;

        /// <summary>Classifies multiple samples stored in OpenCL memory</summary>
        /// <param name="Samples">Samples data to classify</param>
        /// <param name="svm">SVM to use as classifier</param>
        public static float[] MultiClassify(SVM svm, CLCalc.Program.Image2D Samples)
        {
            float[] resp = new float[Samples.Height];

            //svm.WriteToDevice();

            if ((Samples.Width << 2) != svm.HostVLen[0]) throw new Exception("Invalid Samples width, should be the same length of training features");

            if (svm.CLKernelValuesMultiClassify == null || svm.CLKernelValuesMultiClassify.OriginalVarLength != svm.alphaList.Count * Samples.Height)
            {
                svm.CLKernelValuesMultiClassify = new CLCalc.Program.Variable(new float[svm.alphaList.Count * Samples.Height]);
            }

            if (svm.CLAlphas == null || svm.CLAlphas.OriginalVarLength != svm.alphaList.Count)
            {
                svm.CLAlphas = new CLCalc.Program.Variable(svm.alphaList.ToArray());

                float[] ys = new float[svm.TrainingSet.trainingArray.Count];
                for (int i = 0; i < ys.Length; i++) ys[i] = svm.TrainingSet.trainingArray[i].y;

                svm.CLys = new CLCalc.Program.Variable(ys);
            }
            if (svm.CLb==null)
            {
                svm.CLb = new CLCalc.Program.Variable(new float[] { svm.b });
                svm.CLQtdSupVecs = new CLCalc.Program.Variable(new int[] { svm.alphaList.Count });
                CLMultiClassifSums = new CLCalc.Program.Variable(new float[Samples.Height]);
            }

            if (CLMultiClassifSums.OriginalVarLength != Samples.Height)
            {
                CLMultiClassifSums = new CLCalc.Program.Variable(new float[Samples.Height]);
            }

            //svm.CLAlphas.WriteToDevice(svm.alphaList.ToArray());
            //svm.CLys.WriteToDevice(ys);
            //svm.CLb.WriteToDevice(new float[] { svm.b });
            //svm.CLQtdSupVecs.WriteToDevice(new int[] { svm.alphaList.Count });

            CLCalc.Program.MemoryObject[] args = new CLCalc.Program.MemoryObject[] { svm.CLTrainingFeatures, svm.CLQtdSupVecs, svm.CLXVecLen, Samples, svm.CLKernelValuesMultiClassify, svm.CLLambda };
            kernelComputeMultiKernelRBF.Execute(args, new int[] { svm.alphaList.Count, Samples.Height });

            CLCalc.Program.Sync();

            args = new CLCalc.Program.MemoryObject[] { svm.CLAlphas, svm.CLQtdSupVecs, svm.CLXVecLen, svm.CLys, svm.CLKernelValuesMultiClassify, svm.CLb, CLMultiClassifSums };
            kernelSumKernels.Execute(args, Samples.Height);

            CLMultiClassifSums.ReadFromDeviceTo(resp);
            return resp;
        }

        #endregion

        #region Find max/min error value, update error

        #region Max/min variables and kernels
        
        /// <summary>Global work size to compute max/min. Has to be a power of 2</summary>
        private const int MAXMINWORKSIZE = 1024;

        /// <summary>Kernel to compute maximum or minimum of a vector</summary>
        private static CLCalc.Program.Kernel kernelMaxErr, kernelMinErr;

        /// <summary>Kernel to compute maximum or minimum of a vector</summary>
        private static CLCalc.Program.Kernel kernelComputeMax, kernelComputeMin;

        /// <summary>Gets max/min index from device</summary>
        private static CLCalc.Program.Kernel kernelGetResp;

        /// <summary>OpenCL errors holder</summary>
        private CLCalc.Program.Variable CLerr;
        
        /// <summary>Error.Length</summary>
        private CLCalc.Program.Variable CLErrLen;

        /// <summary>Length MAXMINWORKSIZE vector containing local max/min</summary>
        private CLCalc.Program.Variable CLMaxMinErrs;
        /// <summary>Length MAXMINWORKSIZE index list containing index of local max/min</summary>
        private CLCalc.Program.Variable CLMaxMinInds;
        /// <summary>Length 1 containing final index value</summary>
        private CLCalc.Program.Variable CLResp;
        /// <summary>Host memory answer holder</summary>
        private int[] HostResp;



        #endregion

        #region Update error variables and kernels

        /// <summary>Updates device memory errors based on newly calculated alphas</summary>
        private static CLCalc.Program.Kernel kernelUpdateErr;
        /// <summary>Holders of kernel values</summary>
        private CLCalc.Program.Variable CLKi, CLKj;
        /// <summary>Update parameters: alphaNew-alphaOld, Bnew-Bold</summary>
        private CLCalc.Program.Variable CLUpdtErrParams;

        #endregion

        #region Find maximum and minimum errors, write error to device
        /// <summary>Finds maximum E[i] in SVM and returns corresponding i (returns arg max E[i])</summary>
        /// <param name="svm">SVM to check</param>
        private static int CLFindMaxError(SVM svm)
        {
            CLCalc.Program.Variable[] args = new CLCalc.Program.Variable[] { svm.CLerr, svm.CLErrLen, svm.CLMaxMinErrs, svm.CLMaxMinInds };

            lock (CLResource)
            {
                //Majority of maximums
                kernelMaxErr.Execute(args, MAXMINWORKSIZE);

                //Computes values
                args = new CLCalc.Program.Variable[] { svm.CLMaxMinErrs, svm.CLMaxMinInds };
                int i = MAXMINWORKSIZE >> 1;
                while (i > 0)
                {
                    kernelComputeMax.Execute(args, i);
                    i = (i >> 1);
                }

                //Retrieves index
                args = new CLCalc.Program.Variable[] { svm.CLMaxMinInds, svm.CLResp };
                kernelGetResp.Execute(args, 1);

                svm.CLResp.ReadFromDeviceTo(svm.HostResp);
            }

            return svm.HostResp[0];
        }

        /// <summary>Finds minimum E[i] in SVM and returns corresponding i (returns arg min E[i])</summary>
        /// <param name="svm">SVM to check</param>
        private static int CLFindMinError(SVM svm)
        {
            CLCalc.Program.Variable[] args = new CLCalc.Program.Variable[] { svm.CLerr, svm.CLErrLen, svm.CLMaxMinErrs, svm.CLMaxMinInds };
            lock (CLResource)
            {
                //Majority of Minimums
                kernelMinErr.Execute(args, MAXMINWORKSIZE);

                //Computes values
                args = new CLCalc.Program.Variable[] { svm.CLMaxMinErrs, svm.CLMaxMinInds };
                int i = MAXMINWORKSIZE >> 1;
                while (i > 0)
                {
                    kernelComputeMin.Execute(args, i);
                    i = (i >> 1);
                }

                //Retrieves index
                args = new CLCalc.Program.Variable[] { svm.CLMaxMinInds, svm.CLResp };
                kernelGetResp.Execute(args, 1);

                svm.CLResp.ReadFromDeviceTo(svm.HostResp);
            }
            return svm.HostResp[0];
        }

        private static void WriteCLErr(SVM svm)
        {
            lock (CLResource)
            {
                //Initializes OpenCL memory error vector
                if (svm.CLerr == null || svm.CLerr.OriginalVarLength != svm.TrainingSet.errors.Length)
                {
                    svm.CLerr = new CLCalc.Program.Variable(svm.TrainingSet.errors);
                    svm.CLErrLen.WriteToDevice(new int[] { svm.TrainingSet.errors.Length });
                }
                else
                {
                    svm.CLerr.WriteToDevice(svm.TrainingSet.errors);
                }
            }
        }
        #endregion

        #region Update error, read error from device

        private static void CLupdateErrorsCache(TrainingSet trainingSet, SVM svm,
            float oldAlphai, float newAlphai, int iIndex,
            float oldAlphaj, float newAlphaj, int jIndex,
            float oldB, float newB)
        {
            float alphaiDif = newAlphai - oldAlphai;
            float alphajDif = newAlphaj - oldAlphaj;
            float BDif = newB - oldB;

            if (trainingSet.trainingArray[iIndex].y < 0) alphaiDif = -alphaiDif;
            if (trainingSet.trainingArray[jIndex].y < 0) alphajDif = -alphajDif;

            lock (CLResource)
            {
                //Writes kernel values
                if (svm.CLKi == null || svm.CLKi.OriginalVarLength != svm.TrainingSet.errors.Length)
                {
                    svm.CLKi = new CLCalc.Program.Variable(svm.TrainingSet.kernels[iIndex]);
                    svm.CLKj = new CLCalc.Program.Variable(svm.TrainingSet.kernels[jIndex]);
                }
                else
                {
                    svm.CLKi.WriteToDevice(svm.TrainingSet.kernels[iIndex]);
                    svm.CLKj.WriteToDevice(svm.TrainingSet.kernels[jIndex]);
                }
                float[] p = new float[3] { alphaiDif, BDif, alphajDif };
                svm.CLUpdtErrParams.WriteToDevice(p);

                //Executes update using GPU
                kernelUpdateErr.Execute(new CLCalc.Program.Variable[] { svm.CLerr, svm.CLKi, svm.CLKj, svm.CLUpdtErrParams }, svm.TrainingSet.getN);

                svm.CLerr.ReadFromDeviceTo(svm.TrainingSet.errors);
            }
        }
        #endregion

        #endregion
    }
}
