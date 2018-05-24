using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenCLTemplate;

namespace OpenCLTemplate.LinearAlgebra
{
    public partial class floatLinalg
    {
        #region OpenCL Source

        private class LinalgSrc
        {
            #region Block cholesky source
            public string srcBlockCholesky = @"

//http://www.netlib.org/utk/papers/factor/node9.html

#define SUBMATRIXSIZE CONSTSUBMATRIXSIZE
#define GLOBALMSIZE   CONSTGLOBALSIZE

__kernel void CholeskyDiagBlock
             (__global float * cholDec,
              __global int   * diagIdx,
              __global float * L11inv)
{
    int id = get_local_id(0);

    //Computes local column and row
    int temp = (int)floor(sqrt(1.0f + (id << 3)));
    int ii = diagIdx[0];

    int p = (temp - 1) >> 1;
    int q = id - ((p * (p + 1)) >> 1);

    //This is necessary to maintain stability of index retrieval
    if (q > p)
    {
       p++;
       q = id - ((p * (p + 1)) >> 1);
    }
    else if (q < 0)
    {
       p--;
       q = id - ((p * (p + 1)) >> 1);
    }

    //fetches appropriate element to my position
    int iLoc = p + ii;
    int jLoc = q + ii;

    int idGlob = ((iLoc * (iLoc + 1)) >> 1) + jLoc;

    __local float LocalL[GLOBALMSIZE];
    __local float LInv[GLOBALMSIZE];

    __local int PivotIdx[1];

    //Copies elements
    LocalL[id] = cholDec[idGlob];
    barrier(CLK_LOCAL_MEM_FENCE);
    
    //iterates
    for (int k = 0; k < SUBMATRIXSIZE; k++)
    {
        //pivot
        if (k == p && p == q) 
        {
            LocalL[id]  = sqrt(LocalL[id]);
            PivotIdx[0] = id; 
        }
        barrier(CLK_LOCAL_MEM_FENCE);    
        
        //scale column
        if (p > k && q == k) LocalL[id] /= LocalL[PivotIdx[0]];
        barrier(CLK_LOCAL_MEM_FENCE);    
        
        //propagates
        if (p > k && q > k)
        {
           LocalL[id] -= LocalL[((p * (p + 1)) >> 1) + k]*LocalL[((q * (q + 1)) >> 1) + k];
        }
        barrier(CLK_LOCAL_MEM_FENCE);    
    }
    cholDec[idGlob] = LocalL[id];

    //need to invert LocalL
    //Construct identity locally which will hold inverse
    LInv[id] = p==q? 1.0f : 0.0f;
    barrier(CLK_LOCAL_MEM_FENCE);

    for (int k = 0; k < SUBMATRIXSIZE; k++)
    {
        //divide current row by main entry
        if (p == k && q <= k) 
        {
           LInv[id]  /= LocalL[((k * (k + 1)) >> 1) + k];
        }
        barrier(CLK_LOCAL_MEM_FENCE);
        
        //propagates to previous rows
        if (p > k && q <= k)
        {
           LInv[id] -= LocalL[((p * (p + 1)) >> 1) + k]*LInv[((k * (k + 1)) >> 1) + q];
        }
        barrier(CLK_LOCAL_MEM_FENCE);
    }
    

    //Copies elements back
    L11inv[id] = LInv[id];  

    //Update which is next block entry
    if (id == 0) diagIdx[0] += SUBMATRIXSIZE;
}

//global_size = {SUBMATRIXSIZE*numRows, SUBMATRIXSIZE}
//local_size =  {SUBMATRIXSIZE        , SUBMATRIXSIZE}


__kernel void CholeskyComputePanel
             (__global   float * cholDec,
              __constant int   * diagIdx,
              __constant float * L11inv)
{
    int p = get_local_id(0);
    int q = get_local_id(1);
    
    int globP = p + diagIdx[0] + get_group_id(0)*SUBMATRIXSIZE;
    int globQ = q + diagIdx[0] - SUBMATRIXSIZE;
    int globId = ((globP * (globP + 1)) >> 1) + globQ;
    
    //local copy of submatrix
    __local float A[SUBMATRIXSIZE][SUBMATRIXSIZE];
    A[p][q] = cholDec[globId];
    barrier(CLK_LOCAL_MEM_FENCE);
    
    //computes product A21*inv(L11')
    float temp = 0.0f;
    for (int k = 0; k < SUBMATRIXSIZE; k++)
    {
        //Apk * inv(L11') kq = Apk*invL11qk
        temp+=A[p][k]* (q>=k ?
                        L11inv[((q * (q + 1)) >> 1) + k]:
                        0   );
    }

    cholDec[globId] = temp;
    barrier(CLK_LOCAL_MEM_FENCE);

    //A becomes L21
    A[p][q] = temp;
    barrier(CLK_LOCAL_MEM_FENCE);

    //Since we're here, update diagonals of Cholesky decomposition
    //This eliminates the need to fetch a bunch of floats
    globQ = q + diagIdx[0] + get_group_id(0)*SUBMATRIXSIZE;
    globId = ((globP * (globP + 1)) >> 1) + globQ;    

    //Need to compute L21*L21', from A which is now L21
    temp = 0.0f;
    for (int k = 0; k < SUBMATRIXSIZE; k++)
    {
        temp += A[p][k]*A[q][k];
    }
    barrier(CLK_LOCAL_MEM_FENCE);

    //Done, update
    if (globP >= globQ) cholDec[globId] -= temp;
}


//global size = {numSubmatrices*SUBMATRIXSIZE, SUBMATRIXSIZE}
//global size = {numSubmatrices*SUBMATRIXSIZE, SUBMATRIXSIZE}
__kernel void CholeskyForwardProp
             (__global   float * cholDec,
              __constant int   * diagIdx,
              __constant float * L11inv)
{
    int p = get_local_id(0);
    int q = get_local_id(1);
    
    int groupId = get_group_id(0);
    
    //Computes local submatrix to update, I'm updating subM[P,Q] of dimension SUBMATRIXSIZExSUBMATRIXSIZE
    int temp = (int)floor(sqrt(1.0f + (groupId << 3)));
    int ii = diagIdx[0];

    int P = (temp - 1) >> 1;
    int Q = groupId - ((P * (P + 1)) >> 1);

    //This is necessary to maintain stability of index retrieval
    if (Q > P)
    {
       P++;
       Q = groupId - ((P * (P + 1)) >> 1);
    }
    else if (Q < 0)
    {
       P--;
       Q = groupId - ((P * (P + 1)) >> 1);
    }
    
    //Computes global pq. The submatrices start at [diagIdx[0]+SUBMATRIXSIZE diagIdx[0]]
    int A22p = p + P*SUBMATRIXSIZE + ii + SUBMATRIXSIZE;
    int A22q = q + Q*SUBMATRIXSIZE + ii;
    
    //Copies elements from L21 to local memory
    //I need submatrices L21 corresponding to P and to Q
    __local float L21P[SUBMATRIXSIZE][SUBMATRIXSIZE];
    __local float L21Q[SUBMATRIXSIZE][SUBMATRIXSIZE];
    
    int globP = p + ii + P*SUBMATRIXSIZE + SUBMATRIXSIZE;
    int globQ = q + ii - SUBMATRIXSIZE;
    int globId = ((globP * (globP + 1)) >> 1) + globQ;
        
    L21P[p][q] = cholDec[globId];
    
    
    globP = p + ii + Q*SUBMATRIXSIZE;
    globQ = q + ii - SUBMATRIXSIZE;
    int globId2 = ((globP * (globP + 1)) >> 1) + globQ;
        
    L21Q[p][q] = cholDec[globId2];
    barrier(CLK_LOCAL_MEM_FENCE);
    
    //Computes L21P*L21Q'
    float temp2 = 0.0f;
    for (int k = 0; k < SUBMATRIXSIZE; k++)
    {
        temp2 += L21P[p][k]*L21Q[q][k];
    }

    //done, update


    cholDec[((A22p * (A22p + 1)) >> 1) + A22q] -= temp2;

    //DEBUG!!
    //cholDec[((A22p * (A22p + 1)) >> 1) + A22q] = L21Q[p][q];
    //cholDec[((A22p * (A22p + 1)) >> 1) + A22q] = 1000*(p + ii + P*SUBMATRIXSIZE+SUBMATRIXSIZE)+10*(q + ii - SUBMATRIXSIZE); //+L21P[p][q];
    //cholDec[((A22p * (A22p + 1)) >> 1) + A22q] = 1000*(p + ii + Q*SUBMATRIXSIZE)+10*(q + ii - SUBMATRIXSIZE); //+L21P[p][q];
    //cholDec[((A22p * (A22p + 1)) >> 1) + A22q] = 1000*globId; //+L21P[p][q];
    //cholDec[((A22p * (A22p + 1)) >> 1) + A22q] = 10*P+Q;
}      
      

";
            #endregion

            #region Backsubstitution

            public string srcBkSubs = @"

__kernel void FwdUpperBackSubs(__global const float * cholDec,
                               __global       float * y,
                               __global const float * b,
                               __constant     int   * offSet,
                               __constant     int * N)

{
   __local float xLoc[SUBMATRIXSIZE];
   
   int p = get_local_id(0);
   int globP = p + offSet[0];
   int j = get_global_id(1) * N[0];
   
   //x starts with RHS vector b.
   //Local copy
   xLoc[p] = b[globP+j];
   barrier(CLK_LOCAL_MEM_FENCE);   
   
   //this is the forward substitution
   for (int k = 0; k < SUBMATRIXSIZE; k++)
   {
       //pivot
       if (p == k)
       {
          xLoc[p] = xLoc[p] / cholDec[((globP * (globP + 1)) >> 1) + globP];
       }
       barrier(CLK_LOCAL_MEM_FENCE);
       
       //propagation
       if (p > k)
       {
          xLoc[p] -= xLoc[k] * cholDec[((globP * (globP + 1)) >> 1) + k + offSet[0]];
       }
       barrier(CLK_LOCAL_MEM_FENCE);

   }

   y[globP+j] = xLoc[p];
}


//Block BackSubs
__kernel void BkLowerBackSubs( __global const float * cholDec,
                               __global       float * y,
                               __global const float * b,
                               __constant     int   * offSet,
                               __constant     int * N)

{
   __local float xLoc[SUBMATRIXSIZE];
   
   int p = get_local_id(0);
   int globP = p + offSet[0];
   int j = get_global_id(1) * N[0];
      
   //x starts with RHS vector b.
   //Local copy
   xLoc[p] = b[globP+j];
   barrier(CLK_LOCAL_MEM_FENCE);   
   
   
   int locK;
   //this is the back substitution
   for (int k = get_local_size(0)-1; k >= 0; k--)
   {
       //pivot
       if (p == k)
       {
          xLoc[p] = xLoc[p] / cholDec[((globP * (globP + 1)) >> 1) + globP];
       }
       barrier(CLK_LOCAL_MEM_FENCE);
       
       //propagation
       if (p < k)
       {
          locK = k + offSet[0];
          xLoc[p] -= xLoc[k] * cholDec[((locK * (locK + 1)) >> 1) + globP];
       }
       barrier(CLK_LOCAL_MEM_FENCE);
   }

   y[globP+j] = xLoc[p];
}

__kernel void FwdPropag ( __global const float * cholDec,
                          __global       float * y,
                          __global       float * b,
                          __constant     int   * offSet,
                          __constant     int * N)

{
   int i = get_global_id(0) + offSet[0] + SUBMATRIXSIZE;
   int j = get_global_id(1) * N[0];

   int jLoc = get_local_id(1);

   __local float cholDecLoc[SUBMATRIXSIZE];
   cholDecLoc[jLoc] = cholDec[((i * (i + 1)) >> 1) + jLoc + offSet[0]];
   barrier(CLK_LOCAL_MEM_FENCE);
      
   float temp = 0.0f;
   for (int k = 0; k < SUBMATRIXSIZE; k++)
   {
       temp += cholDecLoc[k] * y[k + offSet[0]+j];
   }
   
   b[i+j] -= temp;
}

__kernel void FwdPropag2( __global const float * cholDec,
                          __global       float * y,
                          __global       float * b,
                          __constant     int   * offSet,
                          __constant     int * N)

{
   int i = get_global_id(0) + offSet[0] + SUBMATRIXSIZE;
   int j = get_global_id(1) * N[0];
      
   float temp = 0.0f;
   for (int k = 0; k < SUBMATRIXSIZE; k++)
   {
       temp += cholDec[((i * (i + 1)) >> 1) + k + offSet[0]] * y[k + offSet[0]+j];
   }
   
   b[i+j] -= temp;
}

__kernel void BackPropag2( __global const float * cholDec,
                           __global       float * y,
                           __global       float * b,
                           __constant     int   * offSet,
                           __constant     int * N)
{
   int i = get_global_id(0);
   int j = get_global_id(1) * N[0];
   
   float temp = 0.0f;
   int locK;
   for (int k = SUBMATRIXSIZE-1; k >= 0; k--)
   {
       locK = k + offSet[0];
       temp += cholDec[((locK * (locK + 1)) >> 1) + i] * y[locK+j];
   }
   
   b[i+j] -= temp;
}

__kernel void BackPropag ( __global const float * cholDec,
                           __global       float * y,
                           __global       float * b,
                           __constant     int   * offSet,
                           __constant     int * N)
{
   int i = get_global_id(0);
   int j = get_global_id(1) * N[0];
   
   int jLoc = get_local_id(1);
   int locK;

   __local float cholDecLoc[SUBMATRIXSIZE];
   locK = jLoc + offSet[0];
   cholDecLoc[jLoc] = cholDec[((locK * (locK + 1)) >> 1) + i];
   barrier(CLK_LOCAL_MEM_FENCE);

   float temp = 0.0f;
   for (int k = SUBMATRIXSIZE-1; k >= 0; k--)
   {
       locK = k + offSet[0];
       temp += cholDecLoc[k] * y[locK+j];
   }
   
   b[i+j] -= temp;
}

";

            public string srcBkSubsAnt = @"
__kernel void ComputeTerm(__global const float * cholDec,
                          __global       float * resp,
                          __global const float * b,
                          __constant     int   * offset)
{
   //worksize = 1
   int i = offset[0];
   resp[i] = b[i] / cholDec[((i * (i + 1)) >> 1) + i];
}

__kernel void UpdateBForward(__global const float * cholDec,
                             __global const float * y,
                             __global       float * b,
                             __constant     int   * offset)
{
   int i = offset[0];
   int j = get_global_id(0) + i + 1;
   b[j] -= cholDec[((j * (j + 1)) >> 1) + i] * y[i];
}

__kernel void UpdateYBackward(__global const float * cholDec,
                              __global const float * resp,
                              __global       float * y,
                              __constant     int   * offset)
{
   int i = offset[0];
   int j = get_global_id(0);
   y[j] -= cholDec[((i * (i + 1)) >> 1) + j] * resp[i];
}


//Block FwdSubs
//Computes the backsubstitution of the uppermost part of
//the matrix to solve Mx = b

//global_size = {SUBMATRIXSIZE}

__kernel void FwdUpperBackSubs(__global const float * cholDec,
                               __global       float * y,
                               __global const float * b,
                               __constant     int   * offSet)

{
   __local float xLoc[SUBMATRIXSIZE];
   
   int p = get_local_id(0);
   int globP = p + offSet[0];
   
   //x starts with RHS vector b.
   //Local copy
   xLoc[p] = b[globP];
   barrier(CLK_LOCAL_MEM_FENCE);   
   
   //this is the forward substitution
   for (int k = 0; k < SUBMATRIXSIZE; k++)
   {
       //pivot
       if (p == k)
       {
          xLoc[p] = xLoc[p] / cholDec[((globP * (globP + 1)) >> 1) + globP];
       }
       barrier(CLK_LOCAL_MEM_FENCE);
       
       //propagation
       if (p > k)
       {
          xLoc[p] -= xLoc[k] * cholDec[((globP * (globP + 1)) >> 1) + k + offSet[0]];
       }
       barrier(CLK_LOCAL_MEM_FENCE);

   }

   y[globP] = xLoc[p];
}


//Block BackSubs
__kernel void BkLowerBackSubs( __global const float * cholDec,
                               __global       float * y,
                               __global const float * b,
                               __constant     int   * offSet)

{
   __local float xLoc[SUBMATRIXSIZE];
   
   int p = get_local_id(0);
   int globP = p + offSet[0];
   
   //x starts with RHS vector b.
   //Local copy
   xLoc[p] = b[globP];
   barrier(CLK_LOCAL_MEM_FENCE);   
   
   
   int locK;
   //this is the back substitution
   for (int k = get_local_size(0)-1; k >= 0; k--)
   {
       //pivot
       if (p == k)
       {
          xLoc[p] = xLoc[p] / cholDec[((globP * (globP + 1)) >> 1) + globP];
       }
       barrier(CLK_LOCAL_MEM_FENCE);
       
       //propagation
       if (p < k)
       {
          locK = k + offSet[0];
          xLoc[p] -= xLoc[k] * cholDec[((locK * (locK + 1)) >> 1) + globP];
       }
       barrier(CLK_LOCAL_MEM_FENCE);
   }

   y[globP] = xLoc[p];
}

__kernel void FwdPropag ( __global const float * cholDec,
                          __global       float * y,
                          __global       float * b,
                          __constant     int   * offSet)

{
   int i = get_global_id(0) + offSet[0] + SUBMATRIXSIZE;
   
   float temp = 0.0f;
   for (int k = 0; k < SUBMATRIXSIZE; k++)
   {
       temp += cholDec[((i * (i + 1)) >> 1) + k + offSet[0]] * y[k + offSet[0]];
   }
   
   b[i] -= temp;
}

__kernel void BackPropag ( __global const float * cholDec,
                           __global       float * y,
                           __global       float * b,
                           __constant     int   * offSet)

{
   int i = get_global_id(0);
   
   float temp = 0.0f;
   int locK;
   for (int k = SUBMATRIXSIZE-1; k >= 0; k--)
   {
       locK = k + offSet[0];
       temp += cholDec[((locK * (locK + 1)) >> 1) + i] * y[locK];
   }
   
   b[i] -= temp;
}
";
            #endregion

            #region BLAS functions AtA operations and vector-matrix products
            public string srcOperations = @"

//Vector-symmetric matrix product
__kernel void SymMatrVecMultiply(__global const float * SymM,
                              __global const float * v,
                              __global       float * x)
{
   //global_size = n
   int n = get_global_size(0);
   int p = get_global_id(0);
   
   float val = 0;
   for (int k = 0; k < p; k++)
   {
       val += SymM[((p*(1+p))>>1)+k]*v[k];
   }
   for (int k = p; k < n; k++)
   {
       val += SymM[((k*(1+k))>>1)+p]*v[k];
   }
   
   x[p] = val;
}

//Matrix-symmetric matrix product
__kernel void SymMatrMatrMultiply(__global const float * SymM,
                                        __global const float * v,
                                        __global       float * x)
{
   //global_size = n
   int n = get_global_size(0);
   //int m = get_global_size(1);
   int j = get_global_id(1)*n;
   int p = get_global_id(0);
   
   float val = 0;
   for (int k = 0; k < p; k++)
   {
       val += SymM[((p*(1+p))>>1)+k]*v[k+j];
   }
   for (int k = p; k < n; k++)
   {
       val += SymM[((k*(1+k))>>1)+p]*v[k+j];
   }
   
   x[p+j] = val;
}

//Computes A x B'
//A [m x n]  B [p x n] ans [m x p]
__kernel void RegularMatrTranspMatrProd
              (__global const float * A,
               __global const float * B,
               __global       float * ans,
               __constant     int   * ADim)
               
{
   int m = get_global_size(0);
   int p = get_global_size(1);
   int n = ADim[1];
   
   int i = get_global_id(0);
   int j = get_global_id(1);
   int ni = n*i;
   int nj = n*j;
   
   float temp = 0.0f;
   for (int k = 0; k < n; k++)
   {
      temp += A[k + ni]*B[k + nj];
   }

   ans[j + p*i] = temp;
}

//Computes A x B
//A [m x n]  B [n x p] ans [m x p]
__kernel void RegularMatrMatrProd
              (__global const float * A,
               __global const float * B,
               __global       float * ans,
               __constant     int   * ADim)
               
{
   int m = get_global_size(0);
   int p = get_global_size(1);
   int n = ADim[1];
   
   int i = get_global_id(0);
   int j = get_global_id(1);
   int ni = n*i;
   
   float temp = 0.0f;
   for (int k = 0; k < n; k++)
   {
      int pk = p*k;
      temp += A[k + ni]*B[j + pk];
   }

   ans[j + p*i] = temp;
}


__kernel void TestSquareIdx(__global int * row,
                            __global int * col)
                          
{
   //global_work_size = n(n+1)/2
   int idx = get_global_id(0);
   int temp = (int)floor(sqrt(1.0f + (idx << 3)));
   
   int p = (temp - 1) >> 1;
   int q = idx - ((p * (p + 1)) >> 1);

   //This is necessary to maintain stability of index retrieval
   if (q > p)
   {
      p++;
      q = idx - ((p * (p + 1)) >> 1);
   }
   else if (q < 0)
   {
      p--;
      q = idx - ((p * (p + 1)) >> 1);
   }

   row[idx] = p;
   col[idx] = q;
}



__kernel void ComputeAtWA(__global const float * A,
                          __constant     int *   dimsA,
                          __global const float * W,
                          __global       float * AtWA,
                          __global const float * lambda)
                          
{
   //global_work_size = n(n+1)/2
   int idx = get_global_id(0);
   int temp = (int)floor(sqrt(1.0f + (idx << 3)));
   
   int p = (temp - 1) >> 1;
   int q = idx - ((p * (p + 1)) >> 1);

   //This is necessary to maintain stability of index retrieval
   if (q > p)
   {
      p++;
      q = idx - ((p * (p + 1)) >> 1);
   }
   else if (q < 0)
   {
      p--;
      q = idx - ((p * (p + 1)) >> 1);
   }
   
   int m = dimsA[0];
   int n = dimsA[1];
   
   float val = 0;
   int nk = 0;
   for (int k = 0; k < m; k++)
   {
      //nk = n*k;
      val += A[p + nk]*A[q + nk]*W[k];
      nk += n;
   }
   
   //Regularization term
   val += p == q ? lambda[p] : 0.0f;
   
   AtWA[((p*(1+p))>>1)+q] = val;
}

__kernel void ComputeAinvHAt(__global const float * A,
                             __constant     int *   dimsA,
                             __global const float * invHAt,
                             __global       float * AinvHAt)
                          
{
   //global_work_size = m(m+1)/2
   
   int idx = get_global_id(0);
   int temp = (int)floor(sqrt(1.0f + (idx << 3)));
   
   int p = (temp - 1) >> 1;
   int q = idx - ((p * (p + 1)) >> 1);

   //This is necessary to maintain stability of index retrieval
   if (q > p)
   {
      p++;
      q = idx - ((p * (p + 1)) >> 1);
   }
   else if (q < 0)
   {
      p--;
      q = idx - ((p * (p + 1)) >> 1);
   }
   
   int m = dimsA[0];
   int n = dimsA[1];
   
   float val = 0;
   int np = n*p;
   int nq = n*q;
   for (int k = 0; k < n; k++)
   {
      val += A[k + np]*invHAt[k + nq];
   }
   
   
   AinvHAt[((p*(1+p))>>1)+q] = val;
}


__kernel void CopyBuffer(__global const float * src,
                         __global       float * dst)
{
   int i = get_global_id(0);
   dst[i] = src[i];
}

__kernel void LinearComb(__constant float     * alpha,
                         __constant float     * beta,
                         __global const float * u,
                         __global const float * v,
                         __global       float * ans)
{
   int i = get_global_id(0);
   ans[i] = alpha[0]*u[i] + beta[0]*v[i];
}

__kernel void MatrVecProd(__global const float * M,
                          __constant     int   * Mdims,
                          __global const float * v,
                          __constant     float * alpha,
                          __global       float * ans)
{
   int i = get_global_id(0);
   int maxJ = Mdims[1];
   
   float temp = 0.0f;
   int maxJI = i*maxJ;
   for (int j=0;j<maxJ;j++)
   {
      temp += M[j + maxJI] * alpha[0] * v[j];
   }
   ans[i] = temp;
}

__kernel void TranspMatrVecProdW(__global const float * M,
                           __constant     int   * Mdims,
                           __global const float * v,
                           __constant     float * alpha,
                           __global const float * W,
                           __global       float * ans)
{
   int i = get_global_id(0);
   int maxK = Mdims[0];
   int maxJ = Mdims[1];
   
   float temp = 0.0f;
   
   for (int k=0;k<maxK;k++)
   {
      temp += M[i + maxJ*k] * alpha[0]*v[k]*W[k];
   }
   ans[i] = temp;
}

__kernel void MatrVecProdSumVec(
                          __global const float * M,
                          __constant     int   * Mdims,
                          __global const float * v,
                          __constant     float * alpha,
                          __global const float * u,
                          __constant     float * beta,
                          __global       float * ans)
{
   int i = get_global_id(0);
   int maxJ = Mdims[1];
   
   float temp = 0.0f;
   int maxJI = i*maxJ;
   for (int j=0;j<maxJ;j++)
   {
      temp += M[j + maxJI] * alpha[0]*v[j];
   }
   ans[i] = temp + u[i]*beta[0];
}

__kernel void DiagTranspMatProd(
                          __global const float * D,
                          __global const float * v,
                          __constant     float * alpha,
                          __global       float * ans)
{
   int i = get_global_id(0);
   int j = get_global_id(1);
   int M = get_global_size(1);
   int N = get_global_size(0);

   //ans[i] = alpha[0]*D[i]*v[i];
   ans[j+M*i] = alpha[0]*D[i]*v[i+N*j];
}

__kernel void DiagVecProd(
                          __global const float * D,
                          __global const float * v,
                          __constant     float * alpha,
                          __global       float * ans)
{
   int i = get_global_id(0);
   ans[i] = alpha[0]*D[i]*v[i];
}

__kernel void InnerProd(__global const float * u,
                        __global const float * v,
                        __global       float * ans)
{
   int i = get_global_id(0);
   ans[i] = u[i]*v[i];
}

__kernel void ElemWiseProd(__global const float * u,
                           __global       float * ans)
{
   int i = get_global_id(0);
   float temp = u[i];
   ans[i] = temp*temp;
}

__kernel void ElemWiseInv (__global const float * u,
                           __global       float * ans)
{
   int i = get_global_id(0);
   ans[i] = 1.0f / u[i];
}

__kernel void ElemWiseInv2(__global const float * u,
                           __global       float * ans)
{
   int i = get_global_id(0);
   
   ans[i] = 1.0f / (u[i]*u[i]);
}

__kernel void ElemWiseAbs(__global const float * u,
                          __global       float * ans)
{
   int i = get_global_id(0);
   float temp = u[i];
   ans[i] = fabs(temp);
}

//computes u = u-v
__kernel void InPlaceSubtract(__global       float * u,
                              __global const float * v)
{
   int i = get_global_id(0);
   u[i] -= v[i];
}

//Checks if a vector has a negative entry
__kernel void HasPositiveEntry(__global const float * v,
                               __global       float * Verifier)
{
   int i = get_global_id(0);
   if (v[i] >= 0) Verifier[0] = 10.0f;
}

";
            #region Sum of vector elements
            /// <summary>Coalesced vector sum kernels</summary>
            public string srcVecSum = @"

__kernel void 
        ClearResps(__global const float * v1,
                   __global       float * resps,
                   __constant int * NN)

{
   int i = get_global_id(0);
   resps[i] = 0.0f;
}

__kernel void 
         PreSum  (__global const float * v1,
                  __global       float * resps,
                  __constant int * NN)

{
   int i = get_global_id(0);
   int p = get_global_size(0);
   int n = NN[0];
   
   int ind = n-p+i;
   resps[i] = v1[ind];
}

__kernel void 
         CoalLocalSum(__global const float * v1,
                      __global       float * resps,
                      __constant int * KK)

{
   int i = get_global_id(0);
   int nWorkItems = get_global_size(0);
   int k = KK[0];
   
   float val = 0.0f;
   int ind = i;
   for (int j = 0; j < k; j++)
   {
      val += v1[ind];
      ind += nWorkItems;
   }
   val += resps[i];

   //Reduction part
   int p = get_local_id(0);
   int q = get_group_id(0);
   int maxp = get_local_size(0);
   
   __local float values[256];
   values[p] = val;
   barrier(CLK_LOCAL_MEM_FENCE);
   
   maxp = maxp >> 1;
   while(maxp > 0)
   {
      if (p < maxp)
      {
         values[p] += values[p+maxp];
      }
      maxp = maxp >> 1;
      barrier(CLK_LOCAL_MEM_FENCE);
   }
   
   if (p == 0) resps[q] = values[0];
}

";
            #endregion
            #endregion

            #region pNorm minimization with qNorm regularization

            public string srcpNorm = @"

__kernel void pNorm(__global   float * v,
                    __constant float * power,
                    __global const float * w)
{
  int i = get_global_id(0);

  float temp = v[i];
  temp = fabs(temp);
  temp = powr(temp, power[0]);
  
  v[i] = temp * w[i];
}

__kernel void dpNorm(__global const  float * v,
                     __constant      float * power,
                     __global const  float * w,
                     __global        float * dvPnorm,
                     __global        float * d2vPnorm)
{
  int i = get_global_id(0);

  float temp = v[i];
  
  dvPnorm[i]  = powr(fabs(temp), power[0]-1.0f) * w[i] * sign(temp) * power[0];
  d2vPnorm[i] = powr(fabs(temp), power[0]-2.0f) * w[i] * power[0] * (power[0]-1.0f);
}
";

            #endregion

            #region Feasibility retrieval of t value
            public string strFeasibFunc = @"

__kernel void getLast(__global const float * v,
                      __global const int   * dimV,
                      __global       float * lastVal)
{
   lastVal[0] = v[dimV[0]-1];
}

";
            #endregion

            #region Logistic regression

            public string srcLogistReg = @"

__kernel void ComputeLogistRegParams
              (__global const float * XTheta,
               __global const float * y,
               __global       float * z1,
               __global       float * z2,
               __global       float * cost)

{
    int i = get_global_id(0);
    float eMz = native_exp(-XTheta[i]);

    float hTheta = native_recip(1.0f + eMz);
    z1[i] = hTheta - y[i];
    z2[i] = eMz * hTheta * hTheta;

    cost[i] = y[i] == 0 ? -native_log(1.0f - hTheta) : -native_log(hTheta);
}";

            #endregion
        }
        #endregion

        #region Block Cholesky kernels
        /// <summary>Submatrix size, based on maximum amount of workitems per workgroup</summary>
        protected static int SUBMATRIXSIZE;

        //Block Cholesky Kernels
        /// <summary>Cholesky factorization and inversion of a block</summary>
        protected static CLCalc.Program.Kernel kernelCholeskyDiagBlock;
        /// <summary>Updates lower panel of Cholesky decomp</summary>
        protected static CLCalc.Program.Kernel kernelCholeskyComputePanel;
        /// <summary>Forwards updates to rest of the matrix</summary>
        protected static CLCalc.Program.Kernel kernelCholeskyForwardProp;


        /// <summary>First part of forward substitution</summary>
        protected static CLCalc.Program.Kernel kernelFwdUpperBackSubs;
        /// <summary>First part of backsubstitution</summary>
        protected static CLCalc.Program.Kernel kernelBkLowerBackSubs;

        /// <summary>Forward propagation of values</summary>
        protected static CLCalc.Program.Kernel kernelFwdPropag;
        /// <summary>Forward propagation of values</summary>
        protected static CLCalc.Program.Kernel kernelFwdPropag2;
        /// <summary>Backwards propagation of values</summary>
        protected static CLCalc.Program.Kernel kernelBackPropag;
        /// <summary>Backwards propagation of values (without __local)</summary>
        protected static CLCalc.Program.Kernel kernelBackPropag2;


        #endregion

        #region Linear algebra kernels - BLAS stuff
        /// <summary>Matrix - vector product</summary>
        private static CLCalc.Program.Kernel kernelSymMatrVecMultiply;
        /// <summary>Computes AtWA + lambda</summary>
        private static CLCalc.Program.Kernel kernelComputeAtWA;
        /// <summary>Kernel to compute the last multiplication in the computation of A*inv(H)*A'</summary>
        private static CLCalc.Program.Kernel kernelComputeAinvHAt;

        /// <summary>Copy buffer contents</summary>
        private static CLCalc.Program.Kernel kernelCopyBuffer;
        /// <summary>Linear combination of vectors kernel</summary>
        private static CLCalc.Program.Kernel kernelLinearComb;
        /// <summary>Matrix vector product M*(alpha*v)</summary>
        private static CLCalc.Program.Kernel kernelMatrVecProd;
        /// <summary>Weighted matrix vector product transpose(M)*W*v</summary>
        private static CLCalc.Program.Kernel kernelTranspMatrVecProdW;
        /// <summary>Symmetric matrix matrix transpose multiply</summary>
        private static CLCalc.Program.Kernel kernelSymMatrMatrMultiply;
        /// <summary>Regular A * B' matrix product</summary>
        private static CLCalc.Program.Kernel kernelRegularMatrTranspMatrProd;
        /// <summary>Regular A * B matrix product</summary>
        private static CLCalc.Program.Kernel kernelRegularMatrMatrProd;

        /// <summary>Vector inner product</summary>
        private static CLCalc.Program.Kernel kernelInnerProd;

        /// <summary>Performs in-place vector subtraction u = u-v</summary>
        private static CLCalc.Program.Kernel kernelInPlaceSubtract;
        /// <summary>In-place element wise absolute value</summary>
        private static CLCalc.Program.Kernel kernelElemWiseAbs;

        /// <summary>Matrix vector product M*(alpha*v) + beta*u</summary>
        private static CLCalc.Program.Kernel kernelMatrVecProdSumVec;
        /// <summary>Diagonal vector product D*(alpha*v)</summary>
        private static CLCalc.Program.Kernel kernelDiagVecProd;
        /// <summary>Diagonal matrix product D*(alpha*V)</summary>
        private static CLCalc.Program.Kernel kernelDiagTranspMatProd;
        /// <summary>Element wise multiplication u .* u</summary>
        private static CLCalc.Program.Kernel kernelElemWiseProd;
        /// <summary>Element wise inversion 1 ./ u</summary>
        private static CLCalc.Program.Kernel kernelElemWiseInv;
        /// <summary>Element wise square inversion 1 ./ (u.*u)</summary>
        private static CLCalc.Program.Kernel kernelElemWiseInv2;

        /// <summary>Kernel to set components of a vector to zero</summary>
        private static CLCalc.Program.Kernel kernelClear;
        /// <summary>Pre-sum elements to prepare for coalescence</summary>
        private static CLCalc.Program.Kernel kernelPreSum;
        /// <summary>Local reduced sum of vector components</summary>
        private static CLCalc.Program.Kernel kernelCoalLocalSum;

        /// <summary>Checks if a vector has any positive entries</summary>
        private static CLCalc.Program.Kernel kernelHasPositiveEntry;

        //Lp norm minimization


        #endregion


        #region Static constructor - compiles kernels
        static floatLinalg()
        {
            Init();
        }

        /// <summary>Initializes CL kernels</summary>
        public static void Init()
        {
            if (kernelCholeskyDiagBlock == null)
            {
                if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.Unknown) CLCalc.InitCL();

                if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.UsingCL)
                {
                    if (kernelCholeskyDiagBlock == null)
                    {
                        SUBMATRIXSIZE = (int)Math.Sqrt((double)CLCalc.Program.CommQueues[CLCalc.Program.DefaultCQ].Device.MaxWorkGroupSize);
                        SUBMATRIXSIZE = Math.Min(16, SUBMATRIXSIZE);

                        string strSubSize = SUBMATRIXSIZE.ToString();
                        string strTotSize = (SUBMATRIXSIZE * (SUBMATRIXSIZE + 1) / 2).ToString();

                        LinalgSrc src = new LinalgSrc();
                        string srcBlockChol = src.srcBlockCholesky.Replace("CONSTSUBMATRIXSIZE", strSubSize).Replace("CONSTGLOBALSIZE", strTotSize);
                        CLCalc.Program.Compile(new string[] { srcBlockChol, src.srcBkSubs, src.srcOperations, src.srcVecSum, 
                            src.srcpNorm, src.strFeasibFunc, src.srcLogistReg });

                        kernelCholeskyDiagBlock = new CLCalc.Program.Kernel("CholeskyDiagBlock");
                        kernelCholeskyComputePanel = new CLCalc.Program.Kernel("CholeskyComputePanel");
                        kernelCholeskyForwardProp = new CLCalc.Program.Kernel("CholeskyForwardProp");

                        kernelFwdUpperBackSubs = new CLCalc.Program.Kernel("FwdUpperBackSubs");
                        kernelBkLowerBackSubs = new CLCalc.Program.Kernel("BkLowerBackSubs");
                        kernelFwdPropag = new CLCalc.Program.Kernel("FwdPropag");
                        kernelFwdPropag2 = new CLCalc.Program.Kernel("FwdPropag2");
                        kernelBackPropag = new CLCalc.Program.Kernel("BackPropag");
                        kernelBackPropag2 = new CLCalc.Program.Kernel("BackPropag2");

                        kernelInPlaceSubtract = new CLCalc.Program.Kernel("InPlaceSubtract");
                        kernelElemWiseAbs = new CLCalc.Program.Kernel("ElemWiseAbs");

                        kernelInnerProd = new CLCalc.Program.Kernel("InnerProd");

                        //Linear algebra
                        kernelSymMatrVecMultiply = new CLCalc.Program.Kernel("SymMatrVecMultiply");
                        kernelSymMatrMatrMultiply = new CLCalc.Program.Kernel("SymMatrMatrMultiply");
                        kernelComputeAtWA = new CLCalc.Program.Kernel("ComputeAtWA");
                        kernelComputeAinvHAt = new CLCalc.Program.Kernel("ComputeAinvHAt");
                        kernelRegularMatrTranspMatrProd = new CLCalc.Program.Kernel("RegularMatrTranspMatrProd");
                        kernelRegularMatrMatrProd = new CLCalc.Program.Kernel("RegularMatrMatrProd");

                        kernelCopyBuffer = new CLCalc.Program.Kernel("CopyBuffer");
                        kernelLinearComb = new CLCalc.Program.Kernel("LinearComb");
                        kernelMatrVecProd = new CLCalc.Program.Kernel("MatrVecProd");
                        kernelTranspMatrVecProdW = new CLCalc.Program.Kernel("TranspMatrVecProdW");
                        kernelMatrVecProdSumVec = new CLCalc.Program.Kernel("MatrVecProdSumVec");
                        kernelDiagVecProd = new CLCalc.Program.Kernel("DiagVecProd");
                        kernelDiagTranspMatProd = new CLCalc.Program.Kernel("DiagTranspMatProd");
                        kernelElemWiseProd = new CLCalc.Program.Kernel("ElemWiseProd");
                        kernelElemWiseInv = new CLCalc.Program.Kernel("ElemWiseInv");
                        kernelElemWiseInv2 = new CLCalc.Program.Kernel("ElemWiseInv2");

                        kernelClear = new CLCalc.Program.Kernel("ClearResps");
                        kernelPreSum = new CLCalc.Program.Kernel("PreSum");
                        kernelCoalLocalSum = new CLCalc.Program.Kernel("CoalLocalSum");

                        kernelHasPositiveEntry = new CLCalc.Program.Kernel("HasPositiveEntry");


                        //pNorm minimization
                        floatOptimization.CurveFitting.kernelpNorm = new CLCalc.Program.Kernel("pNorm");
                        floatOptimization.CurveFitting.kerneldpNorm = new CLCalc.Program.Kernel("dpNorm");

                        //Logistic regression
                        floatOptimization.LogisticRegression.kernelComputeLogistRegParams = new CLCalc.Program.Kernel("ComputeLogistRegParams");
                        floatOptimization.LogisticRegression.kernelpNorm = floatOptimization.CurveFitting.kernelpNorm;
                        floatOptimization.LogisticRegression.kerneldpNorm = floatOptimization.CurveFitting.kerneldpNorm;


                        //Feasibility
                        floatOptimization.QuadraticProgramming.kernelgetLast = new CLCalc.Program.Kernel("getLast");
                    }
                }
            }
        }
        #endregion
    }
}
