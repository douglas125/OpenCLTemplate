using System;
using System.Collections.Generic;
using OpenCLTemplate;
using System.Text;

namespace OpenCLTemplate.DifferentialEquations
{
    /// <summary>Float differential equation integrator</summary>
    public class floatODE46
    {
        #region Kernels
        /// <summary>Writes final Y values and estimated absolute errors</summary>
        private CLCalc.Program.Kernel KernelFinalizeCalc;
        /// <summary>Updates X to current time</summary>
        private CLCalc.Program.Kernel KernelUpdateX;
        private CLCalc.Program.Kernel KernelRK46YStep2;
        private CLCalc.Program.Kernel KernelRK46XStep2;
        private CLCalc.Program.Kernel KernelRK46YStep3;
        private CLCalc.Program.Kernel KernelRK46XStep3;
        private CLCalc.Program.Kernel KernelRK46YStep4;
        private CLCalc.Program.Kernel KernelRK46XStep4;
        private CLCalc.Program.Kernel KernelRK46YStep5;
        private CLCalc.Program.Kernel KernelRK46XStep5;
        private CLCalc.Program.Kernel KernelRK46YStep6;
        private CLCalc.Program.Kernel KernelRK46XStep6;
        #endregion

        #region Kernel arguments
        CLCalc.Program.Variable[] ArgsFinalize;
        CLCalc.Program.Variable[] ArgsRK46Y;
        CLCalc.Program.Variable[] ArgsRK46X;

        int[] NStates, NScalar;
        #endregion

        #region OpenCL Variables
        /// <summary>Independent variable current value in OpenCL memory</summary>
        public CLCalc.Program.Variable x;
        /// <summary>Dynamic system current state in OpenCL memory</summary>
        public CLCalc.Program.Variable y;
        private CLCalc.Program.Variable xsav;
        private CLCalc.Program.Variable hdid;
        private CLCalc.Program.Variable ysav;
        private CLCalc.Program.Variable k1;
        private CLCalc.Program.Variable k2;
        private CLCalc.Program.Variable k3;
        private CLCalc.Program.Variable k4;
        private CLCalc.Program.Variable k5;
        private CLCalc.Program.Variable k6;
        private CLCalc.Program.Variable absError;
        #endregion

        #region Integrate control variables
        float currentX = 0;
        float currentStep = 0;
        #endregion

        /// <summary>Function to calculate derivatives vector</summary>
        /// <param name="x">IN: Scalar. Independent variable.</param>
        /// <param name="y">IN: State-space vector.</param>
        /// <param name="dydx">OUT: Derivatives</param>
        public delegate void DerivCalcDeleg(CLCalc.Program.Variable x, CLCalc.Program.Variable y, CLCalc.Program.Variable dydx);

        /// <summary>Derivative calculator</summary>
        public DerivCalcDeleg Derivs;

        /// <summary>Constructor.</summary>
        /// <param name="InitialState">Initial state of system</param>
        /// <param name="StepSize">Desired step per integration pass</param>
        /// <param name="InitialIndepVarValue">Initial independent variable value</param>
        /// <param name="DerivativeCalculator">Function to calculate derivatives vector</param>
        public floatODE46(float InitialIndepVarValue, float StepSize, float[] InitialState, DerivCalcDeleg DerivativeCalculator)
        {
            if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.Unknown)
            {
                CLCalc.InitCL();
            }

            if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.NotUsingCL)
                throw new Exception("OpenCL not available");

            if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.UsingCL)
            {
                ODE46Source Source = new ODE46Source();
                string[] s = new string[] { Source.floatStep2, Source.floatStep3, Source.floatStep4, Source.floatStep5, Source.floatStep6, Source.floatFinalizeCalc };
                CLCalc.Program.Compile(s);

                //Calculador de derivada
                Derivs = DerivativeCalculator;

                //Scalars
                float[] xx = new float[1] { InitialIndepVarValue };
                x = new CLCalc.Program.Variable(xx);
                xsav = new CLCalc.Program.Variable(xx);

                //Sets initial values to Device and local variables
                hdid = new CLCalc.Program.Variable(xx);
                currentX = InitialIndepVarValue;
                SetStep(StepSize);

                //Vectors
                yy = new float[InitialState.Length];
                for (int i = 0; i < InitialState.Length; i++) yy[i] = InitialState[i];


                ysav = new CLCalc.Program.Variable(yy);
                k1 = new CLCalc.Program.Variable(InitialState);
                k2 = new CLCalc.Program.Variable(InitialState);
                k3 = new CLCalc.Program.Variable(InitialState);
                k4 = new CLCalc.Program.Variable(InitialState);
                k5 = new CLCalc.Program.Variable(InitialState);
                k6 = new CLCalc.Program.Variable(InitialState);
                absError = new CLCalc.Program.Variable(new float[InitialState.Length]);

                y = new CLCalc.Program.Variable(yy);

                //Kernels
                KernelFinalizeCalc = new CLCalc.Program.Kernel("floatFinalizeCalc");
                KernelUpdateX = new CLCalc.Program.Kernel("floatUpdateX");
                KernelRK46YStep2 = new CLCalc.Program.Kernel("floatYStep2");
                KernelRK46XStep2 = new CLCalc.Program.Kernel("floatXStep2");
                KernelRK46YStep3 = new CLCalc.Program.Kernel("floatYStep3");
                KernelRK46XStep3 = new CLCalc.Program.Kernel("floatXStep3");
                KernelRK46YStep4 = new CLCalc.Program.Kernel("floatYStep4");
                KernelRK46XStep4 = new CLCalc.Program.Kernel("floatXStep4");
                KernelRK46YStep5 = new CLCalc.Program.Kernel("floatYStep5");
                KernelRK46XStep5 = new CLCalc.Program.Kernel("floatXStep5");
                KernelRK46YStep6 = new CLCalc.Program.Kernel("floatYStep6");
                KernelRK46XStep6 = new CLCalc.Program.Kernel("floatXStep6");


                //Kernel arguments
                ArgsFinalize = new CLCalc.Program.Variable[] { x, hdid, y, ysav, absError, k1, k2, k3, k4, k5, k6 };
                ArgsRK46Y = new CLCalc.Program.Variable[] { x, hdid, y, ysav, k1, k2, k3, k4, k5, k6 };
                ArgsRK46X = new CLCalc.Program.Variable[] { x, hdid, xsav };
                NStates = new int[1] { InitialState.Length };
                NScalar = new int[1] { 1 };

                //Data retrieving
                yerr = new float[NStates[0]];
                xRet = new float[NScalar[0]];

            }

        }

        /// <summary>Integrates equation set to a final value using current stepsize. Ideally, final value 
        /// and currentX should multiples of stepsize.</summary>
        /// <param name="FinalValue">Final value to reach.</param>
        public void Integrate(float FinalValue)
        {
            if (FinalValue < currentX) throw new Exception("Final value already reached");
            if (currentStep <= 0) throw new Exception("Bad stepsize");

            //Number of full steps
            int nSteps = (int)((FinalValue - currentX) / currentStep);

            //Full steps
            for (int i = 0; i < nSteps; i++) this.Step();

            //Partial step
            currentX += nSteps * currentStep;
        }

        /// <summary>Sets current state</summary>
        /// <param name="indepVar">New independent variable value</param>
        /// <param name="State">New state values</param>
        public void ResetState(float indepVar, float[] State)
        {
            if (State.Length != NStates[0]) throw new Exception("Invalid State.Length, should be " + NStates[0].ToString());
            currentX = indepVar;
            x.WriteToDevice(new float[] { indepVar });
            y.WriteToDevice(State);

            absError.WriteToDevice(new float[NStates[0]]);
        }

        #region Space state information retrieval
        float[] yy;
        /// <summary>Gets current values of space-state variables (from Device).</summary>
        public float[] State
        {
            get
            {
                y.ReadFromDeviceTo(yy);
                return yy;
            }
        }

        float[] yerr;
        /// <summary>Gets current absolute error sum</summary>
        public float[] AbsError
        {
            get
            {
                absError.ReadFromDeviceTo(yerr);
                return yerr;
            }
        }

        float[] xRet;
        /// <summary>Gets current independent variable value (from Device).</summary>
        public float IndepVar
        {
            get
            {
                x.ReadFromDeviceTo(xRet);
                return xRet[0];
            }
        }
        #endregion

        #region Differential equation stepping
        /// <summary>Takes an integration step. Saves and returns stepsize back to what it was.</summary>
        /// <param name="StepSize">Step size to use</param>
        private void Step(float StepSize)
        {
            float StepBkp = currentStep;
            SetStep(StepSize);
            Step();
            SetStep(StepBkp);
        }

        /// <summary>Takes an integration step</summary>
        public void Step()
        {
            #region Salva as variaveis iniciais


            CLCalc.Program.CommQueues[CLCalc.Program.DefaultCQ].CopyBuffer<float>((Cloo.ComputeBuffer<float>)x.VarPointer, (Cloo.ComputeBuffer<float>)xsav.VarPointer, null);
            CLCalc.Program.CommQueues[CLCalc.Program.DefaultCQ].CopyBuffer<float>((Cloo.ComputeBuffer<float>)y.VarPointer, (Cloo.ComputeBuffer<float>)ysav.VarPointer, null);


            #endregion

            //Step 1
            Derivs(x, y, k1);

            //Step 2
            KernelRK46YStep2.Execute(ArgsRK46Y, NStates);
            KernelRK46XStep2.Execute(ArgsRK46X, NScalar);
            Derivs(x, y, k2);
            //Step 3
            KernelRK46YStep3.Execute(ArgsRK46Y, NStates);
            KernelRK46XStep3.Execute(ArgsRK46X, NScalar);
            Derivs(x, y, k3);
            //Step 4
            KernelRK46YStep4.Execute(ArgsRK46Y, NStates);
            KernelRK46XStep4.Execute(ArgsRK46X, NScalar);
            Derivs(x, y, k4);
            //Step 5
            KernelRK46YStep5.Execute(ArgsRK46Y, NStates);
            KernelRK46XStep5.Execute(ArgsRK46X, NScalar);
            Derivs(x, y, k5);
            //Step 6
            KernelRK46YStep6.Execute(ArgsRK46Y, NStates);
            KernelRK46XStep6.Execute(ArgsRK46X, NScalar);
            Derivs(x, y, k6);

            //Finalization
            KernelFinalizeCalc.Execute(ArgsFinalize, NStates);
            KernelUpdateX.Execute(ArgsRK46X, NScalar);

        }

        /// <summary>Sets step size.</summary>
        /// <param name="StepSize">Step size to use</param>
        public void SetStep(float StepSize)
        {
            if (StepSize <= 0) throw new Exception("Bad step size: Step size <= 0");
            hdid.WriteToDevice(new float[] { StepSize });
            currentStep = StepSize;
        }
        #endregion

        #region OpenCL Source
        /// <summary>OpenCL source</summary>
        private class ODE46Source
        {
            #region Runge-Kutta steps
            public string floatStep2 = @"
                                __kernel void floatYStep2(     __global       float * x,
                                                               __global       float * hdid,
                                                               __global write_only float * y,
                                                               __global       float * ysav,
                                                               __global       float * k1,
                                                               __global       float * k2,
                                                               __global       float * k3,
                                                               __global       float * k4,
                                                               __global       float * k5,
                                                               __global       float * k6)
                                {
                                    int i = get_global_id(0);
                                    y[i] = ysav[i] + 0.5 * hdid[0] * k1[i];
                                }

                                __kernel void floatXStep2(     __global       float * x,
                                                               __global       float * hdid,
                                                               __global       float * xsav)
                                {
                                    x[0] = xsav[0] + hdid[0] * 0.5;
                                }

                    ";

            public string floatStep3 = @"
                                __kernel void floatYStep3(     __global       float * x,
                                                               __global       float * hdid,
                                                               __global write_only float * y,
                                                               __global       float * ysav,
                                                               __global       float * k1,
                                                               __global       float * k2,
                                                               __global       float * k3,
                                                               __global       float * k4,
                                                               __global       float * k5,
                                                               __global       float * k6)
                                {
                                    int i = get_global_id(0);
                                    y[i] = ysav[i] + 0.25 * hdid[0] * k1[i] + 0.25 * hdid[0] * k2[i];
                                }

                                __kernel void floatXStep3(     __global       float * x,
                                                               __global       float * hdid,
                                                               __global       float * xsav)
                                {
                                    x[0] = xsav[0] + hdid[0] * 0.5;
                                }

                    ";

            public string floatStep4 = @"
                                __kernel void floatYStep4(     __global       float * x,
                                                               __global       float * hdid,
                                                               __global write_only float * y,
                                                               __global       float * ysav,
                                                               __global       float * k1,
                                                               __global       float * k2,
                                                               __global       float * k3,
                                                               __global       float * k4,
                                                               __global       float * k5,
                                                               __global       float * k6)
                                {
                                    int i = get_global_id(0);
                                    y[i] = ysav[i] - hdid[0] * k2[i] + 2 * hdid[0] * k3[i];
                                }

                                __kernel void floatXStep4(     __global       float * x,
                                                               __global       float * hdid,
                                                               __global       float * xsav)
                                {
                                    x[0] = xsav[0] + hdid[0];
                                }

                    ";

            public string floatStep5 = @"
                                __kernel void floatYStep5(     __global       float * x,
                                                               __global       float * hdid,
                                                               __global write_only float * y,
                                                               __global       float * ysav,
                                                               __global       float * k1,
                                                               __global       float * k2,
                                                               __global       float * k3,
                                                               __global       float * k4,
                                                               __global       float * k5,
                                                               __global       float * k6)
                                {
                                    int i = get_global_id(0);
                                    y[i] = ysav[i] + hdid[0] * 0.037037037037037 * (7 * k1[i] + 10 * k2[i] + k4[i]);
                                }

                                __kernel void floatXStep5(     __global       float * x,
                                                               __global       float * hdid,
                                                               __global       float * xsav)
                                {
                                    x[0] = xsav[0] + 0.666666666666667 * hdid[0];
                                }

                    ";

            public string floatStep6 = @"
                                __kernel void floatYStep6(     __global       float * x,
                                                               __global       float * hdid,
                                                               __global write_only float * y,
                                                               __global       float * ysav,
                                                               __global       float * k1,
                                                               __global       float * k2,
                                                               __global       float * k3,
                                                               __global       float * k4,
                                                               __global       float * k5,
                                                               __global       float * k6)
                                {
                                    int i = get_global_id(0);
                                    y[i] = ysav[i] + 0.0016 * hdid[0] * (28 * k1[i] - 125 * k2[i] + 546 * k3[i] + 54 * k4[i] - 378 * k5[i]);
                                }

                                __kernel void floatXStep6(     __global       float * x,
                                                               __global       float * hdid,
                                                               __global       float * xsav)
                                {
                                    x[0] = xsav[0] + 0.2 * hdid[0];
                                }

                    ";
            #endregion

            public string floatFinalizeCalc = @"
                                __kernel void floatFinalizeCalc( __global            float * x,
                                                                 __global            float * hdid,
                                                                 __global write_only float * y,
                                                                 __global            float * ysav,
                                                                 __global            float * absError,
                                                                 __global            float * k1,
                                                                 __global            float * k2,
                                                                 __global            float * k3,
                                                                 __global            float * k4,
                                                                 __global            float * k5,
                                                                 __global            float * k6)
                                {
                                    int i = get_global_id(0);
                                    float temp = hdid[0] * 0.00297619047619048 * (-42 * k1[i] - 224 * k3[i] - 21 * k4[i] + 162 * k5[i] + 125 * k6[i]);
                                    y[i] = ysav[i] + hdid[0] * 0.166666666666667 * (k1[i] + 4 * k3[i] + k4[i]) + temp;
                                    if (temp > 0) absError[i] += temp;
                                    else absError[i] -= temp;
                                }

                                __kernel void floatUpdateX(    __global       float * x,
                                                               __global       float * hdid,
                                                               __global       float * xsav)
                                {
                                    x[0] = xsav[0] + hdid[0];
                                }
                    ";
        }
        #endregion
    }

    /// <summary>double differential equation integrator</summary>
    public class doubleODE46
    {
        #region Kernels
        /// <summary>Writes final Y values and estimated absolute errors</summary>
        private CLCalc.Program.Kernel KernelFinalizeCalc;
        /// <summary>Updates X to current time</summary>
        private CLCalc.Program.Kernel KernelUpdateX;
        private CLCalc.Program.Kernel KernelRK46YStep2;
        private CLCalc.Program.Kernel KernelRK46XStep2;
        private CLCalc.Program.Kernel KernelRK46YStep3;
        private CLCalc.Program.Kernel KernelRK46XStep3;
        private CLCalc.Program.Kernel KernelRK46YStep4;
        private CLCalc.Program.Kernel KernelRK46XStep4;
        private CLCalc.Program.Kernel KernelRK46YStep5;
        private CLCalc.Program.Kernel KernelRK46XStep5;
        private CLCalc.Program.Kernel KernelRK46YStep6;
        private CLCalc.Program.Kernel KernelRK46XStep6;
        #endregion

        #region Kernel arguments
        CLCalc.Program.Variable[] ArgsFinalize;
        CLCalc.Program.Variable[] ArgsRK46Y;
        CLCalc.Program.Variable[] ArgsRK46X;

        int[] NStates, NScalar;
        #endregion

        #region OpenCL Variables
        /// <summary>Independent variable current value in OpenCL memory</summary>
        public CLCalc.Program.Variable x;
        /// <summary>Dynamic system current state in OpenCL memory</summary>
        public CLCalc.Program.Variable y;
        private CLCalc.Program.Variable xsav;
        private CLCalc.Program.Variable hdid;
        private CLCalc.Program.Variable ysav;
        private CLCalc.Program.Variable k1;
        private CLCalc.Program.Variable k2;
        private CLCalc.Program.Variable k3;
        private CLCalc.Program.Variable k4;
        private CLCalc.Program.Variable k5;
        private CLCalc.Program.Variable k6;
        private CLCalc.Program.Variable absError;
        #endregion

        #region Integrate control variables
        double currentX = 0;
        double currentStep = 0;
        #endregion

        /// <summary>Function to calculate derivatives vector</summary>
        /// <param name="x">IN: Scalar. Independent variable.</param>
        /// <param name="y">IN: State-space vector.</param>
        /// <param name="dydx">OUT: Derivatives</param>
        public delegate void DerivCalcDeleg(CLCalc.Program.Variable x, CLCalc.Program.Variable y, CLCalc.Program.Variable dydx);

        /// <summary>Derivative calculator</summary>
        public DerivCalcDeleg Derivs;

        /// <summary>Constructor.</summary>
        /// <param name="InitialState">Initial state of system</param>
        /// <param name="StepSize">Desired step per integration pass</param>
        /// <param name="InitialIndepVarValue">Initial independent variable value</param>
        /// <param name="DerivativeCalculator">Function to calculate derivatives vector</param>
        public doubleODE46(double InitialIndepVarValue, double StepSize, double[] InitialState, DerivCalcDeleg DerivativeCalculator)
        {
            if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.Unknown)
            {
                CLCalc.InitCL();
            }

            if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.NotUsingCL)
                throw new Exception("OpenCL not available");

            if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.UsingCL)
            {
                ODE46Source Source = new ODE46Source();
                string[] s = new string[] { @"
                            #pragma OPENCL EXTENSION cl_khr_fp64 : enable
                            ", Source.doubleStep2, Source.doubleStep3, Source.doubleStep4, Source.doubleStep5, Source.doubleStep6, Source.doubleFinalizeCalc };
                CLCalc.Program.Compile(s);

                //Calculador de derivada
                Derivs = DerivativeCalculator;

                //Scalars
                double[] xx = new double[1] { InitialIndepVarValue };
                x = new CLCalc.Program.Variable(xx);
                xsav = new CLCalc.Program.Variable(xx);

                //Sets initial values to Device and local variables
                hdid = new CLCalc.Program.Variable(xx);
                currentX = InitialIndepVarValue;
                SetStep(StepSize);

                //Vectors
                yy = new double[InitialState.Length];
                for (int i = 0; i < InitialState.Length; i++) yy[i] = InitialState[i];


                ysav = new CLCalc.Program.Variable(yy);
                k1 = new CLCalc.Program.Variable(InitialState);
                k2 = new CLCalc.Program.Variable(InitialState);
                k3 = new CLCalc.Program.Variable(InitialState);
                k4 = new CLCalc.Program.Variable(InitialState);
                k5 = new CLCalc.Program.Variable(InitialState);
                k6 = new CLCalc.Program.Variable(InitialState);
                absError = new CLCalc.Program.Variable(new double[InitialState.Length]);

                y = new CLCalc.Program.Variable(yy);

                //Kernels
                KernelFinalizeCalc = new CLCalc.Program.Kernel("doubleFinalizeCalc");
                KernelUpdateX = new CLCalc.Program.Kernel("doubleUpdateX");
                KernelRK46YStep2 = new CLCalc.Program.Kernel("doubleYStep2");
                KernelRK46XStep2 = new CLCalc.Program.Kernel("doubleXStep2");
                KernelRK46YStep3 = new CLCalc.Program.Kernel("doubleYStep3");
                KernelRK46XStep3 = new CLCalc.Program.Kernel("doubleXStep3");
                KernelRK46YStep4 = new CLCalc.Program.Kernel("doubleYStep4");
                KernelRK46XStep4 = new CLCalc.Program.Kernel("doubleXStep4");
                KernelRK46YStep5 = new CLCalc.Program.Kernel("doubleYStep5");
                KernelRK46XStep5 = new CLCalc.Program.Kernel("doubleXStep5");
                KernelRK46YStep6 = new CLCalc.Program.Kernel("doubleYStep6");
                KernelRK46XStep6 = new CLCalc.Program.Kernel("doubleXStep6");


                //Kernel arguments
                ArgsFinalize = new CLCalc.Program.Variable[] { x, hdid, y, ysav, absError, k1, k2, k3, k4, k5, k6 };
                ArgsRK46Y = new CLCalc.Program.Variable[] { x, hdid, y, ysav, k1, k2, k3, k4, k5, k6 };
                ArgsRK46X = new CLCalc.Program.Variable[] { x, hdid, xsav };
                NStates = new int[1] { InitialState.Length };
                NScalar = new int[1] { 1 };

                //Data retrieving
                yerr = new double[NStates[0]];
                xRet = new double[NScalar[0]];

            }

        }

        /// <summary>Integrates equation set to a final value using current stepsize. Ideally, final value 
        /// and currentX should multiples of stepsize.</summary>
        /// <param name="FinalValue">Final value to reach.</param>
        public void Integrate(double FinalValue)
        {
            if (FinalValue < currentX) throw new Exception("Final value already reached");
            if (currentStep <= 0) throw new Exception("Bad stepsize");

            //Number of full steps
            int nSteps = (int)((FinalValue - currentX) / currentStep);

            //Full steps
            for (int i = 0; i < nSteps; i++) this.Step();

            //Partial step
            currentX += nSteps * currentStep;
        }

        /// <summary>Sets current state</summary>
        /// <param name="indepVar">New independent variable value</param>
        /// <param name="State">New state values</param>
        public void ResetState(double indepVar, double[] State)
        {
            if (State.Length != NStates[0]) throw new Exception("Invalid State.Length, should be " + NStates[0].ToString());
            currentX = indepVar;
            x.WriteToDevice(new double[] { indepVar });
            y.WriteToDevice(State);

            absError.WriteToDevice(new double[NStates[0]]);
        }

        #region Space state information retrieval
        double[] yy;
        /// <summary>Gets current values of space-state variables (from Device).</summary>
        public double[] State
        {
            get
            {
                y.ReadFromDeviceTo(yy);
                return yy;
            }
        }

        double[] yerr;
        /// <summary>Gets current absolute error sum</summary>
        public double[] AbsError
        {
            get
            {
                absError.ReadFromDeviceTo(yerr);
                return yerr;
            }
        }

        double[] xRet;
        /// <summary>Gets current independent variable value (from Device).</summary>
        public double IndepVar
        {
            get
            {
                x.ReadFromDeviceTo(xRet);
                return xRet[0];
            }
        }
        #endregion

        #region Differential equation stepping
        /// <summary>Takes an integration step. Saves and returns stepsize back to what it was.</summary>
        /// <param name="StepSize">Step size to use</param>
        private void Step(double StepSize)
        {
            double StepBkp = currentStep;
            SetStep(StepSize);
            Step();
            SetStep(StepBkp);
        }

        /// <summary>Takes an integration step</summary>
        public void Step()
        {
            #region Salva as variaveis iniciais

            CLCalc.Program.CommQueues[CLCalc.Program.DefaultCQ].CopyBuffer<double>((Cloo.ComputeBuffer<double>)x.VarPointer, (Cloo.ComputeBuffer<double>)xsav.VarPointer, null);
            CLCalc.Program.CommQueues[CLCalc.Program.DefaultCQ].CopyBuffer<double>((Cloo.ComputeBuffer<double>)y.VarPointer, (Cloo.ComputeBuffer<double>)ysav.VarPointer, null);

            #endregion

            //Step 1
            Derivs(x, y, k1);

            //Step 2
            KernelRK46YStep2.Execute(ArgsRK46Y, NStates);
            KernelRK46XStep2.Execute(ArgsRK46X, NScalar);
            Derivs(x, y, k2);
            //Step 3
            KernelRK46YStep3.Execute(ArgsRK46Y, NStates);
            KernelRK46XStep3.Execute(ArgsRK46X, NScalar);
            Derivs(x, y, k3);
            //Step 4
            KernelRK46YStep4.Execute(ArgsRK46Y, NStates);
            KernelRK46XStep4.Execute(ArgsRK46X, NScalar);
            Derivs(x, y, k4);
            //Step 5
            KernelRK46YStep5.Execute(ArgsRK46Y, NStates);
            KernelRK46XStep5.Execute(ArgsRK46X, NScalar);
            Derivs(x, y, k5);
            //Step 6
            KernelRK46YStep6.Execute(ArgsRK46Y, NStates);
            KernelRK46XStep6.Execute(ArgsRK46X, NScalar);
            Derivs(x, y, k6);

            //Finalization
            KernelFinalizeCalc.Execute(ArgsFinalize, NStates);
            KernelUpdateX.Execute(ArgsRK46X, NScalar);

        }

        /// <summary>Sets step size.</summary>
        /// <param name="StepSize">Step size to use</param>
        public void SetStep(double StepSize)
        {
            if (StepSize <= 0) throw new Exception("Bad step size: Step size <= 0");
            hdid.WriteToDevice(new double[] { StepSize });
            currentStep = StepSize;
        }
        #endregion

        #region OpenCL Source
        /// <summary>OpenCL source</summary>
        private class ODE46Source
        {
            #region Runge-Kutta steps
            public string doubleStep2 = @"
                                __kernel void doubleYStep2(     __global       double * x,
                                                               __global       double * hdid,
                                                               __global write_only double * y,
                                                               __global       double * ysav,
                                                               __global       double * k1,
                                                               __global       double * k2,
                                                               __global       double * k3,
                                                               __global       double * k4,
                                                               __global       double * k5,
                                                               __global       double * k6)
                                {
                                    int i = get_global_id(0);
                                    y[i] = ysav[i] + 0.5 * hdid[0] * k1[i];
                                }

                                __kernel void doubleXStep2(     __global       double * x,
                                                               __global       double * hdid,
                                                               __global       double * xsav)
                                {
                                    x[0] = xsav[0] + hdid[0] * 0.5;
                                }

                    ";

            public string doubleStep3 = @"
                                __kernel void doubleYStep3(     __global       double * x,
                                                               __global       double * hdid,
                                                               __global write_only double * y,
                                                               __global       double * ysav,
                                                               __global       double * k1,
                                                               __global       double * k2,
                                                               __global       double * k3,
                                                               __global       double * k4,
                                                               __global       double * k5,
                                                               __global       double * k6)
                                {
                                    int i = get_global_id(0);
                                    y[i] = ysav[i] + 0.25 * hdid[0] * k1[i] + 0.25 * hdid[0] * k2[i];
                                }

                                __kernel void doubleXStep3(     __global       double * x,
                                                               __global       double * hdid,
                                                               __global       double * xsav)
                                {
                                    x[0] = xsav[0] + hdid[0] * 0.5;
                                }

                    ";

            public string doubleStep4 = @"
                                __kernel void doubleYStep4(     __global       double * x,
                                                               __global       double * hdid,
                                                               __global write_only double * y,
                                                               __global       double * ysav,
                                                               __global       double * k1,
                                                               __global       double * k2,
                                                               __global       double * k3,
                                                               __global       double * k4,
                                                               __global       double * k5,
                                                               __global       double * k6)
                                {
                                    int i = get_global_id(0);
                                    y[i] = ysav[i] - hdid[0] * k2[i] + 2 * hdid[0] * k3[i];
                                }

                                __kernel void doubleXStep4(     __global       double * x,
                                                               __global       double * hdid,
                                                               __global       double * xsav)
                                {
                                    x[0] = xsav[0] + hdid[0];
                                }

                    ";

            public string doubleStep5 = @"
                                __kernel void doubleYStep5(     __global       double * x,
                                                               __global       double * hdid,
                                                               __global write_only double * y,
                                                               __global       double * ysav,
                                                               __global       double * k1,
                                                               __global       double * k2,
                                                               __global       double * k3,
                                                               __global       double * k4,
                                                               __global       double * k5,
                                                               __global       double * k6)
                                {
                                    int i = get_global_id(0);
                                    y[i] = ysav[i] + hdid[0] * 0.037037037037037 * (7 * k1[i] + 10 * k2[i] + k4[i]);
                                }

                                __kernel void doubleXStep5(     __global       double * x,
                                                               __global       double * hdid,
                                                               __global       double * xsav)
                                {
                                    x[0] = xsav[0] + hdid[0]*0.666666666666667;
                                }

                    ";

            public string doubleStep6 = @"
                                __kernel void doubleYStep6(     __global       double * x,
                                                               __global       double * hdid,
                                                               __global write_only double * y,
                                                               __global       double * ysav,
                                                               __global       double * k1,
                                                               __global       double * k2,
                                                               __global       double * k3,
                                                               __global       double * k4,
                                                               __global       double * k5,
                                                               __global       double * k6)
                                {
                                    int i = get_global_id(0);
                                    y[i] = ysav[i] + 0.0016 * hdid[0] * (28 * k1[i] - 125 * k2[i] + 546 * k3[i] + 54 * k4[i] - 378 * k5[i]);
                                }

                                __kernel void doubleXStep6(     __global       double * x,
                                                               __global       double * hdid,
                                                               __global       double * xsav)
                                {
                                    x[0] = xsav[0] + 0.2 * hdid[0];
                                }

                    ";
            #endregion

            public string doubleFinalizeCalc = @"
                                __kernel void doubleFinalizeCalc( __global            double * x,
                                                                 __global            double * hdid,
                                                                 __global write_only double * y,
                                                                 __global            double * ysav,
                                                                 __global            double * absError,
                                                                 __global            double * k1,
                                                                 __global            double * k2,
                                                                 __global            double * k3,
                                                                 __global            double * k4,
                                                                 __global            double * k5,
                                                                 __global            double * k6)
                                {
                                    int i = get_global_id(0);
                                    double temp = hdid[0] * 0.00297619047619048 * (-42 * k1[i] - 224 * k3[i] - 21 * k4[i] + 162 * k5[i] + 125 * k6[i]);
                                    y[i] = ysav[i] + hdid[0] * 0.166666666666667 * (k1[i] + 4 * k3[i] + k4[i]) + temp;
                                    if (temp > 0) absError[i] += temp;
                                    else absError[i] -= temp;
                                }

                                __kernel void doubleUpdateX(    __global       double * x,
                                                               __global       double * hdid,
                                                               __global       double * xsav)
                                {
                                    x[0] = xsav[0] + hdid[0];
                                }
                    ";
        }
        #endregion
    }
}
