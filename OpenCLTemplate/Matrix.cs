using System;
using System.Collections.Generic;
using System.Text;

namespace LinearAlgebra
{
    /// <summary>Creates a Matrix of real numbers.</summary>
    public class Matrix
    {

        #region Controle de acesso aos elementos da matriz
        /// <summary>Matrix items</summary>
        public double[,] Items;
        int qtdLinhas, qtdCols;

        /// <summary> Accesses items in current matrix.</summary>
        /// <param name="i">Row of element to access.</param>
        /// <param name="j">Column of element to access.</param>
        public double this[int i, int j]
        {
            set
            {
                Items[i, j] = value;
            }
            get
            {
                return Items[i,j];
            }
        }

        /// <summary> Gets the number of rows in this matrix.</summary>
        public int rowCount
        {
            get
            {
                return qtdLinhas;
            }
        }

        /// <summary> Gets the number of columns in this matrix.</summary>
        public int colCount
        {
            get
            {
                return qtdCols;
            }
        }
        void ReDimensiona(int rows, int cols)
        {
            Items = new double[rows, cols];
            qtdLinhas = rows;
            qtdCols = cols;
        }

        #endregion


        /// <summary> Linear Solve parameter. Maximum mean error allowable to linear solve method.</summary>
        public double LINALGMAXERROR = 1e-7;
        /// <summary> Linear Solve parameter. Limit linear solution correction iterations.</summary>
        public int LIMITITERS = 10;

        /// <summary> Linear Solve parameter. Should the solution method halt if a hard singulariry is found in matrix?</summary>
        public bool IGNOREHARDSINGULARITY = true;
        /// <summary> Linear Solve parameter. Should the method ignore if the matrix has a close-to-zero determinant and keep solving?</summary>
        public bool IGNORENULLDETERMINANT = false;

        #region Construtores
        /// <summary> Constructor. Initializes a [0,0] matrix.</summary>
        public Matrix()
        {
            //teste = new double[1, 1];
            this.ReDimensiona(1,1);
        }

        /// <summary> Constructor. Creates matrix from existing items.</summary>
        /// <param name="MatrixItems">Matrix items to create matrix from.</param>
        public Matrix(double[,] MatrixItems)
        {
            int p = MatrixItems.GetLength(0);
            int q = MatrixItems.GetLength(1);
            this.ReDimensiona(p, q);
            for (int i = 0; i < p; i++)
            {
                for (int j = 0; j < q; j++)
                {
                    this[i, j] = MatrixItems[i, j];
                }
            }
        }

        /// <summary> Copy constructor.</summary>
        /// <param name="m">Matrix to copy from.</param>
        public Matrix(Matrix m)
        {
            int p = m.rowCount;
            int q = m.colCount;
            this.ReDimensiona(p, q);
            for (int i = 0; i < p; i++)
            {
                for (int j = 0; j < q; j++)
                {
                    this[i, j] = m[i, j];
                }
            }
        }

        /// <summary> Constructor. Creates empty matrix with specified dimensions.</summary>
        /// <param name="numRows">Number of rows in matrix.</param>
        /// <param name="numCols">Number of columns in matrix.</param>
        public Matrix(int numRows, int numCols)
        {
            this.ReDimensiona(numRows, numCols);
        }
        #endregion

        #region Operacoes Matematicas Elementares (Soma, Subtracao, Multiplicacao)

        /// <summary> Sums two matrixes.</summary>
        /// <param name="m1">First matrix to sum.</param>
        /// <param name="m2">Second matrix to sum.</param>
        public static Matrix operator +(Matrix m1, Matrix m2)
        {
            int qtdLinhas = m1.rowCount;
            int qtdCols = m1.colCount;
            if (qtdLinhas != m2.rowCount || qtdCols != m2.colCount)
                throw new Exception("Cannot SUM matrixes: incompatible dimensions: ["+qtdLinhas.ToString()+","+qtdCols.ToString()+"] + ["+m2.rowCount.ToString()+","+m2.colCount.ToString()+"]");

            Matrix m = new Matrix(m1.Items);
            for (int i = 0; i < qtdLinhas; i++)
            {
                for (int j = 0; j < qtdCols; j++)
                {
                    m[i,j] += m2[i,j];
                }
            }
            return m;
        }

        /// <summary> Subtracts two matrixes.</summary>
        /// <param name="m1">Matrix to subtract from.</param>
        /// <param name="m2">Matrix to be subtracted.</param>
        public static Matrix operator -(Matrix m1, Matrix m2)
        {
            int qtdLinhas = m1.rowCount;
            int qtdCols = m1.colCount;
            if (qtdLinhas != m2.rowCount || qtdCols != m2.colCount)
                throw new Exception("Cannot SUBTRACT matrixes: incompatible dimensions: [" + qtdLinhas.ToString() + "," + qtdCols.ToString() + "] - [" + m2.rowCount.ToString() + "," + m2.colCount.ToString() + "]");

            Matrix m = new Matrix(m1.Items);
            for (int i = 0; i < qtdLinhas; i++)
            {
                for (int j = 0; j < qtdCols; j++)
                {
                    m[i, j] -= m2[i, j];
                }
            }
            return m;
        }

        /// <summary> Matrix multiplication. Notice that m1 rows should be the same as m2 lines for compatibility.</summary>
        /// <param name="m1">First matrix to multiply.</param>
        /// <param name="m2">Second matrix to multiply.</param>
        public static Matrix operator *(Matrix m1, Matrix m2)
        {
            int qtdLinhasm1 = m1.rowCount;
            int qtdColsm1 = m1.colCount;
            int qtdLinhasm2 = m2.rowCount;
            int qtdColsm2 = m2.colCount;

            if (qtdColsm1 != qtdLinhasm2)
                throw new Exception("Cannot MULTIPLY matrixes: incompatible dimensions: ["+qtdLinhasm1.ToString()+","+qtdColsm1.ToString()+"]*["+qtdLinhasm2.ToString()+","+qtdColsm2.ToString()+"]");

            Matrix m = new Matrix(qtdLinhasm1, qtdColsm2);
            for (int p = 0; p < qtdLinhasm1; p++)
            {
                for (int n = 0; n < qtdColsm2; n++)
                {
                    for (int q = 0; q < qtdColsm1; q++)
                    {
                        m[p, n] += m1[p, q] * m2[q, n];
                    }
                }
            }

            return m;
        }

        /// <summary> Matrix scalar multiplication.</summary>
        /// <param name="m">Matrix to multiply.</param>
        /// <param name="num">Scalar to multiply.</param>
        public static Matrix operator *(double num, Matrix m)
        {
            int qtdLinhas = m.rowCount;
            int qtdCols = m.colCount;

            Matrix m2 = new Matrix(m.Items);
            for (int i = 0; i < qtdLinhas; i++)
            {
                for (int j = 0; j < qtdCols; j++)
                {
                    m2[i, j] *= num;
                }
            }
            return m2;
        }
        /// <summary> Matrix scalar multiplication.</summary>
        /// <param name="m">Matrix to multiply.</param>
        /// <param name="num">Scalar to multiply.</param>
        public static Matrix operator *(Matrix m, double num)
        {
            int qtdLinhas = m.rowCount;
            int qtdCols = m.colCount;

            Matrix m2 = new Matrix(m.Items);
            for (int i = 0; i < qtdLinhas; i++)
            {
                for (int j = 0; j < qtdCols; j++)
                {
                    m2[i, j] *= num;
                }
            }
            return m2;
        }

        /// <summary> Matrix post-vector multiplication. Notice that a vector is a [1,Cols] matrix which means
        /// vector length should be equal matrix number of columns.</summary>
        /// <param name="m">Matrix to multiply.</param>
        /// <param name="vet">vector to multiply.</param>
        public static double[] operator *(Matrix m, double[] vet)
        {
            int qtdLinhas = m.rowCount;
            int qtdCols = m.colCount;

            if (vet.Length != qtdCols)
                throw new Exception("Cannot POST MULTIPLY BY VECTOR. Matrix dimension: ["+qtdLinhas.ToString()+","+qtdCols.ToString()+"] Vector Dimension: ["+vet.Length.ToString()+","+"1]");

            double[] resp = new double[qtdLinhas];
            for (int i = 0; i < qtdLinhas; i++)
            {
                for (int j = 0; j < qtdCols; j++)
                {
                    resp[i] += m[i, j] * vet[j];
                }
            }
            return resp;
        }

        /// <summary> Matrix pre-vector multiplication. Notice that a vector is a [1,Cols] matrix which means
        /// vector length should be equal matrix number of lines.</summary>
        /// <param name="m">Matrix to multiply.</param>
        /// <param name="vet">vector to multiply.</param>
        public static double[] operator *(double[] vet, Matrix m)
        {
            int qtdLinhas = m.rowCount;
            int qtdCols = m.colCount;

            if (vet.Length != qtdLinhas)
                throw new Exception("Cannot PRE MULTIPLY BY VECTOR. Matrix dimension: [" + qtdLinhas.ToString() + "," + qtdCols.ToString() + "] Vector Dimension: [" + vet.Length.ToString() + "," + "1]");

            double[] resp = new double[qtdCols];
            for (int i = 0; i < qtdLinhas; i++)
            {
                for (int j = 0; j < qtdCols; j++)
                {
                    resp[j] += m[i, j] * vet[i];
                }
            }
            return resp;
        }

        /// <summary> Matrix scalar division.</summary>
        /// <param name="m">Matrix to multiply.</param>
        /// <param name="num">Scalar to divide each element of matrix.</param>
        public static Matrix operator /(Matrix m, double num)
        {
            int qtdLinhas = m.rowCount;
            int qtdCols = m.colCount;

            double invnum = 1 / num;

            Matrix m2 = new Matrix(m.Items);
            for (int i = 0; i < qtdLinhas; i++)
            {
                for (int j = 0; j < qtdCols; j++)
                {
                    m2[i, j] *= invnum;
                }
            }
            return m2;
        }
        #endregion
        
        #region Comparacao de Igualdade

        /// <summary>Compares matrixes and returns true if they are identical.</summary>
        /// <param name="Matrix">Matrix to compare to.</param>
        public bool Equals(Matrix Matrix)
        {
            if (Matrix==null) return false;

            int qtdLinhas = Matrix.rowCount;
            int qtdCols = Matrix.colCount;
            if (qtdLinhas != this.rowCount || qtdCols != this.colCount)
                return false;

            bool saoIguais = true;
            for (int i = 0; i < qtdLinhas; i++)
            {
                for (int j = 0; j < qtdCols; j++)
                {
                    if (Matrix[i, j] != this[i, j]) saoIguais = false;
                }
            }

            return saoIguais;
        }

        #endregion

        #region Funcoes Matriciais Elementares (Transpose, Mult Elemento a Elemento, norma Euclidiana, inversa de elementos)


        /// <summary>Returns matrix transpose.</summary>
        public Matrix Transpose()
        {
            int qtdLinhas = this.rowCount;
            int qtdCols = this.colCount;

            Matrix transposta = new Matrix(qtdCols, qtdLinhas);

            for (int i = 0; i < qtdLinhas; i++)
            {
                for (int j = 0; j < qtdCols; j++)
                {
                    transposta[j, i] = this[i, j];
                }
            }

            return transposta;
        }

        /// <summary>Element-wise product. This is not regular matrix product. It multiplies elements 
        /// at corresponding positions.</summary>
        /// <param name="Matrix">Matrix to multiply element-wise.</param>
        public Matrix MultiplyElementWise(Matrix Matrix)
        {
            int qtdLinhas = Matrix.rowCount;
            int qtdCols = Matrix.colCount;
            if (qtdLinhas != this.rowCount || qtdCols != this.colCount)
                throw new Exception("Cannot MULTIPLY ELEMENT-WISE matrixes: incompatible dimensions: [" + qtdLinhas.ToString() + "," + qtdCols.ToString() + "] .* [" + this.rowCount.ToString() + "," + this.colCount.ToString() + "]");

            Matrix m = new Matrix(Matrix);
            for (int i = 0; i < qtdLinhas; i++)
            {
                for (int j = 0; j < qtdCols; j++)
                {
                    m[i, j] *= this[i, j];
                }
            }
            return m;
        }

        /// <summary>Returns Euclidean norm of the matrix.</summary>
        public double NormEuclidean()
        {
            return Math.Sqrt(Dot(this));
        }

        /// <summary>Dot product of two matrixes.</summary>
        /// <param name="Matrix">Matrix to dot product with/</param>
        public double Dot(Matrix Matrix)
        {
            int qtdLinhas = Matrix.rowCount;
            int qtdCols = Matrix.colCount;
            if (qtdLinhas != this.rowCount || qtdCols != this.colCount)
                throw new Exception("Cannot calculate DOT PRODUCT: incompatible dimensions: [" + qtdLinhas.ToString() + "," + qtdCols.ToString() + "] DOT [" + this.rowCount.ToString() + "," + this.colCount.ToString() + "]");

            double dotProd=0;
            for (int i = 0; i < qtdLinhas; i++)
            {
                for (int j = 0; j < qtdCols; j++)
                {
                    dotProd += Matrix[i, j] * this[i, j];
                }
            }
            return dotProd;
        }

        /// <summary>Element-wise inversion. Returns the matrix with each element (x) inverted (1/x).</summary>
        public Matrix InvertElements()
        {
            int qtdLinhas = this.rowCount;
            int qtdCols = this.colCount;

            Matrix m = new Matrix(this);
            for (int i = 0; i < qtdLinhas; i++)
            {
                for (int j = 0; j < qtdCols; j++)
                {
                    m[i, j] = 1 / m[i, j];
                }
            }
            return m;
        }

        #endregion

        #region Solucao de sistema linear, Fatoracao LU, Determinante, Inversa
        //Estrategia: realiza a fatoracao LU caso seja necessario.
        //Se foi necessario, mantem a fatoracao LU na memoria.

        Matrix matrizDecomposta; //matriz decomposta LU - serve para verificar se 
        Matrix FatoracaoLU; //Fatoracao LU
        int[] indx;
        bool trocouLinhasQtdImpar; //Verifica se a quantidade de trocas de linha eh par ou impar

        /// <summary>Creates internal LU factorization of this matrix.</summary>
        public void LUDecomp()
        {
            //Verifica se a matriz eh quadrada
            int n = this.rowCount;
            if (n != this.colCount)
                throw new Exception("Cannot LU decompose non-square matrix. Dimensions: ["+n.ToString()+","+this.colCount.ToString()+"]");

            //Verifica se a matriz ja foi decomposta
            if (!this.Equals(matrizDecomposta)) //se a matriz foi modificada eh necessario refatorar
            {
                matrizDecomposta = new Matrix(this);
                FatoracaoLU = new Matrix(this);

                int k, j, imax=0, i;
                double sum, dum, big, temp;
                double[] vv = new double[n];

                //inicializa indx
                indx = new int[n];
                for (i = 0; i < n; i++)
                {
                    indx[i] = -1;
                }

                trocouLinhasQtdImpar = false;
                for (i = 0; i < n; i++) //encontra maior termo
                {
                    big = 0;
                    for (j = 0; j < n; j++)
                    {
                        temp = FatoracaoLU.Items[i, j];
                        if (temp < 0) temp = -temp;
                        if (temp > big) big = temp;
                    }

                    if (big == 0) throw new Exception("Cannot LU factor: singular matrix");

                    vv[i] = 1 / big;
                }

                for (j = 0; j < n; j++)
                {
                    for (i = 0; i < j; i++)
                    {
                        sum = FatoracaoLU.Items[i, j];
                        for (k = 0; k < i; k++)
                        {
                            sum -= FatoracaoLU.Items[i, k] * FatoracaoLU.Items[k, j];
                        }

                        FatoracaoLU.Items[i, j] = sum;
                    }

                    big = 0;
                    for (i = j; i < n; i++)
                    {
                        sum = FatoracaoLU.Items[i, j];
                        for (k = 0; k < j; k++)
                        {
                            sum -= FatoracaoLU.Items[i, k] * FatoracaoLU.Items[k, j];
                        }
                        FatoracaoLU.Items[i, j] = sum;
                        temp = sum; if (temp < 0) temp = -temp;
                        dum = vv[i] * temp; //melhor pivo?

                        if (dum >= big)
                        {
                            big = dum;
                            imax = i;
                        }
                    }

                    if (j != imax)
                    {
                        for (k = 0; k < n; k++)
                        {
                            dum = FatoracaoLU.Items[imax, k];
                            FatoracaoLU.Items[imax, k] = FatoracaoLU.Items[j, k];
                            FatoracaoLU.Items[j, k] = dum;
                        }
                        trocouLinhasQtdImpar = !trocouLinhasQtdImpar; //coluna trocada
                        vv[imax] = vv[j]; //escala
                    }
                    indx[j] = imax;

                    if (FatoracaoLU.Items[j, j] == 0) throw new Exception("Cannot LU factor: singular matrix");

                    if (j != (n - 1))
                    {
                        dum = 1 / FatoracaoLU.Items[j, j];
                        for (i = j + 1; i < n; i++)
                        {
                            FatoracaoLU.Items[i, j] *= dum;
                        }
                    }
                }

            }
    
        }
        private double[] LUBackSubstitute(double[] bb)
        {
            int j, ip, ii, i;
            double sum;

            int n = FatoracaoLU.rowCount;
            if (n != bb.Length) throw new Exception("LU backsubstitution: Incorrect dimensions");

            //resolve o sistema com uma copia de bb
            double[] b=new double[n];
            for (i = 0; i < n; i++) b[i] = bb[i];


            ii = -1;
            for (i = 0; i < n; i++)
            {
                ip = indx[i];
                sum = b[ip];
                b[ip] = b[i];
                if (ii >= 0)
                {
                    for (j = ii; j < i; j++)
                    {
                        sum -= FatoracaoLU.Items[i, j] * b[j];
                    }
                }
                else if (sum != 0)
                    ii = i;

                b[i] = sum;
            }

            for (i = n - 1; i >= 0; i--)
            {
                sum = b[i];
                for (j = i + 1; j < n; j++)
                {
                    sum -= FatoracaoLU.Items[i, j] * b[j];
                }
                b[i] = sum / FatoracaoLU.Items[i, i];
            }

            return b;
        }

        /// <summary>Returns the solution x to the linear system Ax=b, A being this matrix.</summary>
        /// <param name="b">Right-hand side known values.</param>
        public double[] LinearSolve(double[] b)
        {
            double det = Determinant();
            //LUDecomp();

            if (det < 0) det = -det;
            if (det < 0.1*LINALGMAXERROR && !IGNORENULLDETERMINANT) throw new Exception("Singular matrix (Determinant=0). Cannot solve linear system.");

            int n = FatoracaoLU.rowCount;

            if (n != b.Length) throw new Exception("LinSolve: Incorrect dimensions");

            int counter = 0, i;
            double erro = 1000, temp;
            
            // Mx=b ==> M(x+deltaX)=b+deltaB
            double[] solucao = new double[n];
            double[] deltaX = new double[n];
            double[] deltaB = new double[n];
            double[] bMaisDeltaB = new double[n];
            
            //Solucao preliminar
            solucao = LUBackSubstitute(b);

            while (erro > LINALGMAXERROR && counter < LIMITITERS)
            {
                bMaisDeltaB = matrizDecomposta * solucao;
                for (i = 0; i < n; i++) deltaB[i] = bMaisDeltaB[i] - b[i];
                deltaX = LUBackSubstitute(deltaB);
                for (i = 0; i < n; i++) solucao[i] -=deltaX[i];

                //Estima o erro
                erro = 0;
                for (i = 0; i < n; i++)
                {
                    temp = deltaB[i];
                    if (temp < 0) temp = -temp;
                    if (erro < temp) erro = temp;
                }

                counter++;
            }

            if (erro > LINALGMAXERROR && !IGNOREHARDSINGULARITY)
                throw new Exception("Hard singularity found attempting to solve linear system: impossible?");

            return solucao;
        }

        /// <summary>Returns the determinant of this matrix.</summary>
        public double Determinant()
        {
            LUDecomp();
            double resp = 1;
            if (trocouLinhasQtdImpar) resp = -1;
            int n = matrizDecomposta.rowCount;
            for (int i = 0; i < n; i++) resp *= FatoracaoLU.Items[i, i];
            return resp;
        }

        /// <summary>Returns the inverse of this matrix.</summary>
        public Matrix Inversa()
        {
            double det = Determinant();
            //LUDecomp();

            if (det < 0) det = -det;
            if (det < 0.1 * LINALGMAXERROR && !IGNORENULLDETERMINANT) throw new Exception("Singular matrix (Determinant=0). Cannot invert matrix.");

            int n = FatoracaoLU.rowCount;

            Matrix inversa = new Matrix(n, n);
            double[] col = new double[n];
            double[] resp;

            for (int j = 0; j < n; j++)
            {
                col[j] = 1;
                if (j > 0) col[j - 1] = 0;
                
                //Calcula a linha da inversa correspondente
                //Ao vetor [0 1 0 0 ...] <- j-esima linha

                resp = LUBackSubstitute(col);
                for (int i = 0; i < n; i++) inversa.Items[i, j] = resp[i];
            }
            return inversa;
        }

        #endregion

        #region Identificacao de Sistemas

        //Resolve o sistema linear Xx=Y por pseudo-inversao: x=inv(X`X)*X`*Y
        
        Matrix A; //A=(X`X), matriz a ser invertida no processo
        private double RSquare=0;
        private double RSquareCorrected = 0;
        private double QuadError = 0;

        /// <summary>Returns the R² index of last fit.</summary>
        public double RSquareIndex
        {
            get
            {
                return RSquare;
            }
        }

        /// <summary>Returns corrected R² index of last fit.</summary>
        public double RSquareCorrectedIndex
        {
            get
            {
                return RSquareCorrected;
            }
        }

        /// <summary>Returns the sum of quadratic errors of last fit.</summary>
        public double QuadraticError
        {
            get
            {
                return QuadError;
            }
        }

        /// <summary>Returns the solution x to the linear system A'Ax=A'b, A being this matrix.</summary>
        /// <param name="Y">Right-hand side known values.</param>
        public double[] IdentifyParameters(double[] Y)
        {
            //dimensões N, k e Q. beta[0..k], X[0..N,0..k], Y[0..N], A[0..k,0..k]
            int N = this.rowCount;

            if (Y.Length != N)
                throw new Exception("Incompatible vector dimension for parameter identification: " + Y.Length.ToString() + "  . Should be " + N.ToString());

            A = this.Transpose()*this; //A=(X`X), matriz cuja inversa e' calculada
            double[] beta = A.LinearSolve(this.Transpose()*Y); //beta - parametros indets

            checkQuality(Y, beta, N);

            return beta;
        }

        /// <summary>Returns the weighted solution x to the linear system A'WAx=A'Wb, 
        /// A being this matrix. TO DO: Correct quality check</summary>
        /// <param name="Y">Right-hand side known values.</param>
        /// <param name="W">Weight matrix.</param>
        public double[] IdentifyParametersWeighted(double[] Y, Matrix W)
        {
            //dimensões N, k e Q. beta[0..k], X[0..N,0..k], Y[0..N], A[0..k,0..k]
            //W [0..N, 0..N]
            int N = this.rowCount;

            if (Y.Length != N)
                throw new Exception("Incompatible vector dimension for parameter identification: " + Y.Length.ToString() + "  . Should be " + N.ToString());

            if (W.rowCount!=N || W.colCount!=N)
                throw new Exception("Incompatible weight matrix dimensions. Should be " + N.ToString());


            A = this.Transpose() * W * this; //A=(X`X), matriz cuja inversa e' calculada
            double[] beta = A.LinearSolve(this.Transpose() * W * Y); //beta - parametros indets

            //checkQuality(W * Y, beta, N);

            return beta;
        }

        /// <summary>Returns the weighted solution x to the linear system A'DAx=A'Db, 
        /// A being this matrix. D is a diagonal weight matrix.</summary>
        /// <param name="Y">Right-hand side known values.</param>
        /// <param name="w">Main diagonal elements of diagonal weight matrix D.</param>
        public double[] IdentifyParametersWeighted(double[] Y, double[] w)
        {
            //dimensões N, k e Q. beta[0..k], X[0..N,0..k], Y[0..N], A[0..k,0..k]
            //W [0..N, 0..N]
            int N = this.rowCount;

            if (Y.Length != N)
                throw new Exception("Incompatible vector dimension for parameter identification: " + Y.Length.ToString() + "  . Should be " + N.ToString());

            if (w.Length != N)
                throw new Exception("Incompatible weight diagonal elements dimensions. Should be " + N.ToString());

            Matrix B = new Matrix(this);
            double[] novoY = new double[Y.Length];

            for (int i = 0; i < Y.Length; i++)
            {
                double temp = Math.Sqrt(w[i]);

                for (int j = 0; j < A.colCount; j++)
                    B[i, j] *= temp;

                novoY[i] = Y[i] * temp;
            }

            A = B.Transpose() * B; //A=(X`WX), matriz cuja inversa e' calculada
            double[] beta = A.LinearSolve(B.Transpose() * novoY); //beta: parametros indets

            checkQuality(novoY, beta, N);

            return beta;
        }


        /// <summary>Calculates R², corrected R² and Quadratic Error for the trySolution x to the linear system A'Ax=A'b, 
        /// A being this matrix.</summary>
        /// <param name="Y">Right-hand side known values.</param>
        /// <param name="trySolution">Solution to use to evaluate quality indexers.</param>
        public void CheckQuality(double[] Y, double[] trySolution)
        {
            //dimensões N, k e Q. beta[0..k], X[0..N,0..k], Y[0..N], A[0..k,0..k]
            int N = this.rowCount;

            if (Y.Length != N)
                throw new Exception("Incompatible unknowns vector dimension for quality check: " + Y.Length.ToString() + "  . Should be " + N.ToString());

            A = this.Transpose() * this; //A=(X`X), matriz cuja inversa e' calculada

            int k = A.rowCount;

            if (trySolution.Length != k)
                throw new Exception("Incompatible solution vector dimension for quality check: " + trySolution.Length.ToString() + "  . Should be " + k.ToString());

            checkQuality(Y, trySolution, N);
        }


        //Verifica indicadores de qualidade da solucao, assumindo que A ja foi calculada
        //Essa funcao nao recalcula a matriz A
        void checkQuality(double[] Y, double[] beta, int N) 
        {
            //Soma quadratica de Y
            double YtY = 0, YtYminusc, somaY = 0;
            double invN = 1 / ((double)N-1);

            //Calculo do erro quadratico
            double[] Xbeta = this * beta;

            int k = A.rowCount;

            this.QuadError = 0;
            for (int i = 0; i < N; i++)
            {
                YtY += Y[i] * Y[i];
                somaY += Y[i];
                this.QuadError += (Xbeta[i] - Y[i]) * (Xbeta[i] - Y[i]);
            }

            YtYminusc = YtY - invN * somaY;

            //calcula beta'*X'*X*beta, X'X=A
            double[] temp = beta * A;
            double btAtAb=0;
            for (int i = 0; i < k; i++)
                btAtAb += temp[i] * beta[i];

            RSquare = btAtAb / YtY;

            double dN = (double)N-1, dk = (double)k-1;
            RSquareCorrected = 1 - (YtY-btAtAb)*(dN - 1) / ((dN - dk)*YtYminusc);
        }

        #endregion

        #region Ortogonalizacao

        /// <summary>Applies the Gram-Schmidt orthonormalization method to this matrix, replacing 
        /// it by the orthonormalized matrix.</summary>
        public void GramSchmidt()
        {
            int[] lineOrder = new int[qtdLinhas];
            int indStart = 0;
            double[] y = new double[qtdLinhas];

            for (int i = 0; i < qtdLinhas; i++) lineOrder[i] = i;
            

            GramSchmidt(lineOrder, indStart, ref y);
        }

        /// <summary>Applies the Gram-Schmidt orthonormalization method to this matrix, replacing 
        /// it by the orthonormalized matrix and also correcting right-hand Y values for a linear system solve.</summary>
        /// <param name="y">Right-hand side known values.</param>
        public void GramSchmidt(ref double[] y)
        {
            int[] lineOrder = new int[qtdLinhas];
            int indStart = 0;
            for (int i = 0; i < qtdLinhas; i++) lineOrder[i] = i;
            
            GramSchmidt(lineOrder, indStart, ref y);
        }

        /// <summary>Applies the Gram-Schmidt orthonormalization method to this matrix using 
        /// a pre-set order of normalization. Replaces current matrix
        /// by the orthonormalized matrix and also correcting 
        /// right-hand Y values for a linear system solve.</summary>
        /// <param name="y">Right-hand side known values.</param>
        /// <param name="lineOrder">Line order to apply the orthonormalization method.</param>
        /// <param name="indStart">Starts orthonormalization from line lineOrder[indStart]. Assumes previous lines are already
        /// normalized.</param>
        public void GramSchmidt(int[] lineOrder, int indStart, ref double[] y)
        {
            //Aplica o processo de ortonormalização de Gram Schmidt à matriz
            //corrige também os coeficientes em y.

            for (int i = indStart; i < lineOrder.Length; i++)
            {
                //Remove os componentes das linhas anteriores
                //Detalhe: assume que as linhas anteriores estão devidamente normalizadas
                for (int j = 0; j < i; j++)
                {
                    double coef = ProdEscalar(lineOrder[i], lineOrder[j]);
                    this.SubtraiLinha(lineOrder[i], lineOrder[j], coef);
                    y[lineOrder[i]] -= coef * y[lineOrder[j]];
                }

                //Normaliza a linha
                y[lineOrder[i]] *= this.NormalizaLinha(lineOrder[i]); //NormalizaLinha retorna o inverso da norma
            }

        }

        private void SubtraiLinha(int linha, int LinhaASubtrair, double coef)
        {
            //Subtrai de LinhaASubtrair os valores coef*[linha]
            for (int j = 0; j < qtdCols; j++)
                this[linha, j] -= this[LinhaASubtrair, j] * coef;
        }
        private double NormalizaLinha(int linha)
        {
            //retorna o inverso da norma
            double invnorma = 0;
            invnorma = Math.Sqrt(ProdEscalar(linha,linha));
            if (invnorma != 0) invnorma = 1 / invnorma;

            for (int j = 0; j < qtdCols; j++)
                this[linha, j] *= invnorma;

            return invnorma;
        }
        private double ProdEscalar(int L1, int L2)
        {
            double prod = 0;
            for (int j = 0; j < qtdCols; j++)
                prod += this[L1, j] * this[L2, j];

            return prod;
        }

        #endregion

        /// <summary>Returns a string representing this matrix.</summary>
        public override string ToString()
        {
            string texto="\n";
            int p = this.rowCount;
            int q = this.colCount;
            for (int i = 0; i < p; i++)
            {
                for (int j = 0; j < q; j++)
                {
                    texto += this[i, j].ToString("E") + "\t";
                }
                texto += "\n";
            }
            return texto;
        }
    }
}
