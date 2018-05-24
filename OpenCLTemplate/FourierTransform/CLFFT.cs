using System;
using System.Collections.Generic;
using OpenCLTemplate;
using System.Text;

namespace OpenCLTemplate.FourierTransform
{
    /// <summary>Computes Fast Fourier Transform of floats</summary>
    public static class CLFFTfloat
    {
        #region References
        /*
         * OpenCL Fast Fourier Transform
           Eric Bainville - May 2010
         * http://www.bealto.com/gpu-fft_opencl-1.html
         * 
         * 
         * Discrete Fourier transform basic theory
         * http://en.wikipedia.org/wiki/Discrete_Fourier_transform
         */
        #endregion

        #region OpenCL source
        /// <summary>Fast Fourier Transform source code</summary>
        private class CLFFTSrc
        {
            public string s = @"
#define M_PI 3.14159265358979f

//Global size is x.Length/2, Scale = 1 for direct, 1/N to inverse (iFFT)
__kernel void Conjugate(__global float2 * x, __global const float * Scale)
{
   int i = get_global_id(0);
   
   float temp = Scale[0];
   float2 t = (float2)(temp, -temp);

   x[i] *= t;
}


// Return a*EXP(-I*PI*1/2) = a*(-I)
float2 mul_p1q2(float2 a) { return (float2)(a.y,-a.x); }

// Return a^2
float2 sqr_1(float2 a)
{ return (float2)(a.x*a.x-a.y*a.y,2.0f*a.x*a.y); }

// Return the 2x DFT2 of the four complex numbers in A
// If A=(a,b,c,d) then return (a',b',c',d') where (a',c')=DFT2(a,c)
// and (b',d')=DFT2(b,d).
float8 dft2_4(float8 a) { return (float8)(a.lo+a.hi,a.lo-a.hi); }

// Return the DFT of 4 complex numbers in A
float8 dft4_4(float8 a)
{
  // 2x DFT2
  float8 x = dft2_4(a);
  // Shuffle, twiddle, and 2x DFT2
  return dft2_4((float8)(x.lo.lo,x.hi.lo,x.lo.hi,mul_p1q2(x.hi.hi)));
}

// Complex product, multiply vectors of complex numbers

#define MUL_RE(a,b) (a.even*b.even - a.odd*b.odd)
#define MUL_IM(a,b) (a.even*b.odd + a.odd*b.even)

float2 mul_1(float2 a,float2 b)
{ float2 x; x.even = MUL_RE(a,b); x.odd = MUL_IM(a,b); return x; }

float4 mul_2(float4 a,float4 b)
{ float4 x; x.even = MUL_RE(a,b); x.odd = MUL_IM(a,b); return x; }

// Return the DFT2 of the two complex numbers in vector A
float4 dft2_2(float4 a) { return (float4)(a.lo+a.hi,a.lo-a.hi); }

// Return cos(alpha)+I*sin(alpha)  (3 variants)
float2 exp_alpha_1(float alpha)
{
  float cs,sn;
  // sn = sincos(alpha,&cs);  // sincos
  //cs = native_cos(alpha); sn = native_sin(alpha);  // native sin+cos
  cs = cos(alpha); sn = sin(alpha); // sin+cos
  return (float2)(cs,sn);
}


// T = N/4 = number of threads.
// P is the length of input sub-sequences, 1,4,16,...,N/4.
__kernel void fft_radix4(__global const float2 * x,__global float2 * y,__global int * pp)
{
  int p = pp[0];
  int t = get_global_size(0); // number of threads
  int i = get_global_id(0); // current thread
  int k = i & (p-1); // index in input sequence, in 0..P-1
  // Inputs indices are I+{0,1,2,3}*T
  x += i;
  // Output indices are J+{0,1,2,3}*P, where
  // J is I with two 0 bits inserted at bit log2(P)
  y += ((i-k)<<2) + k;

  // Load and twiddle inputs
  // Twiddling factors are exp(_I*PI*{0,1,2,3}*K/2P)
  float alpha = -M_PI*(float)k/(float)(2*p);

// Load and twiddle
float2 u0 = x[0];
float2 u1 = mul_1(exp_alpha_1(alpha),x[t]);
float2 u2 = mul_1(exp_alpha_1(2*alpha),x[2*t]);
float2 u3 = mul_1(exp_alpha_1(3*alpha),x[3*t]);

// 2x DFT2 and twiddle
float2 v0 = u0 + u2;
float2 v1 = u0 - u2;
float2 v2 = u1 + u3;
float2 v3 = mul_p1q2(u1 - u3); // twiddle

// 2x DFT2 and store
y[0] = v0 + v2;
y[p] = v1 + v3;
y[2*p] = v0 - v2;
y[3*p] = v1 - v3;

}



// mul_p*q*(a) returns a*EXP(-I*PI*P/Q)
#define mul_p0q1(a) (a)

#define mul_p0q2 mul_p0q1
//float2  mul_p1q2(float2 a) { return (float2)(a.y,-a.x); }

__constant float SQRT_1_2 = 0.707106781186548; // cos(Pi/4)
#define mul_p0q4 mul_p0q2
float2  mul_p1q4(float2 a) { return (float2)(SQRT_1_2)*(float2)(a.x+a.y,-a.x+a.y); }
#define mul_p2q4 mul_p1q2
float2  mul_p3q4(float2 a) { return (float2)(SQRT_1_2)*(float2)(-a.x+a.y,-a.x-a.y); }

__constant float COS_8 = 0.923879532511287; // cos(Pi/8)
__constant float SIN_8 = 0.382683432365089; // sin(Pi/8)
#define mul_p0q8 mul_p0q4
float2  mul_p1q8(float2 a) { return mul_1((float2)(COS_8,-SIN_8),a); }
#define mul_p2q8 mul_p1q4
float2  mul_p3q8(float2 a) { return mul_1((float2)(SIN_8,-COS_8),a); }
#define mul_p4q8 mul_p2q4
float2  mul_p5q8(float2 a) { return mul_1((float2)(-SIN_8,-COS_8),a); }
#define mul_p6q8 mul_p3q4
float2  mul_p7q8(float2 a) { return mul_1((float2)(-COS_8,-SIN_8),a); }

// Compute in-place DFT2 and twiddle
#define DFT2_TWIDDLE(a,b,t) { float2 tmp = t(a-b); a += b; b = tmp; }

// T = N/16 = number of threads.
// P is the length of input sub-sequences, 1,16,256,...,N/16.
__kernel void fft_radix16(__global const float2 * x,__global float2 * y, __global int * pp)
{
  int p = pp[0];
  int t = get_global_size(0); // number of threads
  int i = get_global_id(0); // current thread


//////  y[i] = 2*x[i];
//////  return;

  int k = i & (p-1); // index in input sequence, in 0..P-1
  // Inputs indices are I+{0,..,15}*T
  x += i;
  // Output indices are J+{0,..,15}*P, where
  // J is I with four 0 bits inserted at bit log2(P)
  y += ((i-k)<<4) + k;

  // Load
  float2 u[16];
  for (int m=0;m<16;m++) u[m] = x[m*t];

  // Twiddle, twiddling factors are exp(_I*PI*{0,..,15}*K/4P)
  float alpha = -M_PI*(float)k/(float)(8*p);
  for (int m=1;m<16;m++) u[m] = mul_1(exp_alpha_1(m*alpha),u[m]);

  // 8x in-place DFT2 and twiddle (1)
  DFT2_TWIDDLE(u[0],u[8],mul_p0q8);
  DFT2_TWIDDLE(u[1],u[9],mul_p1q8);
  DFT2_TWIDDLE(u[2],u[10],mul_p2q8);
  DFT2_TWIDDLE(u[3],u[11],mul_p3q8);
  DFT2_TWIDDLE(u[4],u[12],mul_p4q8);
  DFT2_TWIDDLE(u[5],u[13],mul_p5q8);
  DFT2_TWIDDLE(u[6],u[14],mul_p6q8);
  DFT2_TWIDDLE(u[7],u[15],mul_p7q8);

  // 8x in-place DFT2 and twiddle (2)
  DFT2_TWIDDLE(u[0],u[4],mul_p0q4);
  DFT2_TWIDDLE(u[1],u[5],mul_p1q4);
  DFT2_TWIDDLE(u[2],u[6],mul_p2q4);
  DFT2_TWIDDLE(u[3],u[7],mul_p3q4);
  DFT2_TWIDDLE(u[8],u[12],mul_p0q4);
  DFT2_TWIDDLE(u[9],u[13],mul_p1q4);
  DFT2_TWIDDLE(u[10],u[14],mul_p2q4);
  DFT2_TWIDDLE(u[11],u[15],mul_p3q4);

  // 8x in-place DFT2 and twiddle (3)
  DFT2_TWIDDLE(u[0],u[2],mul_p0q2);
  DFT2_TWIDDLE(u[1],u[3],mul_p1q2);
  DFT2_TWIDDLE(u[4],u[6],mul_p0q2);
  DFT2_TWIDDLE(u[5],u[7],mul_p1q2);
  DFT2_TWIDDLE(u[8],u[10],mul_p0q2);
  DFT2_TWIDDLE(u[9],u[11],mul_p1q2);
  DFT2_TWIDDLE(u[12],u[14],mul_p0q2);
  DFT2_TWIDDLE(u[13],u[15],mul_p1q2);

  // 8x DFT2 and store (reverse binary permutation)
  y[0]    = u[0]  + u[1];
  y[p]    = u[8]  + u[9];
  y[2*p]  = u[4]  + u[5];
  y[3*p]  = u[12] + u[13];
  y[4*p]  = u[2]  + u[3];
  y[5*p]  = u[10] + u[11];
  y[6*p]  = u[6]  + u[7];
  y[7*p]  = u[14] + u[15];
  y[8*p]  = u[0]  - u[1];
  y[9*p]  = u[8]  - u[9];
  y[10*p] = u[4]  - u[5];
  y[11*p] = u[12] - u[13];
  y[12*p] = u[2]  - u[3];
  y[13*p] = u[10] - u[11];
  y[14*p] = u[6]  - u[7];
  y[15*p] = u[14] - u[15];
}


";
        }
        #endregion

        /// <summary>Radix FFT kernel</summary>
        private static CLCalc.Program.Kernel kernelfft_radix16, kernelfft_radix4, kernelConjugate;
        private static CLCalc.Program.Variable CLx;
        private static CLCalc.Program.Variable CLy;
        private static CLCalc.Program.Variable CLp;
        private static CLCalc.Program.Variable CLScale;


        private static void InitKernels()
        {
            string s = new CLFFTSrc().s;
            CLCalc.InitCL();
            try
            {
                CLCalc.Program.Compile(s);
            }
            catch
            {
            }
            kernelfft_radix16 = new CLCalc.Program.Kernel("fft_radix16");
            kernelfft_radix4 = new CLCalc.Program.Kernel("fft_radix4");
            kernelConjugate = new CLCalc.Program.Kernel("Conjugate");
            CLp = new CLCalc.Program.Variable(new int[1]);
        }
        #region Radix-16

        /// <summary>Computes the Discrete Fourier Transform of a float2 vector x whose length is a power of 16. 
        /// x = { Re[x0] Im[x0] Re[x1] Im[x1] ... Re[xn] Im[xn] }, n = power of 16 (Length = 2*pow(16,n))</summary>
        public static CLCalc.Program.Variable FFT16(ref CLCalc.Program.Variable CLx)
        {
            if (CLy == null || CLy.OriginalVarLength != CLx.OriginalVarLength) CLy = new CLCalc.Program.Variable(new float[CLx.OriginalVarLength]);

            if (CLCalc.CLAcceleration != CLCalc.CLAccelerationType.UsingCL) return null;

            int nn = (int)Math.Log(CLx.OriginalVarLength >> 1, 16);
            nn = 1 << ((nn << 2) + 1);
            if (nn != CLx.OriginalVarLength) throw new Exception("Number of elements should be a power of 16 ( vector length should be 2*pow(16,n) )");

            if (kernelfft_radix16 == null)
            {
                InitKernels();
            }

            CLCalc.Program.Variable[] args = new CLCalc.Program.Variable[] { CLx, CLy, CLp };
            CLCalc.Program.Variable[] args2 = new CLCalc.Program.Variable[] { CLy, CLx, CLp };
            bool usar2 = true;

            int[] p = new int[] { 1 };
            CLp.WriteToDevice(p);
            int n = CLx.OriginalVarLength >> 5;

            while (p[0] <= n)
            {
                usar2 = !usar2;
                if (usar2)
                    kernelfft_radix16.Execute(args2, n);
                else
                    kernelfft_radix16.Execute(args, n);

                p[0] = p[0] << 4;
                CLp.WriteToDevice(p);

            }

            if (usar2)
            {
                CLCalc.Program.Variable temp = CLx;
                CLx = CLy; CLy = temp;
            }

            return CLy;
        }

        /// <summary>Computes the Discrete Fourier Transform of a float2 vector x whose length is a power of 16. 
        /// x = { Re[x0] Im[x0] Re[x1] Im[x1] ... Re[xn] Im[xn] }, n = power of 16 (Length = 2*pow(16,n))</summary>
        public static float[] FFT16(float[] x)
        {

            if (CLx == null || CLx.OriginalVarLength != x.Length)
            {
                CLx = new CLCalc.Program.Variable(x);
                CLy = new CLCalc.Program.Variable(x);
            }

            //Writes original content
            CLx.WriteToDevice(x);

            CLy = FFT16(ref CLx);


            float[] y = new float[x.Length];
            CLy.ReadFromDeviceTo(y);
            return y;
        }

        /// <summary>Computes the inverse Discrete Fourier Transform of a float2 vector x whose length is a power of 16. 
        /// x = { Re[x0] Im[x0] Re[x1] Im[x1] ... Re[xn] Im[xn] }, n = power of 16 (Length = 2*pow(16,n))</summary>
        public static float[] iFFT16(float[] x)
        {
            if (CLx == null || CLx.OriginalVarLength != x.Length)
            {
                CLx = new CLCalc.Program.Variable(x);
                CLy = new CLCalc.Program.Variable(x);
            }

            //Writes original content
            CLx.WriteToDevice(x);

            CLy = iFFT16(CLx);

            float[] y = new float[x.Length];
            CLy.ReadFromDeviceTo(y);

            return y;
        }

        /// <summary>Computes the inverse Discrete Fourier Transform of a float2 vector x whose length is a power of 16. 
        /// x = { Re[x0] Im[x0] Re[x1] Im[x1] ... Re[xn] Im[xn] }, n = power of 16 (Length = 2*pow(16,n))</summary>
        public static CLCalc.Program.Variable iFFT16(CLCalc.Program.Variable CLx)
        {
            if (CLScale == null) CLScale = new CLCalc.Program.Variable(new float[1]);

            //Trick: DFT-1 (x) = DFT(x*)*/N;
            //Conjugate
            float[] vx = new float[CLx.OriginalVarLength];
            CLx.ReadFromDeviceTo(vx);

            float[] scale = new float[] { 1 };
            CLScale.WriteToDevice(scale);
            kernelConjugate.Execute(new CLCalc.Program.Variable[] { CLx, CLScale }, CLx.OriginalVarLength >> 1);
            CLx.ReadFromDeviceTo(vx);

            CLy = FFT16(ref CLx);

            scale[0] = 1 / (float)(CLx.OriginalVarLength >> 1);
            CLScale.WriteToDevice(scale);
            kernelConjugate.Execute(new CLCalc.Program.Variable[] { CLy, CLScale }, CLy.OriginalVarLength >> 1);
            return CLy;
        }


        #endregion

        #region Radix-4

        /// <summary>Computes the Discrete Fourier Transform of a float2 vector x whose length is a power of 4. 
        /// x = { Re[x0] Im[x0] Re[x1] Im[x1] ... Re[xn] Im[xn] }, n = power of 4 (Length = 2*pow(4,n))</summary>
        public static CLCalc.Program.Variable FFT4(ref CLCalc.Program.Variable CLx)
        {
            if (CLy == null || CLy.OriginalVarLength != CLx.OriginalVarLength) CLy = new CLCalc.Program.Variable(new float[CLx.OriginalVarLength]);

            if (CLCalc.CLAcceleration != CLCalc.CLAccelerationType.UsingCL) return null;

            int nn = (int)Math.Log(CLx.OriginalVarLength >> 1, 4);
            nn = 1 << ((nn << 1) + 1);
            if (nn != CLx.OriginalVarLength) throw new Exception("Number of elements should be a power of 4 ( vector length should be 2*pow(4,n) )");

            if (kernelfft_radix4 == null)
            {
                InitKernels();
            }

            CLCalc.Program.Variable[] args = new CLCalc.Program.Variable[] { CLx, CLy, CLp };
            CLCalc.Program.Variable[] args2 = new CLCalc.Program.Variable[] { CLy, CLx, CLp };
            bool usar2 = true;

            int[] p = new int[] { 1 };
            CLp.WriteToDevice(p);
            int n = CLx.OriginalVarLength >> 3;

            while (p[0] <= n)
            {
                usar2 = !usar2;
                if (usar2)
                    kernelfft_radix4.Execute(args2, n);
                else
                    kernelfft_radix4.Execute(args, n);

                p[0] = p[0] << 2;
                CLp.WriteToDevice(p);

            }

            if (usar2)
            {
                CLCalc.Program.Variable temp = CLx;
                CLx = CLy; CLy = temp;
            }

            return CLy;
        }

        /// <summary>Computes the Discrete Fourier Transform of a float2 vector x whose length is a power of 4. 
        /// x = { Re[x0] Im[x0] Re[x1] Im[x1] ... Re[xn] Im[xn] }, n = power of 4 (Length = 2*pow(4,n))</summary>
        public static float[] FFT4(float[] x)
        {
            if (CLx == null || CLx.OriginalVarLength != x.Length)
            {
                CLx = new CLCalc.Program.Variable(x);
                CLy = new CLCalc.Program.Variable(x);
            }

            //Writes original content
            CLx.WriteToDevice(x);


            CLy = FFT4(ref CLx);

            float[] y = new float[x.Length];
            CLy.ReadFromDeviceTo(y);

            return y;
        }

        /// <summary>Computes the inverse Discrete Fourier Transform of a float2 vector x whose length is a power of 4. 
        /// x = { Re[x0] Im[x0] Re[x1] Im[x1] ... Re[xn] Im[xn] }, n = power of 4 (Length = 2*pow(4,n))</summary>
        public static float[] iFFT4(float[] x)
        {
            if (CLx == null || CLx.OriginalVarLength != x.Length)
            {
                CLx = new CLCalc.Program.Variable(x);
                CLy = new CLCalc.Program.Variable(x);
            }

            //Writes original content
            CLx.WriteToDevice(x);

            CLy = iFFT4(CLx);

            float[] y = new float[x.Length];
            CLy.ReadFromDeviceTo(y);

            return y;
        }

        /// <summary>Computes the inverse Discrete Fourier Transform of a float2 vector x whose length is a power of 4. 
        /// x = { Re[x0] Im[x0] Re[x1] Im[x1] ... Re[xn] Im[xn] }, n = power of 4 (Length = 2*pow(4,n))</summary>
        public static CLCalc.Program.Variable iFFT4(CLCalc.Program.Variable CLx)
        {
            if (CLScale == null) CLScale = new CLCalc.Program.Variable(new float[1]);

            //Trick: DFT-1 (x) = DFT(x*)*/N;
            //Conjugate
            float[] vx = new float[CLx.OriginalVarLength];
            CLx.ReadFromDeviceTo(vx);

            float[] scale = new float[] { 1 };
            CLScale.WriteToDevice(scale);
            kernelConjugate.Execute(new CLCalc.Program.Variable[] { CLx, CLScale }, CLx.OriginalVarLength >> 1);
            CLx.ReadFromDeviceTo(vx);

            CLy = FFT4(ref CLx);

            scale[0] = 1 / (float)(CLx.OriginalVarLength >> 1);
            CLScale.WriteToDevice(scale);
            kernelConjugate.Execute(new CLCalc.Program.Variable[] { CLy, CLScale }, CLy.OriginalVarLength >> 1);
            return CLy;
        }

        #endregion

    }

    /*
    #pragma OPENCL EXTENSION cl_khr_fp64 : enable
    */


    /// <summary>Computes Fast Fourier Transform of doubles</summary>
    public static class CLFFTdouble
    {
        #region References
        /*
         * OpenCL Fast Fourier Transform
           Eric Bainville - May 2010
         * http://www.bealto.com/gpu-fft_opencl-1.html
         * 
         * 
         * Discrete Fourier transform basic theory
         * http://en.wikipedia.org/wiki/Discrete_Fourier_transform
         */
        #endregion

        #region OpenCL source
        /// <summary>Fast Fourier Transform source code</summary>
        private class CLFFTSrc
        {
            public string s = @"
#pragma OPENCL EXTENSION cl_khr_fp64 : enable

//Global size is x.Length/2, Scale = 1 for direct, 1/N to inverse (iFFT)
__kernel void Conjugate(__global double2 * x, __global const double * Scale)
{
   int i = get_global_id(0);
   
   double temp = Scale[0];
   double2 t = (double2)(temp, -temp);

   x[i] *= t;
}


// Return a*EXP(-I*PI*1/2) = a*(-I)
double2 mul_p1q2(double2 a) { return (double2)(a.y,-a.x); }

// Return a^2
double2 sqr_1(double2 a)
{ return (double2)(a.x*a.x-a.y*a.y,2.0f*a.x*a.y); }

// Return the 2x DFT2 of the four complex numbers in A
// If A=(a,b,c,d) then return (a',b',c',d') where (a',c')=DFT2(a,c)
// and (b',d')=DFT2(b,d).
double8 dft2_4(double8 a) { return (double8)(a.lo+a.hi,a.lo-a.hi); }

// Return the DFT of 4 complex numbers in A
double8 dft4_4(double8 a)
{
  // 2x DFT2
  double8 x = dft2_4(a);
  // Shuffle, twiddle, and 2x DFT2
  return dft2_4((double8)(x.lo.lo,x.hi.lo,x.lo.hi,mul_p1q2(x.hi.hi)));
}

// Complex product, multiply vectors of complex numbers

#define MUL_RE(a,b) (a.even*b.even - a.odd*b.odd)
#define MUL_IM(a,b) (a.even*b.odd + a.odd*b.even)

double2 mul_1(double2 a,double2 b)
{ double2 x; x.even = MUL_RE(a,b); x.odd = MUL_IM(a,b); return x; }

double4 mul_2(double4 a,double4 b)
{ double4 x; x.even = MUL_RE(a,b); x.odd = MUL_IM(a,b); return x; }

// Return the DFT2 of the two complex numbers in vector A
double4 dft2_2(double4 a) { return (double4)(a.lo+a.hi,a.lo-a.hi); }

// Return cos(alpha)+I*sin(alpha)  (3 variants)
double2 exp_alpha_1(double alpha)
{
  double cs,sn;
  // sn = sincos(alpha,&cs);  // sincos
  //cs = native_cos(alpha); sn = native_sin(alpha);  // native sin+cos
  cs = cos(alpha); sn = sin(alpha); // sin+cos
  return (double2)(cs,sn);
}


// T = N/4 = number of threads.
// P is the length of input sub-sequences, 1,4,16,...,N/4.
__kernel void fft_radix4(__global const double2 * x,__global double2 * y,__global int * pp)
{
  int p = pp[0];
  int t = get_global_size(0); // number of threads
  int i = get_global_id(0); // current thread
  int k = i & (p-1); // index in input sequence, in 0..P-1
  // Inputs indices are I+{0,1,2,3}*T
  x += i;
  // Output indices are J+{0,1,2,3}*P, where
  // J is I with two 0 bits inserted at bit log2(P)
  y += ((i-k)<<2) + k;

  // Load and twiddle inputs
  // Twiddling factors are exp(_I*PI*{0,1,2,3}*K/2P)
  double alpha = -M_PI*(double)k/(double)(2*p);

// Load and twiddle
double2 u0 = x[0];
double2 u1 = mul_1(exp_alpha_1(alpha),x[t]);
double2 u2 = mul_1(exp_alpha_1(2*alpha),x[2*t]);
double2 u3 = mul_1(exp_alpha_1(3*alpha),x[3*t]);

// 2x DFT2 and twiddle
double2 v0 = u0 + u2;
double2 v1 = u0 - u2;
double2 v2 = u1 + u3;
double2 v3 = mul_p1q2(u1 - u3); // twiddle

// 2x DFT2 and store
y[0] = v0 + v2;
y[p] = v1 + v3;
y[2*p] = v0 - v2;
y[3*p] = v1 - v3;

}



// mul_p*q*(a) returns a*EXP(-I*PI*P/Q)
#define mul_p0q1(a) (a)

#define mul_p0q2 mul_p0q1
//double2  mul_p1q2(double2 a) { return (double2)(a.y,-a.x); }

__constant double SQRT_1_2 = 0.707106781186548; // cos(Pi/4)
#define mul_p0q4 mul_p0q2
double2  mul_p1q4(double2 a) { return (double2)(SQRT_1_2)*(double2)(a.x+a.y,-a.x+a.y); }
#define mul_p2q4 mul_p1q2
double2  mul_p3q4(double2 a) { return (double2)(SQRT_1_2)*(double2)(-a.x+a.y,-a.x-a.y); }

__constant double COS_8 = 0.923879532511287; // cos(Pi/8)
__constant double SIN_8 = 0.382683432365089; // sin(Pi/8)


#define mul_p0q8 mul_p0q4
double2  mul_p1q8(double2 a) { return mul_1((double2)(COS_8,-SIN_8),a); }
#define mul_p2q8 mul_p1q4
double2  mul_p3q8(double2 a) { return mul_1((double2)(SIN_8,-COS_8),a); }
#define mul_p4q8 mul_p2q4
double2  mul_p5q8(double2 a) { return mul_1((double2)(-SIN_8,-COS_8),a); }
#define mul_p6q8 mul_p3q4
double2  mul_p7q8(double2 a) { return mul_1((double2)(-COS_8,-SIN_8),a); }

// Compute in-place DFT2 and twiddle
#define DFT2_TWIDDLE(a,b,t) { double2 tmp = t(a-b); a += b; b = tmp; }

// T = N/16 = number of threads.
// P is the length of input sub-sequences, 1,16,256,...,N/16.
__kernel void fft_radix16(__global const double2 * x,__global double2 * y, __global int * pp)
{
  int p = pp[0];
  int t = get_global_size(0); // number of threads
  int i = get_global_id(0); // current thread


//////  y[i] = 2*x[i];
//////  return;

  int k = i & (p-1); // index in input sequence, in 0..P-1
  // Inputs indices are I+{0,..,15}*T
  x += i;
  // Output indices are J+{0,..,15}*P, where
  // J is I with four 0 bits inserted at bit log2(P)
  y += ((i-k)<<4) + k;

  // Load
  double2 u[16];
  for (int m=0;m<16;m++) u[m] = x[m*t];

  // Twiddle, twiddling factors are exp(_I*PI*{0,..,15}*K/4P)
  double alpha = -M_PI*(double)k/(double)(8*p);
  for (int m=1;m<16;m++) u[m] = mul_1(exp_alpha_1(m*alpha),u[m]);

  // 8x in-place DFT2 and twiddle (1)
  DFT2_TWIDDLE(u[0],u[8],mul_p0q8);
  DFT2_TWIDDLE(u[1],u[9],mul_p1q8);
  DFT2_TWIDDLE(u[2],u[10],mul_p2q8);
  DFT2_TWIDDLE(u[3],u[11],mul_p3q8);
  DFT2_TWIDDLE(u[4],u[12],mul_p4q8);
  DFT2_TWIDDLE(u[5],u[13],mul_p5q8);
  DFT2_TWIDDLE(u[6],u[14],mul_p6q8);
  DFT2_TWIDDLE(u[7],u[15],mul_p7q8);

  // 8x in-place DFT2 and twiddle (2)
  DFT2_TWIDDLE(u[0],u[4],mul_p0q4);
  DFT2_TWIDDLE(u[1],u[5],mul_p1q4);
  DFT2_TWIDDLE(u[2],u[6],mul_p2q4);
  DFT2_TWIDDLE(u[3],u[7],mul_p3q4);
  DFT2_TWIDDLE(u[8],u[12],mul_p0q4);
  DFT2_TWIDDLE(u[9],u[13],mul_p1q4);
  DFT2_TWIDDLE(u[10],u[14],mul_p2q4);
  DFT2_TWIDDLE(u[11],u[15],mul_p3q4);

  // 8x in-place DFT2 and twiddle (3)
  DFT2_TWIDDLE(u[0],u[2],mul_p0q2);
  DFT2_TWIDDLE(u[1],u[3],mul_p1q2);
  DFT2_TWIDDLE(u[4],u[6],mul_p0q2);
  DFT2_TWIDDLE(u[5],u[7],mul_p1q2);
  DFT2_TWIDDLE(u[8],u[10],mul_p0q2);
  DFT2_TWIDDLE(u[9],u[11],mul_p1q2);
  DFT2_TWIDDLE(u[12],u[14],mul_p0q2);
  DFT2_TWIDDLE(u[13],u[15],mul_p1q2);

  // 8x DFT2 and store (reverse binary permutation)
  y[0]    = u[0]  + u[1];
  y[p]    = u[8]  + u[9];
  y[2*p]  = u[4]  + u[5];
  y[3*p]  = u[12] + u[13];
  y[4*p]  = u[2]  + u[3];
  y[5*p]  = u[10] + u[11];
  y[6*p]  = u[6]  + u[7];
  y[7*p]  = u[14] + u[15];
  y[8*p]  = u[0]  - u[1];
  y[9*p]  = u[8]  - u[9];
  y[10*p] = u[4]  - u[5];
  y[11*p] = u[12] - u[13];
  y[12*p] = u[2]  - u[3];
  y[13*p] = u[10] - u[11];
  y[14*p] = u[6]  - u[7];
  y[15*p] = u[14] - u[15];
}


";
        }
        #endregion

        /// <summary>Radix FFT kernel</summary>
        private static CLCalc.Program.Kernel kernelfft_radix16, kernelfft_radix4, kernelConjugate;
        private static CLCalc.Program.Variable CLx;
        private static CLCalc.Program.Variable CLy;
        private static CLCalc.Program.Variable CLp;
        private static CLCalc.Program.Variable CLScale;


        private static void InitKernels()
        {
            string s = new CLFFTSrc().s;
            CLCalc.InitCL();
            try
            {
                CLCalc.Program.Compile(s);
            }
            catch
            {
            }
            kernelfft_radix16 = new CLCalc.Program.Kernel("fft_radix16");
            kernelfft_radix4 = new CLCalc.Program.Kernel("fft_radix4");
            kernelConjugate = new CLCalc.Program.Kernel("Conjugate");
            CLp = new CLCalc.Program.Variable(new int[1]);
        }
        #region Radix-16

        /// <summary>Computes the Discrete Fourier Transform of a double2 vector x whose length is a power of 16. 
        /// x = { Re[x0] Im[x0] Re[x1] Im[x1] ... Re[xn] Im[xn] }, n = power of 16 (Length = 2*pow(16,n))</summary>
        public static CLCalc.Program.Variable FFT16(ref CLCalc.Program.Variable CLx)
        {
            if (CLy == null || CLy.OriginalVarLength != CLx.OriginalVarLength) CLy = new CLCalc.Program.Variable(new float[CLx.OriginalVarLength]);

            if (CLCalc.CLAcceleration != CLCalc.CLAccelerationType.UsingCL) return null;

            int nn = (int)Math.Log(CLx.OriginalVarLength >> 1, 16);
            nn = 1 << ((nn << 2) + 1);
            if (nn != CLx.OriginalVarLength) throw new Exception("Number of elements should be a power of 16 ( vector length should be 2*pow(16,n) )");

            if (kernelfft_radix16 == null)
            {
                InitKernels();
            }

            CLCalc.Program.Variable[] args = new CLCalc.Program.Variable[] { CLx, CLy, CLp };
            CLCalc.Program.Variable[] args2 = new CLCalc.Program.Variable[] { CLy, CLx, CLp };
            bool usar2 = true;

            int[] p = new int[] { 1 };
            CLp.WriteToDevice(p);
            int n = CLx.OriginalVarLength >> 5;

            while (p[0] <= n)
            {
                usar2 = !usar2;
                if (usar2)
                    kernelfft_radix16.Execute(args2, n);
                else
                    kernelfft_radix16.Execute(args, n);

                p[0] = p[0] << 4;
                CLp.WriteToDevice(p);

            }

            if (usar2)
            {
                CLCalc.Program.Variable temp = CLx;
                CLx = CLy; CLy = temp;
            }

            return CLy;
        }

        /// <summary>Computes the Discrete Fourier Transform of a double2 vector x whose length is a power of 16. 
        /// x = { Re[x0] Im[x0] Re[x1] Im[x1] ... Re[xn] Im[xn] }, n = power of 16 (Length = 2*pow(16,n))</summary>
        public static double[] FFT16(double[] x)
        {

            if (CLx == null || CLx.OriginalVarLength != x.Length)
            {
                CLx = new CLCalc.Program.Variable(x);
                CLy = new CLCalc.Program.Variable(x);
            }

            //Writes original content
            CLx.WriteToDevice(x);

            CLy = FFT16(ref CLx);


            double[] y = new double[x.Length];
            CLy.ReadFromDeviceTo(y);
            return y;
        }

        /// <summary>Computes the inverse Discrete Fourier Transform of a double2 vector x whose length is a power of 16. 
        /// x = { Re[x0] Im[x0] Re[x1] Im[x1] ... Re[xn] Im[xn] }, n = power of 16 (Length = 2*pow(16,n))</summary>
        public static double[] iFFT16(double[] x)
        {
            if (CLx == null || CLx.OriginalVarLength != x.Length)
            {
                CLx = new CLCalc.Program.Variable(x);
                CLy = new CLCalc.Program.Variable(x);
            }

            //Writes original content
            CLx.WriteToDevice(x);

            CLy = iFFT16(CLx);

            double[] y = new double[x.Length];
            CLy.ReadFromDeviceTo(y);

            return y;
        }

        /// <summary>Computes the inverse Discrete Fourier Transform of a double2 vector x whose length is a power of 16. 
        /// x = { Re[x0] Im[x0] Re[x1] Im[x1] ... Re[xn] Im[xn] }, n = power of 16 (Length = 2*pow(16,n))</summary>
        public static CLCalc.Program.Variable iFFT16(CLCalc.Program.Variable CLx)
        {
            if (CLScale == null) CLScale = new CLCalc.Program.Variable(new double[1]);

            //Trick: DFT-1 (x) = DFT(x*)*/N;
            //Conjugate
            double[] vx = new double[CLx.OriginalVarLength];
            CLx.ReadFromDeviceTo(vx);

            double[] scale = new double[] { 1 };
            CLScale.WriteToDevice(scale);
            kernelConjugate.Execute(new CLCalc.Program.Variable[] { CLx, CLScale }, CLx.OriginalVarLength >> 1);
            CLx.ReadFromDeviceTo(vx);

            CLy = FFT16(ref CLx);

            scale[0] = 1 / (double)(CLx.OriginalVarLength >> 1);
            CLScale.WriteToDevice(scale);
            kernelConjugate.Execute(new CLCalc.Program.Variable[] { CLy, CLScale }, CLy.OriginalVarLength >> 1);
            return CLy;
        }


        #endregion

        #region Radix-4

        /// <summary>Computes the Discrete Fourier Transform of a double2 vector x whose length is a power of 4. 
        /// x = { Re[x0] Im[x0] Re[x1] Im[x1] ... Re[xn] Im[xn] }, n = power of 4 (Length = 2*pow(4,n))</summary>
        public static CLCalc.Program.Variable FFT4(ref CLCalc.Program.Variable CLx)
        {
            if (CLy == null || CLy.OriginalVarLength != CLx.OriginalVarLength) CLy = new CLCalc.Program.Variable(new float[CLx.OriginalVarLength]);

            if (CLCalc.CLAcceleration != CLCalc.CLAccelerationType.UsingCL) return null;

            int nn = (int)Math.Log(CLx.OriginalVarLength >> 1, 4);
            nn = 1 << ((nn << 1) + 1);
            if (nn != CLx.OriginalVarLength) throw new Exception("Number of elements should be a power of 4 ( vector length should be 2*pow(4,n) )");

            if (kernelfft_radix4 == null)
            {
                InitKernels();
            }

            CLCalc.Program.Variable[] args = new CLCalc.Program.Variable[] { CLx, CLy, CLp };
            CLCalc.Program.Variable[] args2 = new CLCalc.Program.Variable[] { CLy, CLx, CLp };
            bool usar2 = true;

            int[] p = new int[] { 1 };
            CLp.WriteToDevice(p);
            int n = CLx.OriginalVarLength >> 3;

            while (p[0] <= n)
            {
                usar2 = !usar2;
                if (usar2)
                    kernelfft_radix4.Execute(args2, n);
                else
                    kernelfft_radix4.Execute(args, n);

                p[0] = p[0] << 2;
                CLp.WriteToDevice(p);

            }

            if (usar2)
            {
                CLCalc.Program.Variable temp = CLx;
                CLx = CLy; CLy = temp;
            }
            
            return CLy;
        }

        /// <summary>Computes the Discrete Fourier Transform of a double2 vector x whose length is a power of 4. 
        /// x = { Re[x0] Im[x0] Re[x1] Im[x1] ... Re[xn] Im[xn] }, n = power of 4 (Length = 2*pow(4,n))</summary>
        public static double[] FFT4(double[] x)
        {
            if (CLx == null || CLx.OriginalVarLength != x.Length)
            {
                CLx = new CLCalc.Program.Variable(x);
                CLy = new CLCalc.Program.Variable(x);
            }

            //Writes original content
            CLx.WriteToDevice(x);


            CLy = FFT4(ref CLx);

            double[] y = new double[x.Length];
            CLy.ReadFromDeviceTo(y);

            return y;
        }

        /// <summary>Computes the inverse Discrete Fourier Transform of a double2 vector x whose length is a power of 4. 
        /// x = { Re[x0] Im[x0] Re[x1] Im[x1] ... Re[xn] Im[xn] }, n = power of 4 (Length = 2*pow(4,n))</summary>
        public static double[] iFFT4(double[] x)
        {
            if (CLx == null || CLx.OriginalVarLength != x.Length)
            {
                CLx = new CLCalc.Program.Variable(x);
                CLy = new CLCalc.Program.Variable(x);
            }

            //Writes original content
            CLx.WriteToDevice(x);

            CLy = iFFT4(CLx);

            double[] y = new double[x.Length];
            CLy.ReadFromDeviceTo(y);

            return y;
        }

        /// <summary>Computes the inverse Discrete Fourier Transform of a double2 vector x whose length is a power of 4. 
        /// x = { Re[x0] Im[x0] Re[x1] Im[x1] ... Re[xn] Im[xn] }, n = power of 4 (Length = 2*pow(4,n))</summary>
        public static CLCalc.Program.Variable iFFT4(CLCalc.Program.Variable CLx)
        {
            if (CLScale == null) CLScale = new CLCalc.Program.Variable(new double[1]);

            //Trick: DFT-1 (x) = DFT(x*)*/N;
            //Conjugate
            double[] vx = new double[CLx.OriginalVarLength];
            CLx.ReadFromDeviceTo(vx);

            double[] scale = new double[] { 1 };
            CLScale.WriteToDevice(scale);
            kernelConjugate.Execute(new CLCalc.Program.Variable[] { CLx, CLScale }, CLx.OriginalVarLength >> 1);
            CLx.ReadFromDeviceTo(vx);

            CLy = FFT4(ref CLx);

            scale[0] = 1 / (double)(CLx.OriginalVarLength >> 1);
            CLScale.WriteToDevice(scale);
            kernelConjugate.Execute(new CLCalc.Program.Variable[] { CLy, CLScale }, CLy.OriginalVarLength >> 1);
            return CLy;
        }

        #endregion

    }
}

