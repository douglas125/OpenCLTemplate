using System;
using System.Collections.Generic;
using OpenCLTemplate;
using System.Text;

#region References
/*
[1] BLACK, Noel; MOORE, Shirley; and WEISSTEIN, Eric W. "Conjugate Gradient Method." From MathWorld--A Wolfram Web Resource. http://mathworld.wolfram.com/ConjugateGradientMethod.html 

[2] SAAD, Yousef. "Iterative methods for sparse linear systems", 2nd Edition, SIAM, 2000.
*/
#endregion

namespace OpenCLTemplate.LinearAlgebra
{
    /// <summary>OpenCL linear algebra functions</summary>
    public class SparseLinalg
    {
        /// <summary>Builds a new OpenCL Image2D containing vector data</summary>
        public class CLImgVector
        {
            /// <summary>Vector length. Allocated length is multiple of 4096 = 2^12</summary>
            private int n;

            /// <summary>Gets the length of this vector</summary>
            public int Length
            {
                get { return n; }
            }

            /// <summary>Number of rows to accomodate the size. Number of columns is 4096 float4's, so up to 4096*4 it will be
            /// only 1 row.</summary>
            private int nRows;

            /// <summary>Image2D representing vector contents</summary>
            public CLCalc.Program.Image2D CLVector;

            /// <summary>Vector to be written/read from Device memory. Call WriteToDevice to effectively copy to Device memory</summary>
            public float[] VectorData;

            /// <summary>Creates a new vector allocated in OpenCL Image2D object.</summary>
            /// <param name="Length">Vector length. For convenience some extra memory is allocated but calculations only go up to vector dimensions</param>
            public CLImgVector(int Length)
            {
                //Stores length and computes number of necessary rows
                n = Length;
                //nRows = n/2^14 (4096 float4's) + 1 (at least one row)
                nRows = ((n - 1) >> 14) + 1;

                //Trick:
                //y = n >> 12; //y = n / 2^12;
                //x = n & 0xfff; //x = n mod (2^12-1);

                if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.Unknown)
                {
                    try { CLCalc.InitCL(); }
                    catch
                    {
                    }
                }

                //Allocates vector. Width = IMGWIDTH, Height = nRows, Total number of elements = 4*Width*Height
                VectorData = new float[IMGWIDTH * nRows * 4];
                if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.UsingCL)
                {
                    CLVector = new CLCalc.Program.Image2D(VectorData, IMGWIDTH, nRows);
                }
            }

            /// <summary>Effectively writes contents of VectorData to Device memory</summary>
            public void WriteToDevice()
            {
                CLVector.WriteToDevice(VectorData);
            }

            /// <summary>Reads contents of CLVector image2d to VectorData</summary>
            public void ReadFromDevice()
            {
                CLVector.ReadFromDeviceTo(VectorData);
            }

        }

        /// <summary>Builds a new OpenCL Image2D containig a sparse matrix</summary>
        public class CLImgSparseMatrix
        {
            /// <summary>Matrix data arranged in format numRows*nonZeroElemsPerRow.</summary>
            public float[] MatrixData;
            /// <summary>True column of element</summary>
            public int[] Columns;

            /// <summary>Image2D representing vector contents</summary>
            public CLCalc.Program.Image2D CLMatrixData;
            /// <summary>Image2D containing column indexes</summary>
            public CLCalc.Program.Image2D CLColumns;

            /// <summary>Total number of elements, N*nonZeroElemsPerRow</summary>
            private int nElems;
            /// <summary>Number of rows of matrix</summary>
            private int numRows;

            /// <summary>Number of elements stored per row</summary>
            private int elemsPerRow;

            /// <summary>Gets number of elements stored per row</summary>
            public int NonZeroElemsPerRow
            {
                get { return elemsPerRow; }
            }
            /// <summary>Gets matrix dimension (M[NxN])</summary>
            public int MatrixDimension
            {
                get { return numRows; }
            }

            #region Inserting and retrieving elements from sparse matrix
            /// <summary>Gets or sets value for matrix elements</summary>
            public float this[int row, int column]
            {
                get
                {
                    return GetValue(row, column);
                }
                set
                {
                    SetValue(value, row, column);
                }
            }

            /// <summary>Stores a matrix value at a particular row and column</summary>
            private void SetValue(float value, int row, int column)
            {
                if (row >= numRows || column >= numRows || row < 0 || column < 0) throw new Exception("Invalid dimension");

                //Deletes element first
                for (int j = 0; j < elemsPerRow; j++)
                {
                    if (Columns[j + elemsPerRow * row] == column)
                    {
                        Columns[j + elemsPerRow * row] = -1;
                        MatrixData[j + elemsPerRow * row] = 0;
                        j = elemsPerRow;
                    }
                }

                //Writes value
                if (value != 0)
                {
                    for (int j = 0; j < elemsPerRow; j++)
                    {
                        if (Columns[j + elemsPerRow * row] < 0)
                        {
                            Columns[j + elemsPerRow * row] = column;
                            MatrixData[j + elemsPerRow * row] = value;
                            return;
                        }
                    }
                    throw new Exception("Maximum number of elements per row exceeded");
                }

            }

            /// <summary>Gets the value of a matrix element</summary>
            /// <param name="row">Row index</param>
            /// <param name="column">Column index</param>
            private float GetValue(int row, int column)
            {
                if (row >= numRows || column >= numRows || row < 0 || column < 0) throw new Exception("Invalid dimension");

                for (int j = 0; j < elemsPerRow; j++)
                {
                    if (Columns[j + elemsPerRow * row] == column)
                    {
                        return MatrixData[j + elemsPerRow * row];
                    }
                }
                return 0;
            }
            #endregion

            #region Copy to and read from Device memory
            /// <summary>Effectively writes contents of Matrix to Device memory</summary>
            public void WriteToDevice()
            {
                CLMatrixData.WriteToDevice(MatrixData);
                CLColumns.WriteToDevice(Columns);
            }

            /// <summary>Reads contents of CLMatrix image2d to Matrix Host memory</summary>
            public void ReadFromDevice()
            {
                CLMatrixData.ReadFromDeviceTo(MatrixData);
                CLColumns.ReadFromDeviceTo(Columns);
            }
            #endregion

            /// <summary>Constructor.</summary>
            /// <param name="N">NxN dimension of the matrix</param>
            /// <param name="nonZeroElemsPerRow">Maximum number of non-zero elements per row</param>
            public CLImgSparseMatrix(int N, int nonZeroElemsPerRow)
            {
                elemsPerRow = nonZeroElemsPerRow - 1;
                elemsPerRow = (elemsPerRow >> 2) + 1;
                elemsPerRow = elemsPerRow << 2;

                numRows = N;

                nElems = numRows * elemsPerRow;

                if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.Unknown)
                {
                    try { CLCalc.InitCL(); }
                    catch
                    {
                    }
                }

                //OpenCL image allocation
                int CLImgNumRows = ((nElems - 1) >> 14) + 1;
                MatrixData = new float[IMGWIDTH * CLImgNumRows * 4];
                Columns = new int[IMGWIDTH * CLImgNumRows * 4];
                for (int i = 0; i < Columns.Length; i++) Columns[i] = -1;

                if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.UsingCL)
                {
                    CLMatrixData = new CLCalc.Program.Image2D(MatrixData, IMGWIDTH, CLImgNumRows);
                    CLColumns = new CLCalc.Program.Image2D(Columns, IMGWIDTH, CLImgNumRows);
                }
            }

            /// <summary>String representation of complete matrix. Returns at most 10000 elements</summary>
            public override string ToString()
            {
                int nWrite = Math.Min(100, numRows);

                string resp = "\n";
                for (int i = 0; i < nWrite; i++)
                {
                    for (int j = 0; j < nWrite; j++)
                    {
                        resp += Math.Round(this[i, j], 5).ToString().PadRight(11) + "\t";
                    }
                    resp += "\n";
                }

                return resp;
            }
        }

        #region Methods
        /// <summary>Width to be used in float4's</summary>
        public const int IMGWIDTH = 4096;
        /// <summary>Total number of workers</summary>
        public const int GLOBALWORKSIZE = 2048;

        /// <summary>Static Constructor. Builds kernels</summary>
        static SparseLinalg()
        {
            if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.Unknown)
            {
                try { CLCalc.InitCL(); }
                catch
                {
                }
            }

            if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.UsingCL)
            {
                //Kernel
                CLLinalgSrc src = new CLLinalgSrc();
                CLCalc.Program.Compile(new string[] { src.srcDotProd, src.srcMatVecMult, src.srcLinConjGrad });
                kernelDotProduct = new CLCalc.Program.Kernel("dotProd");
                kernelSum = new CLCalc.Program.Kernel("sumDotProd");
                kernelGetDotSum = new CLCalc.Program.Kernel("GetResp");

                kernelSparseMatrixVecMult = new CLCalc.Program.Kernel("SparseMatrixVecMult");

                //Linear solving
                kernelInitRP = new CLCalc.Program.Kernel("InitRP");
                kernelMultiplyAdd = new CLCalc.Program.Kernel("MultiplyAdd");
                kernelCopyToTemp = new CLCalc.Program.Kernel("CopyToTemp");
            }
        }

        /// <summary>Constructor.</summary>
        public SparseLinalg()
        {
            if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.Unknown)
            {
                try { CLCalc.InitCL(); }
                catch
                {
                }
            }

            if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.UsingCL)
            {
                //Creates control variables
                dprod = new float[SparseLinalg.GLOBALWORKSIZE];
                dotProd = new CLCalc.Program.Variable(dprod);
                dotProdSum = new CLCalc.Program.Variable(new float[1]);

                int[] i = new int[1];
                vLenBy4 = new CLCalc.Program.Variable(i);

                CLNonZeroElemsPerRow = new CLCalc.Program.Variable(new int[1]);
            }
        }

        #region Vector dot product
        /// <summary>Vector length divided by 4 (plus 1)</summary>
        private CLCalc.Program.Variable vLenBy4;

        /// <summary>OpenCL memory Dot product</summary>
        private CLCalc.Program.Variable dotProd;
        /// <summary>Dot product, host memory</summary>
        private float[] dprod;
        /// <summary>OpenCL memory Dot product final summation</summary>
        private CLCalc.Program.Variable dotProdSum;

        /// <summary>OpenCL dot product Kernel</summary>
        private static CLCalc.Program.Kernel kernelDotProduct;
        /// <summary>OpenCL dot product sum elements kernel</summary>
        private static CLCalc.Program.Kernel kernelSum;
        /// <summary>OpenCL get final answer</summary>
        private static CLCalc.Program.Kernel kernelGetDotSum;

        /// <summary>Computes dot product of 2 vectors using their OpenCL images. Assumes data has been inserted to VectorData and WriteToDevice() has been called</summary>
        /// <param name="v1">First vector</param>
        /// <param name="v2">Second vector</param>
        public float DotProduct(CLImgVector v1, CLImgVector v2)
        {
            if (v1.Length != v2.Length) throw new Exception("Incompatible lengths");

            if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.UsingCL)
            {
                CLDotProd(v1, v2);

                float[] resp = new float[1];

                dotProdSum.ReadFromDeviceTo(resp);
                dprod[0] = resp[0];
            }
            else
            {
                for (int i = 0; i < v1.Length; i++)
                    dprod[0] += v1.VectorData[i] * v2.VectorData[i];

                dprod[0] = dprod[0];
            }

            return dprod[0];
        }

        /// <summary>Computes dot product of two vectors and stores result in
        /// dotProdSum</summary>
        private void CLDotProd(CLImgVector v1, CLImgVector v2)
        {
            int[] vlenby4 = new int[] { (v1.Length >> 2) + 1 };

            vLenBy4.WriteToDevice(vlenby4);

            //Computes products and most sums
            CLCalc.Program.MemoryObject[] args = new CLCalc.Program.MemoryObject[] { v1.CLVector, v2.CLVector, dotProd, vLenBy4 };

            //kernelDotProduct.Execute(args, GLOBALWORKSIZE);
            kernelDotProduct.Execute(args, new int[] { GLOBALWORKSIZE }, new int[] { (int)CLCalc.CLDevices[CLCalc.Program.DefaultCQ].MaxWorkItemSizes[0] });

            //Sums what's left
            int i = GLOBALWORKSIZE >> 3;
            args = new CLCalc.Program.MemoryObject[] { dotProd };
            while (i > 0)
            {
                kernelSum.Execute(args, i);
                i = (i >> 1);
            }

            //Reads final value
            args = new CLCalc.Program.MemoryObject[] { dotProd, dotProdSum };
            kernelGetDotSum.Execute(args, 1);
        }

        /// <summary>Computes dot product of 2 vectors without OpenCL</summary>
        public float DotProductNoCL(CLImgVector v1, CLImgVector v2)
        {
            float dProd = 0;

            for (int i = 0; i < v1.Length; i++)
                dProd += v1.VectorData[i] * v2.VectorData[i];

            return dProd;
        }

        /// <summary>Computes dot product of 2 vectors without OpenCL, in double precision</summary>
        public double ExactDotProductNoCL(CLImgVector v1, CLImgVector v2)
        {
            double dProd = 0;

            for (int i = 0; i < v1.Length; i++)
                dProd += (double)v1.VectorData[i] * (double)v2.VectorData[i];

            return dProd;
        }
        #endregion

        #region Sparse matrix vector product

        /// <summary>OpenCL sparse matrix vector product</summary>
        private static CLCalc.Program.Kernel kernelSparseMatrixVecMult;
        /// <summary>Non-zero elements per row</summary>
        private CLCalc.Program.Variable CLNonZeroElemsPerRow;

        /// <summary>Computes M*x and stores the result in y. Does not automatically read result from device memory</summary>
        /// <param name="M">Sparse matrix</param>
        /// <param name="x">Vector to be multiplied</param>
        /// <param name="y">Result</param>
        public void Multiply(CLImgSparseMatrix M, CLImgVector x, CLImgVector y)
        {
            if (x.Length != M.MatrixDimension || y.Length != M.MatrixDimension) throw new Exception("M, x and y dimensions not compatible");

            if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.UsingCL)
            {
                CLNonZeroElemsPerRow.WriteToDevice(new int[] { M.NonZeroElemsPerRow });
                CLCalc.Program.MemoryObject[] args = new CLCalc.Program.MemoryObject[] { M.CLMatrixData, M.CLColumns, x.CLVector, y.CLVector, CLNonZeroElemsPerRow };

                //Ideally matrix dimension should be a multiple of 4, but OK if it's not
                kernelSparseMatrixVecMult.Execute(args, 1 + ((M.MatrixDimension - 1) >> 2));
            }
            else
            {
                y.VectorData = MultiplyNoCL(M, x);
            }
        }

        /// <summary>Computes exact double-precision product of sparse matrix M and vector x</summary>
        /// <param name="M">Sparse matrix</param>
        /// <param name="x">Vector to be multiplied</param>
        public double[] ExactMultiply(CLImgSparseMatrix M, CLImgVector x)
        {
            if (x.Length != M.MatrixDimension) throw new Exception("M and x dimensions not compatible");

            double[] resp = new double[x.Length];

            for (int i = 0; i < M.MatrixDimension; i++)
            {
                for (int j = 0; j < M.NonZeroElemsPerRow; j++)
                {
                    if (M.Columns[j + M.NonZeroElemsPerRow * i] >= 0)
                        resp[i] += (double)M.MatrixData[j + M.NonZeroElemsPerRow * i] * (double)x.VectorData[M.Columns[j + M.NonZeroElemsPerRow * i]];
                }
            }

            return resp;
        }

        /// <summary>Computes product of sparse matrix M and vector x</summary>
        /// <param name="M">Sparse matrix</param>
        /// <param name="x">Vector to be multiplied</param>
        public float[] MultiplyNoCL(CLImgSparseMatrix M, CLImgVector x)
        {
            if (x.Length != M.MatrixDimension) throw new Exception("M and x dimensions not compatible");

            float[] resp = new float[x.Length];

            for (int i = 0; i < M.MatrixDimension; i++)
            {
                for (int j = 0; j < M.NonZeroElemsPerRow; j++)
                {
                    if (M.Columns[j + M.NonZeroElemsPerRow * i] >= 0)
                        resp[i] += M.MatrixData[j + M.NonZeroElemsPerRow * i] * x.VectorData[M.Columns[j + M.NonZeroElemsPerRow * i]];
                }
            }

            return resp;
        }

        #endregion

        #region Conjugate gradient solver

        /// <summary>Residues</summary>
        private CLImgVector r;
        /// <summary>Gradient direction</summary>
        private CLImgVector p;
        /// <summary>Initial guess/answer holder</summary>
        public CLImgVector x;
        /// <summary>A times p vector</summary>
        private CLImgVector Ap;
        /// <summary>Temporary storage vector</summary>
        private CLImgVector temp;

        /// <summary>Value to store some vector</summary>
        private float[] lambda;
        /// <summary>Value to use to multiply some vector</summary>
        private CLCalc.Program.Variable CLlambda;

        /// <summary>Initialize r and p values</summary>
        private static CLCalc.Program.Kernel kernelInitRP;
        /// <summary>Mad kernel</summary>
        private static CLCalc.Program.Kernel kernelMultiplyAdd;
        /// <summary>Copy to temporary image kernel</summary>
        private static CLCalc.Program.Kernel kernelCopyToTemp;

        /// <summary>Maximum number of iterations</summary>
        public static int MAXITER = 8;

        /// <summary>Solves linear system Mx = b using conjugate gradient method</summary>
        /// <param name="M">Matrix M</param>
        /// <param name="b">Vector b</param>
        /// <param name="tol">Error tolerance</param>
        public float[] LinSolveNoCL(CLImgSparseMatrix M, CLImgVector b, float tol)
        {
            if (b.Length != M.MatrixDimension) throw new Exception("M and x dimensions not compatible");

            int n = b.Length;

            if (r == null || r.Length != n)
            {
                r = new CLImgVector(n);
                p = new CLImgVector(n);
                x = new CLImgVector(n);
                Ap = new CLImgVector(n);
            }

            float alpha, beta, RDotROld, RDotR;

            //Initialization
            Ap.VectorData = MultiplyNoCL(M, x);
            for (int i = 0; i < n; i++)
            {
                r.VectorData[i] = b.VectorData[i] - Ap.VectorData[i];
                p.VectorData[i] = r.VectorData[i];
            }

            //Loop
            int count = 0;
            RDotR = DotProductNoCL(r, r);
            while ((RDotR > tol) && (count < n * MAXITER))
            {
                RDotROld = RDotR;


                Ap.VectorData = MultiplyNoCL(M, p);
                alpha = RDotROld / DotProductNoCL(Ap, p);

                for (int i = 0; i < n; i++)
                {
                    x.VectorData[i] += alpha * p.VectorData[i];
                    r.VectorData[i] -= alpha * Ap.VectorData[i];
                }

                RDotR = DotProductNoCL(r, r);
                beta = RDotR / RDotROld;

                for (int i = 0; i < n; i++)
                {
                    p.VectorData[i] = r.VectorData[i] + beta * p.VectorData[i];
                }

                count++;
            }

            float[] resp = new float[n];
            for (int i = 0; i < n; i++) resp[i] = x.VectorData[i];

            return resp;
        }


        /// <summary>Solves linear system Mx = b using conjugate gradient method. Writes variables to Device memory. Improves solution if accuracy is low.</summary>
        /// <param name="M">Matrix M</param>
        /// <param name="b">Vector b</param>
        /// <param name="tol">Error tolerance</param>
        public float[] LinSolveCL(CLImgSparseMatrix M, CLImgVector b, float tol)
        {
            if (b.Length != M.MatrixDimension) throw new Exception("M and x dimensions not compatible");
            int n = b.Length;
            
            tol = Math.Abs(tol);

            //Writes M to device memory
            M.WriteToDevice();

            //Backs up b data
            float[] bbkp = new float[n];
            for (int i = 0; i < n; i++) bbkp[i] = b.VectorData[i];

            //Residue variables
            double ResidueSumSquares = 1E100;
            double resAnt = 1E200;

            double[] dblResidues = new double[n];

            float[] Solution = new float[n];

            while (ResidueSumSquares > tol && Math.Abs(resAnt - ResidueSumSquares) >= tol && resAnt > ResidueSumSquares)
            {
                //Check if solution is not improving anymore
                resAnt = ResidueSumSquares;

                b.WriteToDevice();

                LinSolveCLStep(M, b, tol);

                //Solution
                x.ReadFromDevice();

                if (ResidueSumSquares == tol * 2)
                {
                    //Copies solution
                    for (int i = 0; i < n; i++)
                    {
                        Solution[i] = x.VectorData[i];
                    }
                }
                else
                {
                    //Improves solution
                    for (int i = 0; i < n; i++)
                    {
                        Solution[i] -= x.VectorData[i];
                        x.VectorData[i] = Solution[i];
                    }
                }

                //Compute residue sum of squares and improves solution
                dblResidues = ExactMultiply(M, x);
                ResidueSumSquares = 0;
                for (int i = 0; i < n; i++)
                {
                    //Computes residues
                    dblResidues[i] = dblResidues[i] - (double)bbkp[i];
                    ResidueSumSquares += dblResidues[i] * dblResidues[i];

                    b.VectorData[i] = (float)dblResidues[i];
                }
            }

            //Restores b data
            for (int i = 0; i < n; i++) b.VectorData[i] = bbkp[i];


            return Solution;
        }

        /// <summary>Solves linear system Mx = b using conjugate gradient method. Doesn't try to improve the solution obtained.</summary>
        /// <param name="M">Matrix M</param>
        /// <param name="b">Vector b</param>
        /// <param name="tol">Error tolerance</param>
        public void LinSolveCLStep(CLImgSparseMatrix M, CLImgVector b, float tol)
        {
            int n = b.Length;
            int nBy4 = 1 + ((n - 1) >> 2);

            if (lambda == null)
            {
                lambda = new float[1];
                CLlambda = new CLCalc.Program.Variable(lambda);
            }

            if (r == null || r.Length != n)
            {
                r = new CLImgVector(n);
                p = new CLImgVector(n);
                x = new CLImgVector(n);
                Ap = new CLImgVector(n);
                temp = new CLImgVector(n);
            }
            if (temp == null) temp = new CLImgVector(n);

            float alpha, beta, RDotROld, RDotR;

            //Initialization
            Multiply(M, x, Ap);

            CLCalc.Program.MemoryObject[] args = new CLCalc.Program.MemoryObject[] { b.CLVector, Ap.CLVector, r.CLVector, p.CLVector };
            kernelInitRP.Execute(args, nBy4);

            //Loop
            int count = 0;

            RDotR = DotProduct(r, r);

            while ((RDotR > tol) && (count < n*MAXITER))
            {
                RDotROld = RDotR;
                
                //if ((count & 0x0080) == 0)
                //{
                //    Multiply(M, x, Ap);

                //    args = new CLCalc.Program.MemoryObject[] { b.CLVector, Ap.CLVector, r.CLVector, p.CLVector };
                //    kernelInitRP.Execute(args, nBy4);
                //}

                Multiply(M, p, Ap);

                alpha = RDotROld / DotProduct(Ap, p);

                //Update x
                kernelCopyToTemp.Execute(new CLCalc.Program.MemoryObject[] { x.CLVector, temp.CLVector }, nBy4);
                lambda[0] = alpha; CLlambda.WriteToDevice(lambda);
                kernelMultiplyAdd.Execute(new CLCalc.Program.MemoryObject[] { CLlambda, p.CLVector, temp.CLVector, x.CLVector }, nBy4);

                //Update r
                kernelCopyToTemp.Execute(new CLCalc.Program.MemoryObject[] { r.CLVector, temp.CLVector }, nBy4);
                lambda[0] = -alpha; CLlambda.WriteToDevice(lambda);
                kernelMultiplyAdd.Execute(new CLCalc.Program.MemoryObject[] { CLlambda, Ap.CLVector, temp.CLVector, r.CLVector }, nBy4);

                RDotR = DotProduct(r, r);
                beta = RDotR / RDotROld;

                //Update p
                kernelCopyToTemp.Execute(new CLCalc.Program.MemoryObject[] { p.CLVector, temp.CLVector }, nBy4);
                lambda[0] = beta; CLlambda.WriteToDevice(lambda);
                kernelMultiplyAdd.Execute(new CLCalc.Program.MemoryObject[] { CLlambda, temp.CLVector, r.CLVector, p.CLVector }, nBy4);

                count++;
            }

        }


        #endregion

        #region OpenCL source

        /// <summary>OpenCL dot product source</summary>
        private class CLLinalgSrc
        {
            #region Linear system Conjugate Gradient

            public string srcLinConjGrad = @"

//initializes residue and p (gradient) vectors

__kernel void InitRP(__read_only   image2d_t b,
                     __read_only   image2d_t Ap,
                     __write_only  image2d_t r,
                     __write_only  image2d_t p)
{
  const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
    CLK_ADDRESS_CLAMP | //Clamp to zeros
    CLK_FILTER_NEAREST; //Don't interpolate

   int i = get_global_id(0);
   
   int2 coords; 
   coords.y = i >> 12;
   coords.x = i & 0xfff;
   
   float4 val = read_imagef(b, smp, coords) - read_imagef(Ap, smp, coords);

   write_imagef(r, coords, val);
   write_imagef(p, coords, val);
}

//Copies vector to a temporary image
__kernel void CopyToTemp(__read_only   image2d_t v,
                         __write_only  image2d_t temp)
{
  const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
    CLK_ADDRESS_CLAMP | //Clamp to zeros
    CLK_FILTER_NEAREST; //Don't interpolate
   int i = get_global_id(0);

   int2 coords; 
   coords.y = i >> 12;
   coords.x = i & 0xfff;
   
   float4 val = read_imagef(v, smp, coords);

   write_imagef(temp, coords, val);
}

//Multiply-add image values
__kernel void MultiplyAdd(__global float *     a,
                          __read_only  image2d_t b,
                          __read_only  image2d_t c,
                          __write_only image2d_t X)
{
  const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
    CLK_ADDRESS_CLAMP | //Clamp to zeros
    CLK_FILTER_NEAREST; //Don't interpolate
   int i = get_global_id(0);
   
   int2 coords; 
   coords.y = i >> 12;
   coords.x = i & 0xfff;
   
   float a0=a[0];
   
   float4 val = mad((float4)(a0,a0,a0,a0), read_imagef(b, smp, coords), read_imagef(c, smp, coords));

   write_imagef(X, coords, val);
}
";

            #endregion

            #region Dot product
            public string srcDotProd = @"

__kernel void dotProd (       __read_only   image2d_t v1, 
                              __read_only   image2d_t v2,
                       __global write_only  float *   dotproduct,
                       __global read_only   int   *   VecLengthBy4)
{
  const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
    CLK_ADDRESS_CLAMP | //Clamp to zeros
    CLK_FILTER_NEAREST; //Don't interpolate

  
  //Vector length divided by 4 (plus 1)
  //Gets initial and final 'pixel' of image to read
  int id = get_global_id(0);
  int n = get_global_size(0);

  int vLenBy4 = VecLengthBy4[0];
  int ivLenBy4 = vLenBy4*id;
  
  //Each worker has to compute VecLengthBy4/n sums
  int ind0 = (ivLenBy4)/n;
  int indf = (ivLenBy4+vLenBy4)/n;
  
  //Image coordinates  
  int2 coords;

  float4 sum = (float4)(0.0f,0.0f,0.0f,0.0f);

  float4 temp1, temp2;
  for (int i = ind0; i < indf; i++)
  {
     coords.y = i >> 12;
     coords.x = i & 0xfff;
     temp1 = read_imagef(v1, smp, coords);
     temp2 = read_imagef(v2, smp, coords);
     
     //sum = fma(temp1, temp2, sum);
     sum = mad(temp1, temp2, sum);
  }
  
  sum.x = sum.x+sum.y+sum.z+sum.w;

  //Sum to dot product.
  dotproduct[id] = sum.x;
  
}

__kernel void sumDotProd(__global float4 * dotproduct)
{
  int i = get_global_id(0);
  int n = get_global_size(0);
  dotproduct[i] += dotproduct[i+n];
}

__kernel void GetResp(__global float * dotproduct,
                      __global float * dott)
{
   dott[0]=dotproduct[0]+dotproduct[1]+dotproduct[2]+dotproduct[3];
}
";
            #endregion

            #region Sparse/full matrix vector product

            public string srcMatVecMult = @"

//execute with global_size(0) = 4*(1+(X.Length-1)/4)

float ReadVecFromImg(int ind, __read_only image2d_t img)
{
  const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
      CLK_ADDRESS_CLAMP | //Clamp to zeros
      CLK_FILTER_NEAREST; //Don't interpolate

  if (ind < 0) return 0;

  int imgPos = ind >> 2;  
  int2 coords;
  coords.x = imgPos & 0xfff;
  coords.y = imgPos >> 12;
  
  float4 temp = read_imagef(img, smp, coords);
  
  imgPos = ind & 0x0003;
  return imgPos < 2 ? 
         (imgPos == 0 ? temp.x : temp.y): 
         (imgPos == 2 ? temp.z : temp.w);
}

//GlobalWorkSize = 1+((nRows-1) >> 2)

__kernel void SparseMatrixVecMult(       __read_only        image2d_t M,
                                         __read_only        image2d_t MCols,
                                         __read_only        image2d_t X,
                                         __write_only       image2d_t y,
                                  __global read_only  int * NonZeroElemsPerRow)
                                  
{
  const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
      CLK_ADDRESS_CLAMP | //Clamp to zeros
      CLK_FILTER_NEAREST; //Don't interpolate
      
  int ii = get_global_id(0);
  int i4 = ii << 2;
  int nonZeroElems = NonZeroElemsPerRow[0];
  int nonZeroElemsBy4 = nonZeroElems >> 2;

  float psum[4];
  
  float4 sum;

  //Coordinates of images
  int2 coords;
  
  for (int i=0; i<4; i++)
  {
    int ind = nonZeroElemsBy4*(i4+i);
    
    //Matrix values
    int4 col;
    float4 mvals;
    
    //Retrieve vector values
    float4 vvals;
    
    sum = (float4)(0.0f,0.0f,0.0f,0.0f);
    
    for (int j = 0; j < nonZeroElemsBy4; j++)
    {
       coords.y = ind >> 12;
       coords.x = ind & 0xfff;
       
       //Matrix values  
       col   = read_imagei(MCols, smp, coords);
       mvals = read_imagef(M,     smp, coords);
       
       //Vector values
       vvals.x = ReadVecFromImg(col.x, X);
       vvals.y = ReadVecFromImg(col.y, X);
       vvals.z = ReadVecFromImg(col.z, X);
       vvals.w = ReadVecFromImg(col.w, X);
       
       sum = mad(mvals, vvals, sum);
       
       ind++;
    }
    
    psum[i] = sum.x+sum.y+sum.z+sum.w;

  }
  
  coords.y = ii >> 12;
  coords.x = ii & 0xfff;
  
  sum = (float4)(psum[0],psum[1],psum[2],psum[3]);
  
  write_imagef(y, coords, sum);
}

//GlobalWorkSize = 1+((nRows-1) >> 2)
__kernel void MatrixVecMult(       __read_only        image2d_t M,
                                   __read_only        image2d_t X,
                                   __write_only       image2d_t y,
                            __global read_only int *  nColumns)
                                  
{
  const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
      CLK_ADDRESS_CLAMP | //Clamp to zeros
      CLK_FILTER_NEAREST; //Don't interpolate
      
  int ii = get_global_id(0);
  int i4 = ii << 2;
  int n = nColumns[0];

  float psum[4];
  
  float4 sum;

  //Coordinates of images
  int2 coords;
  
  for (int i=0; i<4; i++)
  {
    int ind = n*(i4+i);
    int indV = 0;
    
    //Matrix values
    float4 mvals;
    
    //Retrieve vector values
    float4 vvals;
    
    sum = (float4)(0.0f,0.0f,0.0f,0.0f);
    
    for (int j = 0; j < n; j++)
    {
       coords.y = ind >> 12;
       coords.x = ind & 0xfff;
       //Matrix values  
       mvals = read_imagef(M,     smp, coords);
       
       coords.y = indV >> 12;
       coords.x = indV & 0xfff;
       //Vector values
       vvals = read_imagef(X,     smp, coords);
       
       sum = mad(mvals, vvals, sum);
       
       ind++; indV++;
    }
    
    psum[i] = sum.x+sum.y+sum.z+sum.w;

  }
  
  coords.y = ii >> 12;
  coords.x = ii & 0xfff;
  
  sum = (float4)(psum[0],psum[1],psum[2],psum[3]);
  
  write_imagef(y, coords, sum);
}

";
            #endregion

            #region Try greater precision
            public string src2 = @"

#define FACTOR 134217729


void Split(float4 * x, float4 * y, float4 * a)
{
  float4 c = FACTOR * (*a);
  *x = c-(c-*a);
  *y = *a - *x;
}

void TwoProduct(float4 * x, float4 * y, float4 * a, float4 * b)
{
  *x = (*a) * (*b);
  float4 a1, a2, b1, b2;

  Split(&a1, &a2, a);
  Split(&b1, &b2, b);
  *y = a2*b2-(((*x-a1*b1)-a2*b1)-a1*b2);
}

void TwoSum(float4 * x, float4 * y, float4 * a, float4 * b)
{
  *x = *a + (*b);
  float4 z = *x - *a;
  *y = *a-(*x-z)+(*b+z);
}

__kernel void dotProd (__read_only  image2d_t v1, 
                       __read_only  image2d_t v2,
                       __global     float *   dotproduct,
                       __global     int   *   VecLengthBy4)
{
  const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
    CLK_ADDRESS_CLAMP | //Clamp to zeros
    CLK_FILTER_NEAREST; //Don't interpolate

  
  //Vector length divided by 4 (plus 1)
  //Gets initial and final 'pixel' of image to read
  int id = get_global_id(0);
  int n = get_global_size(0);

  int vLenBy4 = VecLengthBy4[0];
  int ivLenBy4 = vLenBy4*id;
  
  //Each worker has to compute VecLengthBy4/n sums
  int ind0 = (ivLenBy4)/n;
  int indf = (ivLenBy4+vLenBy4)/n;
  
  //Image coordinates  
  int2 coords;

  float4 p = (float4)(0.0f,0.0f,0.0f,0.0f);
  float4 q = (float4)(0.0f,0.0f,0.0f,0.0f);
  
  float4 s = (float4)(0.0f,0.0f,0.0f,0.0f);
  float4 h = (float4)(0.0f,0.0f,0.0f,0.0f);
  float4 r = (float4)(0.0f,0.0f,0.0f,0.0f);

  float4 x1, y1;
  for (int i = ind0; i < indf; i++)
  {
     coords.y = i >> 12;
     coords.x = i & 0xfff;
     x1 = read_imagef(v1, smp, coords);
     y1 = read_imagef(v2, smp, coords);
   
     TwoProduct(&h, &r, &x1, &y1);
     TwoSum(&p, &q, &p, &h);
     s += q+r;
  }  
  
  p+=s;
  
  //Sum to dot product.
  dotproduct[id] = p.x+p.y+p.z+p.w;

}

";
            #endregion
        }
        #endregion

        #endregion
    }
}