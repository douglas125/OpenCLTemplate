using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenCLTemplate.MachineLearning
{
    /// <summary>
    /// This class stores all variables related to the configuration of a SMO problem
    /// </summary>
    public class ProblemConfig
    {
        /// <summary>Type of SVM kernel to use</summary>
        public enum KernelType
        {
            /// <summary>Linear: u'*v</summary>
            Linear,
            /// <summary>Polynomial: (gamma*u'*v + coef0)^degree</summary>
            Polynomial,
            /// <summary>Radial basis function: exp(-gamma*||u[i]-v[i]||²)</summary>
            RBF,
            /// <summary>Sigmoid: tanh(gamma*u'*v + coef0)</summary>
            Sigmoid
        }

        /// <summary>Regularization parameter</summary>
        public float c;

        /// <summary>Kernel parameter</summary>
        public float lambda = 1;

        /// <summary>
        /// Numerical tolerance
        /// </summary>
        public float tol;
        /// <summary>
        /// Max # of times to iterate over alphas without changing
        /// </summary>
        public int maxPasses;

        /// <summary>
        /// Set type of kernel function (default 2)
        /// </summary>
        public KernelType kernelType = KernelType.RBF;

        /// <summary>
        /// Constructor with the default kernel
        /// </summary>
        /// <param name="Lambda">Kernel parameter</param>
        /// <param name="newC">Regularization parameter</param>
        /// <param name="newTol">Numerical tolerance</param>
        /// <param name="newMaxPasses">Max # of times to iterate over alphas without changing</param>
        public ProblemConfig(float Lambda, float newC, float newTol, int newMaxPasses)
        {
            this.lambda = Lambda;
            c = newC;
            tol = newTol;
            maxPasses = newMaxPasses;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="Lambda">Kernel parameter</param>
        /// <param name="newC">Regularization parameter</param>
        /// <param name="newTol">Numerical tolerance</param>
        /// <param name="newMaxPasses">Max # of times to iterate over alphas without changing</param>
        /// <param name="newKernelType">Type of kernel function index</param>
        public ProblemConfig(float Lambda, float newC, float newTol, int newMaxPasses, KernelType newKernelType)
        {
            this.lambda = Lambda;
            c = newC;
            tol = newTol;
            maxPasses = newMaxPasses;
            kernelType = newKernelType;
        }

        /// <summary>Creates a new object equal to this</summary>
        public ProblemConfig Clone()
        {
            return new ProblemConfig(this.lambda, this.c, this.tol, this.maxPasses, this.kernelType);
        }
    }
}
