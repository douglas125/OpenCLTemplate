using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Windows.Forms;
using System.Drawing;
using Cloo;
using System.Runtime.InteropServices;

namespace OpenCLTemplate.CLGLInterop
{

    /// <summary>OpenGL render control</summary>
    public class GLRender
    {
        [DllImport("opengl32.dll")]
        extern static IntPtr wglGetCurrentDC();

        #region Initializations
        /// <summary>Parent form</summary>
        private System.Windows.Forms.Form ParentForm;

        /// <summary>OpenGL control</summary>
        public GLControl GLCtrl;

        /// <summary>Sets CL GL shared variables</summary>
        /// <param name="DeviceNumber">Index of device to use from ComputePlatform.Platforms[0].Devices. Use -1 for default</param>
        private void CreateCLGLContext(int DeviceNumber)
        {
            IntPtr curDC = wglGetCurrentDC();

            OpenTK.Graphics.IGraphicsContextInternal ctx = (OpenTK.Graphics.IGraphicsContextInternal)OpenTK.Graphics.GraphicsContext.CurrentContext;

            IntPtr raw_context_handle = ctx.Context.Handle;

            ComputeContextProperty p1 = new ComputeContextProperty(ComputeContextPropertyName.CL_GL_CONTEXT_KHR, raw_context_handle);
            ComputeContextProperty p2 = new ComputeContextProperty(ComputeContextPropertyName.CL_WGL_HDC_KHR, curDC);
            ComputeContextProperty p3 = new ComputeContextProperty(ComputeContextPropertyName.Platform, ComputePlatform.Platforms[0].Handle.Value);
            List<ComputeContextProperty> props = new List<ComputeContextProperty>() { p1, p2, p3 };

            ComputeContextPropertyList Properties = new ComputeContextPropertyList(props);

            List<ComputeDevice> GLDevices = null;
            if (DeviceNumber >= 0 && ComputePlatform.Platforms[0].Devices.Count > 1)
            {
                GLDevices = new List<ComputeDevice>() { ComputePlatform.Platforms[0].Devices[1] };
                CLGLCtx = new ComputeContext(GLDevices, Properties, null, IntPtr.Zero);
                CQ = new ComputeCommandQueue(CLGLCtx, GLDevices[0], ComputeCommandQueueFlags.None);
            }
            else
            {
                CLGLCtx = new ComputeContext(ComputeDeviceTypes.Gpu, Properties, null, IntPtr.Zero);
                CQ = new ComputeCommandQueue(CLGLCtx, CLGLCtx.Devices[0], ComputeCommandQueueFlags.None);
            }


        }


        /// <summary>Constructor. Adds a OpenGL Control to desired form</summary>
        /// <param name="ParentForm">OpenGL control parent form</param>
        /// <param name="CreateCLGLCtx">Create OpenGL/OpenCL shared context?</param>
        /// <param name="DeviceNumber">Index of device to use from ComputePlatform.Platforms[0].Devices. Use -1 for default</param>
        public GLRender(System.Windows.Forms.Form ParentForm, bool CreateCLGLCtx, int DeviceNumber)
        {
            this.ParentForm = ParentForm;

            InitGL();

            if (CreateCLGLCtx)
            {
                CreateCLGLContext(DeviceNumber);
                CLCalc.InitCL(ComputeDeviceTypes.Gpu, CLGLCtx, CQ);
            }
        }

        /// <summary>Typical OpenGL initialization</summary>
        private void InitGL()
        {
            #region OpenGL Control creation with stereo capabilities

            OpenTK.Graphics.ColorFormat cf = new OpenTK.Graphics.ColorFormat();
            OpenTK.Graphics.GraphicsMode gm =
                new OpenTK.Graphics.GraphicsMode(32, 24, 8, 4, cf, 4, true);

            this.GLCtrl = new OpenTK.GLControl(gm);
            ParentForm.Controls.Add(GLCtrl);

            // 
            // sOGL
            // 
            this.GLCtrl.BackColor = System.Drawing.Color.Black;
            this.GLCtrl.Name = "sOGL";
            this.GLCtrl.VSync = false;
            this.GLCtrl.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.sOGL_MouseWheel);
            this.GLCtrl.Paint += new System.Windows.Forms.PaintEventHandler(this.sOGL_Paint);
            this.GLCtrl.MouseMove += new System.Windows.Forms.MouseEventHandler(this.sOGL_MouseMove);
            this.GLCtrl.MouseDown += new System.Windows.Forms.MouseEventHandler(this.sOGL_MouseDown);
            this.GLCtrl.Resize += new System.EventHandler(this.sOGL_Resize);
            this.GLCtrl.MouseUp += new System.Windows.Forms.MouseEventHandler(this.sOGL_MouseUp);
            this.GLCtrl.KeyDown += new System.Windows.Forms.KeyEventHandler(this.sOGL_KeyDown);

            ParentForm.Resize += new EventHandler(sOGL_Resize);

            GLCtrl.Top = 0; GLCtrl.Left = 0;
            GLCtrl.Width = ParentForm.Width; GLCtrl.Height = ParentForm.Height;
            GLCtrl.Cursor = System.Windows.Forms.Cursors.Cross;

            #endregion

            GLCtrl.MakeCurrent();

            //AntiAliasing e blend
            GL.Enable(EnableCap.LineSmooth);
            GL.Hint(HintTarget.LineSmoothHint, HintMode.DontCare);
            GL.Enable(EnableCap.PolygonSmooth);
            GL.Hint(HintTarget.PolygonSmoothHint, HintMode.DontCare);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);


            //Z-Buffer
            GL.ClearDepth(1.0f);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.Enable(EnableCap.DepthTest);
            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.DontCare);

            //Materiais, funcoes para habilitar cor
            GL.ColorMaterial(MaterialFace.FrontAndBack, ColorMaterialParameter.AmbientAndDiffuse); //tem q vir antes do enable
            GL.Enable(EnableCap.ColorMaterial);

            // Create light components
            float[] ambientLight = { 0.5f, 0.5f, 0.5f, 1.0f };
            float[] diffuseLight = { 0.3f, 0.3f, 0.3f, 1.0f };
            float[] specularLight = { 0.1f, 0.1f, 0.1f, 1.0f };
            float[] position = { 0.0f, -40.0f, 0.0f, 1.0f };

            // Assign created components to GL_LIGHT1
            GL.Light(LightName.Light1, LightParameter.Ambient, ambientLight);
            GL.Light(LightName.Light1, LightParameter.Diffuse, diffuseLight);
            GL.Light(LightName.Light1, LightParameter.Specular, specularLight);
            GL.Light(LightName.Light1, LightParameter.Position, position);

            GL.Enable(EnableCap.Light1);// Enable Light One

            GL.ShadeModel(ShadingModel.Smooth);
            GL.Enable(EnableCap.Lighting);


            //Textura
            GL.Enable(EnableCap.Texture2D);

            //Line and point sizes
            GL.LineWidth(2);
            //GL.PointSize(2);

            Create3DMouseModel(new float[] { 1.0f, 0.0f, 0.0f });
        }


        #endregion

        #region Event handling / mouse rotation, translation variables


        private void sOGL_Paint(object sender, PaintEventArgs e)
        {
            //Draws once more after animation stops
            Draw();
        }


        #region Mouse rotation
        bool clicado = false;
        bool clicDireito = false;
        int originalX, origXDireito;
        int originalY, origYDireito;
        /// <summary>Mouse rotation mode</summary>
        public MouseMoveMode MouseMode = MouseMoveMode.RotateModel;
        #endregion

        private void sOGL_Resize(object sender, EventArgs e)
        {
            if (GLCtrl != null)
            {

                GLCtrl.Width = ParentForm.Width; GLCtrl.Height = ParentForm.Height;
                if (GLCtrl.Width < 0) GLCtrl.Width = 1;
                if (GLCtrl.Height < 0) GLCtrl.Height = 1;

                GLCtrl.MakeCurrent();
                GL.Viewport(0, 0, GLCtrl.Width, GLCtrl.Height);
                GLCtrl.Invalidate();
            }
        }

        private void sOGL_MouseDown(object sender, MouseEventArgs e)
        {
            //if (MouseMode == MouseMoveMode.TranslateModel || MouseMode == MouseMoveMode.RotateModel)
            //{
            if (e.Button == MouseButtons.Left)
            {
                if (!clicado)
                {
                    clicado = true;
                    originalX = e.X;
                    originalY = e.Y;
                }
            }

            if (e.Button == MouseButtons.Right)
            {
                if (!clicDireito)
                {
                    clicDireito = true;
                    origXDireito = e.X;
                    origYDireito = e.Y;
                }
            }

            if (Mouse3D != null)
            {
                if (MousePosAnt == null) MousePosAnt = new Vector();
                MousePosAnt.x = Mouse3D.vetTransl.x;
                MousePosAnt.y = Mouse3D.vetTransl.y;
                MousePosAnt.z = Mouse3D.vetTransl.z;
            }

            //}
            //else if (MouseMode == MouseMoveMode.Mouse3D)
            //{
            //}
        }

        private void sOGL_MouseUp(object sender, MouseEventArgs e)
        {
            if (MouseMode == MouseMoveMode.TranslateModel || MouseMode == MouseMoveMode.RotateModel)
            {
                if (e.Button == MouseButtons.Left)
                {
                    clicado = false;
                    this.ConsolidateRepositioning();
                }
            }
            else if (MouseMode == MouseMoveMode.Mouse3D)
            {
                Process3DMouseHit(e);
            }

            if (e.Button == MouseButtons.Left)
            {
                clicado = false;
            }
            if (e.Button == MouseButtons.Right)
            {
                clicDireito = false;
            }
        }
        private Point mousePos = new Point(0, 0);

        private void sOGL_MouseMove(object sender, MouseEventArgs e)
        {
            if (MouseMode == MouseMoveMode.TranslateModel || MouseMode == MouseMoveMode.RotateModel)
            {
                mousePos.X = e.X; mousePos.Y = e.Y;
                if (e.Button == MouseButtons.Left)
                {
                    if (clicado)
                    {
                        this.RepositionCamera((float)e.X - (float)originalX, (float)e.Y - (float)originalY, MouseMode);
                        GLCtrl.Refresh();
                    }
                }
                Mouse3D.ShowModel = false;
            }
            else if (MouseMode == MouseMoveMode.Mouse3D)
            {
                if (MousePosAnt == null) MousePosAnt = new Vector();
                MousePosAnt.x = Mouse3D.vetTransl.x;
                MousePosAnt.y = Mouse3D.vetTransl.y;
                MousePosAnt.z = Mouse3D.vetTransl.z;

                float x = (float)e.X / (float)GLCtrl.Width;
                float y = (float)e.Y / (float)GLCtrl.Height;
                Translate3DMouseXY(x, y, 0);
                GLCtrl.Refresh();
                Mouse3D.ShowModel = true;

                Process3DMouseHit(e);
            }
        }

        //sOGL_MouseWheel
        private void sOGL_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (MouseMode == MouseMoveMode.TranslateModel || MouseMode == MouseMoveMode.RotateModel)
            {
                this.distEye *= 1 - ((float)e.Delta * 0.001);
                RecalcZnearZFar();
            }
            else if (MouseMode == MouseMoveMode.Mouse3D)
            {
                float x = (float)e.X / (float)GLCtrl.Width;
                float y = (float)e.Y / (float)GLCtrl.Height;
                this.Translate3DMouseXY(x, y, -e.Delta);
                GLCtrl.Refresh();

                Process3DMouseHit(e);
            }
        }

        /// <summary>Automatically recalculates zNear and zFar values</summary>
        private void RecalcZnearZFar()
        {
            //this.zFar = ((float)this.distEye+40) * 4;
            this.zFar = ((float)this.distEye) * 5;
            this.zNear = this.zFar * 1e-3f;
            this.RepositionCamera(0, 0, MouseMode);
            //status("View distance set to " + this.distEye.ToString(), -1);
            GLCtrl.Refresh();
        }

        private void sOGL_KeyDown(object sender, KeyEventArgs e)
        {

            //movimento com teclado
            bool invalidar = false;

            #region 3D mouseing commands
            if (e.KeyCode == Keys.Q && Mouse3D != null)
            {
                //Go into 3D mousing mode
                MouseMode = MouseMoveMode.Mouse3D;
                Reset3DMousePos();
            }
            else if (e.KeyCode == Keys.R)
            {
                MouseMode = MouseMoveMode.RotateModel;
                Mouse3D.ShowModel = false;
                invalidar = true;
            }
            else if (e.KeyCode == Keys.Subtract)
            {
                Mouse3D.Scale[0] *= 0.97f;
                Mouse3D.Scale[1] *= 0.97f;
                Mouse3D.Scale[2] *= 0.97f;
                invalidar = true;
            }
            else if (e.KeyCode == Keys.Add)
            {
                Mouse3D.Scale[0] *= 1.02f;
                Mouse3D.Scale[1] *= 1.02f;
                Mouse3D.Scale[2] *= 1.02f;
                invalidar = true;
            }


            #endregion

            if (e.KeyCode == Keys.W)
            {
                this.center -= this.zFar * 0.002 * this.front;
                this.eye -= this.zFar * 0.002 * this.front;
                invalidar = true;
            }
            if (e.KeyCode == Keys.S)
            {
                this.center += this.zFar * 0.002 * this.front;
                this.eye += this.zFar * 0.002 * this.front;
                invalidar = true;
            }
            if (e.KeyCode == Keys.A)
            {
                this.center -= this.zFar * 0.001 * this.esq;
                this.eye -= this.zFar * 0.001 * this.esq;
                invalidar = true;
            }
            if (e.KeyCode == Keys.D)
            {
                this.center += this.zFar * 0.001 * this.esq;
                this.eye += this.zFar * 0.001 * this.esq;
                invalidar = true;
            }

            //Rotacao com teclado
            double cos = Math.Cos(0.01);
            double sin = Math.Sin(0.01);
            if (e.KeyCode == Keys.NumPad4)
            {
                Vector front = new Vector(this.front);
                Vector esq = new Vector(this.esq);

                this.front = cos * front + sin * esq;
                this.esq = -sin * front + cos * esq;

                this.center = this.eye - this.front * this.distEye;

                invalidar = true;
            }
            if (e.KeyCode == Keys.NumPad6)
            {
                Vector front = new Vector(this.front);
                Vector esq = new Vector(this.esq);

                this.front = cos * front - sin * esq;
                this.esq = sin * front + cos * esq;

                this.center = this.eye - this.front * this.distEye;

                invalidar = true;
            }

            if (e.KeyCode == Keys.NumPad2)
            {
                Vector front = new Vector(this.front);
                Vector up = new Vector(this.up);

                this.front = cos * front + sin * up;
                this.up = -sin * front + cos * up;

                this.center = this.eye - this.front * this.distEye;

                invalidar = true;
            }
            if (e.KeyCode == Keys.NumPad8)
            {
                Vector front = new Vector(this.front);
                Vector up = new Vector(this.up);

                this.front = cos * front - sin * up;
                this.up = sin * front + cos * up;

                this.center = this.eye - this.front * this.distEye;

                invalidar = true;
            }


            if (invalidar)
            {
                GLCtrl.Invalidate();
            }
        }

        #endregion

        #region Variables and methods for mouse movement control

        /// <summary>Mouse action when used to move the 3D Model</summary>
        public enum MouseMoveMode
        {
            /// <summary>Mouse rotation mode index.</summary>
            RotateModel,
            /// <summary>Mouse translation mode index.</summary>
            TranslateModel,
            /// <summary>Enter 3D mousing mode</summary>
            Mouse3D,
            /// <summary>No mouse movement.</summary>
            None
        }

        /// <summary>Point where camera is looking at</summary>
        private Vector center = new Vector(0, 0, 0);
        /// <summary>Point where camera is standing</summary>
        private Vector eye = new Vector(0, 0, 215);
        /// <summary>Front vector</summary>
        private Vector front = new Vector(0, 0, 1);
        /// <summary>Up vector</summary>
        private Vector up = new Vector(0, 1, 0);
        /// <summary>Left vector</summary>
        private Vector esq = new Vector(1, 0, 0);
        /// <summary>Camera eye distance.</summary>
        private double distEye = 215;
        /// <summary>Far distance to clip at.</summary>
        public float zFar = 1000.0f;
        /// <summary>Near distance to clip at</summary>
        public float zNear = 1.0f;

        Vector frontCpy = new Vector(0, 0, 1);
        Vector upCpy = new Vector(0, 1, 0);
        Vector esqCpy = new Vector(1, 0, 0);
        Vector centerCpy = new Vector(0, 0, 0);

        //vetor angs = new vetor(0,0,0);
        //vetor angsCpy=new vetor(0,0,0);

        /// <summary>Repositions camera.</summary>
        /// <param name="mouseDX">X mouse displacement.</param>
        /// <param name="mouseDY">Y mouse displacement.</param>
        /// <param name="modo">Mouse displacement mode (user wants translation or rotation?)</param>
        private void RepositionCamera(float mouseDX, float mouseDY, MouseMoveMode modo)
        {
            if (modo == MouseMoveMode.RotateModel)
            {
                //Faz com que pegar o mouse em uma ponta e levar ate a outra
                //gire a cena 360 graus
                double ang2 = -3 * Math.PI * mouseDX / (float)GLCtrl.Width;
                double ang1 = -3 * Math.PI * mouseDY / (float)GLCtrl.Height;

                Console.Write(ang2.ToString());

                //Calcula a rotacao do mouse
                double c1, s1, c2, s2;
                c1 = Math.Cos(ang1);
                s1 = Math.Sin(ang1);
                c2 = Math.Cos(ang2);
                s2 = Math.Sin(ang2);

                front = frontCpy * c1 + upCpy * -s1;
                up = s1 * frontCpy + upCpy * c1;

                Vector temp = new Vector(front);

                front = temp * c2 + s2 * esqCpy;
                esq = -s2 * temp + esqCpy * c2;
            }
            else if (modo == MouseMoveMode.TranslateModel)
            {
                double dx = -distEye * mouseDX / (float)GLCtrl.Width;
                double dy = distEye * mouseDY / (float)GLCtrl.Height;

                center = centerCpy + esqCpy * dx + upCpy * dy;

            }
            //Olho: centro, deslocado na direcao em FRENTE de distEye
            eye = center + front * distEye;
            RepositionLight();
        }

        /// <summary>Function to advance view and allow "fly" simulations.</summary>
        /// <param name="Distance">Distance to fly</param>
        public void Fly(Vector Distance)
        {
            center += Distance.x * front + Distance.y * esq + Distance.z * up;
            eye += Distance.x * front + Distance.y * esq + Distance.z * up;
        }

        /// <summary>Updates temporary displacement vectors to internal drawing vectors.</summary>
        private void ConsolidateRepositioning()
        {
            frontCpy = new Vector(front);
            upCpy = new Vector(up);
            esqCpy = new Vector(esq);
            centerCpy = new Vector(center);
        }

        /// <summary>Repositions light.</summary>
        private void RepositionLight()
        {
            GL.LoadIdentity(); //para evitar pegar matriz de rotacao residual
            float[] position = { (float)distEye, (float)distEye, 0.0f, 1.0f }; //reposiciona a luz dinamicamente
            GL.Light(LightName.Light1, LightParameter.Position, position);
        }


        #endregion

        #region 3D mousing

        /// <summary>Mouse 3D model</summary>
        public GLVBOModel Mouse3D;

        /// <summary>Show mouse to center distance in this label if not null</summary>
        public ToolStripStatusLabel lblMouseToCenterDist;

        /// <summary>Creates a 3D Model for the mouse</summary>
        /// <param name="Color">Desired color</param>
        public void Create3DMouseModel(float[] Color)
        {
            if (Mouse3D == null)
            {
                Mouse3D = new GLVBOModel(BeginMode.Triangles);
            }

            #region Creates sphere
            int N = 20;

            float[] Vertex = new float[3 * N * N];
            float[] Normals = new float[3 * N * N];
            float[] Colors = new float[4 * N * N];

            for (int u = 0; u < N; u++)
            {
                float uu = 2.0f * (float)Math.PI * (float)u / (float)(N - 1);
                for (int v = 0; v < N; v++)
                {
                    float vv = (float)Math.PI * ((float)v / (float)(N - 1) - 0.5f);
                    Vertex[3 * (u + N * v)] = (float)(Math.Cos(uu) * Math.Cos(vv));
                    Vertex[1 + 3 * (u + N * v)] = (float)(Math.Sin(uu) * Math.Cos(vv));
                    Vertex[2 + 3 * (u + N * v)] = (float)(Math.Sin(vv));
                    Normals[3 * (u + N * v)] = Vertex[3 * (u + N * v)];
                    Normals[1 + 3 * (u + N * v)] = Vertex[1 + 3 * (u + N * v)];
                    Normals[2 + 3 * (u + N * v)] = Vertex[2 + 3 * (u + N * v)];

                    Colors[4 * (u + N * v)] = Color[0]; Colors[1 + 4 * (u + N * v)] = Color[1]; Colors[2 + 4 * (u + N * v)] = Color[2] + 0.5f * (1.0f + Vertex[2 + 3 * (u + N * v)]); Colors[3 + 4 * (u + N * v)] = 0.3f;
                }
            }

            int[] Elems = new int[6 * (N - 1) * (N - 1)];
            for (int u = 0; u < N - 1; u++)
            {
                for (int v = 0; v < N - 1; v++)
                {
                    Elems[6 * (u + (N - 1) * v)] = u + N * v;
                    Elems[1 + 6 * (u + (N - 1) * v)] = 1 + u + N * v;
                    Elems[2 + 6 * (u + (N - 1) * v)] = 1 + u + N * (1 + v);

                    Elems[3 + 6 * (u + (N - 1) * v)] = u + N * v;
                    Elems[4 + 6 * (u + (N - 1) * v)] = 1 + u + N * (1 + v);
                    Elems[5 + 6 * (u + (N - 1) * v)] = u + N * (1 + v);
                }
            }


            #endregion

            Mouse3D.Name = "3D Mouse"; BufferUsageHint h = BufferUsageHint.StaticDraw;
            Mouse3D.ShowModel = false;
            Mouse3D.SetVertexData(Vertex, h);
            Mouse3D.SetNormalData(Normals, h);
            Mouse3D.SetColorData(Colors, h);
            Mouse3D.SetElemData(Elems, h);

            Reset3DMousePos();
            ReDraw();
        }

        /// <summary>Resets 3D mouse position to center of view</summary>
        public void Reset3DMousePos()
        {
            Mouse3D.vetTransl.x = center.x; Mouse3D.vetTransl.y = center.y; Mouse3D.vetTransl.z = center.z;
            Mouse3D.Scale[0] = (float)distEye * 0.1f;
            Mouse3D.Scale[1] = (float)distEye * 0.1f;
            Mouse3D.Scale[2] = (float)distEye * 0.1f;
            ReDraw();
        }

        /// <summary>Translates 3D mouse to a given Left - Top</summary>
        /// <param name="x">Left relative value, 0 to 1</param>
        /// <param name="y">Up relative value, 0 to 1</param>
        /// <param name="dz">Z (depth) relative value</param>
        public void Translate3DMouseXY(float x, float y, float dz)
        {
            double mouseZ = Vector.DotProduct(front, Mouse3D.vetTransl - center);
            float scaleFac = ((float)mouseZ + (float)distEye) * 0.00012f;
            Mouse3D.vetTransl += scaleFac * front * dz;

            mouseZ = Vector.DotProduct(front, Mouse3D.vetTransl - center);
            scaleFac = ((float)distEye - (float)mouseZ) * 1.4f;
            Mouse3D.vetTransl = center + scaleFac * (esq * (x - 0.5f) - up * (y - 0.5f)) + mouseZ * front;

            if (lblMouseToCenterDist != null)
            {
                double dist = (Mouse3D.vetTransl - center).norm();
                lblMouseToCenterDist.Text = Math.Round(dist, 3).ToString();
            }
        }

        /// <summary>Increments current mouse position</summary>
        /// <param name="dx">Left relative value</param>
        /// <param name="dy">Up relative value</param>
        /// <param name="dz">Z (depth) relative value</param>
        public void Increment3DMousePos(float dx, float dy, float dz)
        {
            double mouseZ = Vector.DotProduct(front, Mouse3D.vetTransl);
            float scaleFac = ((float)mouseZ + (float)distEye) * 0.0006f;

            Mouse3D.vetTransl += scaleFac * (esq * dx + up * dy + front * dz);
        }

        #region OpenCL displacement/hide objects

        /// <summary>Processes 3D mouse event</summary>
        private void Process3DMouseHit(MouseEventArgs e)
        {
            if (clicDireito) HideElements();
            if (clicado) DisplaceElements();
        }

        /// <summary>OpenGL/CL shared context</summary>
        private ComputeContext CLGLCtx;
        /// <summary>OpenGL/CL shared command queue</summary>
        private ComputeCommandQueue CQ;

        /// <summary>Mouse position in GPU memory</summary>
        private ComputeBuffer<float> CLMousePos;

        /// <summary>Previous mouse position (when clicked)</summary>
        private Vector MousePosAnt;

        /// <summary>Previous mouse position in GPU memory</summary>
        private ComputeBuffer<float> CLMousePosAnt;

        /// <summary>Mouse radius in GPU memory</summary>
        private ComputeBuffer<float> CLMouseRadius;

        /// <summary>Hide/show kernel</summary>
        private ComputeKernel kernelHide, kernelShowAll, kernelHideLines;

        /// <summary>Displacement kernel</summary>
        private ComputeKernel kernelDisplace;

        /// <summary>Initializes OpenCL kernels to calculate displacement and hide objects</summary>
        private void InitCLDisp()
        {
            //Kernels
            OpenCLDispSrc src = new OpenCLDispSrc();
            ComputeProgram prog = new ComputeProgram(this.CLGLCtx, src.src);
            prog.Build(CLGLCtx.Devices, "", null, IntPtr.Zero);

            kernelHide = prog.CreateKernel("HideElems");
            kernelShowAll = prog.CreateKernel("ShowAllElems");
            kernelDisplace = prog.CreateKernel("DisplaceElems");
            kernelHideLines = prog.CreateKernel("HideLineElems");

            //Mouse arguments
            CLMousePos = new ComputeBuffer<float>(CLGLCtx, ComputeMemoryFlags.ReadWrite, 3);
            CLMousePosAnt = new ComputeBuffer<float>(CLGLCtx, ComputeMemoryFlags.ReadWrite, 3);
            CLMouseRadius = new ComputeBuffer<float>(CLGLCtx, ComputeMemoryFlags.ReadWrite, 1);


            kernelHide.SetMemoryArgument(2, CLMousePos);
            kernelHide.SetMemoryArgument(3, CLMouseRadius);

            kernelHideLines.SetMemoryArgument(2, CLMousePos);
            kernelHideLines.SetMemoryArgument(3, CLMouseRadius);

            kernelDisplace.SetMemoryArgument(1, CLMousePosAnt);
            kernelDisplace.SetMemoryArgument(2, CLMousePos);
            kernelDisplace.SetMemoryArgument(3, CLMouseRadius);
        }

        /// <summary>Writes information to a buffer</summary>
        /// <param name="buffer">Buffer object</param>
        /// <param name="Values">Values to write</param>
        private void CQWrite(ComputeBuffer<float> buffer, float[] Values)
        {
            unsafe
            {
                fixed (void* ponteiro = Values)
                {
                    CQ.Write<float>(buffer, true, 0, Values.Length, (IntPtr)ponteiro, null);
                }
            }
        }

        /// <summary>Undoes all hide operations and shows all elements</summary>
        public void ShowAllElements()
        {
            if (kernelHide == null) InitCLDisp();
            GL.Finish();

            CQWrite(CLMousePos, new float[] { (float)Mouse3D.vetTransl.x, (float)Mouse3D.vetTransl.y, (float)Mouse3D.vetTransl.z });

            CQWrite(CLMouseRadius, new float[] { (float)Mouse3D.Scale[0] });

            foreach (GLVBOModel model in this.Models)
            {
                if (model != Mouse3D)
                {
                    lock (model)
                    {
                        //Create from GL buffers
                        ComputeBuffer<int> CLGLElems = ComputeBuffer<int>.CreateFromGLBuffer<int>(CLGLCtx, ComputeMemoryFlags.ReadWrite, model.GLElemBuffer);

                        //Acquire
                        List<ComputeMemory> c = new List<ComputeMemory>() { CLGLElems };
                        CQ.AcquireGLObjects(c, null);


                        //Use
                        kernelShowAll.SetMemoryArgument(0, CLGLElems);
                        CQ.Execute(kernelShowAll, null, new long[] { model.ElemLength }, null, null);

                        //Release and dispose
                        CQ.ReleaseGLObjects(c, null);

                        CLGLElems.Dispose();
                    }
                }
            }
            CQ.Finish();

            ReDraw();
        }

        /// <summary>Hides elements in this GLRender which are close to the 3D mouse</summary>
        private void HideElements()
        {
            if (kernelHide == null) InitCLDisp();
            GL.Finish();

            CQWrite(CLMousePos, new float[] { (float)Mouse3D.vetTransl.x, (float)Mouse3D.vetTransl.y, (float)Mouse3D.vetTransl.z });
            CQWrite(CLMouseRadius, new float[] { (float)Mouse3D.Scale[0] });

            foreach (GLVBOModel model in this.Models)
            {
                if (model != Mouse3D && model.ShowModel)
                {
                    lock (model)
                    {
                        //Create from GL buffers
                        ComputeBuffer<int> CLGLElems = ComputeBuffer<int>.CreateFromGLBuffer<int>(CLGLCtx, ComputeMemoryFlags.ReadWrite, model.GLElemBuffer);
                        ComputeBuffer<float> CLGLVertexes = ComputeBuffer<float>.CreateFromGLBuffer<float>(CLGLCtx, ComputeMemoryFlags.ReadWrite, model.GLVertexBuffer);

                        //Acquire
                        List<ComputeMemory> c = new List<ComputeMemory>() { CLGLElems, CLGLVertexes };
                        CQ.AcquireGLObjects(c, null);

                        if (model.DrawMode == BeginMode.Triangles)
                        {
                            //Use
                            kernelHide.SetMemoryArgument(0, CLGLElems);
                            kernelHide.SetMemoryArgument(1, CLGLVertexes);
                            CQ.Execute(kernelHide, null, new long[] { model.ElemLength / 3 }, null, null);
                        }
                        else if (model.DrawMode == BeginMode.Lines)
                        {
                            //Use
                            kernelHideLines.SetMemoryArgument(0, CLGLElems);
                            kernelHideLines.SetMemoryArgument(1, CLGLVertexes);
                            CQ.Execute(kernelHideLines, null, new long[] { model.ElemLength / 2 }, null, null);
                        }

                        //Release and dispose
                        CQ.ReleaseGLObjects(c, null);


                        CLGLElems.Dispose();
                        CLGLVertexes.Dispose();
                    }
                }
            }
            CQ.Finish();

            ReDraw();
        }

        /// <summary>Displace elements according to mouse command</summary>
        private void DisplaceElements()
        {
            if (kernelHide == null) InitCLDisp();
            GL.Finish();

            CQWrite(CLMousePos, new float[] { (float)Mouse3D.vetTransl.x, (float)Mouse3D.vetTransl.y, (float)Mouse3D.vetTransl.z });
            CQWrite(CLMousePosAnt, new float[] { (float)MousePosAnt.x, (float)MousePosAnt.y, (float)MousePosAnt.z });
            CQWrite(CLMouseRadius, new float[] { (float)Mouse3D.Scale[0] });

            foreach (GLVBOModel model in this.Models)
            {
                if (model != Mouse3D)
                {
                    lock (model)
                    {
                        if (model.ShowModel)
                        {
                            //Create from GL buffers
                            ComputeBuffer<float> CLGLVertexes = ComputeBuffer<float>.CreateFromGLBuffer<float>(CLGLCtx, ComputeMemoryFlags.ReadWrite, model.GLVertexBuffer);

                            //Acquire
                            List<ComputeMemory> c = new List<ComputeMemory>() { CLGLVertexes };
                            CQ.AcquireGLObjects(c, null);


                            //Use
                            kernelDisplace.SetMemoryArgument(0, CLGLVertexes);
                            CQ.Execute(kernelDisplace, null, new long[] { model.numVertexes }, null, null);

                            //Release and dispose
                            CQ.ReleaseGLObjects(c, null);

                            CLGLVertexes.Dispose();
                        }
                    }
                }
            }
            CQ.Finish();

            ReDraw();
        }

        private class OpenCLDispSrc
        {
            public string src = @"
//global_size(0) = number of elements = elems.Length/3

__kernel void HideElems(__global int   * elems,
                        __global float * VertexCoords,
                        __global float * mouseCoords,
                        __global float * mouseRadius)
                        
 {
    int i3 = 3*get_global_id(0);
    if (elems[i3]>=0)
    {
        //Mouse coords
        float4 mousePos = (float4)(mouseCoords[0], mouseCoords[1], mouseCoords[2],0);
        float r = mouseRadius[0];
        
        //Triangle vertexes
        float4 v1 = (float4)(VertexCoords[3*elems[i3  ]], VertexCoords[3*elems[i3  ]+1], VertexCoords[3*elems[i3  ]+2], 0);
        float4 v2 = (float4)(VertexCoords[3*elems[i3+1]], VertexCoords[3*elems[i3+1]+1], VertexCoords[3*elems[i3+1]+2], 0);
        float4 v3 = (float4)(VertexCoords[3*elems[i3+2]], VertexCoords[3*elems[i3+2]+1], VertexCoords[3*elems[i3+2]+2], 0);
        
        float dist1, dist2, dist3;

        dist1 = fast_distance(mousePos, v1);
        
        if (dist1 <= 3*r)
        {
            dist2 = fast_distance(mousePos, v2);
            dist3 = fast_distance(mousePos, v3);

            if (dist1 <= r || dist2 <= r || dist3 <= r)
            {
                 elems[i3]=-elems[i3]-1;
                 elems[i3+1]=-elems[i3+1]-1;
                 elems[i3+2]=-elems[i3+2]-1;
            }
        }
    }
 }
 
__kernel void HideLineElems(__global int   * elems,
                            __global float * VertexCoords,
                            __global float * mouseCoords,
                            __global float * mouseRadius)
                        
 {
    int i2 = 2*get_global_id(0);
    if (elems[i2]>=0)
    {
        //Mouse coords
        float4 mousePos = (float4)(mouseCoords[0], mouseCoords[1], mouseCoords[2],0);
        float r = mouseRadius[0];
        
        //Triangle vertexes
        float4 v1 = (float4)(VertexCoords[3*elems[i2  ]], VertexCoords[3*elems[i2  ]+1], VertexCoords[3*elems[i2  ]+2], 0);
        float4 v2 = (float4)(VertexCoords[3*elems[i2+1]], VertexCoords[3*elems[i2+1]+1], VertexCoords[3*elems[i2+1]+2], 0);
        
        float dist1, dist2;

        dist1 = fast_distance(mousePos, v1);
        
        if (dist1 <= 3*r)
        {
            dist2 = fast_distance(mousePos, v2);

            if (dist1 <= r || dist2 <= r)
            {
                 elems[i2]=-elems[i2]-1;
                 elems[i2+1]=-elems[i2+1]-1;
            }
        }
    }
 }

 __kernel void ShowAllElems(__global int * elems)
 {
    
    int i = get_global_id(0);
    if (elems[i]<0)
    {
         elems[i]=-elems[i]-1;
    }
 }

//global_size(0) = number of vertexes = vertexes.Length/3

__kernel void DisplaceElems(__global float * VertexCoords,
                            __global float * mouseCoords0,
                            __global float * mouseCoordsf,
                            __global float * mouseRadius)
                        
 {
    int i3 = 3*get_global_id(0);
    
    //Mouse coords
    float4 mousePos0 = (float4)(mouseCoords0[0], mouseCoords0[1], mouseCoords0[2],0);
    float4 mousePosf = (float4)(mouseCoordsf[0], mouseCoordsf[1], mouseCoordsf[2],0);
    float invr = native_recip(mouseRadius[0]);
    
    //Vertex coordinates
    float4 v1 = (float4)(VertexCoords[i3], VertexCoords[i3+1], VertexCoords[i3+2], 0);
    
    float dist1 = fast_distance(mousePos0, v1);
    
    //Displacement vector
    mousePosf -= mousePos0;
    
    //Displacement intensity

    float temp = 0.707f*dist1*invr;
//    dist1 = 0.7978845608f*invr*native_exp(-temp*temp);
    dist1 = 7.978845608f*invr*native_exp(-temp*temp);
    
    //Final vertex coord
    v1 += mousePosf*dist1;
    
    VertexCoords[i3] = v1.x;    VertexCoords[i3+1] = v1.y;    VertexCoords[i3+2] = v1.z;
 }

";
        }

        #endregion

        #endregion

        #region OpenGL scene drawing
        /// <summary>List of OpenGL VBOs to draw</summary>
        public List<GLVBOModel> Models = new List<GLVBOModel>();

        /// <summary>Draw in stereographic projection?</summary>
        public bool StereoscopicDraw = false;

        /// <summary>Stereographic distance.</summary>
        public float StereoDistance = 0.005f;

        /// <summary>Background color</summary>
        public float[] ClearColor = new float[3] { 0.0f, 0.0f, 0.0f };

        /// <summary>Forces the control to redraw its contents</summary>
        public void ReDraw()
        {
            GLCtrl.Invalidate();
        }

        /// <summary>Void function delegate</summary>
        public delegate void VoidFunc();
        /// <summary>Function to be invoked prior to every drawing</summary>
        public VoidFunc PreDrawFunc;

        /// <summary>Controls the camera positioning for the OpenGL scene</summary>
        private void Draw()
        {
            //Prevents strange zFars from happening
            if (zFar < 0) zFar = 300;
            if (zFar > 1e35f) zFar = 1e35f;

            //Invoke pre draw function
            if (PreDrawFunc != null) PreDrawFunc();

            if (StereoscopicDraw)
            {
                #region Left eye
                Vector eyeStereo = eye - distEye * StereoDistance * esq;

                GL.DrawBuffer(DrawBufferMode.BackRight);
                GL.ClearColor(ClearColor[0], ClearColor[1], ClearColor[2], 0.0f);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                GL.LoadIdentity();

                OpenTK.Matrix4d m1 = OpenTK.Matrix4d.CreatePerspectiveFieldOfView(Math.PI * 0.25f,
                        (double)GLCtrl.Width / (double)GLCtrl.Height, (double)zNear, (double)zFar);
                GL.LoadMatrix(ref m1);
                OpenTK.Matrix4d m2 = OpenTK.Matrix4d.LookAt(eyeStereo.x, eyeStereo.y, eyeStereo.z, center.x, center.y, center.z,
                    up.x, up.y, up.z);
                GL.MultMatrix(ref m2);

                DoDraw();
                #endregion
                #region Right eye
                eyeStereo = eye + distEye * StereoDistance * esq;
                GL.DrawBuffer(DrawBufferMode.BackLeft);
                GL.ClearColor(ClearColor[0], ClearColor[1], ClearColor[2], 0.0f);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                GL.LoadIdentity();


                OpenTK.Matrix4d m10 = OpenTK.Matrix4d.CreatePerspectiveFieldOfView(Math.PI * 0.25f,
                        (double)GLCtrl.Width / (double)GLCtrl.Height, (double)zNear, (double)zFar);
                GL.LoadMatrix(ref m10);
                OpenTK.Matrix4d m20 = OpenTK.Matrix4d.LookAt(eyeStereo.x, eyeStereo.y, eyeStereo.z, center.x, center.y, center.z,
                    up.x, up.y, up.z);
                GL.MultMatrix(ref m20);


                DoDraw();
                #endregion
            }
            else
            {
                GL.DrawBuffer(DrawBufferMode.Back);

                GL.ClearColor(ClearColor[0], ClearColor[1], ClearColor[2], 0.0f);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                GL.LoadIdentity();

                OpenTK.Matrix4d m1 = OpenTK.Matrix4d.CreatePerspectiveFieldOfView(Math.PI * 0.25f,
                    (double)GLCtrl.Width / (double)GLCtrl.Height, (double)zNear, (double)zFar);
                GL.LoadMatrix(ref m1);
                OpenTK.Matrix4d m2 = OpenTK.Matrix4d.LookAt(eye.x, eye.y, eye.z, center.x, center.y, center.z,
                    up.x, up.y, up.z);
                GL.MultMatrix(ref m2);

                DoDraw();


                //Material
                float[] specular = { 0.9f, 0.9f, 0.9f, 1 };

                GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Shininess, specular);

                GLCtrl.SwapBuffers();
            }
        }

        /// <summary>Draw axes at center?</summary>
        public bool DrawAxes = true;

        /// <summary>Draws OpenGL scene</summary>
        private void DoDraw()
        {
            GL.Begin(BeginMode.Lines);
            GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);

            Vector v = new Vector(center);
            if (DrawAxes)
            {
                GL.Vertex3((float)v.x, (float)v.y, (float)v.z);
                GL.Vertex3((float)v.x + 60f, (float)v.y, (float)v.z);
                GL.Vertex3((float)v.x, (float)v.y, (float)v.z);
                GL.Vertex3((float)v.x, (float)v.y + 60f, (float)v.z);
                GL.Vertex3((float)v.x, (float)v.y, (float)v.z);
                GL.Vertex3((float)v.x, (float)v.y, (float)v.z + 60f);
            }
            if (lblMouseToCenterDist != null && MouseMode == MouseMoveMode.Mouse3D)
            {
                GL.Vertex3((float)center.x, (float)center.y, (float)center.z);

                GL.Vertex3((float)Mouse3D.vetTransl.x, (float)Mouse3D.vetTransl.y, (float)Mouse3D.vetTransl.z);
            }
            GL.End();

            lock (Models)
            {
                foreach (GLVBOModel model in Models)
                {
                    lock (model)
                    {
                        model.DrawModel();
                    }
                }
            }


            if (Mouse3D != null) Mouse3D.DrawModel();
        }

        #endregion

        #region Camera repositioning

        /// <summary>Sets center of camera and recalculates appropriate vectors</summary>
        /// <param name="NewCenter">Desired center</param>
        public void SetCenter(Vector NewCenter)
        {
            try
            {
                //Centraliza no modelo
                center = new Vector(NewCenter);

                this.RepositionCamera(0.0f, 0.0f, MouseMoveMode.RotateModel);

            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Exception: " + ex.ToString(), "Select Model");
            }
        }

        /// <summary>Sets the camera distance from the center of where it's looking</summary>
        /// <param name="Distance">New distance</param>
        public void SetDistance(double Distance)
        {
            distEye = Distance;
            RecalcZnearZFar();
        }

        /// <summary>Gets camera center (the center of where the camera is looking at)</summary>
        public Vector GetCenter()
        {
            return new Vector(center);
        }
        /// <summary>Gets camera distance from the center</summary>
        public double GetDistance()
        {
            return distEye;
        }


        #endregion


        /// <summary>OpenGL Model created from vertex buffer objects</summary>
        public class GLVBOModel
        {
            /// <summary>Model name</summary>
            public string Name;

            #region Constructor/destructor
            /// <summary>Constructor. Receives draw mode of the model. REMINDER: Vertex, color and element data are necessary for drawing.</summary>
            /// <param name="DrawMode">OpenGL Draw model</param>
            public GLVBOModel(BeginMode DrawMode)
            {
                this.DrawMode = DrawMode;
            }

            /// <summary>Constructor. Reuses the same Vertex Buffer Elements of an existing 3D model.</summary>
            /// <param name="Source">Source model to reuse buffer elements</param>
            public GLVBOModel(GLVBOModel Source)
            {
                this.DrawMode = Source.DrawMode;
                this.CLColorBuffer = Source.CLColorBuffer;
                this.CLElemBuffer = Source.CLElemBuffer;
                this.CLNormalBuffer = Source.CLNormalBuffer;
                this.CLTexCoordBuffer = Source.CLTexCoordBuffer;
                this.CLVertexBuffer = Source.CLVertexBuffer;

                this.GLColorBuffer = Source.GLColorBuffer;
                this.GLElemBuffer = Source.GLElemBuffer;
                this.GLNormalBuffer = Source.GLNormalBuffer;
                this.GLTexCoordBuffer = Source.GLTexCoordBuffer;
                this.GLVertexBuffer = Source.GLVertexBuffer;
                this.ElemLength = Source.ElemLength;
            }

            //~GLVBOModel()
            //{
            //    //Dispose();
            //}

            /// <summary>Disposes buffer objects</summary>
            public void Dispose()
            {
                if (GLVertexBuffer != 0) GL.DeleteBuffers(1, ref GLVertexBuffer);
                if (GLColorBuffer != 0) GL.DeleteBuffers(1, ref GLColorBuffer);
                if (GLNormalBuffer != 0) GL.DeleteBuffers(1, ref GLNormalBuffer);
                if (GLTexCoordBuffer != 0) GL.DeleteBuffers(1, ref GLTexCoordBuffer);
                if (GLElemBuffer != 0) GL.DeleteBuffers(1, ref GLElemBuffer);

                GLVertexBuffer = 0;
                GLColorBuffer = 0;
                GLNormalBuffer = 0;
                GLTexCoordBuffer = 0;
                GLElemBuffer = 0;
            }
            #endregion

            #region Buffers creation

            /// <summary>VBO draw mode</summary>
            public BeginMode DrawMode;

            /// <summary>GL Vertex VBO (xyz)</summary>
            public int GLVertexBuffer = 0;
            /// <summary>GL Color VBO (RGBA)</summary>
            public int GLColorBuffer = 0;
            /// <summary>GL Normals VBO (xyz)</summary>
            public int GLNormalBuffer = 0;
            /// <summary>GL Tex Coords VBO (xy)</summary>
            public int GLTexCoordBuffer = 0;
            /// <summary>GL Element buffer VBO (v1 v2 v3)</summary>
            public int GLElemBuffer = 0;

            /// <summary>Length of elements vector (total triangles = ElemLength/3)</summary>
            public int ElemLength = 1;

            /// <summary>How many vertexes are there?</summary>
            public int numVertexes = 1;

            /// <summary>Sets vertex data information</summary>
            /// <param name="VertexData">Vertex data information. v[3i] = x component of i-th vector, x[3i+1] = y component, x[3i+2] = z component</param>
            public void SetVertexData(float[] VertexData)
            {
                /*
                 * http://www.songho.ca/opengl/gl_vbo.html
                 * "Static" means the data in VBO will not be changed (specified once and used many times), 
                 * "dynamic" means the data will be changed frequently (specified and used repeatedly), and "stream" 
                 * means the data will be changed every frame (specified once and used once). "Draw" means the data will 
                 * be sent to GPU in order to draw (application to GL), "read" means the data will be read by the client's 
                 * application (GL to application), and "copy" means the data will be used both drawing and reading (GL to GL). 
                 */
                SetVertexData(VertexData, BufferUsageHint.DynamicDraw);
            }

            /// <summary>Sets vertex data information</summary>
            /// <param name="VertexData">Vertex data information. v[3i] = x component of i-th vector, x[3i+1] = y component, x[3i+2] = z component</param>
            /// <param name="Hint">OpenGL buffer usage hint</param>
            public void SetVertexData(float[] VertexData, BufferUsageHint Hint)
            {
                lock (this)
                {
                    if (GLVertexBuffer == 0)
                    {
                        GL.GenBuffers(1, out GLVertexBuffer);
                    }

                    GL.BindBuffer(BufferTarget.ArrayBuffer, GLVertexBuffer);
                    GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(VertexData.Length * sizeof(float)), VertexData, Hint);

                    numVertexes = VertexData.Length / 3;
                }
            }

            /// <summary>Sets vertex normals data information</summary>
            /// <param name="NormalVertexData">Normals data information. v[3i] = x component of i-th vector normal, x[3i+1] = y component, x[3i+2] = z component</param>
            public void SetNormalData(float[] NormalVertexData)
            {
                SetNormalData(NormalVertexData, BufferUsageHint.DynamicDraw);
            }

            /// <summary>Sets vertex normals data information</summary>
            /// <param name="NormalVertexData">Normals data information. v[3i] = x component of i-th vector normal, x[3i+1] = y component, x[3i+2] = z component</param>
            /// <param name="Hint">OpenGL buffer usage hint</param>
            public void SetNormalData(float[] NormalVertexData, BufferUsageHint Hint)
            {
                lock (this)
                {

                    if (GLNormalBuffer == 0)
                    {
                        GL.GenBuffers(1, out GLNormalBuffer);
                    }

                    GL.BindBuffer(BufferTarget.ArrayBuffer, GLNormalBuffer);
                    GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(NormalVertexData.Length * sizeof(float)), NormalVertexData, Hint);
                }
            }


            /// <summary>Sets texture coordinate data information</summary>
            /// <param name="TexData">Vertex data information. v[2i] = x texture coord, x[2i+1] = y texture coordinate</param>
            public void SetTexCoordData(float[] TexData)
            {
                SetTexCoordData(TexData, BufferUsageHint.DynamicDraw);
            }

            /// <summary>Sets texture coordinate data information</summary>
            /// <param name="TexData">Vertex data information. v[2i] = x texture coord, x[2i+1] = y texture coordinate</param>
            /// <param name="Hint">OpenGL buffer usage hint</param>
            public void SetTexCoordData(float[] TexData, BufferUsageHint Hint)
            {
                lock (this)
                {

                    if (GLTexCoordBuffer == 0)
                    {
                        GL.GenBuffers(1, out GLTexCoordBuffer);
                    }

                    GL.BindBuffer(BufferTarget.ArrayBuffer, GLTexCoordBuffer);
                    GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(TexData.Length * sizeof(float)), TexData, Hint);
                }
            }

            /// <summary>Sets color information</summary>
            /// <param name="ColorData">Vertex data information. v[4i] = R, x[4i+1] = G, x[4i+2]=B, x[4i+3]=alpha</param>
            public void SetColorData(float[] ColorData)
            {
                SetColorData(ColorData, BufferUsageHint.DynamicDraw);
            }

            /// <summary>Sets color information</summary>
            /// <param name="ColorData">Vertex data information. v[4i] = R, x[4i+1] = G, x[4i+2]=B, x[4i+3]=alpha</param>
            /// <param name="Hint">OpenGL buffer usage hint</param>
            public void SetColorData(float[] ColorData, BufferUsageHint Hint)
            {
                lock (this)
                {

                    if (GLColorBuffer == 0)
                    {
                        GL.GenBuffers(1, out GLColorBuffer);
                    }

                    GL.BindBuffer(BufferTarget.ArrayBuffer, GLColorBuffer);
                    GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(ColorData.Length * sizeof(float)), ColorData, Hint);
                }
            }

            /// <summary>Sets vertex normals data information</summary>
            /// <param name="ElemData">Element data information. v[3i]  v[3i+1] and v[3i+2] are indexes of vertexes that will be drawn</param>
            public void SetElemData(int[] ElemData)
            {
                SetElemData(ElemData, BufferUsageHint.DynamicDraw);
            }

            /// <summary>Sets vertex normals data information</summary>
            /// <param name="ElemData">Element data information. v[3i]  v[3i+1] and v[3i+2] are indexes of vertexes that will be drawn</param>
            /// <param name="Hint">OpenGL buffer usage hint</param>
            public void SetElemData(int[] ElemData, BufferUsageHint Hint)
            {
                lock (this)
                {

                    if (GLElemBuffer == 0)
                    {
                        GL.GenBuffers(1, out GLElemBuffer);
                    }

                    GL.BindBuffer(BufferTarget.ArrayBuffer, GLElemBuffer);
                    GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(ElemData.Length * sizeof(int)), ElemData, Hint);

                    ElemLength = ElemData.Length;
                }
            }
            #endregion

            #region Model draw

            /// <summary>Radian to degree conversion</summary>
            private static float rad2deg = (float)180 / (float)Math.PI;

            /// <summary>Show this model?</summary>
            public bool ShowModel = true;

            /// <summary>Object translation vector from origin.</summary>
            public Vector vetTransl = new Vector(0, 0, 0);
            /// <summary>Object rotation vector in Euler angles (psi-theta-phi).</summary>
            public Vector vetRot = new Vector(0, 0, 0); //em angulos de Euler psi theta phi
            /// <summary>Model scaling {ScaleX, ScaleY, ScaleZ}</summary>
            public float[] Scale = new float [] {1.0f, 1.0f,1.0f};

            /// <summary>This can be used to set model color if color buffer is not being used. Order: RGBA</summary>
            public float[] ModelColor = new float[] { 1.0f, 1.0f, 1.0f, 1.0f };

            /// <summary>Draws this model</summary>
            public void DrawModel()
            {
                if (!this.ShowModel) return;

                GL.PushMatrix();
                GL.Translate((float)vetTransl.x, (float)vetTransl.y, (float)vetTransl.z);

                GL.Rotate((float)vetRot.z * rad2deg, 0.0f, 0.0f, 1.0f);
                GL.Rotate((float)vetRot.y * rad2deg, 0.0f, 1.0f, 0.0f);
                GL.Rotate((float)vetRot.x * rad2deg, 1.0f, 0.0f, 0.0f);

                GL.Scale(Scale[0], Scale[1], Scale[2]);

                GL.Color4(ModelColor[0], ModelColor[1], ModelColor[2], ModelColor[3]);

                //Draws Vertex Buffer Objects onto the screen
                DrawModelVBOs();

                GL.PopMatrix();
            }

            private void DrawModelVBOs()
            {
                #region Draw buffer objects

                if (GLVertexBuffer != 0)
                {
                    GL.BindBuffer(BufferTarget.ArrayBuffer, GLVertexBuffer);
                    GL.VertexPointer(3, VertexPointerType.Float, 0, 0);

                    GL.EnableClientState(ArrayCap.VertexArray);
                }

                if (GLNormalBuffer != 0)
                {
                    GL.BindBuffer(BufferTarget.ArrayBuffer, GLNormalBuffer);
                    GL.NormalPointer(NormalPointerType.Float, 0, 0);

                    GL.EnableClientState(ArrayCap.NormalArray);
                }

                if (GLColorBuffer != 0)
                {
                    GL.BindBuffer(BufferTarget.ArrayBuffer, GLColorBuffer);
                    GL.ColorPointer(4, ColorPointerType.Float, 0, 0);

                    GL.EnableClientState(ArrayCap.ColorArray);
                }


                if (GLTexCoordBuffer != 0)
                {
                    GL.BindBuffer(BufferTarget.ArrayBuffer, GLTexCoordBuffer);
                    GL.TexCoordPointer(2, TexCoordPointerType.Float, 0, 0);

                    GL.EnableClientState(ArrayCap.TextureCoordArray);
                }

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, GLElemBuffer);
                GL.DrawElements(DrawMode, ElemLength, DrawElementsType.UnsignedInt, 0);

                if (GLVertexBuffer != 0) GL.DisableClientState(ArrayCap.VertexArray);
                if (GLColorBuffer != 0) GL.DisableClientState(ArrayCap.ColorArray);
                if (GLNormalBuffer != 0) GL.DisableClientState(ArrayCap.NormalArray);
                if (GLTexCoordBuffer != 0) GL.DisableClientState(ArrayCap.TextureCoordArray);

                #endregion

            }
            #endregion

            #region Retrieving OpenCL buffers from OpenGL buffers

            /// <summary>Local storage of element data buffer</summary>
            private CLCalc.Program.Variable CLElemBuffer;
            /// <summary>Retrieves an OpenCL float buffer from this object's OpenGL elements VBO (3 ints per element)</summary>
            public CLCalc.Program.Variable GetCLElemBuffer()
            {
                if (CLElemBuffer == null)
                {
                    CLElemBuffer = new CLCalc.Program.Variable(GLElemBuffer,typeof(int));
                }
                return CLElemBuffer;
            }

            /// <summary>Local storage of color buffer</summary>
            private CLCalc.Program.Variable CLColorBuffer;
            /// <summary>Retrieves an OpenCL float buffer from this object's OpenGL color data VBO (4 floats per vertex)</summary>
            public CLCalc.Program.Variable GetCLColorBuffer()
            {
                if (CLColorBuffer == null)
                {
                    CLColorBuffer = new CLCalc.Program.Variable(GLColorBuffer, typeof(float));
                }
                return CLColorBuffer;
            }

            /// <summary>Local storage of texture coordinates buffer</summary>
            private CLCalc.Program.Variable CLTexCoordBuffer;
            /// <summary>Retrieves an OpenCL float buffer from this object's OpenGL texture coordinate data VBO (2 floats per vertex)</summary>
            public CLCalc.Program.Variable GetCLTexCoordBuffer()
            {
                if (CLTexCoordBuffer == null)
                {
                    CLTexCoordBuffer = new CLCalc.Program.Variable(GLTexCoordBuffer, typeof(float));
                }
                return CLTexCoordBuffer;
            }

            /// <summary>Local storage of vertex normals buffer</summary>
            private CLCalc.Program.Variable CLNormalBuffer;
            /// <summary>Retrieves an OpenCL float buffer from this object's OpenGL vertex normals data VBO (3 floats per vertex)</summary>
            public CLCalc.Program.Variable GetCLNormalBuffer()
            {
                if (CLNormalBuffer == null)
                {
                    CLNormalBuffer = new CLCalc.Program.Variable(GLNormalBuffer, typeof(float));
                }
                return CLNormalBuffer;
            }

            /// <summary>Local storage of vertex buffer</summary>
            private CLCalc.Program.Variable CLVertexBuffer;
            /// <summary>Retrieves an OpenCL float buffer from this object's OpenGL vertex data VBO (3 floats per vertex)</summary>
            public CLCalc.Program.Variable GetCLVertexBuffer()
            {
                if (CLVertexBuffer == null)
                {
                    CLVertexBuffer = new CLCalc.Program.Variable(GLVertexBuffer, typeof(float));
                }
                return CLVertexBuffer;
            }



            #endregion

            #region Creating buffers from equations
            /// <summary>Creates a surface from given equations. Parameters are u and v (strings). Eg: vertexEqs[0] = "u+v"</summary>
            /// <param name="uParams">U coordinate parameters: [0] - uMin, [1] - uMax, [2] - number of points</param>
            /// <param name="vParams">V coordinate parameters: [0] - vMin, [1] - vMax, [2] - number of points</param>
            /// <param name="vertexEqs">Array containing 3 strings that will define vertex positions. [0] x(u,v), [1] y(u,v), [2] z(u,v)</param>
            /// <param name="colorEqs">Array containing 4 strings that will define vertex colors R(u,v), G(u,v), B(u,v), A(u,v). May contain only RGB</param>
            /// <param name="normalsEqs">Array containing strings that will define vertex normals</param>
            public static GLVBOModel CreateSurface(float[] uParams, float[] vParams, string[] vertexEqs, string[] colorEqs, string[] normalsEqs)
            {
                GLVBOModel model = new GLVBOModel(BeginMode.Triangles);

                int uPts = (int)uParams[2];
                int vPts = (int)vParams[2];

                float[] vertexes = new float[3 * uPts * vPts];
                float[] normals = new float[3 * uPts * vPts];
                float[] colors = new float[4 * uPts * vPts];

                int[] elems = new int[6 * (uPts - 1) * (vPts - 1)]; //3 vertices per element, 2 triangles to make a square

                model.SetColorData(colors);
                model.SetNormalData(normals);
                model.SetVertexData(vertexes);
                model.SetElemData(elems);

                //Reads GL buffers
                CLCalc.Program.Variable CLvertex = model.GetCLVertexBuffer();
                CLCalc.Program.Variable CLnormal = model.GetCLNormalBuffer();
                CLCalc.Program.Variable CLcolor = model.GetCLColorBuffer();
                CLCalc.Program.Variable CLelem = model.GetCLElemBuffer();

                CLCalc.Program.Variable[] args = new CLCalc.Program.Variable[] { CLvertex, CLnormal, CLcolor, CLelem };


                //Creates source
                #region Assembles OpenCL source
                string src = @"

//enqueue with dimensions uPts-1, vPts-1
__kernel void CreateElems(__global int* elems)
{
  int i = get_global_id(0);
  int w = get_global_size(0);
  
  int j = get_global_id(1);
  
  int ind = 6*(i+w*j);
  w++;
  elems[ind] = i+w*j;
  elems[ind+1] = i+1+w*j;
  elems[ind+2] = i+1+w*j+w;
  
  elems[ind+3] = i+w*j;
  elems[ind+4] = i+1+w*j+w;
  elems[ind+5] = i+w*j+w;
}

__kernel void f(__global float* vertex,
                __global float* normal,
                __global float* colors,
                __constant float* uvMinStep)

{
   int i = get_global_id(0);
   int w = get_global_size(0); //Matrix width
   
   int j = get_global_id(1);
   
   float uMin = uvMinStep[0];
   float uStep = uvMinStep[1];

   float vMin = uvMinStep[2];
   float vStep = uvMinStep[3];
   
   float u = uMin + uStep*(float)i;
   float v = vMin + vStep*(float)j;
   
   //Vertexes
   int ind = 3*(i+w*j);
   vertex[ind] = " + vertexEqs[0] + @";
   vertex[ind+1] = " + vertexEqs[1] + @";
   vertex[ind+2] = " + vertexEqs[2] + @";
   
   //Normals
   float4 n;
   n.x = " + normalsEqs[0] + @";
   n.y = " + normalsEqs[1] + @";
   n.z = " + normalsEqs[2] + @";
   n.w = 0;
   n = normalize(n);
   normal[ind] = n.x;
   normal[ind+1] = n.y;
   normal[ind+2] = n.z;
   
   
   //Colors
   ind = (i+w*j)<<2;
   colors[ind] = " + colorEqs[0] + @";
   colors[ind+1] = " + colorEqs[1] + @";
   colors[ind+2] = " + colorEqs[2] + @";
   colors[ind+3] = " + (colorEqs.Length >= 4 ? colorEqs[4] : "1") + @";
   
}
";
                #endregion

                //Creates kernel
                CLCalc.Program.Compile(src);

                CLCalc.Program.Kernel kernelEquations = new CLCalc.Program.Kernel("f");
                CLCalc.Program.Kernel createElems = new CLCalc.Program.Kernel("CreateElems");

                //Information vector
                float[] uvminStep = new float[] { uParams[0], (uParams[1] - uParams[0]) / (uPts-1), vParams[0], (vParams[1] - vParams[0]) / (vPts-1)};
                CLCalc.Program.Variable CLuvminStep = new CLCalc.Program.Variable(uvminStep);

                CLCalc.Program.Variable CLElem2 = new CLCalc.Program.Variable(elems);

                //Acquires to OpenCL
                CLGLInterop.CLGLInteropFunctions.AcquireGLElements(args);

                //Runs kernels
                createElems.Execute(new CLCalc.Program.Variable[] {CLelem}, new int[] { uPts - 1, vPts - 1 });
                kernelEquations.Execute(new CLCalc.Program.Variable[] 
                {
                    CLvertex,CLnormal,CLcolor,CLuvminStep
                }
                , new int[] { uPts, vPts });



                //Releases from OpenCL
                CLGLInterop.CLGLInteropFunctions.ReleaseGLElements(args);

                return model;
            }
            #endregion


        }

        /// <summary>OpenGL 3D font creator</summary>
        public class GLFont
        {
            /// <summary>Stores 3D character models</summary>
            public GLRender.GLVBOModel[] GLchars;

            /// <summary>Reference width of letter O</summary>
            private float referenceWidth;

            /// <summary>Width of characters in OpenGL scale</summary>
            private float[] WidthInGLScale;

            #region 3D font constructor and loader from file.
            /// <summary>Creates a new 3D font from specified font</summary>
            /// <param name="f">Font prototype to use in this 3D font</param>
            /// <param name="GLNormalizationScale">Character reference width in OpenGL scale</param>
            public GLFont(Font f, float GLNormalizationScale)
            {
                byte[] b = new byte[1];
                Graphics g = Graphics.FromImage(new Bitmap(1, 1));
                string s;

                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                System.Diagnostics.Stopwatch sw2 = new System.Diagnostics.Stopwatch();

                //Preliminary size
                SizeF sizePrelim = g.MeasureString("O", f);
                this.referenceWidth = sizePrelim.Width;
                int w = (int)(2.0f * sizePrelim.Width);
                int h = (int)(sizePrelim.Height);

                float[, ,] bmpVals = new float[w, h, 3];
                OpenCLTemplate.Isosurface.MarchingCubes mc = new OpenCLTemplate.Isosurface.MarchingCubes(bmpVals);

                //GLchars[i].Scale = size.Width * 0.4f;
                //Centers in Z
                float temp = GLNormalizationScale / referenceWidth;
                mc.Increments = new float[] { temp, temp, GLNormalizationScale * 0.1f };
                mc.InitValues = new float[] { 0.0f, 0.0f, -GLNormalizationScale * 0.1f };

                //Creates 3D models for each character
                GLchars = new GLRender.GLVBOModel[256];
                WidthInGLScale = new float[256];
                for (int i = 0; i < 256; i++)
                {
                    sw.Start();
                    b[0] = (byte)i;
                    s = System.Text.ASCIIEncoding.Default.GetString(b);
                    //Measures string size
                    SizeF size = g.MeasureString(s, f);

                    //Creates a bitmap to store the letter and draws it onto the bitmap
                    Bitmap bmp = new Bitmap(1 + (int)size.Width, 1 + (int)size.Height);
                    Graphics g2 = Graphics.FromImage(bmp);

                    WidthInGLScale[i] = 0.7f * GLNormalizationScale * (float)size.Width / (float)sizePrelim.Width;

                    g2.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    g2.FillRectangle(Brushes.Black, 0, 0, bmp.Width, bmp.Height);
                    g2.DrawString(s, f, Brushes.White, 0, 0);

                    //Creates a float array to store bitmap values (input to isoSurface generator)

                    for (int x = 0; x < w; x++)
                    {
                        for (int y = 0; y < h; y++)
                        {
                            bmpVals[x, y, 0] = 0;
                            bmpVals[x, y, 1] = 0;
                            bmpVals[x, y, 2] = 0;
                        }
                    }

                    bool intensitiesAdded = false;

                    #region Reads bitmap to float values array
                    System.Drawing.Imaging.BitmapData bmdbmp = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                               System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    unsafe
                    {
                        for (int y = 0; y < bmdbmp.Height; y++)
                        {
                            byte* row = (byte*)bmdbmp.Scan0 + (y * bmdbmp.Stride);

                            for (int x = 0; x < bmdbmp.Width; x++)
                            {
                                if (row[x << 2] != 0)
                                {
                                    bmpVals[x, h - y - 1, 1] = row[(x << 2)];
                                    intensitiesAdded = true;
                                }
                            }
                        }
                    }


                    #endregion

                    bmp.UnlockBits(bmdbmp);

                    //p.Image = bmp;
                    bmp.Dispose();

                    //If there's data to create a 3D model do so
                    if (intensitiesAdded)
                    {

                        GLchars[i] = new GLRender.GLVBOModel(BeginMode.Triangles);
                        mc.SetFuncVals(bmpVals);

                        sw2.Start();
                        mc.CalcIsoSurface(027.0f);
                        List<float> Vertex, Normals; List<int> Elems;
                        mc.GetEdgeInfo(out Vertex, out Normals, out Elems);
                        sw2.Stop();

                        //float[] Colors = new float[(Vertex.Count / 3) << 2];
                        //for (int k = 0; k < Colors.Length; k++) Colors[k] = 1.0f;

                        //GLchars[i].vetRot.z = Math.PI / 2;
                        //GLchars[i].vetRot.x = -Math.PI / 2;
                        //GLchars[i].Scale = 100.0f;

                        GLchars[i].SetNormalData(Normals.ToArray());
                        GLchars[i].SetVertexData(Vertex.ToArray());
                        GLchars[i].SetElemData(Elems.ToArray());
                        //GLchars[i].SetColorData(Colors);
                        sw.Stop();
                    }
                    else WidthInGLScale[i] = 0.6f * GLNormalizationScale;

                    sw.Stop();
                }

            }

            /// <summary>Loads a 3D font from a file. Does NOT require OpenCL/GL interop</summary>
            /// <param name="filename">3D Font file</param>
            public GLFont(string filename)
            {
                //Reads file information
                byte[] Data;
                using (System.IO.BinaryReader b = new System.IO.BinaryReader(System.IO.File.Open(filename, System.IO.FileMode.Open)))
                {
                    long length = b.BaseStream.Length;

                    Data = new byte[length];

                    long pos = 0;
                    int bytesToRead = 1000;
                    while (pos < length)
                    {
                        byte[] bt = b.ReadBytes(bytesToRead);

                        for (int i = 0; i < bt.Length; i++) Data[pos + i] = bt[i];

                        pos += bytesToRead;
                        if (length - pos < bytesToRead) bytesToRead = (int)(length - pos);

                    }

                    b.Close();
                }

                //Creates 3D models for each character
                GLchars = new GLRender.GLVBOModel[256];
                WidthInGLScale = new float[256];

                //Last stored char. So far, it has to be 255
                byte nChars = Data[0];
                int DataPos = 1;
                for (int indChar = 0; indChar <= nChars; indChar++)
                {
                    //Current char
                    byte CurChar = Data[DataPos]; DataPos++;

                    //Width of this char
                    WidthInGLScale[indChar] = BitConverter.ToSingle(Data, DataPos); DataPos += 4;

                    //number of vertexes/normals
                    int nVert = BitConverter.ToInt32(Data, DataPos); DataPos += 4;

                    //number of elements
                    int nElem = BitConverter.ToInt32(Data, DataPos); DataPos += 4;

                    //Only creates character if there are elements
                    if (nVert > 0)
                    {
                        float[] verts = new float[3 * nVert];
                        float[] normals = new float[3 * nVert];
                        int[] elems = new int[3 * nElem];

                        for (int p = 0; p < 3 * nVert; p += 3)
                        {
                            verts[p] = BitConverter.ToSingle(Data, DataPos); DataPos += 4;
                            verts[1 + p] = BitConverter.ToSingle(Data, DataPos); DataPos += 4;
                            verts[2 + p] = BitConverter.ToSingle(Data, DataPos); DataPos += 4;
                        }
                        for (int p = 0; p < 3 * nVert; p += 3)
                        {
                            normals[p] = BitConverter.ToSingle(Data, DataPos); DataPos += 4;
                            normals[1 + p] = BitConverter.ToSingle(Data, DataPos); DataPos += 4;
                            normals[2 + p] = BitConverter.ToSingle(Data, DataPos); DataPos += 4;
                        }
                        for (int p = 0; p < 3 * nElem; p += 3)
                        {
                            elems[p] = BitConverter.ToInt32(Data, DataPos); DataPos += 4;
                            elems[1 + p] = BitConverter.ToInt32(Data, DataPos); DataPos += 4;
                            elems[2 + p] = BitConverter.ToInt32(Data, DataPos); DataPos += 4;
                        }
                        //Creates the model
                        GLchars[indChar] = new GLRender.GLVBOModel(BeginMode.Triangles);
                        GLchars[indChar].SetNormalData(normals);
                        GLchars[indChar].SetVertexData(verts);
                        GLchars[indChar].SetElemData(elems);
                    }
                    else
                    {
                    }


                }
            }
            #endregion

            #region Drawing 3D strings
            /// <summary>Creates an array of 3D models containing the given string. If target!=null adds them to target`s display list</summary>
            /// <param name="s">String to write</param>
            /// <param name="target">Target GLWindow to write</param>
            public List<GLRender.GLVBOModel> Draw3DString(string s, GLRender target)
            {
                List<GLRender.GLVBOModel> GLstr = new List<GLRender.GLVBOModel>();
                byte[] sb = System.Text.ASCIIEncoding.Default.GetBytes(s);

                float curX = 0;
                for (int i = 0; i < sb.Length; i++)
                {
                    if (GLchars[sb[i]] != null)
                    {
                        GLRender.GLVBOModel m = new GLRender.GLVBOModel(GLchars[sb[i]]);

                        m.vetTransl.x = curX;
                        GLstr.Add(m);

                        if (target != null) target.Models.Add(m);
                    }

                    curX += this.WidthInGLScale[sb[i]];
                }

                return GLstr;
            }


            /// <summary>Creates an array of 3D models containing the given string</summary>
            /// <param name="s">String to write</param>
            public List<GLRender.GLVBOModel> Draw3DString(string s)
            {
                return Draw3DString(s, null);
            }
            #endregion

            #region 3D font save

            /// <summary>Saves this 3D font to a file. Requires OpenCL/GL interoperation.</summary>
            /// <param name="file">File to save to.</param>
            public void Save(string file)
            {
                //Stores all font information
                List<byte> Data = new List<byte>();
                byte[] b;

                //Stores data for 256 characters. Last is 255
                Data.Add(255);

                for (int i = 0; i < 256; i++)
                {
                    //Dealing with the i-th character
                    Data.Add((byte)i);

                    //Stores its width
                    b = BitConverter.GetBytes(WidthInGLScale[i]);
                    for (int k = 0; k < b.Length; k++) Data.Add(b[k]);

                    if (GLchars[i] != null)
                    {
                        //Stores the number of vertexes. Vertex bytes written afterwards will be 3*numVertexes*(4 bytes per float)
                        b = BitConverter.GetBytes(GLchars[i].numVertexes);
                        for (int k = 0; k < b.Length; k++) Data.Add(b[k]);

                        //Stores the number of elements. Element bytes written afterwards will be 3*number of elements*(4 bytes per int)
                        b = BitConverter.GetBytes(GLchars[i].ElemLength / 3);
                        for (int k = 0; k < b.Length; k++) Data.Add(b[k]);

                        CLCalc.Program.Variable CLvertex = GLchars[i].GetCLVertexBuffer();
                        CLCalc.Program.Variable CLnormals = GLchars[i].GetCLNormalBuffer();
                        CLCalc.Program.Variable CLelems = GLchars[i].GetCLElemBuffer();
                        CLCalc.Program.Variable[] vars = new CLCalc.Program.Variable[] { CLvertex, CLnormals, CLelems };

                        CLGLInteropFunctions.AcquireGLElements(vars);
                        float[] vertex = new float[CLvertex.OriginalVarLength];
                        float[] normals = new float[CLnormals.OriginalVarLength];
                        int[] elems = new int[CLelems.OriginalVarLength];

                        CLvertex.ReadFromDeviceTo(vertex);
                        CLnormals.ReadFromDeviceTo(normals);
                        CLelems.ReadFromDeviceTo(elems);

                        CLGLInteropFunctions.ReleaseGLElements(vars);

                        //Stores each vertex
                        for (int p = 0; p < vertex.Length; p++)
                        {
                            b = BitConverter.GetBytes(vertex[p]);
                            for (int k = 0; k < b.Length; k++) Data.Add(b[k]);
                        }

                        //Stores each normal
                        for (int p = 0; p < normals.Length; p++)
                        {
                            b = BitConverter.GetBytes(normals[p]);
                            for (int k = 0; k < b.Length; k++) Data.Add(b[k]);
                        }

                        //Stores each element
                        for (int p = 0; p < elems.Length; p++)
                        {
                            b = BitConverter.GetBytes(elems[p]);
                            for (int k = 0; k < b.Length; k++) Data.Add(b[k]);
                        }
                    }
                    else
                    {
                        //Stores zero as number of vertexes
                        b = BitConverter.GetBytes((int)0);
                        for (int k = 0; k < b.Length; k++) Data.Add(b[k]);

                        //Stores zero as number of elements
                        b = BitConverter.GetBytes((int)0);
                        for (int k = 0; k < b.Length; k++) Data.Add(b[k]);
                    }
                }

                System.IO.FileStream fs = new System.IO.FileStream(file, System.IO.FileMode.Create);

                using (System.IO.BinaryWriter bw = new System.IO.BinaryWriter(fs))
                {
                    bw.Write(Data.ToArray());

                    bw.Close();
                }
                fs.Close();
            }

            #endregion

            #region Generate texture from string

            /// <summary>Returns a Bitmap containing a text drawn. Useful to set as texture.</summary>
            /// <param name="s">String to be written</param>
            /// <param name="TextFont">Font to use</param>
            /// <param name="TextLeftColor">Left color of Text.</param>
            /// <param name="TextRightColor">Right color of Text.</param>
            /// <param name="BackgroundLeftColor">Left color of Background.</param>
            /// <param name="BackgroundRightColor">Right color of Background.</param>
            public static Bitmap DrawString(string s, Font TextFont, Color TextLeftColor, Color TextRightColor,
                Color BackgroundLeftColor, Color BackgroundRightColor)
            {
                if (s == "") return null;

                Bitmap dum = new Bitmap(10, 10);
                Graphics g = Graphics.FromImage(dum);

                SizeF size = g.MeasureString(s, TextFont);

                Bitmap bmp = new Bitmap((int)size.Width, (int)size.Height);
                Graphics gbmp = Graphics.FromImage(bmp);

                Brush bBckg = new System.Drawing.Drawing2D.LinearGradientBrush(new PointF(0, 0), new PointF(size.Width, size.Height), BackgroundLeftColor, BackgroundRightColor);
                gbmp.FillRectangle(bBckg, 0, 0, bmp.Width, bmp.Height);

                Brush bTexto = new System.Drawing.Drawing2D.LinearGradientBrush(new PointF(0, 0), new PointF(size.Width, size.Height), TextLeftColor, TextRightColor);
                gbmp.DrawString(s, TextFont, bTexto, 0, 0);

                dum.Dispose();

                return bmp;
            }

            /// <summary>Returns a Bitmap containing a text drawn. Useful to set as texture.</summary>
            /// <param name="s">String to be written</param>
            /// <param name="TextFont">Font to use</param>
            /// <param name="TextColor">Text color.</param>
            /// <param name="BackgroundColor">Background color.</param>
            public static Bitmap DrawString(string s, Font TextFont, Color TextColor, Color BackgroundColor)
            {
                return DrawString(s, TextFont, TextColor, TextColor, BackgroundColor, BackgroundColor);
            }

            /// <summary>Returns a Bitmap containing a text drawn. Useful to set as texture.</summary>
            /// <param name="s">String to be written</param>
            /// <param name="TextFont">Font to use</param>
            public static Bitmap DrawString(string s, Font TextFont)
            {
                return DrawString(s, TextFont, Color.Black, Color.Black, Color.White, Color.White);
            }

            #endregion
        }

        /// <summary>Copies bitmap data to a OpenGL texture</summary>
        /// <param name="TextureBitmap">Bitmap to be copied to OpenGL memory</param>
        /// <param name="ind">A valid OpenGL texture generated with GLGenTexture. If less than zero a new OpenGL texture is created and stored in ind</param>
        public static void ApplyTexture(Bitmap TextureBitmap, ref int ind)
        {
            if (TextureBitmap != null)
            {
                if (ind <= 0) ind = GL.GenTexture();

                //texture, if there is one
                System.Drawing.Bitmap image = new System.Drawing.Bitmap(TextureBitmap);
                //image.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipY); //this takes too long
                System.Drawing.Imaging.BitmapData bitmapdata;
                System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, image.Width, image.Height);

                bitmapdata = image.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                GL.BindTexture(TextureTarget.Texture2D, ind);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Nearest);

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, image.Width, image.Height,
                    0, (OpenTK.Graphics.OpenGL.PixelFormat)(int)All.BgrExt, PixelType.UnsignedByte, bitmapdata.Scan0);

                image.UnlockBits(bitmapdata);
                image.Dispose();

                GL.BindTexture(TextureTarget.Texture2D, ind);
            }
        }


    }

    /// <summary>Encapsulates functions needed to acquire and release OpenCL/GL shared objects</summary>
    public static class CLGLInteropFunctions
    {
        /// <summary>Acquires one OpenCL variable created from GL buffers in order to use it. Ignores variables not created from OpenGL buffer</summary>
        /// <param name="CLGLVar">Variable to be acquired</param>
        public static void AcquireGLElements(CLCalc.Program.Variable CLGLVar)
        {
            AcquireGLElements(new CLCalc.Program.Variable[] { CLGLVar });
        }

        /// <summary>Acquires OpenCL variables created from GL buffers in order to use them. Ignores variables not created from OpenGL buffer</summary>
        /// <param name="CLGLVars">Variables to be acquired</param>
        public static void AcquireGLElements(CLCalc.Program.Variable[] CLGLVars)
        {
            GL.Finish();

            List<ComputeMemory> ClooCLGLBuffers = new List<ComputeMemory>();
            foreach (CLCalc.Program.Variable var in CLGLVars)
            {
                if (var.CreatedFromGLBuffer && (!var.AcquiredInOpenCL))
                {
                    ClooCLGLBuffers.Add(var.VarPointer);
                    var.AcquiredInOpenCL = true;
                }
            }

            CLCalc.Program.CommQueues[CLCalc.Program.DefaultCQ].AcquireGLObjects(ClooCLGLBuffers, null);
        }

        /// <summary>Releases one OpenCL variable created from GL buffers. Ignores variables not created from OpenGL buffer</summary>
        /// <param name="CLGLVar">Variable to be released</param>
        public static void ReleaseGLElements(CLCalc.Program.Variable CLGLVar)
        {
            ReleaseGLElements(new CLCalc.Program.Variable[] { CLGLVar });
        }

        /// <summary>Releases OpenCL variables created from GL buffers. Ignores variables not created from OpenGL buffer</summary>
        /// <param name="CLGLVars">Variables to be acquired</param>
        public static void ReleaseGLElements(CLCalc.Program.Variable[] CLGLVars)
        {
            GL.Finish();

            List<ComputeMemory> ClooCLGLBuffers = new List<ComputeMemory>();
            foreach (CLCalc.Program.Variable var in CLGLVars)
            {
                if (var.CreatedFromGLBuffer && var.AcquiredInOpenCL)
                {
                    ClooCLGLBuffers.Add(var.VarPointer);
                    var.AcquiredInOpenCL = false;
                }
            }

            CLCalc.Program.CommQueues[CLCalc.Program.DefaultCQ].ReleaseGLObjects(ClooCLGLBuffers, null);
            CLCalc.Program.CommQueues[CLCalc.Program.DefaultCQ].Finish();
        }
    }

    /// <summary>Vector class with math operations and dot / cross products.</summary>
    public class Vector : IComparable<Vector>
    {
        /// <summary>Vector X component.</summary>
        public double x;
        /// <summary>Vector Y component.</summary>
        public double y;
        /// <summary>Vector Z component.</summary>
        public double z;

        #region "Construtores e ToString"
        /// <summary>Constructor. Initializes zero vector.</summary>
        public Vector()
        {
            x = 0; y = 0; z = 0;
        }

        /// <summary>Construtor. Initializes given values.</summary>
        /// <param name="xComponent">Vector X component.</param>
        /// <param name="yComponent">Vector Y component.</param>
        /// <param name="zComponent">Vector Z component.</param>
        public Vector(double xComponent, double yComponent, double zComponent)
        {
            this.x = xComponent;
            this.y = yComponent;
            this.z = zComponent;
        }

        /// <summary>Construtor. Copies a given vector.</summary>
        /// <param name="v">Vector to copy.</param>
        public Vector(Vector v)
        {
            this.x = v.x;
            this.y = v.y;
            this.z = v.z;
        }

        /// <summary>Returns a string that represents this vector.</summary>
        public override string ToString()
        {
            return "(" + this.x.ToString() + ";" + this.y.ToString() + ";" + this.z.ToString() + ")";
        }
        #endregion

        #region "Operadores aritméticos e comparação de igualdade"
        /// <summary>Vector sum.</summary>
        /// <param name="v1">First vector to sum.</param>
        /// <param name="v2">Second vector to sum.</param>
        public static Vector operator +(Vector v1, Vector v2)
        {
            Vector resp = new Vector();
            resp.x = v1.x + v2.x;
            resp.y = v1.y + v2.y;
            resp.z = v1.z + v2.z;
            return resp;
        }
        /// <summary>Vector subtraction.</summary>
        /// <param name="v1">Vector to subtract from.</param>
        /// <param name="v2">Vector to be subtracted.</param>
        public static Vector operator -(Vector v1, Vector v2)
        {
            Vector resp = new Vector();
            resp.x = v1.x - v2.x;
            resp.y = v1.y - v2.y;
            resp.z = v1.z - v2.z;
            return resp;
        }
        /// <summary>Vector scalar product.</summary>
        /// <param name="num">Scalar to multiply.</param>
        /// <param name="v">Vector to multiply.</param>
        public static Vector operator *(double num, Vector v)
        {
            Vector resp = new Vector();
            resp.x = v.x * num;
            resp.y = v.y * num;
            resp.z = v.z * num;
            return resp;
        }
        /// <summary>Vector scalar product.</summary>
        /// <param name="num">Scalar to multiply.</param>
        /// <param name="v">Vector to multiply.</param>
        public static Vector operator *(Vector v, double num)
        {
            Vector resp = new Vector();
            resp.x = v.x * num;
            resp.y = v.y * num;
            resp.z = v.z * num;
            return resp;
        }
        /// <summary>Vector scalar division.</summary>
        /// <param name="num">Scalar to divide by.</param>
        /// <param name="v">Vector to be divided.</param>
        public static Vector operator /(double num, Vector v)
        {
            Vector resp = new Vector();
            resp.x = v.x / num;
            resp.y = v.y / num;
            resp.z = v.z / num;
            return resp;
        }
        /// <summary>Vector scalar division.</summary>
        /// <param name="num">Scalar to divide by.</param>
        /// <param name="v">Vector to be divided.</param>
        public static Vector operator /(Vector v, double num)
        {
            Vector resp = new Vector();
            resp.x = v.x / num;
            resp.y = v.y / num;
            resp.z = v.z / num;
            return resp;
        }

        /// <summary>Equality comparison.</summary>
        /// <param name="v">Vector to compare to.</param>
        public int CompareTo(Vector v)
        {
            if (this.x == v.x && this.y == v.y && this.z == v.z)
            {
                return 0;
            }
            else
            {
                return 1;
            }

        }
        #endregion

        #region "Produtos escalar e vetorial"
        /// <summary>Returns vector dot product.</summary>
        /// <param name="v1">First vector of Dot Product.</param>
        /// <param name="v2">Second vector of Dot Product.</param>
        public static double DotProduct(Vector v1, Vector v2)
        {
            return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
        }

        /// <summary>Returns vector cross product.</summary>
        /// <param name="v1">First vector of Cross Product.</param>
        /// <param name="v2">Second vector of Cross Product.</param>
        public static Vector CrossProduct(Vector v1, Vector v2)
        {
            //i    j    k
            //this
            //v

            Vector resp = new Vector();
            resp.x = v1.y * v2.z - v2.y * v1.z;
            resp.y = -v1.x * v2.z + v2.x * v1.z;
            resp.z = v1.x * v2.y - v2.x * v1.y;
            return resp;
        }
        #endregion

        #region "Normalização para comprimento unitário"
        /// <summary>Returns vector norm.</summary>
        public double norm()
        {
            return Math.Sqrt(Vector.DotProduct(this, this));
        }

        /// <summary>Normalizes this vector.</summary>
        public void normalize()
        {
            double invNorma = 1 / this.norm();
            this.x *= invNorma;
            this.y *= invNorma;
            this.z *= invNorma;
        }
        #endregion

    }

}
