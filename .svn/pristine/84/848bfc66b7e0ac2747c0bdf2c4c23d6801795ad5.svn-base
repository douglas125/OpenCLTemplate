using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace OpenCLTemplate
{
    /// <summary>Class to help editing OpenCL code</summary>
    public class OpenCLRTBController
    {
        private RichTextBox rTB;

        /// <summary>Constructor. Takes care of a Rich Text Box KeyUp event to paint things</summary>
        /// <param name="rTBOpenCL">RichTextBox to control</param>
        public OpenCLRTBController(RichTextBox rTBOpenCL)
        {
            #region OpenCL extensions

            StringsToMark Extensions = new StringsToMark();
            Extensions.Description = "OpenCL Extensions";
            Extensions.StringsFont = new Font("Courier New", 10, FontStyle.Bold);
            Extensions.StringsColor = Color.DarkGoldenrod;
            Extensions.Strings = new List<string>()
            { 
                "#pragma","OPENCL EXTENSION", "cl_khr_fp64", 
                "cl_khr_global_int32_base_atomics", "cl_khr_global_int32_extended_atomics",
                "cl_khr_local_int32_base_atomics", "cl_khr_local_int32_extended_atomics",
                "cl_khr_int64_base_atomics","cl_khr_int64_extended_atomics",
                "cl_khr_3d_image_writes","cl_khr_byte_addressable_store","cl_khr_fp16"

            };

            OpenCLStrings.Add(Extensions);

            #endregion

            #region OpenCL qualifiers
            StringsToMark Qualif = new StringsToMark();
            Qualif.StringsFont = new Font("Courier New", 10, FontStyle.Bold);
            Qualif.Description = "OpenCL qualifiers";
            Qualif.StringsColor = Color.DarkRed;
            Qualif.Strings = new List<string>() { 
                "kernel", "read_only", "write_only", "global", "local", "constant", "private", 
                "__kernel", "__read_only", "__write_only", "__global", "__local", "__constant", "__private",
                "__attribute__", "reqd_work_group_size", "work_group_size_hint", "vec_type_hint"
            };

            OpenCLStrings.Add(Qualif);
            #endregion

            #region Built-in scalar data types

            StringsToMark BuiltInScalars = new StringsToMark();
            BuiltInScalars.Description = "Built-in scalar data types";
            BuiltInScalars.StringsColor = Color.Blue;
            BuiltInScalars.Strings = new List<string>() { "void", "bool", "char", "uchar", "short", "ushort", "int", "uint", "long", "ulong", "float", "double", "half", "size_t" };

            OpenCLStrings.Add(BuiltInScalars);

            #endregion

            #region Built-in vector data types

            StringsToMark BuiltInVecs = new StringsToMark();
            BuiltInVecs.Description = "Built-in vector data types";
            BuiltInVecs.StringsFont = new Font("Courier New", 10, FontStyle.Bold);
            BuiltInVecs.StringsColor = Color.Blue;
            BuiltInVecs.Strings = new List<string>()
            { 
                "char2", "uchar2", "short2", "ushort2", "int2", "uint2", "long2", "ulong2", "float2", "double2",
                "char4", "uchar4", "short4", "ushort4", "int4", "uint4", "long4", "ulong4", "float4", "double4",
                "char8", "uchar8", "short8", "ushort8", "int8", "uint8", "long8", "ulong8", "float8", "double8",
                "char16", "uchar16", "short16", "ushort16", "int16", "uint16", "long16", "ulong16", "float16", "double16",
            };

            OpenCLStrings.Add(BuiltInVecs);

            #endregion

            #region OpenCL flow
            StringsToMark FlowControl = new StringsToMark();
            FlowControl.StringsFont = new Font("Courier New", 10, FontStyle.Regular);
            FlowControl.Description = "OpenCL flow control";
            FlowControl.StringsColor = Color.Blue;
            FlowControl.Strings = new List<string>() { "for", "while", "if", "else", "return" };

            OpenCLStrings.Add(FlowControl);
            #endregion

            #region Work-item functions
            StringsToMark WorkItemFuncs = new StringsToMark();
            WorkItemFuncs.StringsFont = new Font("Courier New", 10, FontStyle.Bold);
            WorkItemFuncs.Description = "Work-item functions";
            WorkItemFuncs.StringsColor = Color.Purple;
            WorkItemFuncs.Strings = new List<string>() { "get_work_dim", "get_global_size", "get_global_id", "get_local_size", "get_local_id", "get_num_groups", "get_group_id" };


            OpenCLStrings.Add(WorkItemFuncs);
            #endregion

            #region Constants
            StringsToMark Consts = new StringsToMark();
            Consts.StringsFont = new Font("Courier New", 10, FontStyle.Bold);
            Consts.Description = "Pre-defined constants";
            Consts.StringsColor = Color.FromArgb(50, 50, 50);
            Consts.Strings = new List<string>() { "#define", "MAXFLOAT", "HUGE_VALF", "INFINITY", "NAN", "M_E", "M_LOG2E", "M_LOG10E", "M_LN2", "M_LN10", "M_PI", "M_PI_2", "M_PI_4", "M_1_PI", "M_2_PI", "M_2_SQRTPI", "M_SQRT2", "M_SQRT1_2" };


            OpenCLStrings.Add(Consts);
            #endregion

            #region Math functions
            StringsToMark MathFuncs = new StringsToMark();
            MathFuncs.StringsFont = new Font("Courier New", 10, FontStyle.Regular);
            MathFuncs.Description = "Math functions";
            MathFuncs.StringsColor = Color.DodgerBlue;
            MathFuncs.Strings = new List<string>() { "acos", "acosh", "acospi", "asin", "asinh", "asinpi", "atan", "atan2", "atanh", "atanpi", "atan2pi", "cbrt", "ceil", "copysign", "cos", "cosh", "cospi", "erfc", "erf", "exp", "exp2", "exp10", "expm1", "fabs", "fdim", "floor", "fma", "fmax", "fmin", "fmod", "fract", "frexp", "hypot", "ilogb", "ldexp", "lgamma", "lgamma_r", "log", "log2", "log10", "log1p", "logb", "mad", "modf", "nan", "nextafter", "pow", "pown", "powr", "remainder", "remquo", "rint", "rootn", "round", "rsqrt", "sin", "sincos", "sinh", "sinpi", "sqrt", "tan", "tanh", "tanpi", "tgamma", "trunc" };


            OpenCLStrings.Add(MathFuncs);
            #endregion

            #region Half and native functions
            StringsToMark HNMathFuncs = new StringsToMark();
            HNMathFuncs.StringsFont = new Font("Courier New", 10, FontStyle.Italic);
            HNMathFuncs.Description = "Half and native math functions";
            HNMathFuncs.StringsColor = Color.DodgerBlue;
            HNMathFuncs.Strings = new List<string>() 
            { "native_cos", "native_divide", "native_exp", "native_exp2", "native_exp10", "native_log", "native_log2", "native_log10", "native_powr", "native_recip", "native_rsqrt", "native_sin", "native_sqrt", "native_tan",
            "native_cos", "native_divide", "native_exp", "native_exp2", "native_exp10", "native_log", "native_log2", "native_log10", "native_powr", "native_recip", "native_rsqrt", "native_sin", "native_sqrt", "native_tan" };


            OpenCLStrings.Add(HNMathFuncs);
            #endregion

            #region Common functions
            StringsToMark CommonFuncs = new StringsToMark();
            CommonFuncs.StringsFont = new Font("Courier New", 10, FontStyle.Regular);
            CommonFuncs.Description = "Common functions";
            CommonFuncs.StringsColor = Color.DodgerBlue;
            CommonFuncs.Strings = new List<string>() { "clamp", "degrees", "max", "min", "mix", "radians", "step", "smoothstep", "sign" };


            OpenCLStrings.Add(CommonFuncs);
            #endregion

            #region Geometric functions
            StringsToMark GeomFuncs = new StringsToMark();
            GeomFuncs.StringsFont = new Font("Courier New", 10, FontStyle.Bold);
            GeomFuncs.Description = "Geometric functions";
            GeomFuncs.StringsColor = Color.DodgerBlue;
            GeomFuncs.Strings = new List<string>() { "cross", "dot", "distance", "length", "normalize" };

            OpenCLStrings.Add(GeomFuncs);
            #endregion

            #region Geometric functions (fast)
            StringsToMark FastGeomFuncs = new StringsToMark();
            FastGeomFuncs.StringsFont = new Font("Courier New", 10, FontStyle.Bold | FontStyle.Italic);
            FastGeomFuncs.Description = "Fast geometric functions";
            FastGeomFuncs.StringsColor = Color.DodgerBlue;
            FastGeomFuncs.Strings = new List<string>() { "fast_distance", "fast_length", "fast_normalize" };

            OpenCLStrings.Add(FastGeomFuncs);
            #endregion

            #region Image Functions
            StringsToMark ImgFuncs = new StringsToMark();
            ImgFuncs.StringsFont = new Font("Courier New", 10, FontStyle.Bold);
            ImgFuncs.Description = "Image functions and constants";
            ImgFuncs.StringsColor = Color.Orange;
            ImgFuncs.Strings = new List<string>() { "sampler_t", "image2d_t","read_imagef", "read_imagei", "read_imageui", "write_imagef", "write_imagei", "write_imageui", 
                "CLK_FILTER_NEAREST", "CLK_FILTER_LINEAR", "CLK_NORMALIZED_COORDS_FALSE", "CLK_NORMALIZED_COORDS_TRUE", "CLK_ADDRESS_CLAMP_TO_EDGE",  "CLK_ADDRESS_CLAMP", "CLK_ADDRESS_NONE"};


            OpenCLStrings.Add(ImgFuncs);
            #endregion


            #region Synchronization functions and constants
            StringsToMark FenceFuncs = new StringsToMark();
            FenceFuncs.StringsFont = new Font("Courier New", 10, FontStyle.Bold);
            FenceFuncs.Description = "Synchronization functions and constants";
            FenceFuncs.StringsColor = Color.Red;
            FenceFuncs.Strings = new List<string>() { "barrier", "mem_fence", "CLK_LOCAL_MEM_FENCE", "CLK_GLOBAL_MEM_FENCE", "read_mem_fence", "write_mem_fence" };


            OpenCLStrings.Add(FenceFuncs);
            #endregion



            #region Starts handling events
            rTB = rTBOpenCL;
            rTB.KeyUp += new KeyEventHandler(rTB_KeyUp);
            #endregion

        }

        #region Settings
        /// <summary>Help indentation?</summary>
        public bool HelpIndentation = true;

        /// <summary>Regular text color</summary>
        public Color NormalTextColor = Color.Black;
        /// <summary>Regular text font</summary>
        public Font NormalTextFont = new Font("Courier New", 10, FontStyle.Regular);

        /// <summary>Comments color</summary>
        public Color CommentColor = Color.DarkGreen;
        /// <summary>Comments font</summary>
        public Font CommentTextFont = new Font("Courier New", 10, FontStyle.Regular);
        #endregion

        #region Definition of Strings to Mark classes and list
        /// <summary>Defines a structure of strings to mark</summary>
        public class StringsToMark
        {
            /// <summary>Description of string type</summary>
            public string Description;
            /// <summary>Color to use for this string type</summary>
            public Color StringsColor;
            /// <summary>Font to be used in this list of strings</summary>
            public Font StringsFont = new Font("Courier New", 10, FontStyle.Regular);
            /// <summary>List of strings of this type</summary>
            public List<string> Strings;
        }

        /// <summary>List of string structures to mark</summary>
        public List<StringsToMark> OpenCLStrings = new List<StringsToMark>();
        #endregion

        /// <summary>KeyUp event handler</summary>
        private void rTB_KeyUp(object sender, KeyEventArgs e)
        {
            #region Indentation help
            if (HelpIndentation && e.KeyCode == Keys.Enter)
            {
                int pos = rTB.SelectionStart;
                int newLineHits = 0, spaceCount = 0;

                for (int i = pos - 1; i >= 0; i--)
                {
                    string letra = rTB.Text.Substring(i, 1);
                    if (letra == "\n")
                    {
                        newLineHits++;
                    }

                    if (newLineHits == 1)
                    {
                        if (letra == " ") spaceCount++;
                        else
                        {
                            spaceCount = 0;
                        }
                    }
                    else if (newLineHits == 2)
                    {
                        i = -1;
                    }
                }

                rTB.SelectedText = "".PadLeft(spaceCount);

                rTB.SelectionStart = pos + spaceCount;
            }
            #endregion

            #region Marks all StringsToMark
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter || e.KeyCode == Keys.Back ||
                (48 < e.KeyValue && e.KeyValue < 123))
            {
                //Starts updating RichTextBox. Disables refresh
                StartedUpdating(rTB);

                //Cursor position
                int pos = rTB.SelectionStart;

                #region Keywords marking
                rTB.SelectAll();
                rTB.SelectionColor = NormalTextColor;
                rTB.SelectionFont = NormalTextFont;

                //Mark strings
                foreach (StringsToMark StringsToMark in OpenCLStrings)
                {
                    foreach (string s in StringsToMark.Strings)
                    {
                        int b = 0;
                        while ((b = rTB.Find(s, b, rTB.Rtf.Length,
                            RichTextBoxFinds.MatchCase | RichTextBoxFinds.WholeWord)) >= 0)
                        {
                            rTB.Select(b, s.Length);
                            rTB.SelectionFont = StringsToMark.StringsFont;
                            rTB.SelectionColor = StringsToMark.StringsColor;
                            b += s.Length;
                        }
                    }
                }
                #endregion

                #region Mark comments  /**/
                int CommentPos = 0, endCommentPos = 0;
                while (endCommentPos >= 0 && CommentPos < rTB.Text.Length && (CommentPos = rTB.Find("/*", CommentPos, RichTextBoxFinds.None)) >= 0)
                {
                    endCommentPos = rTB.Find("*/", CommentPos, RichTextBoxFinds.NoHighlight);
                    if (endCommentPos > CommentPos)
                    {
                        rTB.Select(CommentPos, endCommentPos - CommentPos + 3);
                        rTB.SelectionFont = CommentTextFont;
                        rTB.SelectionColor = CommentColor;

                        CommentPos = endCommentPos + 2;
                    }
                    CommentPos += 2;

                }
                #endregion

                #region Mark comments //

                CommentPos = 0;
                while (CommentPos < rTB.Text.Length && (CommentPos = rTB.Find("//", CommentPos, RichTextBoxFinds.None)) >= 0)
                {
                    endCommentPos = CommentPos + 2;
                    string letra = "";
                    while (endCommentPos < rTB.Text.Length && letra != "\n")
                    {
                        letra = rTB.Text.Substring(endCommentPos, 1);
                        endCommentPos++;
                    }

                    if (letra == "\n")
                    {
                        rTB.Select(CommentPos, endCommentPos - CommentPos);
                        rTB.SelectionFont = CommentTextFont;
                        rTB.SelectionColor = CommentColor;

                        CommentPos = endCommentPos;
                    }
                    else
                    {
                        CommentPos = rTB.Text.Length;
                    }
                }

                #endregion

                rTB.DeselectAll();

                //Returns cursor to its original position
                rTB.SelectionStart = pos;

                StoppedUpdating(rTB);
                rTB.Refresh();
            }
            #endregion
        }

        /// <summary>Forces OpenCL RichTextBox to be updated</summary>
        public void Update()
        {
            rTB_KeyUp(null, new KeyEventArgs(Keys.Enter));
        }


        #region Use Interop to stop richtextbox blink

        private const int WM_SETREDRAW = 0x000B;
        private const int WM_USER = 0x400;
        private const int EM_GETEVENTMASK = (WM_USER + 59);
        private const int EM_SETEVENTMASK = (WM_USER + 69);

        [DllImport("user32", CharSet = CharSet.Auto)]
        private extern static IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);

        IntPtr eventMask = IntPtr.Zero;

        /// <summary>Stops updating text box while coloring text</summary>
        /// <param name="rTB">Rich text box to lock</param>
        private void StartedUpdating(RichTextBox rTB)
        {
            SendMessage(rTB.Handle, WM_SETREDRAW, 0, IntPtr.Zero);
            eventMask = SendMessage(rTB.Handle, EM_GETEVENTMASK, 0, IntPtr.Zero);
        }

        /// <summary>Restarts updating text box</summary>
        /// <param name="rTB">Rich text box to unlock</param>
        private void StoppedUpdating(RichTextBox rTB)
        {
            SendMessage(rTB.Handle, EM_SETEVENTMASK, 0, eventMask);
            SendMessage(rTB.Handle, WM_SETREDRAW, 1, IntPtr.Zero);

        }

        #endregion

    }
}
