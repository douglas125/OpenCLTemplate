using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Drawing.Imaging;
using System.Drawing;

namespace OpenCLTemplate.MachineLearning
{
    /// <summary>Object classifier Class</summary>
    public class CLObjClassifier
    {
        #region OpenCL

        #region Extract features
        /// <summary>Kernel to extract features</summary>
        private static CLCalc.Program.Kernel kernelExtractFeatures;
        /// <summary>Kernel to segregate skin</summary>
        private static CLCalc.Program.Kernel kernelSegregateSkin;

        /// <summary>OpenCL source code to extract features</summary>
        private class CLExtractFeatSrc
        {
            public string src = @"

__kernel void ExtractFeatures(__global     int *     subframes,
                              __write_only image2d_t subfeats, 
                              __read_only  image2d_t bmp)
{
  const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
    CLK_ADDRESS_CLAMP | //Clamp to zeros
    CLK_FILTER_NEAREST; //Don't interpolate


    int i = get_global_id(0);
    int subsize = subframes[3 * i + 2];
    int x0 = subframes[3 * i];
    int y0 = subframes[3 * i + 1];
    
    int2 coords;

    float scalefac = (float)subsize * 0.0526315789473684f;

    //Histogram normalization variables
    float minn = 10000.0f, maxx = -10000.0f, c = 0.0f;

    float localFeats[19][19];
    uint4 val;
    //Write data
    {
        int xmin, xmax, ymin, ymax;
        ymax = y0;
        for (int y = 0; y < 19; y++)
        {
            ymin = ymax;
            ymax = y0 + (int)((y + 1) * scalefac);

            xmax = x0;

            for (int x = 0; x < 19; x++)
            {
                xmin = xmax;
                xmax = x0+(int)((x + 1) * scalefac);

                c = 0;
                for (int yy = ymin; yy < ymax; yy++)
                {
                    coords.y = yy;
                    for (int xx = xmin; xx < xmax; xx++)
                    {
                        coords.x = xx;

                        val = read_imageui(bmp, smp, coords);

                        c += (float)(5 * val.z + 3 * val.y + 2 * val.x);
                    }
                }

                c /= (float)(xmax - xmin) * (float)(ymax - ymin);

                localFeats[x][y] = c;
                if (y<12)
                {
                   maxx = fmax(c, maxx);
                   minn = fmin(c, minn);
                }
            }
        }
    }

    minn += 270;
    maxx -= 100;

    maxx = 1 / (maxx - minn);

    coords.y = i;
    coords.x = 0;
    float4 ff;
    int count=0;
    for (int y = 0; y < 19; y++)
    {
        for (int x = 0; x < 19; x++)
        {
            c = (localFeats[x][y] - minn) * maxx;
            c = fmin(1.0f, fmax(0.0f, c));
            if (count == 0) ff.x = c;
            if (count == 1) ff.y = c;
            if (count == 2) ff.z = c;
            if (count == 3) ff.w = c;
            
            count++;
            if (count == 4)
            {
               count=0;
               write_imagef(subfeats, coords, ff);
               coords.x++;
            }
        }
    }
    ff.y=0; ff.z=0; ff.w=0;
    write_imagef(subfeats, coords, ff);
  
}

";
        }
        #endregion

        #region Bracket moving regions
        /// <summary>Kernel to compute frame difference</summary>
        private static CLCalc.Program.Kernel kernelComputeFrameDiff;

        /// <summary>OpenCL source to bracket regions</summary>
        private class CLBracketRegionsSrc
        {
            public string src = @"
#pragma OPENCL EXTENSION cl_khr_byte_addressable_store : enable

__kernel void ComputeFrameDiff(__read_only image2d_t bmp,
                               __read_only image2d_t bmpAnt,
                               __global uchar *      frameDiff)

{
  const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
    CLK_ADDRESS_CLAMP | //Clamp to zeros
    CLK_FILTER_NEAREST; //Don't interpolate

   int x = get_global_id(0);
   int y = get_global_id(1);
   
   //Where to read pixels
   int2 coords = (int2)(8*x, 8*y);
   
   int w = get_global_size(0);
   
   uint4 val1, val2;
   uint4 dif = (uint4)(0,0,0,0);
   

   for (int xx=0;xx<8;xx++)
   {
     for (int yy=0;yy<8;yy++)
     {
       val1 = read_imageui(bmp,    smp, coords);
       val2 = read_imageui(bmpAnt, smp, coords);
       
          dif += abs_diff(val1, val2);

       coords.y++;
     }
     coords.x++;
   }
   
   uint resp = (uint)((float)(dif.x+dif.y+dif.z)*0.005208f);
   
   frameDiff[x+w*y] = (uchar)resp;
}

__kernel void SegregateSkin(__read_only  image2d_t bmpIn,
                            __write_only image2d_t bmpOut)
{
  
  const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
    CLK_ADDRESS_CLAMP | //Clamp to zeros
    CLK_FILTER_NEAREST; //Don't interpolate

   int x = get_global_id(0);
   int y = get_global_id(1);

   int2 coords = (int2)(x,y);

   uint4 val = read_imageui(bmpIn, smp, coords);

   float R, G, B;

   //Skin classification
   R = (float)val.z; G = (float)val.y; B = (float)val.z;

   if ( //(B > 160 && R < 180 && G < 180) || // Too much blue
        //(G > 160 && R < 180 && B < 180) || // Too much green
        //(B < 100 && R < 100 && G < 100) || // Too dark
        //(G > 200) || // Green
        //(R + G > 400) || // Too much red and green (yellow like color)
        //(G > 150 && B < 90) || // Yellow like also
        (B / (R + G + B) > .50f) || // Too much blue in contrast to others
        (G / (R + G + B) > .50f) //|| // Too much green in contrast to others
        //(R < 102 && G > 100 && B > 110 && G < 140 && B < 160) // Ocean
        )
   {
      val = (uint4)(120,120,120,val.w);
   }

   write_imageui(bmpOut, coords, val);
}
";
        }
        #endregion

        private void InitKernel()
        {
            if (kernelExtractFeatures == null)
            {
                CLExtractFeatSrc src = new CLExtractFeatSrc();
                CLBracketRegionsSrc src2 = new CLBracketRegionsSrc();

                CLCalc.Program.Compile(new string[] { src2.src, src.src });

                kernelExtractFeatures = new CLCalc.Program.Kernel("ExtractFeatures");
                kernelComputeFrameDiff = new CLCalc.Program.Kernel("ComputeFrameDiff");
                kernelSegregateSkin = new CLCalc.Program.Kernel("SegregateSkin");
            }
        }

        #endregion

        #region Constructor, data reader and SVM training

        /// <summary>Constructor. Loads and classifies face dataset if desired</summary>
        /// <param name="TrainFaceDataset">Load and classify face dataset?</param>
        public CLObjClassifier(bool TrainFaceDataset)
        {
            if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.Unknown) CLCalc.InitCL();

            if (TrainFaceDataset)
            {
                LoadMITFaceClassifier();
                SVM = new MultiClassSVM(tSet);
                InitKernel();
            }
        }

        /// <summary>Constructor. Loads parameters from a file.</summary>
        /// <param name="svmFile">File to read</param>
        public CLObjClassifier(string svmFile)
        {
            if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.Unknown) CLCalc.InitCL();
            SVM = new MultiClassSVM(new TrainingSet());
            SVM.SVMs.Add(new SVM());
            SVM.Classifications.Add(1.0f);
            SVM.SVMs[0].Load(svmFile);

            InitKernel();
        }

        /// <summary>Training set for SVM</summary>
        private TrainingSet tSet; //, testSet;
        /// <summary>Face classification SVM</summary>
        public MultiClassSVM SVM;

        /// <summary>Loads and classifies dataset</summary>
        private void LoadMITFaceClassifier()
        {
            /*

CBCL Face Database #1
MIT Center For Biological and Computation Learning
 * 
 */
            string p = System.Windows.Forms.Application.StartupPath;
            string fileTrain = p + "\\svm.train.normgrey";
            string fileTest = p + "\\svm.test.normgrey";


            tSet = new TrainingSet();

            //Fills both, we're not testing the results
            FillTrainingSet(fileTrain, tSet);
            FillTrainingSet(fileTest, tSet);

            SVM = new MultiClassSVM(tSet);
        }

        /// <summary>Adds training units to a set from a file</summary>
        /// <param name="filename">File containing features</param>
        /// <param name="TrSet">Training set to be populated</param>
        private void FillTrainingSet(string filename, TrainingSet TrSet)
        {
            string sepdec = (1.5).ToString().Substring(1, 1);
            using (StreamReader sr = new StreamReader(filename))
            {
                string line;

                line = sr.ReadLine();
                int n = int.Parse(line);
                line = sr.ReadLine();
                int dim = (int)Math.Sqrt(double.Parse(line));

                for (int i = 0; i < n; i++)
                {
                    line = sr.ReadLine().Replace(".", sepdec);
                    string[] s = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    float[] x = new float[364];
                    float y;

                    for (int j = 0; j < s.Length - 1; j++) x[j] = float.Parse(s[j]);

                    y = float.Parse(s[s.Length - 1]);

                    TrSet.addTrainingUnit(new TrainingUnit(x, y));

                    /*
                     * Features Haar
                     * [04:05:53] Edmundo ITA05(FOX.Howler): 1,1
[04:05:57] Edmundo ITA05(FOX.Howler): 7,4
[04:06:55] Edmundo ITA05(FOX.Howler): new Rectangle(1, 1, 7, 4)
[04:07:06] Edmundo ITA05(FOX.Howler): 7 x 4
[04:08:15] Edmundo ITA05(FOX.Howler): new Rectangle(11, 4, 7, 4)
[04:08:24] Edmundo ITA05(FOX.Howler): divide por 7*4
[04:09:04] Edmundo ITA05(FOX.Howler): subtrai da área do Rect(8, 1, 3, 4)
[04:09:13] Edmundo ITA05(FOX.Howler): dividido pela 3 *4
                     * 
                     * 
                     * 
                     * [04:05:53] Edmundo ITA05(FOX.Howler): 1,1
[04:05:57] Edmundo ITA05(FOX.Howler): 7,4
[04:06:55] Edmundo ITA05(FOX.Howler): new Rectangle(1, 1, 7, 4)
[04:07:06] Edmundo ITA05(FOX.Howler): 7 x 4
[04:08:15] Edmundo ITA05(FOX.Howler): new Rectangle(11, 4, 7, 4)
[04:08:24] Edmundo ITA05(FOX.Howler): divide por 7*4
[04:09:04] Edmundo ITA05(FOX.Howler): subtrai da área do Rect(8, 1, 3, 4)
[04:09:13] Edmundo ITA05(FOX.Howler): dividido pela 3 *4
[04:13:38] Edmundo ITA05(FOX.Howler): ((1,6,5,5) + (13,6,5,5)) / 2 - (7,6,5,6)
                     * 
                     * 
                     * [04:17:15] Edmundo ITA05(FOX.Howler): (1,1,17,5) - (1,6,17,8) dividir pela area
                     */

                }
            }
        }
        #endregion

        #region Configuration variables
        /// <summary>Configuration</summary>
        public static class Config
        {
            ///// <summary>Minimum percentual window size</summary>
            //public static float MINSUBSIZEPERC = 0.15f;
            ///// <summary>Maximum percentual window size</summary>
            //public static float MAXSUBSIZEPERC = 0.21f;
            ///// <summary>At each iteration, subsize = SUBSIZEINCREMENT * subsize</summary>
            //public static float SUBSIZEINCREMENT = 1.08f; //1.085f;

            /// <summary>Window sizes in pixels to use to look for objects</summary>
            public static int[] WINDOWSIZES = new int[] { 38, 57, 76, 100 };//{ 70, 86, 103, 120 };

            /// <summary>Extra coverage to use. E.g.: 10 windows would cover entire screen, algorithm uses OVERLAP * 10</summary>
            public static float OVERLAP = 2.2f; //2.2f;
            /// <summary>Pixel size</summary>
            public static int PIXELSIZE = 4;
            /// <summary>Only classifies as face if kernel value is above REQCERTAINTY</summary>
            public static float REQCERTAINTY = 0.05f;
            /// <summary>Refine region if it looks like a face to this degree</summary>
            public static float REFINEUNCERTAINTY = -0.5f;

            /// <summary>Threshold to consider as relevant movement</summary>
            public static int MOVEMENTTHRESHOLD = 19;
            /// <summary>Threshold to consider as relevant movement to keep search</summary>
            public static int KEEPSEARCHTHRESHOLD = 3;
        }
        #endregion

        #region Image segmentation

        /// <summary>Specific function when SVM contains only one object, such as faces</summary>
        /// <param name="bmp">Next frame to process</param>
        public List<int> FindSingleObj(Bitmap bmp)
        {
            //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch(), sw2 = new System.Diagnostics.Stopwatch();
            //sw.Start();

            if (SVM == null) return null;

            if (imgWidth != bmp.Width || imgHeight != bmp.Height)
            {
                imgWidth = bmp.Width;
                imgHeight = bmp.Height;
                SubFramePos = 0;

                List<int> subFrames = new List<int>();
                ComputeSubFrames(0, 0, bmp.Width, bmp.Height, subFrames);
                SubFrames = subFrames.ToArray();


                SubFeatures = new float[(SubFrames.Length / 3) * 364];

                if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.UsingCL)
                {
                    CLSubFrames = new CLCalc.Program.Variable(SubFrames);
                    CLSubFeatures = new CLCalc.Program.Image2D(SubFeatures, 91, SubFrames.Length / 3);
                    CLBmp = new CLCalc.Program.Image2D(bmp);
                    //CLBmpTemp = new CLCalc.Program.Image2D(bmp);
                    CLBmpPrev = new CLCalc.Program.Image2D(bmp);
                }
            }

            //Swaps current and previous bitmap pointers
            CLCalc.Program.Image2D temp = CLBmp;
            CLBmp = CLBmpPrev;
            CLBmpPrev = temp;

            //Computes frame difference
            ComputeFrameDiff();

            //Replaces subFrames based on moving regions
            for (int k = 0; k < MovingRegionBoxes.Count >> 2; k++)
            {
                List<int> sframes = new List<int>();
                int ind = 4 * k;
                ComputeSubFrames(MovingRegionBoxes[ind] << 3, MovingRegionBoxes[ind + 2] << 3, MovingRegionBoxes[ind + 1] << 3, MovingRegionBoxes[ind + 3] << 3, sframes);

                for (int p = 0; p < sframes.Count; p += 3)
                {
                    SubFrames[SubFramePos] = sframes[p];
                    SubFrames[SubFramePos + 1] = sframes[p + 1];
                    SubFrames[SubFramePos + 2] = sframes[p + 2];

                    SubFramePos += 3; if (SubFramePos > SubFrames.Length - 1) SubFramePos = 0;
                }
            }
            CLSubFrames.WriteToDevice(SubFrames);


            CLBmp.WriteBitmap(bmp);

            ////Segments skin
            //kernelSegregateSkin.Execute(new CLCalc.Program.MemoryObject[] { CLBmpTemp, CLBmp }, new int[] { bmp.Width, bmp.Height });

            //Extract features using OpenCL
            CLCalc.Program.MemoryObject[] args = new CLCalc.Program.MemoryObject[] { CLSubFrames, CLSubFeatures, CLBmp };
            kernelExtractFeatures.Execute(args, SubFrames.Length / 3);

            #region No OpenCL
            //float[] testSubFeats = new float[364 * (SubFrames.Length / 3)];
            //CLSubFeatures.ReadFromDeviceTo(testSubFeats);


            //Extract features without OpenCL
            //ExtractFeatures(SubFrames, SubFeatures, bmp);
            //CLSubFeatures.WriteToDevice(SubFeatures);
            #endregion

            //sw2.Start();
            float[] maxvals = OpenCLTemplate.MachineLearning.SVM.MultiClassify(SVM.SVMs[0], CLSubFeatures);
            //SVM.Classify(CLSubFeatures, out maxvals);
            //sw2.Stop();


            List<int> FacesPos = new List<int>();
            List<float> MaxVals = new List<float>();


            //Goes in decreasing window size order
            for (int kk = Config.WINDOWSIZES.Length - 1; kk >= 0; kk--)
            {
                for (int i = maxvals.Length - 1; i >= 0; i--)
                {

                    if (SubFrames[3 * i + 2] == Config.WINDOWSIZES[kk] && maxvals[i] > Config.REQCERTAINTY)
                    {
                        //Checks if a face already has been found in that region
                        bool contido = false;

                        int i3 = 3 * i;
                        int kmax = FacesPos.Count / 3;
                        for (int k = 0; k < kmax; k++)
                        {
                            int k3 = 3 * k;

                            if (
                                (FacesPos[k3] <= SubFrames[i3] && SubFrames[i3] <= FacesPos[k3] + FacesPos[k3 + 2] &&
                                FacesPos[k3 + 1] <= SubFrames[i3 + 1] && SubFrames[i3 + 1] <= FacesPos[k3 + 1] + FacesPos[k3 + 2]) ||

                                (FacesPos[k3] <= SubFrames[i3] + SubFrames[i3 + 2] && SubFrames[i3] + SubFrames[i3 + 2] <= FacesPos[k3] + FacesPos[k3 + 2] &&
                                FacesPos[k3 + 1] <= SubFrames[i3 + 1] + SubFrames[i3 + 2] && SubFrames[i3 + 1] + SubFrames[i3 + 2] <= FacesPos[k3 + 1] + FacesPos[k3 + 2]) ||

                                (FacesPos[k3] <= SubFrames[i3] && SubFrames[i3] <= FacesPos[k3] + FacesPos[k3 + 2] &&
                                FacesPos[k3 + 1] <= SubFrames[i3 + 1] + SubFrames[i3 + 2] && SubFrames[i3 + 1] + SubFrames[i3 + 2] <= FacesPos[k3 + 1] + FacesPos[k3 + 2]) ||

                                (FacesPos[k3] <= SubFrames[i3] + SubFrames[i3 + 2] && SubFrames[i3] + SubFrames[i3 + 2] <= FacesPos[k3] + FacesPos[k3 + 2] &&
                                FacesPos[k3 + 1] <= SubFrames[i3 + 1] && SubFrames[i3 + 1] <= FacesPos[k3 + 1] + FacesPos[k3 + 2])

                                )
                            {
                                contido = true;

                                //Replaces if better
                                if (maxvals[i] > MaxVals[k] && SubFrames[3 * i + 2] == FacesPos[3 * k + 2])
                                {
                                    FacesPos[k3] = SubFrames[i3];
                                    FacesPos[k3 + 1] = SubFrames[i3 + 1];
                                    FacesPos[k3 + 2] = SubFrames[i3 + 2];
                                    MaxVals[k] = maxvals[i];
                                }

                                k = FacesPos.Count;
                            }
                        }

                        if (!contido)
                        {
                            FacesPos.Add(SubFrames[3 * i]);
                            FacesPos.Add(SubFrames[3 * i + 1]);
                            FacesPos.Add(SubFrames[3 * i + 2]);
                            MaxVals.Add(maxvals[i]);
                        }
                    }
                }
            }

            //sw.Stop();
            Random rnd = new Random();

            //Updates frame search region
            if (MovingRegionBoxes.Count > 0)
            {
                for (int i = 0; i < maxvals.Length; i++)
                {
                    if (maxvals[i] > Config.REFINEUNCERTAINTY)
                    {
                        int i3 = 3 * i;

                        List<int> sframes = new List<int>();
                        int cx = SubFrames[i3] + (SubFrames[i3 + 2] >> 1) + rnd.Next(7) - 3;
                        int cy = SubFrames[i3 + 1] + (SubFrames[i3 + 2] >> 1) + rnd.Next(7) - 3;

                        int bigwSize = Config.WINDOWSIZES[Config.WINDOWSIZES.Length - 1];

                        try
                        {
                            ComputeSubFrames(cx - (bigwSize >> 1), cy - (bigwSize >> 1), cx + (bigwSize >> 1), cy + (bigwSize >> 1), sframes);

                            for (int p = 0; p < sframes.Count; p += 3)
                            {
                                SubFrames[SubFramePos] = sframes[p];
                                SubFrames[SubFramePos + 1] = sframes[p + 1];
                                SubFrames[SubFramePos + 2] = sframes[p + 2];

                                SubFramePos += 3; if (SubFramePos > SubFrames.Length - 1) SubFramePos = 0;
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }


            return FacesPos;
        }



        /// <summary>Image dimensions</summary>
        private int imgWidth, imgHeight;

        /// <summary>Where to extract features in image</summary>
        private int[] SubFrames;
        /// <summary>Where to replace next subframe?</summary>
        private int SubFramePos;
        /// <summary>Where to extract features in OpenCL memory</summary>
        private CLCalc.Program.Variable CLSubFrames;

        /// <summary>Subimages features</summary>
        private float[] SubFeatures;
        /// <summary>Subimage features in OpenCL memory</summary>
        private CLCalc.Program.Image2D CLSubFeatures;

        /// <summary>Bitmap in device memory</summary>
        private CLCalc.Program.Image2D CLBmp;
        ///// <summary>Temporary bitmap in device memory before skin extraction</summary>
        //private CLCalc.Program.Image2D CLBmpTemp;
        /// <summary>Previous bitmap in device memory</summary>
        private CLCalc.Program.Image2D CLBmpPrev;

        /// <summary>Computes subframe vector to check where to extract features</summary>
        private void ComputeSubFrames(int x0, int y0, int xf, int yf, List<int> subFrames)
        {
            int ww = xf - x0;
            int hh = yf - y0;

            int subSize;
            for (int ii = 0; ii < Config.WINDOWSIZES.Length; ii++)
            {
                subSize = Config.WINDOWSIZES[ii];
                if (ww < subSize || hh < subSize)
                    return;

                //Computes spacing
                int nXDivs = 1 + (int)(Config.OVERLAP * (float)ww / (float)subSize);
                int nYDivs = 1 + (int)(Config.OVERLAP * (float)hh / (float)subSize);

                //Computes x's
                float[] x = new float[nXDivs];
                float[] y = new float[nYDivs];

                float invNXDivs = (1.0f - (float)subSize / (float)ww) / ((float)nXDivs - 1.0f);
                float invNYDivs = (1.0f - (float)subSize / (float)hh) / ((float)nYDivs - 1.0f);

                for (int i = 0; i < x.Length; i++) x[i] = x0 + (float)i * invNXDivs * (float)ww;
                for (int i = 0; i < y.Length; i++) y[i] = y0 + (float)i * invNYDivs * (float)hh;

                for (int j = 0; j < y.Length; j++)
                {
                    for (int i = 0; i < x.Length; i++)
                    {
                        subFrames.Add((int)x[i]);
                        subFrames.Add((int)y[j]);
                        subFrames.Add(subSize);
                    }
                }
            }

        }

        private void ExtractFeatures(int[] subframes, float[] subfeats, Bitmap bmp)
        {
            int n = subframes.Length / 3;

            BitmapData bmdbmp = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
 System.Drawing.Imaging.ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);


            for (int i = 0; i < n; i++)
            {
                //int i = get_global_id(0);
                int subsize = subframes[3 * i + 2];
                int x0 = subframes[3 * i];
                int y0 = subframes[3 * i + 1];

                float scalefac = (float)subsize / 19.0f;

                //Histogram normalization variables
                float min = 10000, max = -10000, c = 0;

                float[,] localFeats = new float[19, 19];

                //Write data
                unsafe
                {
                    int xmin, xmax, ymin, ymax;
                    ymax = y0;
                    for (int y = 0; y < 19; y++)
                    {
                        ymin = ymax;
                        ymax = y0 + (int)((y + 1) * scalefac);

                        xmax = x0;

                        for (int x = 0; x < 19; x++)
                        {
                            xmin = xmax;
                            xmax = x0 + (int)((x + 1) * scalefac);

                            c = 0;
                            for (int yy = ymin; yy < ymax; yy++)
                            {
                                byte* rowBmp = (byte*)bmdbmp.Scan0 + (yy * bmdbmp.Stride);
                                for (int xx = xmin; xx < xmax; xx++)
                                {
                                    c += (float)(5 * rowBmp[2 + xx * Config.PIXELSIZE] + 3 * rowBmp[1 + xx * Config.PIXELSIZE] + 2 * rowBmp[xx * Config.PIXELSIZE]);
                                }
                            }

                            c /= (float)(xmax - xmin) * (float)(ymax - ymin);

                            localFeats[x, y] = c;
                            if (y < 12)
                            {
                                max = Math.Max(c, max);
                                min = Math.Min(c, min);
                            }
                        }
                    }
                }

                min += 270;
                max -= 100;

                max = 1 / (max - min);

                for (int x = 0; x < 19; x++)
                {
                    for (int y = 0; y < 19; y++)
                    {
                        c = (localFeats[x, y] - min) * max;

                        subfeats[x + 19 * y + 364 * i] = Math.Min(1, Math.Max(0, c));
                    }
                }



            }

            //Unlock bits
            bmp.UnlockBits(bmdbmp);

        }

        #endregion

        #region Self training

        /// <summary>Self training set</summary>
        public TrainingSet SelfTSet;

        /// <summary>Adds a new self training example</summary>
        public void AddSelfTraining(int[] sbFrames, int faceIndex, Bitmap bmp)
        {
            if (SelfTSet == null) SelfTSet = new TrainingSet();
            float[] subF = new float[(sbFrames.Length / 3) * 364];

            ExtractFeatures(sbFrames, subF, bmp);

            for (int i = 0; i < sbFrames.Length / 3; i++)
            {
                float[] x = new float[364];

                for (int k = 0; k < 364; k++) x[k] = subF[k + i * 364];

                TrainingUnit tu = new TrainingUnit(x, i == faceIndex ? 1.0f : -1.0f);
                SelfTSet.addTrainingUnit(tu);
            }
        }

        /// <summary>Trains SVM with self data</summary>
        public void SelfTrain()
        {
            try
            {
                SVM = new MultiClassSVM(SelfTSet);
            }
            catch
            {
            }
        }


        #endregion

        #region Bracket moving regions

        /// <summary>OpenCL memory frame difference</summary>
        private CLCalc.Program.Variable CLframeDiff;
        /// <summary>Frame difference</summary>
        private byte[] frameDiff;
        /// <summary>Moving region boxes. List of boxes found [xmin0, xmax0, ymin0, ymax0, xmin1, xmax1, ....]</summary>
        public List<int> MovingRegionBoxes;

        /// <summary>Computes frame difference</summary>
        private void ComputeFrameDiff()
        {
            //Needs both images to compute
            if (CLBmp == null || CLBmpPrev == null || CLBmp.Width != CLBmpPrev.Width || CLBmp.Height != CLBmpPrev.Height) return;

            if (frameDiff == null || frameDiff.Length != ((CLBmp.Height * CLBmp.Width) >> 6))
            {
                //Reduces image size by 8
                frameDiff = new byte[(CLBmp.Height * CLBmp.Width) >> 6];

                CLframeDiff = new CLCalc.Program.Variable(frameDiff);

                MovingRegionBoxes = new List<int>();
            }

            CLCalc.Program.MemoryObject[] args = new CLCalc.Program.MemoryObject[] { CLBmp, CLBmpPrev, CLframeDiff };

            kernelComputeFrameDiff.Execute(args, new int[] { CLBmp.Width >> 3, CLBmp.Height >> 3 });

            CLframeDiff.ReadFromDeviceTo(frameDiff);

            MovingRegionBoxes.Clear();
            BracketMovingRegions(frameDiff, CLBmp.Width >> 3, CLBmp.Height >> 3, MovingRegionBoxes);
        }

        #region Iterative Region bracketing


        /// <summary>Tries to bracked moving regions</summary>
        /// <param name="FrameDiff">Frame difference matrix</param>
        /// <param name="fWidth">FrameDiff width</param>
        /// <param name="fHeight">FrameDiff height</param>
        /// <param name="boxes">List of boxes found [xmin0, xmax0, ymin0, ymax0, xmin1, xmax1, ....]</param>
        private void BracketMovingRegions(byte[] FrameDiff, int fWidth, int fHeight, List<int> boxes)
        {


            float max = 0;
            int xMax = 0, yMax = 0;
            for (int x = 0; x < fWidth; x++)
            {
                for (int y = 0; y < fHeight; y++)
                {
                    if (FrameDiff[x + fWidth * y] > max)
                    {
                        max = FrameDiff[x + fWidth * y];
                        xMax = x;
                        yMax = y;
                    }
                }
            }

            bool[] Visited = new bool[fWidth * fHeight];
            while (max >= Config.MOVEMENTTHRESHOLD)
            {
                //There are relevant moving regions
                //Maximum displacement region marking
                int[] box = new int[] { xMax, xMax, yMax, yMax };
                BracketRegionAround(xMax, yMax, FrameDiff, fWidth, fHeight, Visited, box, Config.KEEPSEARCHTHRESHOLD);

                //Adds box
                boxes.Add(box[0]); boxes.Add(box[1]); boxes.Add(box[2]); boxes.Add(box[3]);

                //Recalculates max
                xMax = 0; yMax = 0; max = 0;
                for (int x = 0; x < fWidth; x++)
                {
                    for (int y = 0; y < fHeight; y++)
                    {
                        int ind = x + fWidth * y;
                        if (!Visited[ind] && FrameDiff[ind] > max)
                        {
                            max = FrameDiff[ind];
                            xMax = x;
                            yMax = y;
                        }
                    }
                }

            }
        }

        /// <summary>Brackets region around a box where displacement occurred</summary>
        /// <param name="x">X position (column) in FrameDiff Matrix</param>
        /// <param name="y">Y position (line) in FrameDiff Matrix</param>
        /// <param name="FrameDiff">Frame difference matrix</param>
        /// <param name="fWidth">Width of FrameDiff</param>
        /// <param name="fHeight">Height of FrameDiff</param>
        /// <param name="Visited">Visited elements of FrameDiff</param>
        /// <param name="box">Box containing boundaries of region [xmin, xmax, ymin, ymax]</param>
        /// <param name="Threshold">Threshold to consider as relevant difference region</param>
        private void BracketRegionAround(int x, int y, byte[] FrameDiff, int fWidth, int fHeight, bool[] Visited, int[] box, int Threshold)
        {
            //Checks consistency of point to be visited. Indexes have to be in FrameDiff bounds
            if (x < 0 || y < 0 || x >= fWidth || y >= fHeight) return;

            //If this spot has been visited it can't be visited again
            if (Visited[x + fWidth * y]) return;

            List<Point> CheckList = new List<Point>();
            CheckList.Add(new Point(x, y));

            while (CheckList.Count > 0)
            {
                for (int i = 0; i < CheckList.Count; i++)
                {
                    if (frameDiff[CheckList[i].X + fWidth * CheckList[i].Y] > Threshold && (!Visited[CheckList[i].X + fWidth * CheckList[i].Y]))
                    {
                        Visited[CheckList[i].X + fWidth * CheckList[i].Y] = true;

                        //Updates box
                        if (CheckList[i].X < box[0]) box[0] = CheckList[i].X;
                        if (CheckList[i].X > box[1]) box[1] = CheckList[i].X;
                        if (CheckList[i].Y < box[2]) box[2] = CheckList[i].Y;
                        if (CheckList[i].Y > box[3]) box[3] = CheckList[i].Y;


                        if (CheckList[i].X > 0) CheckList.Add(new Point(CheckList[i].X - 1, CheckList[i].Y));
                        if (CheckList[i].Y > 0) CheckList.Add(new Point(CheckList[i].X, CheckList[i].Y - 1));
                        if (CheckList[i].X < fWidth - 1) CheckList.Add(new Point(CheckList[i].X + 1, CheckList[i].Y));
                        if (CheckList[i].Y < fHeight - 1) CheckList.Add(new Point(CheckList[i].X, CheckList[i].Y + 1));
                    }
                    CheckList.RemoveAt(i); i--;
                }
            }
        }

        #endregion

        #endregion
    }
}
