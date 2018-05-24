using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using GASS.OpenCL;

namespace OpenCLTemplate
{
    public partial class Form1 : Form
    {
        /// <summary>Form1</summary>
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {



            CLCalc.InitCL();


            double[] a = new double[] { 2, 147483647, 2, 7 };
            double[] b = new double[] { 1, 2, 7, 4 };
            double[] c = new double[4];

            CLCalc.Program.Variable v1 = new CLCalc.Program.Variable(a);
            CLCalc.Program.Variable v2 = new CLCalc.Program.Variable(b);
            CLCalc.Program.Variable v3 = new CLCalc.Program.Variable(c);
            
            CLCalc.CLPrograms.VectorSum VecSum = new CLCalc.CLPrograms.VectorSum();
            CLCalc.CLPrograms.MinDifs Mdifs = new CLCalc.CLPrograms.MinDifs();

            //string[] s = new string[] { VecSum.intVectorSum, VecSum.floatVectorSum };
            string[] s = new string[] { VecSum.doubleVectorSum };


            CLCalc.Program.Compile(s);

            CLCalc.Program.Kernel k = new CLCalc.Program.Kernel("doubleVectorSum");
            //CLCalc.Program.Kernel k2 = new CLCalc.Program.Kernel("intVectorSum");
            //CLCalc.Program.Kernel k = new CLCalc.Program.Kernel("floatMinDifs");

            CLCalc.Program.Variable[] vv = new CLCalc.Program.Variable[3] { v1, v2, v3 };

            int[] max=new int[1] {a.Length};

            k.Execute(vv, max);

            CLCalc.Program.Sync();

            v3.ReadFromDeviceTo(c);

            CLCalc.FinishCL();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < max; i++)
            {
                for (int j = 0; j < max; j++)
                {
                    teste(i, j);
                }
            }
        }

        int max = 5;
        float[] resp = new float[25];
        float[] v1 = new float[] { 1, 2, 7, 4, 5 };
        float[] v2 = new float[] { 1, 2, 7, 4, 5 };
        private void teste(int i, int j)
        {
            int k = i + j;
            if (k >= max) k = k - max;

            resp[5 * i + j] = v1[i] - v2[k];
        }

        private void button3_Click(object sender, EventArgs e)
        {
            float[] x = new float[] { 1, 2, 3, 0.123f };
            float[] y = new float[] { 1, 2, 1, 1 };

            string s = @"
                       kernel void
                       sum (global float4 * x, global float4 * y)
                       {
                           x[0] = x[0] + y[0];
                       }
";

            CLCalc.Program.Compile(new string[] { s });
            CLCalc.Program.Kernel sum = new CLCalc.Program.Kernel("sum");
            
            CLCalc.Program.Variable varx=new CLCalc.Program.Variable(x);
            CLCalc.Program.Variable vary=new CLCalc.Program.Variable(y);
            CLCalc.Program.Variable[] args = { varx, vary };

            int[] max = new int[] { 1 };

            sum.Execute(args, max);

            varx.ReadFromDeviceTo(x);

            //float[] t = new float[1] { 0 };
            //float[] pos = new float[] { 1, 2, 3, 4, 5, 6 };
            //float[] vel = new float[] { 1, 0, 0, 0, 0, 0 };
            //float[] forces = new float[] { 0, 1, 0, 0, 0, 0 };

            //float[] masses = new float[] { 1, 1 };
            //float[] colSizes = new float[] { 0.1f, 0.1f };

            
            //CLCalc.InitCL();

            //CLCalc.CLPrograms.floatBodyPhysics phys = new CLCalc.CLPrograms.floatBodyPhysics(10);
            //CLCalc.CLPrograms.floatBodyPhysics phys2 = new CLCalc.CLPrograms.floatBodyPhysics(20);


            //CLCalc.FinishCL();
        }

        CLCalc.CLPrograms.doubleLinearAlgebra Linalg;
        private void btnLinalg_Click(object sender, EventArgs e)
        {
            if (Linalg == null) Linalg = new CLCalc.CLPrograms.doubleLinearAlgebra();
            //CLCalc.Program.DefaultCQ = 0;

            //float[,] M = new float[5, 3] { { 1, 1, 1 }, { 1, -1, -1 }, { -2, 3, 4 }, { -1, 1, 5 }, { 2, 4, 7 } };
            //float[] b = new float[5] { 1, 2, 3, 4, 5 };
            //float[] err;
            //float[] x = new float[3];

            //x = Linalg.LeastSquaresGS(M, b, x, out err);

            int n = 1500;
            Random rnd = new Random();
            double[,] M = new double[n, n];
            double[] b = new double[n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    M[i, j] = 1;
                    //M[i, j] = rnd.NextDouble();
                }
                M[i, i] += n;
                b[i] = i+1;
            }

            //double[] err;
            //double[] x = new double[n];

            //x = Linalg.LeastSquaresGS(M, b, x, out err);

            //Testes com LU decomp
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            //OpenCL
            sw.Start();
            double[] sol3 = Linalg.LinSolve(M, b, 1e-18, 20);
            sw.Stop();
            this.Text = " " + sw.Elapsed.TotalSeconds.ToString();
            sw.Reset();
            Application.DoEvents();

            //Convencional
            sw.Start();
            LinearAlgebra.Matrix MM = new LinearAlgebra.Matrix(M);
            double[] sol2 = MM.LinearSolve(b);
            sw.Stop();
            this.Text += " " + sw.Elapsed.TotalSeconds.ToString();
            sw.Reset();
            Application.DoEvents();



            double erro = 0;
            for (int i = 0; i < sol3.Length; i++) erro += (sol3[i] - sol2[i]) * (sol3[i] - sol2[i]);

            this.Text += " " + erro.ToString();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            CLCalc.CLPrograms.floatLinearAlgebra Linalg=new CLCalc.CLPrograms.floatLinearAlgebra();

            //int p = 1300, q = 1450, r = 1550;
            int p = 230, q = 245, r = 255;

            float[,] x = new float[p, q];
            float[,] y = new float[q, r];
            float[,] z1 = new float[p, r];

            Random rnd = new Random();
            for (int i = 0; i < p; i++) for (int j = 0; j < q; j++) x[i, j] = (float)rnd.NextDouble();
            for (int i = 0; i < q; i++) for (int j = 0; j < r; j++) y[i, j] = (float)rnd.NextDouble();

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            //GPU
            sw.Start();
            float[,] z = Linalg.MatrixMultiply(x, y);
            sw.Stop();
            this.Text = " " + sw.Elapsed.TotalSeconds.ToString() + "s";
            sw.Reset();

            //CPU multithread
            sw.Start();
            CLCalc.Program.DefaultCQ = 0;
            float[,] z2 = Linalg.MatrixMultiply(x, y);
            sw.Stop();
            this.Text += " " + sw.Elapsed.TotalSeconds.ToString() + "s";
            sw.Reset();


            sw.Start();

            for (int i = 0; i < p; i++)
            {
                for (int j = 0; j < r; j++)
                {
                    z1[i, j] = 0;
                    for (int k = 0; k < q; k++)
                    {
                        z1[i, j] += x[i, k] * y[k, j];
                    }
                }
            }

            sw.Stop();
            this.Text += " " + sw.Elapsed.TotalSeconds.ToString() + "s";
            sw.Reset();



            double dif = 0;
            for (int i = 0; i < p; i++) for (int j = 0; j < r; j++) dif += (z[i, j] - z1[i, j]) * (z[i, j] - z1[i, j]);
            this.Text += " Error:" + dif.ToString();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            nMassas = new int[1] { 10 };

            //Integrador de EDO
            float x0 = 0;
            float[] y0 = new float[2 * nMassas[0]];

            //y0[0]=posicao, y0[1] = velocidade
            for (int i = 0; i < 2 * nMassas[0]; i += 2)
            {
                y0[i] = 1;
            }

            CLCalc.CLPrograms.floatODE46 ode46 = new CLCalc.CLPrograms.floatODE46(x0, 0.005f, y0, MassaMola);

            //CLCalc.Program.DefaultCQ = 0;

            //Retorno de derivadas
            string[] s = new string[] {CLCalc.EnableDblSupport, floatDerivsMMola };
            CLCalc.Program.Compile(s);
            KernelDerivs = new CLCalc.Program.Kernel("floatDerivs");

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            ode46.Integrate(20);
            sw.Stop();
            this.Text = sw.ElapsedMilliseconds.ToString();
            Application.DoEvents();

            float[] y = ode46.State;

            ode46.Integrate(25);
            y = ode46.State;
            float[] yerr = ode46.AbsError;
            float x = ode46.IndepVar;
        }


//        private string floatDerivsLorent = @"
//                                __kernel void floatDerivs( __global       float * x,
//                                                           __global       float * y,
//                                                           __global       float * dydx)
//                                {
//                                    int i = get_global_id(0);
//
//                                    //Lorentz
//                                    float sigma = 10;
//                                    float rho = 28;
//                                    float beta = 2.66666666666667;
//                                    if (i==0) dydx[0] = sigma*(y[1]-y[0]);
//                                    if (i==1) dydx[1] = y[0]*(rho-y[2])-y[1];
//                                    if (i==2) dydx[2] = y[0]*y[1]-beta*y[2];
//                                }
//                    ";


        private string floatDerivsMMola = @"
                                __kernel void floatDerivs( __global read_only float * x,
                                                           __global read_only float * y,
                                                           __global           float * dydx)
                                {
                                    int i = get_global_id(0);
                                    int n = get_global_size(0);

                                    //Derivada da posicao = velocidade
                                    dydx[2*i] = y[2*i + 1];

                                    if (i == 0) dydx[1] = - 0.2f * y[0];
                                    else dydx[2*i + 1] = - 0.2f * (y[2*i] - y[2*i - 2]);

                                    if (i < n - 1) dydx[2*i + 1] += - 0.2f * (y[2*i] - y[2*i+2]);
                                }
                    ";

//        private string doubleDerivsMMola = @"
//                                __kernel void doubleDerivs( __global read_only double * x,
//                                                           __global read_only double * y,
//                                                           __global           double * dydx)
//                                {
//                                    int i = get_global_id(0);
//                                    int n = get_global_size(0);
//
//                                    //Derivada da posicao = velocidade
//                                    dydx[2*i] = y[2*i + 1];
//
//                                    if (i == 0) dydx[1] = - 0.2 * y[0];
//                                    else dydx[2*i + 1] = - 0.2 * (y[2*i] - y[2*i - 2]);
//
//                                    if (i < n - 1) dydx[2*i + 1] += - 0.2 * (y[2*i] - y[2*i+2]);
//                                }
//                    ";

        CLCalc.Program.Kernel KernelDerivs;
        int[] nMassas;
        private void MassaMola(CLCalc.Program.Variable x, CLCalc.Program.Variable y, CLCalc.Program.Variable dydx)
        {
            CLCalc.Program.Variable[] args = new CLCalc.Program.Variable[3] { x, y, dydx };
            KernelDerivs.Execute(args, nMassas);
        }

        CLCalc.CLPrograms.floatLinearAlgebra floatLinalg = new CLCalc.CLPrograms.floatLinearAlgebra();
        private void btnLinalgLU_Click(object sender, EventArgs e)
        {
            if (Linalg == null) Linalg = new CLCalc.CLPrograms.doubleLinearAlgebra();

            float[,] M = new float[,] { { 3, 100, 2, 3 }, { 4, -1, 1, 10 }, { 5, -20, 1, 1 }, { 6, 1, 200, 3 } };
            float[] b = new float[] { 1, 2, 3, 4 };

            double[,] M2 = new double[,] { { 3, 100, 2, 3 }, { 4, -1, 1, 10 }, { 5, -20, 1, 1 }, { 6, 1, 200, 3 } };
            double[] b2 = new double[] { 1, 2, 3, 4 };

            //float[,] M = new float[,] { { 3, 1, 1 }, { 2, -10, 1 }, { 15, 1, 1 } };
            //float[] b = new float[] { 1, 2, 3 };

            //double[,] M2 = new double[,] { { 3, 1, 1 }, { 2, -10, 1 }, { 15, 1, 1 } };
            //double[] b2 = new double[] { 1, 2, 3 };

            LinearAlgebra.Matrix MM = new LinearAlgebra.Matrix(M2);
            double[] sol2 = MM.LinearSolve(b2);

            double[] sol3 = Linalg.LinSolve(M2, b2, 1e-10, 10);

            float[] sol = floatLinalg.LinSolve(M, b, 1e-5f, 10);
        }


    }
}
