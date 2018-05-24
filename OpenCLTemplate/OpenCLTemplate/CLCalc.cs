using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Cloo;

namespace OpenCLTemplate
{
    /// <summary>OpenCL calculations class</summary>
    public static class CLCalc
    {

        #region Double precision support
        /// <summary>String to include to enable Double Precision calculations</summary>
        private static string dblInclude = @"
                            #pragma OPENCL EXTENSION cl_khr_fp64 : enable
                            ";

        /// <summary>Gets string to include to enable Double Precision calculations</summary>
        public static string EnableDblSupport
        {
            get
            {
                return dblInclude;
            }
        }
        #endregion

        #region Inicializacao/finalizacao
        /// <summary>OpenCL accelerations</summary>
        public enum CLAccelerationType
        {
            /// <summary>Has not tested what type of acceleration is going to be used.</summary>
            Unknown,
            /// <summary>OpenCL used to accelerate calculations</summary>
            UsingCL,
            /// <summary>No OpenCL used/supported</summary>
            NotUsingCL
        };
        /// <summary>Currently used acceleration</summary>
        private static CLAccelerationType CLAccel = CLAccelerationType.Unknown;
        /// <summary>Gets acceleration type being used</summary>
        public static CLAccelerationType CLAcceleration { get { return CLAccel; } }

        /// <summary>Initialization error</summary>
        private static string CLInitErr = "";
        /// <summary>Gets initialization error description</summary>
        public static string CLInitError { get { return CLInitErr; } }

        /// <summary>Sets CLCalc status to NotUsingCL</summary>
        public static void DisableCL()
        {
            CLAccel = CLAccelerationType.NotUsingCL;
        }

        /// <summary>Initializes OpenCL and reads devices</summary>
        public static void InitCL()
        {
            InitCL(ComputeDeviceTypes.Gpu);
        }

        /// <summary>Initializes OpenCL and reads devices</summary>
        public static void InitCL(ComputeDeviceTypes DevicesToUse)
        {
            InitCL(DevicesToUse, null, null);
        }

        /// <summary>Initializes OpenCL and reads devices. Uses previously created context and command queue if supplied. In that case DevicesToUse is ignored.</summary>
        public static void InitCL(ComputeDeviceTypes DevicesToUse, ComputeContext PrevCtx, ComputeCommandQueue PrevCQ)
        {
            if (CLAcceleration != CLAccelerationType.UsingCL)
            {
                try
                {
                    if (ComputePlatform.Platforms.Count > 0) CLAccel = CLAccelerationType.UsingCL;
                    else CLAccel = CLAccelerationType.NotUsingCL;

                    //Program.Event = new List<ComputeEventBase>();

                    CLPlatforms = new List<ComputePlatform>();
                    foreach (ComputePlatform pp in ComputePlatform.Platforms) CLPlatforms.Add(pp);

                    ComputeContextPropertyList Properties = new ComputeContextPropertyList(ComputePlatform.Platforms[0]);

                    if (PrevCtx == null)
                    {
                        Program.Context = new ComputeContext(DevicesToUse, Properties, null, IntPtr.Zero);
                    }
                    else Program.Context = PrevCtx;

                    CLDevices = new List<ComputeDevice>();
                    for (int i = 0; i < Program.Context.Devices.Count; i++)
                    {
                        CLDevices.Add(Program.Context.Devices[i]);

                    }

                    Program.CommQueues = new List<ComputeCommandQueue>();
                    Program.AsyncCommQueues = new List<ComputeCommandQueue>();
                    Program.DefaultCQ = -1;

                    if (PrevCQ == null)
                    {
                        for (int i = 0; i < CLDevices.Count; i++)
                        {
                            //Comandos para os devices
                            ComputeCommandQueue CQ = new ComputeCommandQueue(Program.Context, CLDevices[i], ComputeCommandQueueFlags.None);

                            ComputeCommandQueue AsyncCQ = new ComputeCommandQueue(Program.Context, CLDevices[i], ComputeCommandQueueFlags.OutOfOrderExecution);



                            //Comando para a primeira GPU
                            if ((CLDevices[i].Type == ComputeDeviceTypes.Gpu || CLDevices[i].Type == ComputeDeviceTypes.Accelerator) && Program.DefaultCQ < 0)
                                Program.DefaultCQ = i;

                            Program.CommQueues.Add(CQ);
                            Program.AsyncCommQueues.Add(AsyncCQ);

                        }
                        //Só tem CPU
                        if (Program.DefaultCQ < 0 && Program.CommQueues.Count > 0) Program.DefaultCQ = 0;
                    }
                    else
                    {
                        Program.CommQueues.Add(PrevCQ);
                        Program.DefaultCQ = 0;
                    }
                }
                catch (Exception ex)
                {
                    CLInitErr = ex.ToString();
                    CLAccel = CLAccelerationType.NotUsingCL;
                }
            }
        }




        #endregion


        #region Hardware information

        /// <summary>List of available platforms</summary>
        public static List<ComputePlatform> CLPlatforms;

        /// <summary>List of available devices</summary>
        public static List<ComputeDevice> CLDevices;


        #endregion

        /// <summary>Program related stuff</summary>
        public static class Program
        {
            ///// <summary>Event list</summary>
            //public static List<ComputeEventBase> Event;

            /// <summary>OpenCL context using all devices</summary>
            public static ComputeContext Context;

            /// <summary>Synchronous command queues that are executed in call order</summary>
            public static List<ComputeCommandQueue> CommQueues;
            /// <summary>Asynchronous command queues</summary>
            public static List<ComputeCommandQueue> AsyncCommQueues;

            /// <summary>Default synchronous command queue set as the first GPU, for ease of use.</summary>
            public static int DefaultCQ;

            /// <summary>Compiled program</summary>
            public static ComputeProgram Prog;

            /// <summary>Ends all commands being executed</summary>
            public static void Sync()
            {
                for (int i = 0; i < CommQueues.Count; i++)
                {
                    CommQueues[i].Finish();
                    AsyncCommQueues[i].Finish();
                }
            }

            #region Compilation
            /// <summary>Compiles program contained in a single string.</summary>
            /// <param name="SourceCode">Source code to compile</param>
            public static void Compile(string SourceCode)
            {
                List<string> Logs;
                Compile(new string[] { SourceCode }, out Logs);
            }

            /// <summary>Compiles program contained in a single string. Returns build logs for each device.</summary>
            /// <param name="SourceCode">Source code to compile</param>
            /// <param name="BuildLogs">Build logs for each device</param>
            public static void Compile(string SourceCode, out List<string> BuildLogs)
            {
                Compile(new string[] { SourceCode }, out BuildLogs);
            }

            /// <summary>Compiles the program.</summary>
            /// <param name="SourceCode">Source code to compile</param>
            public static void Compile(string[] SourceCode)
            {
                List<string> Logs;
                Compile(SourceCode, out Logs);
            }

            /// <summary>Compiles the program. Returns the build logs for each device.</summary>
            /// <param name="SourceCode">Source code array to compile</param>
            /// <param name="BuildLogs">Build logs for each device</param>
            public static void Compile(string[] SourceCode, out List<string> BuildLogs)
            {
                //CLProgram Prog = OpenCLDriver.clCreateProgramWithSource(ContextoGPUs, 1, new string[] { sProgramSource }, null, ref Err);
                Prog = new ComputeProgram(Context, SourceCode);


                //Verifica se compilou em algum device
                bool funcionou = false;

                for (int i = 0; i < CLCalc.CLDevices.Count; i++)
                {
                    try
                    {
                        Prog.Build(new List<ComputeDevice>() { CLCalc.CLDevices[i] }, "", null, IntPtr.Zero);
                        funcionou = true;
                    }
                    catch
                    {
                        
                    }
                }

                //Build Information
                BuildLogs = new List<string>();
                for (int i = 0; i < CLDevices.Count; i++)
                {
                    string LogInfo = "";
                    try
                    {
                        LogInfo = Prog.GetBuildLog(CLCalc.CLDevices[i]);
                    }
                    catch
                    {
                        LogInfo = "Error retrieving build info";
                    }
                    //if (!CLCalc.CLDevices[i].CLDeviceAvailable) LogInfo = "Possible compilation failure for device " + i.ToString() + "\n" + LogInfo;
                    BuildLogs.Add(LogInfo);
                }

                //Nao compilou em nenhum, joga exception
                if (!funcionou)
                {
                    throw new Exception("Could not compile program");
                }
            }
            #endregion

            /// <summary>Generic memory object (buffer or image)</summary>
            public class MemoryObject : IDisposable
            {
                /// <summary>Size of data to be stored</summary>
                public int VarSize;

                /// <summary>Original variable length</summary>
                public int OriginalVarLength;

                /// <summary>Handle to memory object</summary>
                public ComputeMemory VarPointer;

                /// <summary>Returns the size of the stored variable</summary>
                public int Size
                {
                    get
                    {
                        return VarSize;
                    }
                }

                /// <summary>Releases variable from memory.</summary>
                public void Dispose()
                {
                    //Let Cloo handle
                    //VarPointer.Dispose();
                }

                /// <summary>Destructor</summary>
                ~MemoryObject()
                {
                    Dispose();
                }

                /// <summary>Sets this variable as an argument for a kernel</summary>
                /// <param name="ArgIndex">Index of kernel argument</param>
                /// <param name="Kernel">Kernel to receive argument</param>
                public void SetAsArgument(int ArgIndex, ComputeKernel Kernel)
                {
                    //Is this a buffer object?
                    if (this is Variable)
                    {
                        Variable v = (Variable)this;
                        if (v.CreatedFromGLBuffer && (!v.AcquiredInOpenCL))
                        {
                            throw new Exception("Attempting to use a variable created from OpenGL buffer without acquiring. Should use CLGLInteropFunctions to properly acquire and release these variables");
                        }
                    }

                    Kernel.SetMemoryArgument(ArgIndex, VarPointer);
                }
            }

            /// <summary>Variables class</summary>
            public class Variable : MemoryObject
            {

                #region Constructor. int[], float[], long[], double[], byte[]

                /// <summary>Was this buffer created from a OpenGL buffer?</summary>
                private bool _CreatedFromGLBuffer = false;

                /// <summary>Returns true if this Variable was created from an OpenGL buffer</summary>
                public bool CreatedFromGLBuffer
                {
                    get { return _CreatedFromGLBuffer; }
                }

                /// <summary>Was this buffer acquired in OpenCL?</summary>
                private bool _AcquiredInOpenCL = false;

                /// <summary>Returns true if this variable has been acquired for use in OpenCL (available for OpenCL)</summary>
                public bool AcquiredInOpenCL
                {
                    get { return _AcquiredInOpenCL; }
                    set { _AcquiredInOpenCL = value; }
                }

                /// <summary>Creates variable from OpenGL buffer</summary>
                /// <param name="GLBuffer">Valid OpenGL Buffer</param>
                /// <param name="BufferType">Type of OpenGL Buffer: typeof (int, float, double, long)</param>
                public Variable(int GLBuffer, Type BufferType)
                {
                    ComputeMemory ClooBuffer = null;
                    if (BufferType == typeof(float))
                    {
                        //Creates from OpenGL buffer
                        ClooBuffer = ComputeBuffer<float>.CreateFromGLBuffer<float>(CLCalc.Program.Context,
                        ComputeMemoryFlags.ReadWrite, GLBuffer);

                        OriginalVarLength = (int)((ComputeBuffer<float>)ClooBuffer).Count;
                    }
                    else if (BufferType == typeof(int))
                    {
                        //Creates from OpenGL buffer
                        ClooBuffer = ComputeBuffer<int>.CreateFromGLBuffer<int>(CLCalc.Program.Context,
                        ComputeMemoryFlags.ReadWrite, GLBuffer);

                        OriginalVarLength = (int)((ComputeBuffer<int>)ClooBuffer).Count;
                    }
                    else if (BufferType == typeof(long))
                    {
                        //Creates from OpenGL buffer
                        ClooBuffer = ComputeBuffer<long>.CreateFromGLBuffer<long>(CLCalc.Program.Context,
                        ComputeMemoryFlags.ReadWrite, GLBuffer);

                        OriginalVarLength = (int)((ComputeBuffer<long>)ClooBuffer).Count;
                    }
                    else if (BufferType == typeof(double))
                    {
                        //Creates from OpenGL buffer
                        ClooBuffer = ComputeBuffer<double>.CreateFromGLBuffer<double>(CLCalc.Program.Context,
                        ComputeMemoryFlags.ReadWrite, GLBuffer);

                        OriginalVarLength = (int)((ComputeBuffer<double>)ClooBuffer).Count;
                    }

                    this.VarPointer = ClooBuffer;
                    VarSize = (int)ClooBuffer.Size;
                    _CreatedFromGLBuffer = true;
                }

                /// <summary>Creates a OpenCLTemplate variable from a Cloo ComputeBuffer</summary>
                /// <param name="ClooMemoryObject">Cloo computebuffer to create from</param>
                /// <param name="Size">ClooMemoryObject.Size</param>
                /// <param name="Count">ClooMemoryObject.Count</param>
                public Variable(ComputeMemory ClooMemoryObject, int Size, int Count)
                {
                    VarSize = Size;
                    OriginalVarLength = Count;
                    this.VarPointer = ClooMemoryObject;
                }

                /// <summary>Constructor.</summary>
                /// <param name="Values">Variable whose size will be allocated in device memory.</param>
                public Variable(float[] Values)
                {
                    //Aloca memoria no contexto especificado
                    unsafe
                    {
                        OriginalVarLength = Values.Length;
                        VarSize = Values.Length * sizeof(float);

                        VarPointer = new ComputeBuffer<float>(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, Values);
                    }
                }
                /// <summary>Constructor.</summary>
                /// <param name="Values">Variable whose size will be allocated in device memory.</param>
                public Variable(int[] Values)
                {
                    //Aloca memoria no contexto especificado
                    unsafe
                    {
                        OriginalVarLength = Values.Length;
                        VarSize = Values.Length * sizeof(int);

                        VarPointer = new ComputeBuffer<int>(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, Values);
                    }
                }
                /// <summary>Constructor.</summary>
                /// <param name="Values">Variable whose size will be allocated in device memory.</param>
                public Variable(long[] Values)
                {
                    //Aloca memoria no contexto especificado
                    unsafe
                    {
                        OriginalVarLength = Values.Length;
                        VarSize = Values.Length * sizeof(long);

                        VarPointer = new ComputeBuffer<long>(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, Values);

                    }
                }
                /// <summary>Constructor.</summary>
                /// <param name="Values">Variable whose size will be allocated in device memory.</param>
                public Variable(double[] Values)
                {
                    //Aloca memoria no contexto especificado
                    unsafe
                    {
                        OriginalVarLength = Values.Length;
                        VarSize = Values.Length * sizeof(double);

                        VarPointer = new ComputeBuffer<double>(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, Values);

                    }
                }

                /// <summary>Constructor.</summary>
                /// <param name="Values">Variable whose size will be allocated in device memory.</param>
                public Variable(char[] Values)
                {
                    //Aloca memoria no contexto especificado
                    unsafe
                    {
                        OriginalVarLength = Values.Length;
                        VarSize = Values.Length * sizeof(char);

                        VarPointer = new ComputeBuffer<char>(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, Values);

                    }
                }

                /// <summary>Constructor.</summary>
                /// <param name="Values">Variable whose size will be allocated in device memory.</param>
                public Variable(byte[] Values)
                {
                    //Aloca memoria no contexto especificado
                    unsafe
                    {
                        OriginalVarLength = Values.Length;
                        VarSize = Values.Length * sizeof(byte);

                        VarPointer = new ComputeBuffer<byte>(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, Values);

                    }
                }

                #endregion

                #region Write to Device memory. int[], float[], long[], double[], byte[]
                //private unsafe void WriteToDevice(void* p, ComputeCommandQueue CQ, bool BlockingWrite, ComputeEvent Event, CLEvent[] Event_Wait_List)
                //{
                //    uint n = 0;
                //    if (Event_Wait_List != null) n = (uint)Event_Wait_List.Length;

                //    CQ.w

                //    Err = OpenCLDriver.clEnqueueWriteBuffer(CQ, VarPointer, BlockingWrite, new GASS.Types.SizeT(0),
                //        new GASS.Types.SizeT(Size), new IntPtr(p), n, Event_Wait_List, ref Event);

                //    LastError = Err;
                //}

                /// <summary>Writes variable to device</summary>
                /// <param name="Values">Values to write to device</param>
                /// <param name="CQ">Command queue to use</param>
                /// <param name="BlockingWrite">TRUE to return only after completed writing.</param>
                /// <param name="events">OpenCL Event associated to this operation</param>
                public void WriteToDevice(float[] Values, ComputeCommandQueue CQ, bool BlockingWrite, ICollection<ComputeEventBase> events)
                {
                    if (Values.Length != OriginalVarLength) throw new Exception("Values length should be the same as allocated length");
                    if (CreatedFromGLBuffer && (!AcquiredInOpenCL)) throw new Exception("Attempting to use a variable created from OpenGL buffer without acquiring. Should use CLGLInteropFunctions to properly acquire and release these variables");
                    unsafe
                    {
                        fixed (void* ponteiro = Values)
                        {
                            CQ.Write<float>((ComputeBuffer<float>)VarPointer, BlockingWrite, 0, Values.Length, (IntPtr)ponteiro, events);
                        }
                    }
                }

                /// <summary>Writes variable to device</summary>
                /// <param name="Values">Values to write to device</param>
                public void WriteToDevice(float[] Values)
                {
                    //CLEvent Event = new CLEvent();
                    WriteToDevice(Values, CommQueues[DefaultCQ], true, null);
                    //OpenCLDriver.clReleaseEvent(Event);
                }

                /// <summary>Writes variable to device</summary>
                /// <param name="Values">Values to write to device</param>
                /// <param name="CQ">Command queue to use</param>
                /// <param name="BlockingWrite">TRUE to return only after completed writing.</param>
                /// <param name="events">OpenCL Event associated to this operation</param>
                public void WriteToDevice(int[] Values, ComputeCommandQueue CQ, bool BlockingWrite, ICollection<ComputeEventBase> events)
                {
                    if (Values.Length != OriginalVarLength) throw new Exception("Values length should be the same as allocated length");
                    unsafe
                    {
                        fixed (void* ponteiro = Values)
                        {
                            CQ.Write<int>((ComputeBuffer<int>)VarPointer, BlockingWrite, 0, Values.Length, (IntPtr)ponteiro, events);
                        }
                    }
                }

                /// <summary>Writes variable to device</summary>
                /// <param name="Values">Values to write to device</param>
                public void WriteToDevice(int[] Values)
                {
                    //CLEvent Event = new CLEvent();
                    WriteToDevice(Values, CommQueues[DefaultCQ], true, null);
                    //OpenCLDriver.clReleaseEvent(Event);
                }

                /// <summary>Writes variable to device</summary>
                /// <param name="Values">Values to write to device</param>
                /// <param name="CQ">Command queue to use</param>
                /// <param name="BlockingWrite">TRUE to return only after completed writing.</param>
                /// <param name="events">OpenCL Event associated to this operation</param>
                public void WriteToDevice(long[] Values, ComputeCommandQueue CQ, bool BlockingWrite, ICollection<ComputeEventBase> events)
                {
                    if (Values.Length != OriginalVarLength) throw new Exception("Values length should be the same as allocated length");
                    if (CreatedFromGLBuffer && (!AcquiredInOpenCL)) throw new Exception("Attempting to use a variable created from OpenGL buffer without acquiring. Should use CLGLInteropFunctions to properly acquire and release these variables");
                    unsafe
                    {
                        fixed (void* ponteiro = Values)
                        {
                            CQ.Write<long>((ComputeBuffer<long>)VarPointer, BlockingWrite, 0, Values.Length, (IntPtr)ponteiro, events);
                        }
                    }
                }

                /// <summary>Writes variable to device</summary>
                /// <param name="Values">Values to write to device</param>
                public void WriteToDevice(long[] Values)
                {
                    //CLEvent Event = new CLEvent();
                    WriteToDevice(Values, CommQueues[DefaultCQ], true, null);
                    //OpenCLDriver.clReleaseEvent(Event);
                }

                /// <summary>Writes variable to device</summary>
                /// <param name="Values">Values to write to device</param>
                /// <param name="CQ">Command queue to use</param>
                /// <param name="BlockingWrite">TRUE to return only after completed writing.</param>
                /// <param name="events">OpenCL Event associated to this operation</param>
                public void WriteToDevice(double[] Values, ComputeCommandQueue CQ, bool BlockingWrite, ICollection<ComputeEventBase> events)
                {
                    if (Values.Length != OriginalVarLength) throw new Exception("Values length should be the same as allocated length");
                    if (CreatedFromGLBuffer && (!AcquiredInOpenCL)) throw new Exception("Attempting to use a variable created from OpenGL buffer without acquiring. Should use CLGLInteropFunctions to properly acquire and release these variables");
                    unsafe
                    {
                        fixed (void* ponteiro = Values)
                        {
                            CQ.Write<double>((ComputeBuffer<double>)VarPointer, BlockingWrite, 0, Values.Length, (IntPtr)ponteiro, events);
                        }
                    }
                }

                /// <summary>Writes variable to device</summary>
                /// <param name="Values">Values to write to device</param>
                public void WriteToDevice(double[] Values)
                {
                    //CLEvent Event = new CLEvent();
                    WriteToDevice(Values, CommQueues[DefaultCQ], true, null);
                    //OpenCLDriver.clReleaseEvent(Event);
                }

                /// <summary>Writes variable to device</summary>
                /// <param name="Values">Values to write to device</param>
                /// <param name="CQ">Command queue to use</param>
                /// <param name="BlockingWrite">TRUE to return only after completed writing.</param>
                /// <param name="events">OpenCL Event associated to this operation</param>
                 
                public void WriteToDevice(char[] Values, ComputeCommandQueue CQ, bool BlockingWrite, ICollection<ComputeEventBase> events)
                {
                    if (Values.Length != OriginalVarLength) throw new Exception("Values length should be the same as allocated length");
                    if (CreatedFromGLBuffer && (!AcquiredInOpenCL)) throw new Exception("Attempting to use a variable created from OpenGL buffer without acquiring. Should use CLGLInteropFunctions to properly acquire and release these variables");
                    unsafe
                    {
                        fixed (void* ponteiro = Values)
                        {
                            CQ.Write<char>((ComputeBuffer<char>)VarPointer, BlockingWrite, 0, Values.Length, (IntPtr)ponteiro, events);
                        }
                    }
                }

                /// <summary>Writes variable to device</summary>
                /// <param name="Values">Values to write to device</param>
                public void WriteToDevice(char[] Values)
                {
                    //CLEvent Event = new CLEvent();
                    WriteToDevice(Values, CommQueues[DefaultCQ], true, null);
                    //OpenCLDriver.clReleaseEvent(Event);
                }

                /// <summary>Writes variable to device</summary>
                /// <param name="Values">Values to write to device</param>
                /// <param name="CQ">Command queue to use</param>
                /// <param name="BlockingWrite">TRUE to return only after completed writing.</param>
                /// <param name="events">OpenCL Event associated to this operation</param>
                 
                public void WriteToDevice(byte[] Values, ComputeCommandQueue CQ, bool BlockingWrite, ICollection<ComputeEventBase> events)
                {
                    if (Values.Length != OriginalVarLength) throw new Exception("Values length should be the same as allocated length");
                    if (CreatedFromGLBuffer && (!AcquiredInOpenCL)) throw new Exception("Attempting to use a variable created from OpenGL buffer without acquiring. Should use CLGLInteropFunctions to properly acquire and release these variables");
                    unsafe
                    {
                        fixed (void* ponteiro = Values)
                        {
                            CQ.Write<byte>((ComputeBuffer<byte>)VarPointer, BlockingWrite, 0, Values.Length, (IntPtr)ponteiro, events);
                        }
                    }
                }

                /// <summary>Writes variable to device</summary>
                /// <param name="Values">Values to write to device</param>
                public void WriteToDevice(byte[] Values)
                {
                    //CLEvent Event = new CLEvent();
                    WriteToDevice(Values, CommQueues[DefaultCQ], true, null);
                    //OpenCLDriver.clReleaseEvent(Event);
                }

                #endregion

                #region Read from Device memory. int[], float[], long[], double[], byte[]

                //private unsafe void ReadFromDeviceTo(void* p, CLCommandQueue CQ, CLBool BlockingRead, CLEvent Event, CLEvent[] Event_Wait_List)
                //{
                //    uint n = 0;
                //    if (Event_Wait_List != null) n = (uint)Event_Wait_List.Length;

                //    Err = OpenCLDriver.clEnqueueReadBuffer(CQ, VarPointer, BlockingRead, new GASS.Types.SizeT(0),
                //        new GASS.Types.SizeT(Size), new IntPtr(p), n, Event_Wait_List, ref Event);

                //    LastError = Err;
                //}

                /// <summary>Reads variable from device.</summary>
                /// <param name="Values">Values to store data coming from device</param>
                /// <param name="CQ">Command queue to use</param>
                /// <param name="BlockingRead">TRUE to return only after completed reading.</param>
                /// <param name="events">OpenCL Event associated with this operation</param>
                 
                public void ReadFromDeviceTo(float[] Values, ComputeCommandQueue CQ, bool BlockingRead, ICollection<ComputeEventBase> events)
                {
                    if (Values.Length != OriginalVarLength) throw new Exception("Values length should be the same as allocated length");
                    if (CreatedFromGLBuffer && (!AcquiredInOpenCL)) throw new Exception("Attempting to use a variable created from OpenGL buffer without acquiring. Should use CLGLInteropFunctions to properly acquire and release these variables");
                    unsafe
                    {
                        fixed (void* ponteiro = Values)
                        {
                            CQ.Read<float>((ComputeBuffer<float>)VarPointer, BlockingRead, 0, Values.Length, (IntPtr)ponteiro, events);
                        }
                    }
                }

                /// <summary>Reads variable from device. Does not return until data has been copied.</summary>
                /// <param name="Values">Values to store data coming from device</param>
                public void ReadFromDeviceTo(float[] Values)
                {
                    //CLEvent Event = new CLEvent();
                    ReadFromDeviceTo(Values, CommQueues[DefaultCQ], true, null);

                    //OpenCLDriver.clReleaseEvent(Event);
                }

                /// <summary>Reads variable from device.</summary>
                /// <param name="Values">Values to store data coming from device</param>
                /// <param name="CQ">Command queue to use</param>
                /// <param name="BlockingRead">TRUE to return only after completed reading.</param>
                /// <param name="events">OpenCL Event associated with this operation</param>
                public void ReadFromDeviceTo(int[] Values, ComputeCommandQueue CQ, bool BlockingRead, ICollection<ComputeEventBase> events)
                {
                    if (Values.Length != OriginalVarLength) throw new Exception("Values length should be the same as allocated length");
                    if (CreatedFromGLBuffer && (!AcquiredInOpenCL)) throw new Exception("Attempting to use a variable created from OpenGL buffer without acquiring. Should use CLGLInteropFunctions to properly acquire and release these variables");
                    unsafe
                    {
                        fixed (void* ponteiro = Values)
                        {
                            CQ.Read<int>((ComputeBuffer<int>)VarPointer, BlockingRead, 0, Values.Length, (IntPtr)ponteiro, events);
                        }
                    }
                }

                /// <summary>Reads variable from device. Does not return until data has been copied.</summary>
                /// <param name="Values">Values to store data coming from device</param>
                public void ReadFromDeviceTo(int[] Values)
                {
                    //CLEvent Event = new CLEvent();
                    ReadFromDeviceTo(Values, CommQueues[DefaultCQ], true, null);

                    //OpenCLDriver.clReleaseEvent(Event);
                }

                /// <summary>Reads variable from device.</summary>
                /// <param name="Values">Values to store data coming from device</param>
                /// <param name="CQ">Command queue to use</param>
                /// <param name="BlockingRead">TRUE to return only after completed reading.</param>
                /// <param name="events">OpenCL Event associated with this operation</param>
                 
                public void ReadFromDeviceTo(long[] Values, ComputeCommandQueue CQ, bool BlockingRead, ICollection<ComputeEventBase> events)
                {
                    if (Values.Length != OriginalVarLength) throw new Exception("Values length should be the same as allocated length");
                    if (CreatedFromGLBuffer && (!AcquiredInOpenCL)) throw new Exception("Attempting to use a variable created from OpenGL buffer without acquiring. Should use CLGLInteropFunctions to properly acquire and release these variables");
                    unsafe
                    {
                        fixed (void* ponteiro = Values)
                        {
                            CQ.Read<long>((ComputeBuffer<long>)VarPointer, BlockingRead, 0, Values.Length, (IntPtr)ponteiro, events);
                        }
                    }
                }

                /// <summary>Reads variable from device. Does not return until data has been copied.</summary>
                /// <param name="Values">Values to store data coming from device</param>
                public void ReadFromDeviceTo(long[] Values)
                {
                    //CLEvent Event = new CLEvent();
                    ReadFromDeviceTo(Values, CommQueues[DefaultCQ], true, null);

                    //OpenCLDriver.clReleaseEvent(Event);
                }

                /// <summary>Reads variable from device.</summary>
                /// <param name="Values">Values to store data coming from device</param>
                /// <param name="CQ">Command queue to use</param>
                /// <param name="BlockingRead">TRUE to return only after completed reading.</param>
                /// <param name="events">OpenCL Event associated with this operation</param>
                 
                public void ReadFromDeviceTo(double[] Values, ComputeCommandQueue CQ, bool BlockingRead, ICollection<ComputeEventBase> events)
                {
                    if (Values.Length != OriginalVarLength) throw new Exception("Values length should be the same as allocated length");
                    if (CreatedFromGLBuffer && (!AcquiredInOpenCL)) throw new Exception("Attempting to use a variable created from OpenGL buffer without acquiring. Should use CLGLInteropFunctions to properly acquire and release these variables");
                    unsafe
                    {
                        fixed (void* ponteiro = Values)
                        {
                            CQ.Read<double>((ComputeBuffer<double>)VarPointer, BlockingRead, 0, Values.Length, (IntPtr)ponteiro, events);
                        }
                    }
                }

                /// <summary>Reads variable from device. Does not return until data has been copied.</summary>
                /// <param name="Values">Values to store data coming from device</param>
                public void ReadFromDeviceTo(double[] Values)
                {
                    //CLEvent Event = new CLEvent();
                    ReadFromDeviceTo(Values, CommQueues[DefaultCQ], true, null);

                    //OpenCLDriver.clReleaseEvent(Event);
                }

                /// <summary>Reads variable from device.</summary>
                /// <param name="Values">Values to store data coming from device</param>
                /// <param name="CQ">Command queue to use</param>
                /// <param name="BlockingRead">TRUE to return only after completed reading.</param>
                /// <param name="events">OpenCL Event associated with this operation</param>
                public void ReadFromDeviceTo(char[] Values, ComputeCommandQueue CQ, bool BlockingRead, ICollection<ComputeEventBase> events)
                {
                    if (Values.Length != OriginalVarLength) throw new Exception("Values length should be the same as allocated length");
                    if (CreatedFromGLBuffer && (!AcquiredInOpenCL)) throw new Exception("Attempting to use a variable created from OpenGL buffer without acquiring. Should use CLGLInteropFunctions to properly acquire and release these variables");
                    unsafe
                    {
                        fixed (void* ponteiro = Values)
                        {
                            CQ.Read<char>((ComputeBuffer<char>)VarPointer, BlockingRead, 0, Values.Length, (IntPtr)ponteiro, events);
                        }
                    }
                }

                /// <summary>Reads variable from device. Does not return until data has been copied.</summary>
                /// <param name="Values">Values to store data coming from device</param>
                public void ReadFromDeviceTo(char[] Values)
                {
                    //CLEvent Event = new CLEvent();
                    ReadFromDeviceTo(Values, CommQueues[DefaultCQ], true, null);


                    //OpenCLDriver.clReleaseEvent(Event);
                }

                /// <summary>Reads variable from device.</summary>
                /// <param name="Values">Values to store data coming from device</param>
                /// <param name="CQ">Command queue to use</param>
                /// <param name="BlockingRead">TRUE to return only after completed reading.</param>
                /// <param name="events">OpenCL Event associated with this operation</param>
                public void ReadFromDeviceTo(byte[] Values, ComputeCommandQueue CQ, bool BlockingRead, ICollection<ComputeEventBase> events)
                {
                    if (Values.Length != OriginalVarLength) throw new Exception("Values length should be the same as allocated length");
                    if (CreatedFromGLBuffer && (!AcquiredInOpenCL)) throw new Exception("Attempting to use a variable created from OpenGL buffer without acquiring. Should use CLGLInteropFunctions to properly acquire and release these variables");
                    unsafe
                    {
                        fixed (void* ponteiro = Values)
                        {
                            CQ.Read<byte>((ComputeBuffer<byte>)VarPointer, BlockingRead, 0, Values.Length, (IntPtr)ponteiro, events);
                        }
                    }
                }

                /// <summary>Reads variable from device. Does not return until data has been copied.</summary>
                /// <param name="Values">Values to store data coming from device</param>
                public void ReadFromDeviceTo(byte[] Values)
                {
                    //CLEvent Event = new CLEvent();
                    ReadFromDeviceTo(Values, CommQueues[DefaultCQ], true, null);

                    //OpenCLDriver.clReleaseEvent(Event);
                }

                #endregion

            }

            /// <summary>Image2D class. Uses channel type RGBA.</summary>
            public class Image2D : MemoryObject
            {
                /// <summary>Image width</summary>
                private int width;
                /// <summary>Image height</summary>
                private int height;

                /// <summary>Gets image2D width</summary>
                public int Width
                {
                    get { return width; }
                }

                /// <summary>Gets image2D height</summary>
                public int Height
                {
                    get { return height; }
                }

                #region Constructor. float[], int[], byte[]

                /// <summary>Unsafe allocation of memory</summary>
                /// <param name="p">Pointer to data</param>
                /// <param name="DataType">Data type: float, uint8 (byte), int32, etc.</param>
                private unsafe void CLMalloc(void* p, ComputeImageChannelType DataType)
                {
                    ComputeImageFormat format = new ComputeImageFormat(ComputeImageChannelOrder.Rgba, DataType);

                    if (OriginalVarLength != 4 * width * height) throw new Exception("Vector length should be 4*width*height");

                    VarPointer = new ComputeImage2D(Program.Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, format, width, height, 0, new IntPtr(p));

                }

                /// <summary>Constructor.</summary>
                /// <param name="Values">Variable whose size will be allocated in device memory.</param>
                /// <param name="Width">Image width.</param>
                /// <param name="Height">Image height.</param>
                public Image2D(float[] Values, int Width, int Height)
                {
                    //Aloca memoria no contexto especificado
                    unsafe
                    {
                        ComputeImageChannelType DataType = ComputeImageChannelType.Float;
                        VarSize = Values.Length * sizeof(float);

                        width = Width;
                        height = Height;
                        OriginalVarLength = Values.Length;

                        fixed (void* ponteiro = Values)
                        {
                            CLMalloc(ponteiro, DataType);
                        }
                    }
                }

                /// <summary>Constructor.</summary>
                /// <param name="Values">Variable whose size will be allocated in device memory.</param>
                /// <param name="Width">Image width.</param>
                /// <param name="Height">Image height.</param>
                public Image2D(int[] Values, int Width, int Height)
                {
                    //Aloca memoria no contexto especificado
                    unsafe
                    {
                        ComputeImageChannelType DataType = ComputeImageChannelType.SignedInt32;
                        VarSize = Values.Length * sizeof(int);

                        width = Width;
                        height = Height;
                        OriginalVarLength = Values.Length;

                        fixed (void* ponteiro = Values)
                        {
                            CLMalloc(ponteiro, DataType);
                        }
                    }
                }

                /// <summary>Constructor.</summary>
                /// <param name="Values">Variable whose size will be allocated in device memory.</param>
                /// <param name="Width">Image width.</param>
                /// <param name="Height">Image height.</param>
                public Image2D(byte[] Values, int Width, int Height)
                {
                    //Aloca memoria no contexto especificado
                    unsafe
                    {
                        ComputeImageChannelType DataType = ComputeImageChannelType.UnsignedInt8;
                        VarSize = Values.Length * sizeof(byte);

                        width = Width;
                        height = Height;
                        OriginalVarLength = Values.Length;

                        fixed (void* ponteiro = Values)
                        {
                            CLMalloc(ponteiro, DataType);
                        }
                    }
                }

                /// <summary>Constructor. Remember, Bitmap uses the BGRA byte order.</summary>
                /// <param name="bmp">Bitmap to create OpenCL image from.</param>
                public Image2D(System.Drawing.Bitmap bmp)
                {
                    System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);

                    System.Drawing.Imaging.BitmapData bitmapdata = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    width = bmp.Width; height = bmp.Height;
                    OriginalVarLength = 4 * width * height;
                    VarSize = 4 * OriginalVarLength * sizeof(byte);

                    unsafe
                    {
                        CLMalloc((void*)bitmapdata.Scan0, ComputeImageChannelType.UnsignedInt8);
                    }


                    bmp.UnlockBits(bitmapdata);
                }

                #endregion

                #region Write to Device memory. float[], int[], byte[], Bitmap


                private unsafe void WriteToDevice(void* p, ComputeCommandQueue CQ, bool BlockingWrite, ICollection<ComputeEventBase> events)
                {
                    CQ.Write((ComputeImage)VarPointer, BlockingWrite, new SysIntX3(0, 0, 0), new SysIntX3(width, height, 1), 0, 0, new IntPtr(p), events);
                }

                /// <summary>Writes variable to device</summary>
                /// <param name="Values">Values to write to device</param>
                /// <param name="CQ">Command queue to use</param>
                /// <param name="BlockingWrite">TRUE to return only after completed writing.</param>
                /// <param name="events">OpenCL Event associated to this operation</param>
                public void WriteToDevice(float[] Values, ComputeCommandQueue CQ, bool BlockingWrite, ICollection<ComputeEventBase> events)
                {
                    if (Values.Length != OriginalVarLength) throw new Exception("Values length should be the same as allocated length");
                    unsafe
                    {
                        fixed (void* ponteiro = Values)
                        {
                            WriteToDevice(ponteiro, CQ, BlockingWrite, events);
                        }
                    }
                }

                /// <summary>Writes variable to device</summary>
                /// <param name="Values">Values to write to device</param>
                public void WriteToDevice(float[] Values)
                {
                    //CLEvent Event = new CLEvent();
                    WriteToDevice(Values, CommQueues[DefaultCQ], true, null);
                    //OpenCLDriver.clReleaseEvent(Event);
                }

                /// <summary>Writes variable to device</summary>
                /// <param name="Values">Values to write to device</param>
                /// <param name="CQ">Command queue to use</param>
                /// <param name="BlockingWrite">TRUE to return only after completed writing.</param>
                /// <param name="events">OpenCL Event associated to this operation</param>
                 
                public void WriteToDevice(int[] Values, ComputeCommandQueue CQ, bool BlockingWrite, ICollection<ComputeEventBase> events)
                {
                    if (Values.Length != OriginalVarLength) throw new Exception("Values length should be the same as allocated length");
                    unsafe
                    {
                        fixed (void* ponteiro = Values)
                        {
                            WriteToDevice(ponteiro, CQ, BlockingWrite, events);
                        }
                    }
                }

                /// <summary>Writes variable to device</summary>
                /// <param name="Values">Values to write to device</param>
                public void WriteToDevice(int[] Values)
                {
                    //CLEvent Event = new CLEvent();
                    WriteToDevice(Values, CommQueues[DefaultCQ], true, null);
                    //OpenCLDriver.clReleaseEvent(Event);
                }

                /// <summary>Writes variable to device</summary>
                /// <param name="Values">Values to write to device</param>
                /// <param name="CQ">Command queue to use</param>
                /// <param name="BlockingWrite">TRUE to return only after completed writing.</param>
                /// <param name="events">OpenCL Event associated to this operation</param>
                 
                public void WriteToDevice(byte[] Values, ComputeCommandQueue CQ, bool BlockingWrite, ICollection<ComputeEventBase> events)
                {
                    if (Values.Length != OriginalVarLength) throw new Exception("Values length should be the same as allocated length");
                    unsafe
                    {
                        fixed (void* ponteiro = Values)
                        {
                            WriteToDevice(ponteiro, CQ, BlockingWrite, events);
                        }
                    }
                }

                /// <summary>Writes variable to device</summary>
                /// <param name="Values">Values to write to device</param>
                public void WriteToDevice(byte[] Values)
                {
                    //CLEvent Event = new CLEvent();
                    WriteToDevice(Values, CommQueues[DefaultCQ], true, null);
                    //OpenCLDriver.clReleaseEvent(Event);
                }

                /// <summary>Writes bitmap to device memory. Remember, Bitmap uses the BGRA byte order.</summary>
                /// <param name="bmp">Bitmap to write</param>
                /// <param name="CQ">Command queue to use</param>
                /// <param name="BlockingWrite">TRUE to return only after completed reading.</param>
                /// <param name="events">OpenCL Event associated with this operation</param>
                public void WriteBitmap(System.Drawing.Bitmap bmp, ComputeCommandQueue CQ, bool BlockingWrite, ICollection<ComputeEventBase> events)
                {
                    if (bmp.Width != this.width || bmp.Height != this.height) throw new Exception("Bitmap dimensions not compatible");

                    System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);

                    System.Drawing.Imaging.BitmapData bitmapdata = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
                            System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    unsafe
                    {
                        WriteToDevice((void*)bitmapdata.Scan0, CQ, BlockingWrite, events);
                    }


                    bmp.UnlockBits(bitmapdata);
                }

                /// <summary>Writes bitmap to device memory</summary>
                public void WriteBitmap(System.Drawing.Bitmap bmp)
                {
                    //CLEvent Event = new CLEvent();
                    WriteBitmap(bmp, CommQueues[DefaultCQ],true, null);

                    //OpenCLDriver.clReleaseEvent(Event);
                }

                #endregion

                #region Read from Device memory. float[], int[], byte[], Bitmap

                private unsafe void ReadFromDeviceTo(void* p, ComputeCommandQueue CQ, bool BlockingRead, ICollection<ComputeEventBase> events)
                {
                    CQ.Read((ComputeImage)VarPointer, BlockingRead, new SysIntX3(0, 0, 0), new SysIntX3(width, height, 1), 0, 0, new IntPtr(p), events);
                }

                /// <summary>Reads variable from device.</summary>
                /// <param name="Values">Values to store data coming from device</param>
                /// <param name="CQ">Command queue to use</param>
                /// <param name="BlockingRead">TRUE to return only after completed reading.</param>
                /// <param name="events">OpenCL Event associated with this operation</param>
                 
                public void ReadFromDeviceTo(float[] Values, ComputeCommandQueue CQ, bool BlockingRead, ICollection<ComputeEventBase> events)
                {
                    if (Values.Length != OriginalVarLength) throw new Exception("Values length should be the same as allocated length");

                    unsafe
                    {
                        fixed (void* ponteiro = Values)
                        {
                            ReadFromDeviceTo(ponteiro, CQ, BlockingRead, events);
                        }
                    }
                }

                /// <summary>Reads variable from device. Does not return until data has been copied.</summary>
                /// <param name="Values">Values to store data coming from device</param>
                public void ReadFromDeviceTo(float[] Values)
                {
                    //CLEvent Event = new CLEvent();
                    ReadFromDeviceTo(Values, CommQueues[DefaultCQ], true, null);

                    //OpenCLDriver.clReleaseEvent(Event);
                }

                /// <summary>Reads variable from device.</summary>
                /// <param name="Values">Values to store data coming from device</param>
                /// <param name="CQ">Command queue to use</param>
                /// <param name="BlockingRead">TRUE to return only after completed reading.</param>
                /// <param name="events">OpenCL Event associated with this operation</param>
                public void ReadFromDeviceTo(int[] Values, ComputeCommandQueue CQ, bool BlockingRead, ICollection<ComputeEventBase> events)
                {
                    if (Values.Length != OriginalVarLength) throw new Exception("Values length should be the same as allocated length");

                    unsafe
                    {
                        fixed (void* ponteiro = Values)
                        {
                            ReadFromDeviceTo(ponteiro, CQ, BlockingRead, events);
                        }
                    }
                }

                /// <summary>Reads variable from device. Does not return until data has been copied.</summary>
                /// <param name="Values">Values to store data coming from device</param>
                public void ReadFromDeviceTo(int[] Values)
                {
                    //CLEvent Event = new CLEvent();
                    ReadFromDeviceTo(Values, CommQueues[DefaultCQ], true, null);

                    //OpenCLDriver.clReleaseEvent(Event);
                }

                /// <summary>Reads variable from device.</summary>
                /// <param name="Values">Values to store data coming from device</param>
                /// <param name="CQ">Command queue to use</param>
                /// <param name="BlockingRead">TRUE to return only after completed reading.</param>
                /// <param name="events">OpenCL Event associated with this operation</param>
                 
                public void ReadFromDeviceTo(byte[] Values, ComputeCommandQueue CQ, bool BlockingRead, ICollection<ComputeEventBase> events)
                {
                    if (Values.Length != OriginalVarLength) throw new Exception("Values length should be the same as allocated length");

                    unsafe
                    {
                        fixed (void* ponteiro = Values)
                        {
                            ReadFromDeviceTo(ponteiro, CQ, BlockingRead, events);
                        }
                    }
                }

                /// <summary>Reads variable from device. Does not return until data has been copied.</summary>
                /// <param name="Values">Values to store data coming from device</param>
                public void ReadFromDeviceTo(byte[] Values)
                {
                    //CLEvent Event = new CLEvent();
                    ReadFromDeviceTo(Values, CommQueues[DefaultCQ], true, null);

                    //OpenCLDriver.clReleaseEvent(Event);
                }

                /// <summary>Reads contents of device memory as bytes and writes bitmap. Remember, Bitmap uses the BGRA byte order.</summary>
                /// <param name="CQ">Command queue to use</param>
                /// <param name="BlockingRead">TRUE to return only after completed reading.</param>
                /// <param name="events">OpenCL Event associated with this operation</param>
                 
                public System.Drawing.Bitmap ReadBitmap(ComputeCommandQueue CQ, bool BlockingRead, ICollection<ComputeEventBase> events)
                {
                    System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height);
                    System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);

                    System.Drawing.Imaging.BitmapData bitmapdata = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly,
                            System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    unsafe
                    {
                        ReadFromDeviceTo((void*)bitmapdata.Scan0, CQ, BlockingRead, events);
                    }


                    bmp.UnlockBits(bitmapdata);

                    return bmp;
                }

                /// <summary>Reads contents of device memory as bytes and writes bitmap</summary>
                public System.Drawing.Bitmap ReadBitmap()
                {
                    //CLEvent Event = new CLEvent();
                    return ReadBitmap(CommQueues[DefaultCQ], true, null);

                    //OpenCLDriver.clReleaseEvent(Event);
                }

                #endregion

            }


            /// <summary>Kernels class</summary>
            public class Kernel : IDisposable
            {
                /// <summary>Local kernel storage</summary>
                private ComputeKernel kernel;

                ///// <summary>Number of arguments</summary>
                //private int nArgs = 0;
                ///// <summary>Gets how many arguments this kernel has</summary>
                //public int NumberOfArguments { get { return nArgs; } }


                /// <summary>Creates a new Kernel</summary>
                /// <param name="KernelName"></param>
                public Kernel(string KernelName)
                {
                    kernel = Prog.CreateKernel(KernelName);
                }

                /// <summary>"Remember" variables</summary>
                private CLCalc.Program.MemoryObject[] Vars;

                /// <summary>Sets kernel arguments</summary>
                /// <param name="Variables">Variables to be set as arguments</param>
                private void SetArguments(CLCalc.Program.MemoryObject[] Variables)
                {
                    //if (Variables.Length != nArgs) throw new Exception("Wrong number of arguments");
                    if (Vars != Variables)
                    {
                        Vars = Variables;
                        for (int i = 0; i < Variables.Length; i++)
                        {
                            Variables[i].SetAsArgument(i, kernel);
                        }
                    }
                }

                /// <summary>Execute this kernel</summary>
                /// <param name="CQ">Command queue to use</param>
                /// <param name="Arguments">Arguments of the kernel function</param>
                /// <param name="GlobalWorkSize">Array of maximum index arrays. Total work-items = product(max[i],i+0..n-1), n=max.Length</param>
                /// <param name="events">Event of this command</param>
                public void Execute(ComputeCommandQueue CQ, CLCalc.Program.MemoryObject[] Arguments, int[] GlobalWorkSize, ICollection<ComputeEventBase> events)
                {
                    SetArguments(Arguments);

                    long[] globWSize=new long[GlobalWorkSize.Length];
                    for (int i=0;i<globWSize.Length;i++) globWSize[i]=GlobalWorkSize[i];

                    CQ.Execute(kernel, null, globWSize, null, events);

                }

                /// <summary>Execute this kernel</summary>
                /// <param name="CQ">Command queue to use</param>
                /// <param name="Arguments">Arguments of the kernel function</param>
                /// <param name="GlobalWorkSize">Array of maximum index arrays. Total work-items = product(max[i],i+0..n-1), n=max.Length</param>
                /// <param name="LocalWorkSize">Local work sizes</param>
                /// <param name="events">Event of this command</param>
                public void Execute(ComputeCommandQueue CQ, CLCalc.Program.MemoryObject[] Arguments, int[] GlobalWorkSize, int[] LocalWorkSize, ICollection<ComputeEventBase> events)
                {
                    SetArguments(Arguments);
                    if (LocalWorkSize != null && GlobalWorkSize.Length != LocalWorkSize.Length) throw new Exception("Global and local work size must have same dimension");


                    long[] globWSize = new long[GlobalWorkSize.Length];
                    for (int i = 0; i < globWSize.Length; i++) globWSize[i] = GlobalWorkSize[i];
                    long[] locWSize = null;

                    if (LocalWorkSize != null)
                    {
                        locWSize = new long[LocalWorkSize.Length];
                        for (int i = 0; i < locWSize.Length; i++) locWSize[i] = LocalWorkSize[i];
                    }

                    CQ.Execute(kernel, null, globWSize, locWSize, events);

                }

                /// <summary>Execute this kernel</summary>
                /// <param name="GlobalWorkSize">Array of maximum index arrays. Total work-items = product(max[i],i+0..n-1), n=max.Length</param>
                /// <param name="Arguments">Arguments of the kernel function</param>
                public void Execute(CLCalc.Program.MemoryObject[] Arguments, int[] GlobalWorkSize)
                {
                    //CLEvent Event=new CLEvent();
                    Execute(CommQueues[DefaultCQ], Arguments, GlobalWorkSize, null, null);
                    //OpenCLDriver.clReleaseEvent(Event);
                }

                /// <summary>Execute this kernel using work_dim = 1</summary>
                /// <param name="GlobalWorkSize">Global work size in one-dimension. global_work_size = new int[1] {GlobalWorkSize}</param>
                /// <param name="Arguments">Arguments of the kernel function</param>
                public void Execute(CLCalc.Program.MemoryObject[] Arguments, int GlobalWorkSize)
                {
                    //CLEvent Event=new CLEvent();
                    Execute(CommQueues[DefaultCQ], Arguments, new int[] { GlobalWorkSize }, null, null);
                    //OpenCLDriver.clReleaseEvent(Event);
                }

                /// <summary>Execute this kernel</summary>
                /// <param name="GlobalWorkSize">Array of maximum index arrays. Total work-items = product(max[i],i+0..n-1), n=max.Length</param>
                /// <param name="LocalWorkSize">Local work sizes</param>
                /// <param name="Arguments">Arguments of the kernel function</param>
                public void Execute(CLCalc.Program.MemoryObject[] Arguments, int[] GlobalWorkSize, int[] LocalWorkSize)
                {
                    //CLEvent Event=new CLEvent();
                    Execute(CommQueues[DefaultCQ], Arguments, GlobalWorkSize, LocalWorkSize, null);
                    //OpenCLDriver.clReleaseEvent(Event);
                }

                /// <summary>Execute this kernel</summary>
                /// <param name="GlobalWorkSize">Array of maximum index arrays. Total work-items = product(max[i],i+0..n-1), n=max.Length</param>
                /// <param name="LocalWorkSize">Local work sizes</param>
                /// <param name="Arguments">Arguments of the kernel function</param>
                /// <param name="events">Events list</param>
                public void Execute(CLCalc.Program.MemoryObject[] Arguments, int[] GlobalWorkSize, int[] LocalWorkSize, ICollection<ComputeEventBase> events)
                {
                    //CLEvent Event=new CLEvent();
                    Execute(CommQueues[DefaultCQ], Arguments, GlobalWorkSize, LocalWorkSize, events);
                    //OpenCLDriver.clReleaseEvent(Event);
                }


                /// <summary>Releases kernel from memory</summary>
                public void Dispose()
                {
                    //Let Cloo handle
                    //kernel.Dispose();
                }

                /// <summary>Destructor</summary>
                ~Kernel()
                {
                    Dispose();
                }
            }
        }

        /// <summary>OpenCL programs</summary>
        public static class CLPrograms
        {
            /// <summary>Basic linear algebra functions</summary>
            public class floatLinearAlgebra
            {
                #region Kernels
                /// <summary>Float vector sum kernel</summary>
                private CLCalc.Program.Kernel floatVecSum;
                
                /// <summary>float matrix multiplication kernel</summary>
                private CLCalc.Program.Kernel floatMatrixMult;

                /// <summary>float Gauss Seidel method</summary>
                private CLCalc.Program.Kernel floatGaussSeidel;
                private CLCalc.Program.Kernel floatGaussSeidelError;
                private CLCalc.Program.Kernel floatCalcMtM;
                private CLCalc.Program.Kernel floatCalcMtb;

                /// <summary>LU factorizaton method</summary>
                private CLCalc.Program.Kernel floatLUScale;
                private CLCalc.Program.Kernel floatLUCalcBetas;
                private CLCalc.Program.Kernel floatLUCalcAlphas;
                private CLCalc.Program.Kernel floatLUCalcPivo;
                private CLCalc.Program.Kernel floatLUTrocaCols;
                private CLCalc.Program.Kernel floatLUDivByPivot;
                private CLCalc.Program.Kernel floatLUForwardSubs;
                private CLCalc.Program.Kernel floatLUBackSubs;
                private CLCalc.Program.Kernel floatLUDivide;
                private CLCalc.Program.Kernel floatLUUnscramble;
                private CLCalc.Program.Kernel floatSolveError;
                private CLCalc.Program.Kernel floatLUSubErr;

                #endregion

                /// <summary>Constructor. Builds OpenCL program.</summary>
                public floatLinearAlgebra()
                {
                    if (CLCalc.CLDevices == null)
                    {
                        try
                        {
                            CLCalc.InitCL();
                        }
                        catch
                        {
                            throw new Exception("Could not initialize OpenCL");
                        }
                    }

                    try
                    {
                        LinalgSource Source = new LinalgSource();
                        string[] s = new string[] { Source.dblInclude, Source.vecSum, Source.matrixMult, 
                            Source.GaussSeidel, Source.LUDecomp };
                        CLCalc.Program.Compile(s);
                        //sum
                        floatVecSum = new Program.Kernel("floatVectorSum");
                        //multiplication
                        floatMatrixMult = new Program.Kernel("floatMatrixMult");
                        //Linear system (Gauss Seidel)
                        floatGaussSeidel = new Program.Kernel("floatGaussSeidel");
                        floatGaussSeidelError = new Program.Kernel("floatGaussSeidelError");
                        floatCalcMtM = new Program.Kernel("floatCalcMtM");
                        floatCalcMtb = new Program.Kernel("floatCalcMtb");

                        //Linear system (LU factorization)
                        floatLUScale = new Program.Kernel("floatLUScale");
                        floatLUCalcBetas = new Program.Kernel("floatLUCalcBetas");
                        floatLUCalcAlphas = new Program.Kernel("floatLUCalcAlphas");
                        floatLUCalcPivo = new Program.Kernel("floatLUCalcPivo");
                        floatLUTrocaCols = new Program.Kernel("floatLUTrocaCols");
                        floatLUDivByPivot = new Program.Kernel("floatLUDivByPivot");
                        floatLUForwardSubs = new Program.Kernel("floatLUForwardSubs");
                        floatLUBackSubs = new Program.Kernel("floatLUBackSubs");
                        floatLUDivide = new Program.Kernel("floatLUDivide");
                        floatLUUnscramble = new Program.Kernel("floatLUUnscramble");
                        floatSolveError = new Program.Kernel("floatSolveError");
                        floatLUSubErr = new Program.Kernel("floatLUSubErr");
                    }
                    catch
                    {
                        throw new Exception("Could not compile program");
                    }

                }

                #region Matrix copy to and from vector (float[,] double[,])

                /// <summary>Converts vector to matrix</summary>
                /// <param name="v">Vector</param>
                /// <param name="maxi">Matrix first dimension</param>
                /// <param name="maxj">Matrix second dimension</param>
                private float[,] VectorToMatrix(float[] v, ref int maxi, ref int maxj)
                {
                    float[,] M = new float[maxi, maxj];

                    for (int i = 0; i < maxi; i++)
                        for (int j = 0; j < maxj; j++)
                            M[i, j] = v[i + maxi * j];

                    return M;
                }

                /// <summary>Converts matrix to vector</summary>
                /// <param name="M">Matrix</param>
                /// <param name="maxi">Matrix first dimension</param>
                /// <param name="maxj">Matrix second dimension</param>
                private float[] MatrixToVector(float[,] M, ref int maxi, ref int maxj)
                {
                    float[] v = new float[maxi * maxj];

                    for (int i = 0; i < maxi; i++)
                        for (int j = 0; j < maxj; j++)
                            v[i + maxi * j] = M[i, j];

                    return v;
                }

                #endregion

                #region Matrix sum (float[,] double[,])
                /// <summary>Returns the sum of two matrices</summary>
                /// <param name="M1">Matrix 1</param>
                /// <param name="M2">Matrix 2</param>
                public float[,] MatrixSum(float[,] M1, float[,] M2)
                {
                    //Creates OpenCL variables to store data
                    int maxi = M1.GetLength(0);
                    int maxj = M1.GetLength(1);
                    float[] vecM1 = new float[maxi * maxj];
                    float[] vecM2 = new float[maxi * maxj];

                    if (M2.GetLength(0) != maxi || M2.GetLength(1) != maxj) throw new Exception("Incompatible dimensions");

                    for (int i = 0; i < maxi; i++)
                    {
                        for (int j = 0; j < maxj; j++)
                        {
                            vecM1[i + maxi * j] = M1[i, j];
                            vecM2[i + maxi * j] = M2[i, j];
                        }
                    }

                    CLCalc.Program.Variable varM1 = new Program.Variable(vecM1);
                    CLCalc.Program.Variable varM2 = new Program.Variable(vecM2);

                    CLCalc.Program.Variable[] args = new Program.Variable[2] { varM1, varM2 };
                    int[] max = new int[1] { maxi * maxj };

                    floatVecSum.Execute(args, max);

                    //Escreve o resultado em varM1; devo ler os resultados de la
                    varM1.ReadFromDeviceTo(vecM1);

                    return VectorToMatrix(vecM1, ref maxi, ref maxj);
                }



                #endregion

                #region Matrix multiplication (float)

                /// <summary>Matrix multiplication</summary>
                /// <param name="M1">Matrix 1</param>
                /// <param name="M2">Matrix 2</param>
                public float[,] MatrixMultiply(float[,] M1, float[,] M2)
                {
                    //M pxq, N qxr
                    int p = M1.GetLength(0);
                    int q = M1.GetLength(1);
                    int r = M2.GetLength(1);

                    if (q != M2.GetLength(0)) throw new Exception("Matrix dimensions do not match for multiplication");

                    float[] vecM1 = MatrixToVector(M1, ref p, ref q);
                    float[] vecM2 = MatrixToVector(M2, ref q, ref r);
                    float[] vecResp = new float[p * r];

                    CLCalc.Program.Variable varResp = new Program.Variable(vecResp);

                    CLCalc.Program.Variable varM1 = new Program.Variable(vecM1);
                    CLCalc.Program.Variable varM2 = new Program.Variable(vecM2);


                    //Finaliza a soma dos elementos
                    int[] vecQ = new int[1] { q };
                    CLCalc.Program.Variable varQ = new Program.Variable(vecQ);
                    CLCalc.Program.Variable[] args = new Program.Variable[4] { varResp, varM1, varM2, varQ };
                    int[] max = new int[2] { p, r };
                    floatMatrixMult.Execute(args, max);

                    varResp.ReadFromDeviceTo(vecResp);

                    varResp.Dispose();


                    return VectorToMatrix(vecResp, ref p, ref r);
                }
                #endregion

                #region Iterative linear system solving

                /// <summary>Gauss Seidel method for iterative linear system solving. Returns unknown x</summary>
                /// <param name="M">Matrix M so that Mx=b</param>
                /// <param name="x">Initial estimate</param>
                /// <param name="b">Known vector b</param>
                /// <param name="Iterations">Gauss-Seidel iterations per step</param>
                /// <param name="MaxIter">Maximum number of times Gauss-Seidel iterations</param>
                /// <param name="totalError">Desired sqrt(Sum(error[i]^2))*number of equations</param>
                /// <param name="err">Estimated absolute error per component</param>
                public float[] LeastSquaresGS(float[,] M, float[] b, float[] x, int Iterations, int MaxIter, float totalError, out float[] err)
                {
                    //M pxp
                    int p = M.GetLength(0);
                    int q = M.GetLength(1);
                    //Consistencia
                    if (p != b.Length) throw new Exception("Matrix and vector b dimensions not compatible");
                    if (q != x.Length) throw new Exception("Matrix and initial guess x dimensions not compatible");

                    float[] vecM = MatrixToVector(M, ref p, ref q);
                    //Calculo de MtM e Mtb
                    CLCalc.Program.Variable varMtM = new Program.Variable(new float[q * q]);
                    CLCalc.Program.Variable varMtb = new Program.Variable(new float[q]);

                    {
                        CLCalc.Program.Variable varM = new Program.Variable(vecM);
                        CLCalc.Program.Variable varb = new Program.Variable(b);
                        CLCalc.Program.Variable varp = new Program.Variable(new int[1] { p });

                        CLCalc.Program.Variable[] arg = new Program.Variable[] { varM, varMtM, varp };
                        int[] maxs = new int[2] { q, q };
                        floatCalcMtM.Execute(arg, maxs);

                        arg = new Program.Variable[] { varM, varb, varMtb, varp };
                        maxs = new int[1] { q };
                        floatCalcMtb.Execute(arg, maxs);

                        varM.Dispose(); varb.Dispose(); varp.Dispose();
                    }

                    //Solucao do sistema
                    CLCalc.Program.Variable varx = new Program.Variable(x);
                    CLCalc.Program.Variable varerrx = new Program.Variable(x);
                    CLCalc.Program.Variable[] args = new Program.Variable[3] { varMtM, varMtb, varx };
                    CLCalc.Program.Variable[] args2 = new Program.Variable[4] { varMtM, varMtb, varerrx, varx };


                    int[] max = new int[1] { q };

                    float[] resp = new float[q];
                    err = new float[q];
                    float absErr = totalError + 1;
                    int i = 0;
                    while (i < MaxIter && absErr > totalError * (float)q)
                    //while (i < MaxIter && absErr > totalError)
                    {
                        for (int j = 0; j < Iterations; j++)
                        {
                            floatGaussSeidel.Execute(args, max);
                            i++;
                        }
                        //Calcula o erro
                        floatGaussSeidelError.Execute(args2, max);
                        varerrx.ReadFromDeviceTo(err);

                        absErr = 0;
                        for (int j = 0; j < q; j++) absErr += err[j] * err[j];
                        absErr = (float)Math.Sqrt(absErr);
                    }

                    //Retorna a resposta
                    varx.ReadFromDeviceTo(resp);
                    varerrx.ReadFromDeviceTo(err);

                    //Limpa memoria
                    varMtM.Dispose(); varx.Dispose(); varerrx.Dispose(); varMtb.Dispose();

                    return resp;
                }

                /// <summary>Gauss Seidel method for iterative linear system solving. Returns unknown x</summary>
                /// <param name="M">Matrix M so that Mx=b</param>
                /// <param name="x">Initial estimate</param>
                /// <param name="b">Known vector b</param>
                /// <param name="err">Estimated error per equation</param>
                public float[] LeastSquaresGS(float[,] M, float[] b, float[] x, out float[] err)
                {
                    return LeastSquaresGS(M, b, x, 15, 400, 4e-7f, out err);
                }

                #endregion

                #region LU Decomposition linear system solving
                /// <summary>Solves linear system Mx = b by LU decomposition. Returns x</summary>
                /// <param name="M">Matrix M</param>
                /// <param name="b">Vector b</param>
                /// <param name="maxAbsErr">Maximum acceptable absolute error</param>
                /// <param name="maxIters">Maximum iterations</param>
                public float[] LinSolve(float[,] M, float[] b, float maxAbsErr, int maxIters)
                {
                    int n = M.GetLength(0);
                    if (M.GetLength(1) != n) throw new Exception("Matrix not square");
                    if (b.Length != n) throw new Exception("Incompatible vector dimension");

                    float[] x = new float[b.Length];
                    float[] errx = new float[b.Length];
                    CLCalc.Program.Variable varindx;
                    CLCalc.Program.Variable MLUDecomp = LUDecomp(M, n, out varindx);

                    //Backsubstitution
                    CLCalc.Program.Variable varx = LUBackSubstitute(MLUDecomp, b, n, varindx);

                    //Error control
                    CLCalc.Program.Variable varerrx = new Program.Variable(errx);
                    float[] vecM = MatrixToVector(M, ref n, ref n);
                    CLCalc.Program.Variable varM = new Program.Variable(vecM);
                    CLCalc.Program.Variable varb = new Program.Variable(b);

                    CLCalc.Program.Variable[] args = new Program.Variable[] { varM, varb, varerrx, varx };
                    int[] max = new int[] { n };

                    float absErr = maxAbsErr + 1;

                    int iter = 0;
                    while (absErr > maxAbsErr && iter < maxIters)
                    {
                        CLCalc.Program.Variable vardelta = LUBackSubstitute(MLUDecomp, errx, n, varindx);

                        //Acopla correção na solução
                        CLCalc.Program.Variable[] args2 = new Program.Variable[] { vardelta, varx };
                        floatLUSubErr.Execute(args2, max);

                        floatSolveError.Execute(args, max);
                        varerrx.ReadFromDeviceTo(errx);
                        absErr = 0;
                        for (int j = 0; j < n; j++) absErr += errx[j] * errx[j];
                        absErr = (float)Math.Sqrt(absErr);

                        iter++;
                    }


                    varx.ReadFromDeviceTo(x);
                    return x;
                }


                private CLCalc.Program.Variable LUBackSubstitute(CLCalc.Program.Variable MLUDecomp, float[] b, int n, CLCalc.Program.Variable varindx)
                {
                    CLCalc.Program.Variable varx = new Program.Variable(b);
                    CLCalc.Program.Variable varN = new Program.Variable(new int[1] { n });
                    int[] J = new int[1];
                    CLCalc.Program.Variable varJ = new Program.Variable(J);

                    CLCalc.Program.Variable[] args = new Program.Variable[] { MLUDecomp, varx, varindx, varN, varJ };
                    int[] max = new int[1];

                    //ajeita o vetor com respeito as trocas de linha
                    max[0] = 1;
                    floatLUUnscramble.Execute(args, max);
                    
                    //Forward subst
                    for (int i = n - 1; i >= 1; i--)
                    {
                        max[0] = i;
                        floatLUForwardSubs.Execute(args, max);
                    }

                    //Backward subst
                    for (int j = n - 1; j >= 1; j--)
                    {
                        max[0] = 1;
                        J[0] = j;
                        varJ.WriteToDevice(J);
                        floatLUDivide.Execute(args, max);

                        max[0] = j;
                        floatLUBackSubs.Execute(args, max);
                    }
                    //Primeiro elemento
                    max[0] = 1;
                    J[0] = 0;
                    varJ.WriteToDevice(J);
                    floatLUDivide.Execute(args, max);

                    return varx;
                }

                /// <summary>Calculates LU decomposition of M matrix</summary>
                /// <param name="M">Matrix to decompose</param>
                /// <param name="n">Matrix dimension</param>
                /// <param name="varindx">Swap index</param>
                private CLCalc.Program.Variable LUDecomp(float[,] M, int n, out CLCalc.Program.Variable varindx)
                {
                    //arguments and work_dim
                    CLCalc.Program.Variable[] args;
                    int[] max;

                    //Matrix to vector
                    float[] vecM = MatrixToVector(M, ref n, ref n);
                    CLCalc.Program.Variable varM = new Program.Variable(vecM);

                    //Scaling transformation
                    float[] vv = new float[n];
                    CLCalc.Program.Variable varvv = new Program.Variable(vv);
                    max = new int[1] { n };
                    args = new CLCalc.Program.Variable[] { varM, varvv };
                    floatLUScale.Execute(args, max);

                    //In order LU factorization (Crout)
                    int[] J = new int[1] { 0 };
                    CLCalc.Program.Variable varJ = new Program.Variable(J);
                    int[] N = new int[1] { n };
                    CLCalc.Program.Variable varN = new Program.Variable(N);
                    int[] indx = new int[n];
                    varindx = new Program.Variable(indx);

                    args = new Program.Variable[] { varM, varJ, varN, varindx, varvv };
                    for (J[0] = 0; J[0] < n; J[0]++)
                    {
                        varJ.WriteToDevice(J);
                        max[0] = J[0];
                        floatLUCalcBetas.Execute(args,max);

                        max[0] = n - J[0];
                        floatLUCalcAlphas.Execute(args, max);

                        max[0] = 1;
                        floatLUCalcPivo.Execute(args, max);

                        max[0] = n;
                        floatLUTrocaCols.Execute(args, max);

                        if (J[0] != n - 1)
                        {
                            max[0] = n - J[0] - 1;
                            floatLUDivByPivot.Execute(args, max);
                        }
                    }

                    return varM;
                }

                #endregion

                #region OpenCL Source
                private class LinalgSource
                {
                    public string dblInclude = @"
                            #pragma OPENCL EXTENSION cl_khr_fp64 : enable
                            ";

                    #region OpenCL source for LU linear system solving
                    public string LUDecomp = @"
                            __kernel void
                            floatLUScale(    __global read_only  float * M,
                                             __global write_only float * vv)
                            {
                                // Vector element index
                                int i = get_global_id(0);
                                int n = get_global_size(0);

                                float big = 0;
                                for (int j = 0; j < n; j++)
                                {
                                    float temp = M[i + n*j];
                                    if (temp < 0) temp = -temp;
                                    if (temp > big) big = temp;
                                }

                                vv[i] = 1 / big;
                            }

                            __kernel void
                            floatLUCalcBetas(   __global            float * M,
                                                __global read_only  int * J,
                                                __global read_only  int * N, 
                                                __global            int * indx, 
                                                __global            float * vv)
                            {
                                    int i = get_global_id(0);
                                    int j = J[0];
                                    int n = N[0];
                                    float sum = M[i + n*j];
                                    for (int k = 0; k < i; k++)
                                    {
                                        sum -= M[i + n*k] * M[k + n*j];
                                    }

                                    M[i + n*j] = sum;
                            }

                            __kernel void
                            floatLUCalcAlphas(  __global            float * M,
                                                __global read_only  int * J,
                                                __global read_only  int * N, 
                                                __global            int * indx, 
                                                __global            float * vv)
                            {
                                    int j = J[0];
                                    int n = N[0];
                                    int i = get_global_id(0) + j;

                                    float sum = M[i + n*j];
                                    for (int k = 0; k < j; k++)
                                    {
                                        sum -= M[i + n*k] * M[k + n*j];
                                    }
                                    M[i + n*j] = sum;
                            }


                            __kernel void
                            floatLUCalcPivo(  __global            float * M,
                                              __global read_only  int * J,
                                              __global read_only  int * N, 
                                              __global            int * indx, 
                                              __global            float * vv)
                            {
                                int j = J[0];
                                int n = N[0];

                                float big = 0; float temp=0; float dum=0; int imax;
                                for (int i = j; i < n; i++)
                                {
                                    temp = M[i + n*j]; if (temp < 0) temp = -temp;
                                    dum = vv[i] * temp; //melhor pivo?

                                    if (dum >= big)
                                    {
                                        big = dum;
                                        imax = i;
                                    }
                                }
                                indx[j] = imax;
                                vv[imax] = vv[j];
                            }

                            __kernel void
                            floatLUTrocaCols(   __global            float * M,
                                                __global read_only  int * J,
                                                __global read_only  int * N, 
                                                __global            int * indx, 
                                                __global            float * vv)
                            {
                                    int j = J[0];
                                    int n = N[0];
                                    int k = get_global_id(0);
                                    int imax = indx[j];

                                    float dum = M[imax + n*k];
                                    M[imax + n*k] = M[j + n*k];
                                    M[j + n*k] = dum;
                            }

                            __kernel void
                            floatLUDivByPivot(  __global            float * M,
                                                __global read_only  int * J,
                                                __global read_only  int * N, 
                                                __global            int * indx, 
                                                __global            float * vv)
                            {
                                    int j = J[0];
                                    int n = N[0];
                                    int i = get_global_id(0) + j + 1;

                                    M[i + n*j] /= M[j+n*j];
                            }

                            //desfaz as trocas realizadas em indx
                            __kernel void
                            floatLUUnscramble(  __global            float * M,
                                                __global            float * x,
                                                __global read_only  int * indx,
                                                __global read_only  int * N,
                                                __global read_only  int * J)
                            {
                                int n = N[0]; int ip = 0;
                                float temp;
                                for (int i = 0; i < n; i++)
                                {
                                    ip = indx[i];
                                    temp = x[ip];
                                    x[ip] = x[i];
                                    x[i] = temp;
                                } 
                            }

                            //Chamar com global_sizes iguais a n-1, n-2, ..., 1
                            __kernel void
                            floatLUBackSubs  (  __global            float * M,
                                                __global            float * x,
                                                __global read_only  int * indx,
                                                __global read_only  int * N,
                                                __global read_only  int * J)
                            {
                                // Vector element index
                                int i = get_global_id(0);
                                int j = get_global_size(0);
                                int n = N[0];

                                //subtrai o valor do vetor solução atual
                                x[i] -= x[j] * M[i + n*j];
                            }

                            __kernel void
                            floatLUDivide    (  __global            float * M,
                                                __global            float * x,
                                                __global read_only  int * indx,
                                                __global read_only  int * N,
                                                __global read_only  int * J)
                            {
                                // Vector element index
                                int j = J[0];
                                int n = N[0];

                                x[j] /= M[j + n*j];
                            }

                            //Chamar com global_sizes iguais a n-1, n-2, ..., 1
                            __kernel void
                            floatLUForwardSubs (__global            float * M,
                                                __global            float * x,
                                                __global read_only  int * indx,
                                                __global read_only  int * N,
                                                __global read_only  int * J)
                            {
                                // Vector element index
                                int n = N[0];
                                int j = n - get_global_size(0) - 1;
                                int i = n - get_global_id(0) - 1;

                                //subtrai o valor do vetor solução atual
                                x[i] -= x[j] * M[i + n*j];
                            }

                            __kernel void
                            floatSolveError(__global read_only float * M,
                                            __global read_only float * b,
                                            __global           float * xerr,
                                            __global           float * x)
                            {
                                // Vector element index
                                int i = get_global_id(0);
                                int n = get_global_size(0);
                                float temp=0;
                                for (int j = 0; j < n; j++)
                                {
                                   temp += M[i+n*j] * x[j];
                                }
                                xerr[i] = b[i] - temp;
                            }

                            __kernel void
                            floatLUSubErr(__global           float * delta,
                                          __global           float * x)
                            {
                                // Vector element index
                                int i = get_global_id(0);
                                x[i] += delta[i];
                            }

                            ";
                    #endregion

                    #region OpenCL source for Gauss Seidel linear solve
                    /// <summary>Gauss Seidel method. Make sure to send x = b. Replaces x.</summary>
                    public string GaussSeidel = @"
                            __kernel void
                            floatGaussSeidel(     __global read_only float * M,
                                                  __global read_only float * b,
                                                  __global           float * x)
                            {
                                // Vector element index
                                int i = get_global_id(0);
                                int n = get_global_size(0);
                                float temp=0;
                                for (int j = 0; j < n; j++)
                                {
                                    if (j != i)
                                    {
                                       temp += M[i+n*j] * x[j];
                                    }
                                }
                                x[i] = (b[i] - temp)/M[i + n*i];
                            }

                            __kernel void
                            floatGaussSeidelError(__global read_only float * M,
                                                  __global read_only float * b,
                                                  __global           float * xerr,
                                                  __global           float * x)
                            {
                                // Vector element index
                                int i = get_global_id(0);
                                int n = get_global_size(0);
                                float temp=0;
                                for (int j = 0; j < n; j++)
                                {
                                   temp += M[i+n*j] * x[j];
                                }
                                xerr[i] = b[i] - temp;
                            }

                            __kernel void
                            floatCalcMtM(__global read_only float * M,
                                         __global           float * MtM,
                                         __global           int * p)
                            {
                                // Vector element index
                                int i = get_global_id(0);
                                int q = get_global_size(0);
                                int j = get_global_id(1);

                                MtM[i + q*j] = 0;
                                int pp = p[0];
                                for (int k = 0; k < pp; k++)
                                {
                                    MtM[i + q*j] += M[k + pp*i] * M[k + pp*j];
                                }
                            }

                            __kernel void
                            floatCalcMtb(__global read_only float * M,
                                         __global           float * b,
                                         __global           float * Mtb,
                                         __global           int * p)
                            {
                                // Vector element index
                                int i = get_global_id(0);
                                int q = get_global_size(0);

                                Mtb[i] = 0;
                                int pp = p[0];
                                for (int k = 0; k < pp; k++)
                                {
                                    Mtb[i] += M[k + pp*i] * b[k];
                                }
                            }"
                        ;
                    #endregion

                    #region OpenCL source for matrix multiplication
                    /// <summary>Matrix multiplication. Dimensions { p, r }.
                    /// </summary>
                    public string matrixMult = @"
                            __kernel void

                            floatMatrixMult(     __global       float * MResp,
                                                 __global       float * M1,
                                                 __global       float * M2,
                                                 __global       int * q)
                            {
                                // Vector element index
                                int i = get_global_id(0);
                                int j = get_global_id(1);

                                int p = get_global_size(0);
                                int r = get_global_size(1);

                                MResp[i + p * j] = 0;
                                for (int k = 0; k < q[0]; k++)
                                {
                                    MResp[i + p * j] += M1[i + p * k] * M2[k + q[0] * j];
                                }
                            }"
                        ;
                    #endregion

                    #region OpenCL source code for vector sum
                    public string vecSum = @"
                            __kernel void
                            intVectorSum( __global       int * v1,
                                          __global       int * v2)
                            {
                                // Vector element index
                                int i = get_global_id(0);
                                v1[i] = v1[i] + v2[i];
                            }

                            __kernel void
                            floatVectorSum(__global       float * v1,
                                           __global       float * v2)
                            {
                                // Vector element index
                                int i = get_global_id(0);
                                v1[i] = v1[i] + v2[i];
                            }

                            __kernel void
                            longVectorSum( __global       long * v1,
                                           __global       long * v2)
                            {
                                // Vector element index
                                int i = get_global_id(0);
                                v1[i] = v1[i] + v2[i];
                            }
                            ";
                    #endregion
                }
                #endregion
            }

            /// <summary>Basic linear algebra functions</summary>
            public class doubleLinearAlgebra
            {
                #region Kernels
                /// <summary>Float vector sum kernel</summary>
                private CLCalc.Program.Kernel doubleVecSum;

                /// <summary>double matrix multiplication kernel</summary>
                private CLCalc.Program.Kernel doubleMatrixMult;

                /// <summary>double Gauss Seidel method</summary>
                private CLCalc.Program.Kernel doubleGaussSeidel;
                private CLCalc.Program.Kernel doubleGaussSeidelError;
                private CLCalc.Program.Kernel doubleCalcMtM;
                private CLCalc.Program.Kernel doubleCalcMtb;

                /// <summary>LU factorizaton method</summary>
                private CLCalc.Program.Kernel doubleLUScale;
                private CLCalc.Program.Kernel doubleLUCalcBetas;
                private CLCalc.Program.Kernel doubleLUCalcAlphas;
                private CLCalc.Program.Kernel doubleLUCalcPivo;
                private CLCalc.Program.Kernel doubleLUTrocaCols;
                private CLCalc.Program.Kernel doubleLUDivByPivot;
                private CLCalc.Program.Kernel doubleLUForwardSubs;
                private CLCalc.Program.Kernel doubleLUBackSubs;
                private CLCalc.Program.Kernel doubleLUDivide;
                private CLCalc.Program.Kernel doubleLUUnscramble;
                private CLCalc.Program.Kernel doubleSolveError;
                private CLCalc.Program.Kernel doubleLUSubErr;

                #endregion

                /// <summary>Constructor. Builds OpenCL program.</summary>
                public doubleLinearAlgebra()
                {
                    if (CLCalc.CLDevices == null)
                    {
                        try
                        {
                            CLCalc.InitCL();
                        }
                        catch
                        {
                            throw new Exception("Could not initialize OpenCL");
                        }
                    }

                    try
                    {
                        LinalgSource Source = new LinalgSource();
                        string[] s = new string[] { Source.dblInclude, Source.vecSum, Source.matrixMult, 
                            Source.GaussSeidel, Source.LUDecomp };


                        CLCalc.Program.Compile(s);



                        //sum
                        doubleVecSum = new Program.Kernel("doubleVectorSum");
                        //multiplication
                        doubleMatrixMult = new Program.Kernel("doubleMatrixMult");
                        //Linear system (Gauss Seidel)
                        doubleGaussSeidel = new Program.Kernel("doubleGaussSeidel");
                        doubleGaussSeidelError = new Program.Kernel("doubleGaussSeidelError");
                        doubleCalcMtM = new Program.Kernel("doubleCalcMtM");
                        doubleCalcMtb = new Program.Kernel("doubleCalcMtb");

                        //Linear system (LU factorization)
                        doubleLUScale = new Program.Kernel("doubleLUScale");
                        doubleLUCalcBetas = new Program.Kernel("doubleLUCalcBetas");
                        doubleLUCalcAlphas = new Program.Kernel("doubleLUCalcAlphas");
                        doubleLUCalcPivo = new Program.Kernel("doubleLUCalcPivo");
                        doubleLUTrocaCols = new Program.Kernel("doubleLUTrocaCols");
                        doubleLUDivByPivot = new Program.Kernel("doubleLUDivByPivot");
                        doubleLUForwardSubs = new Program.Kernel("doubleLUForwardSubs");
                        doubleLUBackSubs = new Program.Kernel("doubleLUBackSubs");
                        doubleLUDivide = new Program.Kernel("doubleLUDivide");
                        doubleLUUnscramble = new Program.Kernel("doubleLUUnscramble");
                        doubleSolveError = new Program.Kernel("doubleSolveError");
                        doubleLUSubErr = new Program.Kernel("doubleLUSubErr");
                    }
                    catch
                    {
                        throw new Exception("Could not compile program");
                    }

                }

                #region Matrix copy to and from vector (double[,] double[,])

                /// <summary>Converts vector to matrix</summary>
                /// <param name="v">Vector</param>
                /// <param name="maxi">Matrix first dimension</param>
                /// <param name="maxj">Matrix second dimension</param>
                private double[,] VectorToMatrix(double[] v, ref int maxi, ref int maxj)
                {
                    double[,] M = new double[maxi, maxj];

                    for (int i = 0; i < maxi; i++)
                        for (int j = 0; j < maxj; j++)
                            M[i, j] = v[i + maxi * j];

                    return M;
                }

                /// <summary>Converts matrix to vector</summary>
                /// <param name="M">Matrix</param>
                /// <param name="maxi">Matrix first dimension</param>
                /// <param name="maxj">Matrix second dimension</param>
                private double[] MatrixToVector(double[,] M, ref int maxi, ref int maxj)
                {
                    double[] v = new double[maxi * maxj];

                    for (int i = 0; i < maxi; i++)
                        for (int j = 0; j < maxj; j++)
                            v[i + maxi * j] = M[i, j];

                    return v;
                }

                #endregion

                #region Matrix sum (double[,] double[,])
                /// <summary>Returns the sum of two matrices</summary>
                /// <param name="M1">Matrix 1</param>
                /// <param name="M2">Matrix 2</param>
                public double[,] MatrixSum(double[,] M1, double[,] M2)
                {
                    //Creates OpenCL variables to store data
                    int maxi = M1.GetLength(0);
                    int maxj = M1.GetLength(1);
                    double[] vecM1 = new double[maxi * maxj];
                    double[] vecM2 = new double[maxi * maxj];

                    if (M2.GetLength(0) != maxi || M2.GetLength(1) != maxj) throw new Exception("Incompatible dimensions");

                    for (int i = 0; i < maxi; i++)
                    {
                        for (int j = 0; j < maxj; j++)
                        {
                            vecM1[i + maxi * j] = M1[i, j];
                            vecM2[i + maxi * j] = M2[i, j];
                        }
                    }

                    CLCalc.Program.Variable varM1 = new Program.Variable(vecM1);
                    CLCalc.Program.Variable varM2 = new Program.Variable(vecM2);

                    CLCalc.Program.Variable[] args = new Program.Variable[2] { varM1, varM2 };
                    int[] max = new int[1] { maxi * maxj };

                    doubleVecSum.Execute(args, max);

                    //Escreve o resultado em varM1; devo ler os resultados de la
                    varM1.ReadFromDeviceTo(vecM1);

                    return VectorToMatrix(vecM1, ref maxi, ref maxj);
                }



                #endregion

                #region Matrix multiplication (double)

                /// <summary>Matrix multiplication</summary>
                /// <param name="M1">Matrix 1</param>
                /// <param name="M2">Matrix 2</param>
                public double[,] MatrixMultiply(double[,] M1, double[,] M2)
                {
                    //M pxq, N qxr
                    int p = M1.GetLength(0);
                    int q = M1.GetLength(1);
                    int r = M2.GetLength(1);

                    if (q != M2.GetLength(0)) throw new Exception("Matrix dimensions do not match for multiplication");

                    double[] vecM1 = MatrixToVector(M1, ref p, ref q);
                    double[] vecM2 = MatrixToVector(M2, ref q, ref r);
                    double[] vecResp = new double[p * r];

                    CLCalc.Program.Variable varResp = new Program.Variable(vecResp);

                    CLCalc.Program.Variable varM1 = new Program.Variable(vecM1);
                    CLCalc.Program.Variable varM2 = new Program.Variable(vecM2);


                    //Finaliza a soma dos elementos
                    int[] vecQ = new int[1] { q };
                    CLCalc.Program.Variable varQ = new Program.Variable(vecQ);
                    CLCalc.Program.Variable[] args = new Program.Variable[4] { varResp, varM1, varM2, varQ };
                    int[] max = new int[2] { p, r };
                    doubleMatrixMult.Execute(args, max);

                    varResp.ReadFromDeviceTo(vecResp);

                    varResp.Dispose();


                    return VectorToMatrix(vecResp, ref p, ref r);
                }
                #endregion

                #region Iterative linear system solving

                /// <summary>Gauss Seidel method for iterative linear system solving. Returns unknown x</summary>
                /// <param name="M">Matrix M so that Mx=b</param>
                /// <param name="x">Initial estimate</param>
                /// <param name="b">Known vector b</param>
                /// <param name="Iterations">Gauss-Seidel iterations per step</param>
                /// <param name="MaxIter">Maximum number of times Gauss-Seidel iterations</param>
                /// <param name="totalError">Desired sqrt(Sum(error[i]^2))*number of equations</param>
                /// <param name="err">Estimated absolute error per component</param>
                public double[] LeastSquaresGS(double[,] M, double[] b, double[] x, int Iterations, int MaxIter, double totalError, out double[] err)
                {
                    //M pxp
                    int p = M.GetLength(0);
                    int q = M.GetLength(1);
                    //Consistencia
                    if (p != b.Length) throw new Exception("Matrix and vector b dimensions not compatible");
                    if (q != x.Length) throw new Exception("Matrix and initial guess x dimensions not compatible");

                    double[] vecM = MatrixToVector(M, ref p, ref q);
                    //Calculo de MtM e Mtb
                    CLCalc.Program.Variable varMtM = new Program.Variable(new double[q * q]);
                    CLCalc.Program.Variable varMtb = new Program.Variable(new double[q]);

                    {
                        CLCalc.Program.Variable varM = new Program.Variable(vecM);
                        CLCalc.Program.Variable varb = new Program.Variable(b);
                        CLCalc.Program.Variable varp = new Program.Variable(new int[1] { p });

                        CLCalc.Program.Variable[] arg = new Program.Variable[] { varM, varMtM, varp };
                        int[] maxs = new int[2] { q, q };
                        doubleCalcMtM.Execute(arg, maxs);

                        arg = new Program.Variable[] { varM, varb, varMtb, varp };
                        maxs = new int[1] { q };
                        doubleCalcMtb.Execute(arg, maxs);

                        varM.Dispose(); varb.Dispose(); varp.Dispose();
                    }

                    //Solucao do sistema
                    CLCalc.Program.Variable varx = new Program.Variable(x);
                    CLCalc.Program.Variable varerrx = new Program.Variable(x);
                    CLCalc.Program.Variable[] args = new Program.Variable[3] { varMtM, varMtb, varx };
                    CLCalc.Program.Variable[] args2 = new Program.Variable[4] { varMtM, varMtb, varerrx, varx };


                    int[] max = new int[1] { q };

                    double[] resp = new double[q];
                    err = new double[q];
                    double absErr = totalError + 1;
                    int i = 0;
                    while (i < MaxIter && absErr > totalError * (double)q)
                    //while (i < MaxIter && absErr > totalError)
                    {
                        for (int j = 0; j < Iterations; j++)
                        {
                            doubleGaussSeidel.Execute(args, max);
                            i++;
                        }
                        //Calcula o erro
                        doubleGaussSeidelError.Execute(args2, max);
                        varerrx.ReadFromDeviceTo(err);

                        absErr = 0;
                        for (int j = 0; j < q; j++) absErr += err[j] * err[j];
                        absErr = (double)Math.Sqrt(absErr);
                    }

                    //Retorna a resposta
                    varx.ReadFromDeviceTo(resp);
                    varerrx.ReadFromDeviceTo(err);

                    //Limpa memoria
                    varMtM.Dispose(); varx.Dispose(); varerrx.Dispose(); varMtb.Dispose();

                    return resp;
                }

                /// <summary>Gauss Seidel method for iterative linear system solving. Returns unknown x</summary>
                /// <param name="M">Matrix M so that Mx=b</param>
                /// <param name="x">Initial estimate</param>
                /// <param name="b">Known vector b</param>
                /// <param name="err">Estimated error per equation</param>
                public double[] LeastSquaresGS(double[,] M, double[] b, double[] x, out double[] err)
                {
                    return LeastSquaresGS(M, b, x, 15, 400, 4e-7f, out err);
                }

                #endregion

                #region LU Decomposition linear system solving
                /// <summary>Solves linear system Mx = b by LU decomposition. Returns x</summary>
                /// <param name="M">Matrix M</param>
                /// <param name="b">Vector b</param>
                /// <param name="maxAbsErr">Maximum acceptable absolute error</param>
                /// <param name="maxIters">Maximum iterations</param>
                public double[] LinSolve(double[,] M, double[] b, double maxAbsErr, int maxIters)
                {
                    int n = M.GetLength(0);
                    if (M.GetLength(1) != n) throw new Exception("Matrix not square");
                    if (b.Length != n) throw new Exception("Incompatible vector dimension");

                    double[] x = new double[b.Length];
                    double[] errx = new double[b.Length];
                    CLCalc.Program.Variable varindx;
                    CLCalc.Program.Variable MLUDecomp = LUDecomp(M, n, out varindx);

                    double[] localMLUDecomp = new double[n*n];
                    MLUDecomp.ReadFromDeviceTo(localMLUDecomp);

                    
                    //Backsubstitution
                    CLCalc.Program.Variable varx = LUBackSubstitute(MLUDecomp, b, n, varindx);

                    //Error control
                    CLCalc.Program.Variable varerrx = new Program.Variable(errx);
                    double[] vecM = MatrixToVector(M, ref n, ref n);
                    CLCalc.Program.Variable varM = new Program.Variable(vecM);
                    CLCalc.Program.Variable varb = new Program.Variable(b);

                    CLCalc.Program.Variable[] args = new Program.Variable[] { varM, varb, varerrx, varx };
                    int[] max = new int[] { n };

                    double absErr = maxAbsErr + 1;

                    int iter = 0;
                    while (absErr > maxAbsErr && iter < maxIters)
                    {
                        CLCalc.Program.Variable vardelta = LUBackSubstitute(MLUDecomp, errx, n, varindx);

                        //Acopla correção na solução
                        CLCalc.Program.Variable[] args2 = new Program.Variable[] { vardelta, varx };
                        doubleLUSubErr.Execute(args2, max);

                        doubleSolveError.Execute(args, max);
                        varerrx.ReadFromDeviceTo(errx);
                        absErr = 0;
                        for (int j = 0; j < n; j++) absErr += errx[j] * errx[j];
                        absErr = (double)Math.Sqrt(absErr);

                        iter++;
                    }


                    varx.ReadFromDeviceTo(x);
                    return x;
                }


                private CLCalc.Program.Variable LUBackSubstitute(CLCalc.Program.Variable MLUDecomp, double[] b, int n, CLCalc.Program.Variable varindx)
                {
                    CLCalc.Program.Variable varx = new Program.Variable(b);
                    CLCalc.Program.Variable varN = new Program.Variable(new int[1] { n });
                    int[] J = new int[1];
                    CLCalc.Program.Variable varJ = new Program.Variable(J);

                    CLCalc.Program.Variable[] args = new Program.Variable[] { MLUDecomp, varx, varindx, varN, varJ };
                    int[] max = new int[1];

                    //ajeita o vetor com respeito as trocas de linha
                    max[0] = 1;
                    doubleLUUnscramble.Execute(args, max);

                    //Forward subst
                    for (int i = n - 1; i >= 1; i--)
                    {
                        max[0] = i;
                        doubleLUForwardSubs.Execute(args, max);
                    }

                    //Backward subst
                    for (int j = n - 1; j >= 1; j--)
                    {
                        max[0] = 1;
                        J[0] = j;
                        varJ.WriteToDevice(J);
                        doubleLUDivide.Execute(args, max);

                        max[0] = j;
                        doubleLUBackSubs.Execute(args, max);
                    }
                    //Primeiro elemento
                    max[0] = 1;
                    J[0] = 0;
                    varJ.WriteToDevice(J);
                    doubleLUDivide.Execute(args, max);

                    return varx;
                }

                /// <summary>Calculates LU decomposition of M matrix</summary>
                /// <param name="M">Matrix to decompose</param>
                /// <param name="n">Matrix dimension</param>
                /// <param name="varindx">Swap index</param>
                private CLCalc.Program.Variable LUDecomp(double[,] M, int n, out CLCalc.Program.Variable varindx)
                {
                    //arguments and work_dim
                    CLCalc.Program.Variable[] args;
                    int[] max;

                    //Matrix to vector
                    double[] vecM = MatrixToVector(M, ref n, ref n);
                    CLCalc.Program.Variable varM = new Program.Variable(vecM);

                    //Scaling transformation
                    double[] vv = new double[n];
                    CLCalc.Program.Variable varvv = new Program.Variable(vv);
                    max = new int[1] { n };
                    args = new CLCalc.Program.Variable[] { varM, varvv };
                    doubleLUScale.Execute(args, max);

                    //In order LU factorization (Crout)
                    int[] J = new int[1] { 0 };
                    CLCalc.Program.Variable varJ = new Program.Variable(J);
                    int[] N = new int[1] { n };
                    CLCalc.Program.Variable varN = new Program.Variable(N);
                    int[] indx = new int[n];
                    varindx = new Program.Variable(indx);

                    args = new Program.Variable[] { varM, varJ, varN, varindx, varvv };
                    for (J[0] = 0; J[0] < n; J[0]++)
                    {
                        varJ.WriteToDevice(J);
                        max[0] = J[0];
                        doubleLUCalcBetas.Execute(args, max);

                        max[0] = n - J[0];
                        doubleLUCalcAlphas.Execute(args, max);

                        max[0] = 1;
                        doubleLUCalcPivo.Execute(args, max);

                        max[0] = n;
                        doubleLUTrocaCols.Execute(args, max);

                        if (J[0] != n - 1)
                        {
                            max[0] = n - J[0] - 1;
                            doubleLUDivByPivot.Execute(args, max);
                        }
                    }

                    return varM;
                }

                #endregion

                #region OpenCL Source
                private class LinalgSource
                {
                    public string dblInclude = @"
                            #pragma OPENCL EXTENSION cl_khr_fp64 : enable
                            ";

                    #region OpenCL source for LU linear system solving
                    public string LUDecomp = @"
                            __kernel void
                            doubleLUScale(   __global read_only  double * M,
                                             __global write_only double * vv)
                            {
                                // Vector element index
                                int i = get_global_id(0);
                                int n = get_global_size(0);

                                double big = 0;
                                for (int j = 0; j < n; j++)
                                {
                                    double temp = M[i + n*j];
                                    if (temp < 0) temp = -temp;
                                    if (temp > big) big = temp;
                                }

                                vv[i] = 1 / big;
                            }

                            __kernel void
                            doubleLUCalcBetas(   __global            double * M,
                                                __global read_only  int * J,
                                                __global read_only  int * N, 
                                                __global            int * indx, 
                                                __global            double * vv)
                            {
                                    int i = get_global_id(0);
                                    int j = J[0];
                                    int n = N[0];
                                    double sum = M[i + n*j];
                                    for (int k = 0; k < i; k++)
                                    {
                                        sum -= M[i + n*k] * M[k + n*j];
                                    }

                                    M[i + n*j] = sum;
                            }

                            __kernel void
                            doubleLUCalcAlphas(  __global            double * M,
                                                __global read_only  int * J,
                                                __global read_only  int * N, 
                                                __global            int * indx, 
                                                __global            double * vv)
                            {
                                    int j = J[0];
                                    int n = N[0];
                                    int i = get_global_id(0) + j;

                                    double sum = M[i + n*j];
                                    for (int k = 0; k < j; k++)
                                    {
                                        sum -= M[i + n*k] * M[k + n*j];
                                    }
                                    M[i + n*j] = sum;
                            }


                            __kernel void
                            doubleLUCalcPivo(  __global            double * M,
                                              __global read_only  int * J,
                                              __global read_only  int * N, 
                                              __global            int * indx, 
                                              __global            double * vv)
                            {
                                int j = J[0];
                                int n = N[0];

                                double big = 0; double temp=0; double dum=0; int imax;
                                for (int i = j; i < n; i++)
                                {
                                    temp = M[i + n*j]; if (temp < 0) temp = -temp;
                                    dum = vv[i] * temp; //melhor pivo?

                                    if (dum >= big)
                                    {
                                        big = dum;
                                        imax = i;
                                    }
                                }
                                indx[j] = imax;
                                vv[imax] = vv[j];
                            }

                            __kernel void
                            doubleLUTrocaCols(   __global            double * M,
                                                __global read_only  int * J,
                                                __global read_only  int * N, 
                                                __global            int * indx, 
                                                __global            double * vv)
                            {
                                    int j = J[0];
                                    int n = N[0];
                                    int k = get_global_id(0);
                                    int imax = indx[j];

                                    double dum = M[imax + n*k];
                                    M[imax + n*k] = M[j + n*k];
                                    M[j + n*k] = dum;
                            }

                            __kernel void
                            doubleLUDivByPivot( __global            double * M,
                                                __global read_only  int * J,
                                                __global read_only  int * N, 
                                                __global            int * indx, 
                                                __global            double * vv)
                            {
                                    int j = J[0];
                                    int n = N[0];
                                    int i = get_global_id(0) + j + 1;

                                    M[i + n*j] /= M[j+n*j];
                            }

                            //desfaz as trocas realizadas em indx
                            __kernel void
                            doubleLUUnscramble( __global            double * M,
                                                __global            double * x,
                                                __global read_only  int * indx,
                                                __global read_only  int * N,
                                                __global read_only  int * J)
                            {
                                int n = N[0]; int ip = 0;
                                double temp;
                                for (int i = 0; i < n; i++)
                                {
                                    ip = indx[i];
                                    temp = x[ip];
                                    x[ip] = x[i];
                                    x[i] = temp;
                                } 
                            }

                            //Chamar com global_sizes iguais a n-1, n-2, ..., 1
                            __kernel void
                            doubleLUBackSubs  ( __global            double * M,
                                                __global            double * x,
                                                __global read_only  int * indx,
                                                __global read_only  int * N,
                                                __global read_only  int * J)
                            {
                                // Vector element index
                                int i = get_global_id(0);
                                int j = get_global_size(0);
                                int n = N[0];

                                //subtrai o valor do vetor solução atual
                                x[i] -= x[j] * M[i + n*j];
                            }

                            __kernel void
                            doubleLUDivide    ( __global            double * M,
                                                __global            double * x,
                                                __global read_only  int * indx,
                                                __global read_only  int * N,
                                                __global read_only  int * J)
                            {
                                // Vector element index
                                int j = J[0];
                                int n = N[0];

                                x[j] /= M[j + n*j];
                            }

                            //Chamar com global_sizes iguais a n-1, n-2, ..., 1
                            __kernel void
                            doubleLUForwardSubs (__global            double * M,
                                                __global            double * x,
                                                __global read_only  int * indx,
                                                __global read_only  int * N,
                                                __global read_only  int * J)
                            {
                                // Vector element index
                                int n = N[0];
                                int j = n - get_global_size(0) - 1;
                                int i = n - get_global_id(0) - 1;

                                //subtrai o valor do vetor solução atual
                                x[i] -= x[j] * M[i + n*j];
                            }

                            __kernel void
                            doubleSolveError(__global read_only double * M,
                                            __global read_only double * b,
                                            __global           double * xerr,
                                            __global           double * x)
                            {
                                // Vector element index
                                int i = get_global_id(0);
                                int n = get_global_size(0);
                                double temp=0;
                                for (int j = 0; j < n; j++)
                                {
                                   temp += M[i+n*j] * x[j];
                                }
                                xerr[i] = b[i] - temp;
                            }

                            __kernel void
                            doubleLUSubErr(__global           double * delta,
                                          __global           double * x)
                            {
                                // Vector element index
                                int i = get_global_id(0);
                                x[i] += delta[i];
                            }

                            ";
                    #endregion

                    #region OpenCL source for Gauss Seidel linear solve
                    /// <summary>Gauss Seidel method. Make sure to send x = b. Replaces x.</summary>
                    public string GaussSeidel = @"
                            __kernel void
                            doubleGaussSeidel(     __global read_only double * M,
                                                  __global read_only double * b,
                                                  __global           double * x)
                            {
                                // Vector element index
                                int i = get_global_id(0);
                                int n = get_global_size(0);
                                double temp=0;
                                for (int j = 0; j < n; j++)
                                {
                                    if (j != i)
                                    {
                                       temp += M[i+n*j] * x[j];
                                    }
                                }
                                x[i] = (b[i] - temp)/M[i + n*i];
                            }

                            __kernel void
                            doubleGaussSeidelError(__global read_only double * M,
                                                  __global read_only double * b,
                                                  __global           double * xerr,
                                                  __global           double * x)
                            {
                                // Vector element index
                                int i = get_global_id(0);
                                int n = get_global_size(0);
                                double temp=0;
                                for (int j = 0; j < n; j++)
                                {
                                   temp += M[i+n*j] * x[j];
                                }
                                xerr[i] = b[i] - temp;
                            }

                            __kernel void
                            doubleCalcMtM(__global read_only double * M,
                                         __global           double * MtM,
                                         __global           int * p)
                            {
                                // Vector element index
                                int i = get_global_id(0);
                                int q = get_global_size(0);
                                int j = get_global_id(1);

                                MtM[i + q*j] = 0;
                                int pp = p[0];
                                for (int k = 0; k < pp; k++)
                                {
                                    MtM[i + q*j] += M[k + pp*i] * M[k + pp*j];
                                }
                            }

                            __kernel void
                            doubleCalcMtb(__global read_only double * M,
                                         __global           double * b,
                                         __global           double * Mtb,
                                         __global           int * p)
                            {
                                // Vector element index
                                int i = get_global_id(0);
                                int q = get_global_size(0);

                                Mtb[i] = 0;
                                int pp = p[0];
                                for (int k = 0; k < pp; k++)
                                {
                                    Mtb[i] += M[k + pp*i] * b[k];
                                }
                            }"
                        ;
                    #endregion

                    #region OpenCL source for matrix multiplication
                    /// <summary>Matrix multiplication. Dimensions { p, r }.
                    /// </summary>
                    public string matrixMult = @"
                            __kernel void

                            doubleMatrixMult(     __global       double * MResp,
                                                 __global       double * M1,
                                                 __global       double * M2,
                                                 __global       int * q)
                            {
                                // Vector element index
                                int i = get_global_id(0);
                                int j = get_global_id(1);

                                int p = get_global_size(0);
                                int r = get_global_size(1);

                                MResp[i + p * j] = 0;
                                for (int k = 0; k < q[0]; k++)
                                {
                                    MResp[i + p * j] += M1[i + p * k] * M2[k + q[0] * j];
                                }
                            }"
                        ;
                    #endregion

                    #region OpenCL source code for vector sum
                    public string vecSum = @"
                            __kernel void
                            intVectorSum( __global       int * v1,
                                          __global       int * v2)
                            {
                                // Vector element index
                                int i = get_global_id(0);
                                v1[i] = v1[i] + v2[i];
                            }

                            __kernel void
                            doubleVectorSum(__global       double * v1,
                                            __global       double * v2)
                            {
                                // Vector element index
                                int i = get_global_id(0);
                                v1[i] = v1[i] + v2[i];
                            }

                            __kernel void
                            longVectorSum( __global       long * v1,
                                           __global       long * v2)
                            {
                                // Vector element index
                                int i = get_global_id(0);
                                v1[i] = v1[i] + v2[i];
                            }
                            ";
                    #endregion
                }
                #endregion
            }




            /// <summary>Discrete element modeling. Calculates derivatives of n particle-spring model into a 6n space-state
            /// system (positions, velocities, x,y,z each).</summary>
            public class floatDEM
            {
                #region OpenCL variables

                //Input
                /// <summary>Mass values (n)</summary>
                CLCalc.Program.Variable m;
                /// <summary>Original positions (3n)</summary>
                CLCalc.Program.Variable posOrig;
                /// <summary>Origins (L) origs[i] connects to dests[i]</summary>
                CLCalc.Program.Variable origs;
                /// <summary>Destinations (L)</summary>
                CLCalc.Program.Variable dests;
                /// <summary>Spring constants (L)</summary>
                CLCalc.Program.Variable k;
                /// <summary>Spring constants to ground (n)</summary>
                CLCalc.Program.Variable kGround;
                /// <summary>Damping (L)</summary>
                CLCalc.Program.Variable c;
                /// <summary>Damping to ground (n)</summary>
                CLCalc.Program.Variable cGround;

                //Output
                //Displaced positions read from state

                /// <summary>Number of Connections (1)</summary>
                CLCalc.Program.Variable nConnec;
                /// <summary>Initial distances (L)</summary>
                CLCalc.Program.Variable L0;
                /// <summary>Actuating forces (3*n)</summary>
                CLCalc.Program.Variable forces;
                /// <summary>Connection forces (L)</summary>
                CLCalc.Program.Variable connForces;
                /// <summary>Nodes connections (int, 20*n)</summary>
                CLCalc.Program.Variable nodesConnections;

                #endregion

                #region Kernels
                int[] nConn, nM;
                /// <summary>Initial lengths kernel. work_dim = 1, globalsize = n</summary>
                CLCalc.Program.Kernel KernelcalcL0;
                /// <summary>Initial length arguments</summary>
                CLCalc.Program.Variable[] argscalcL0;

                /// <summary>Reset forces kernel. work_dim = 1, globalsize = 3n</summary>
                CLCalc.Program.Kernel KernelresetForces;
                /// <summary>Reset forces arguments</summary>
                CLCalc.Program.Variable[] argsresetForces;

                /// <summary>Calculate forces kernel. work_dim = 1, globalsize = L</summary>
                CLCalc.Program.Kernel KernelcalcForces;
                /// <summary>Calculate forces arguments</summary>
                CLCalc.Program.Variable[] argscalcForces;

                /// <summary>Calculate forces kernel. work_dim = 1, globalsize = n</summary>
                CLCalc.Program.Kernel KernelcalcGroundForces;
                /// <summary>Calculate forces arguments</summary>
                CLCalc.Program.Variable[] argscalcGroundForces;


                /// <summary>Calculate forces kernel. work_dim = 1, globalsize = L</summary>
                CLCalc.Program.Kernel Kernelderivs;
                /// <summary>Calculate forces arguments</summary>
                CLCalc.Program.Variable[] argsderivs;

                /// <summary>Calculate nodes connections. work_dim = 1, globalsize = n</summary>
                CLCalc.Program.Kernel KernelcalcNodesConnections;
                /// <summary>Calculate nodes connections arguments</summary>
                CLCalc.Program.Variable[] argscalcNodesConnections;

                #endregion


                /// <summary>Constructor.</summary>
                /// <param name="nMasses">Number of masses in the system</param>
                /// <param name="nConnections">Number of connections</param>
                /// <param name="Masses">Mass of each vertex</param>
                /// <param name="InitialStateSpace">Position and velocity of vertexes 
                /// [2*3*i] - posx, [2*(3*i+1)] - posy, [2*(3*i+2)] - posz, 
                /// [1+2*3*i] - velx, [1+2*(3*i+1)] - vely, [1+2*(3*i+2)] - velz</param>
                /// <param name="Origins">Origin vertex of connections. Spring connects Origin[i] to Dests[i]</param>
                /// <param name="Dests">Destination vertex of connections. Spring connects Origin[i] to Dests[i]</param>
                /// <param name="SpringKs">Spring constant for each connection</param>
                /// <param name="GroundKs">Spring constant for each mass, connecting to ground (nMass)</param>
                /// <param name="Damp">Structural damping (relative-speed dependant) (nConnections)</param>
                /// <param name="GroundDamp">Absolute damping proportional to speed relative to Earth (nMass)</param>
                public floatDEM(int nMasses, int nConnections,
                    float[] Masses, float[] InitialStateSpace,
                    int[] Origins, int[] Dests, float[] SpringKs, float[] Damp, float[] GroundKs, float[] GroundDamp)
                {
                    #region Consistency check
                    if (Masses.Length != nMasses)
                        throw new Exception("Invalid Masses length (!=nMasses)");
                    if (InitialStateSpace.Length != 6 * nMasses)
                        throw new Exception("Invalid positions length (!=6*nMasses - x, y, z)");
                    if (Origins.Length != nConnections)
                        throw new Exception("Invalid Origins length (!=nConnections)");
                    if (Dests.Length != nConnections)
                        throw new Exception("Invalid Dests length (!=nConnections)");
                    if (SpringKs.Length != nConnections)
                        throw new Exception("Invalid SpringKs length (!=nConnections)");
                    if (GroundKs.Length != nMasses)
                        throw new Exception("Invalid GroundKs length (!=nMasses)");
                    if (Damp.Length != nConnections)
                        throw new Exception("Invalid Damp length (!=nConnections)");
                    if (GroundDamp.Length != nMasses)
                        throw new Exception("Invalid GroundDamp length (!=nMasses)");
                    #endregion

                    if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.Unknown)
                    {
                        CLCalc.InitCL();
                    }

                    #region Variables reading
                    //Sizes
                    nConn = new int[1] { nConnections };
                    nM = new int[1] { nMasses };

                    //Inputs
                    m = new Program.Variable(Masses);

                    float[] InitialPositions = new float[3 * nMasses];
                    for (int i = 0; i < 3 * nMasses; i++) InitialPositions[i] = InitialStateSpace[2 * i];

                    posOrig = new Program.Variable(InitialPositions);
                    origs = new Program.Variable(Origins);
                    dests = new Program.Variable(Dests);
                    k = new Program.Variable(SpringKs);
                    kGround = new Program.Variable(GroundKs);
                    c = new Program.Variable(Damp);
                    cGround = new Program.Variable(GroundDamp);

                    //Outputs
                    L0 = new Program.Variable(new float[nConnections]);
                    forces = new Program.Variable(new float[3 * nMasses]);
                    connForces = new Program.Variable(new float[3 * nConnections]);
                    nConnec = new Program.Variable(new int[1] { nConnections });

                    int[] nodesConnects = new int[30 * nMasses];
                    for (int i = 0; i < nodesConnects.Length; i++) nodesConnects[i] = -1;

                    nodesConnections = new Program.Variable(nodesConnects);
                    #endregion

                    #region Kernels initialization
                    DEMSource Source = new DEMSource();
                    string[] s = new string[] { Source.floatcalcL0, Source.floatresetForces, Source.floatcalcForces, Source.floatderivs, Source.floatcalcGroundForces, Source.floatcalcNodesConnections };
                    CLCalc.Program.Compile(s);

                    KernelcalcL0 = new Program.Kernel("floatcalcL0");
                    argscalcL0 = new Program.Variable[] { posOrig, origs, dests, L0 };

                    KernelcalcNodesConnections = new Program.Kernel("floatcalcNodesConnections");
                    argscalcNodesConnections = new Program.Variable[] { nodesConnections, nConnec, origs, dests };

                    KernelresetForces = new Program.Kernel("floatresetForces");
                    argsresetForces = new Program.Variable[] { forces };

                    KernelcalcForces = new Program.Kernel("floatcalcForces");
                    KernelcalcGroundForces = new Program.Kernel("floatcalcGroundForces");

                    Kernelderivs = new Program.Kernel("floatderivs");

                    #endregion

                    // Initial lengths calculation
                    KernelcalcL0.Execute(argscalcL0, nConn);

                    //Connections calculation
                    KernelcalcNodesConnections.Execute(argscalcNodesConnections, nM);

                    //nodesConnections.ReadFromDeviceTo(nodesConnects);
                }

                /// <summary>Calculates derivatives of deformable body space-state vector dydx[6n]. dydx[2i] - i-th position deriv, 
                /// dydx[2i+1] - ith velocity deriv</summary>
                public void derivs(Program.Variable x, Program.Variable y, Program.Variable dydx)
                {
                    //Reseta forcas
                    int[] nM3 = new int[1] { 3 * nM[0] };
                    KernelresetForces.Execute(argsresetForces, nM);

                    //Calcula forcas
                    argscalcForces = new Program.Variable[] { y, origs, dests, L0, k, c, connForces };
                    KernelcalcForces.Execute(argscalcForces, nConn);

                    //float[] varL0 = new float[nConn[0]];
                    //L0.ReadFromDeviceTo(varL0);

                    //float[] vary = new float[6 * nM[0]];
                    //y.ReadFromDeviceTo(vary);

                    //float[] varforces = new float[3 * nConn[0]];
                    //connForces.ReadFromDeviceTo(varforces);

                    //Calcula forcas de solo e compoe as internas
                    argscalcGroundForces = new Program.Variable[] { y, kGround, cGround, posOrig, forces, origs, dests, connForces, nConnec, nodesConnections };
                    KernelcalcGroundForces.Execute(argscalcGroundForces, nM);


                    //varforces = new float[3 * nM[0]];
                    //forces.ReadFromDeviceTo(varforces);

                    //Calcula derivadas
                    argsderivs = new Program.Variable[] { y, dydx, m, forces };
                    Kernelderivs.Execute(argsderivs, nM3);
                }


                #region OpenCL Source
                private class DEMSource
                {
                    #region Reset forces Kernel
                    /// <summary>Reset forces. Work_dim = 1, nmax = { nMasses }</summary>
                    public string floatresetForces = @"
                            __kernel void
                            floatresetForces(  __global       float * forces)
                            {
                                // Vector element index
                                int i = get_global_id(0);

                                forces[3*i] = 0;
                                forces[3*i+1] = 0;
                                forces[3*i+2] = 0;
                            }";
                    #endregion

                    #region Derivatives Kernel - 2nd Newton Law

                    /// <summary>Derivatives sketch. Work_dim = 1, nmax = { 3 * nMasses }</summary>
                    /// <param name="forces">Forces</param>
                    /// <param name="masses">Masses</param>
                    /// <param name="x">Independent variable</param>
                    /// <param name="y">State space vector</param>
                    /// <param name="dydx">Derivatives</param>
                    private void derivs(float[] x, float[] y, float[] dydx, float[] masses, float[] forces)
                    {
                        int max = masses.Length * 3;
                        for (int i = 0; i < max; i++)
                        {
                            //i = get_global_id(0);
                            dydx[2 * i] = y[2 * i + 1];
                            dydx[2 * i + 1] = forces[i] / masses[i / 3];
                        }
                    }
                    public string floatderivs = @"
                            __kernel void
                            floatderivs(  __global       float * y,
                                          __global       float * dydx,
                                          __global       float * masses,
                                          __global       float * forces)
                            {
                                // Vector element index
                                int i = get_global_id(0);

                                dydx[2 * i] = y[2 * i + 1];
                                dydx[2 * i + 1] = forces[i] / masses[i / 3];
                            }";
                    #endregion

                    #region Original distance Kernel

                    /// <summary>Initial L0 calculation. work_dim = 1, global_work_size[0]=nConnections</summary>
                    public string floatcalcL0 = @"
                            __kernel void
                            floatcalcL0(  __global       float * posOrig,
                                          __global       int * origs,
                                          __global       int * dests,
                                          __global       float * L0)
                            {
                                // Vector element index
                                int i = get_global_id(0);

                                float4 v1 = (float4)(posOrig[3 * origs[i]], posOrig[1 + 3 * origs[i]], posOrig[2 + 3 * origs[i]], 0);
                                float4 v2 = (float4)(posOrig[3 * dests[i]], posOrig[1 + 3 * dests[i]], posOrig[2 + 3 * dests[i]], 0);

                                L0[i] = distance(v1, v2);
                            }";
                    #endregion

                    #region Forces calculation Kernel

                    /// <summary>Forces calculation. Returns forces. Work_dim = 1, nmax = { nConnections }</summary>
                    public string floatcalcForces = @"
                            __kernel void
                            floatcalcForces(  __global       float * y,
                                              __global       int * origs,
                                              __global       int * dests,
                                              __global       float * L0,
                                              __global       float * k,
                                              __global       float * c,
                                              __global       float * connForces)
                            {
                                // Vector element index
                                int i = get_global_id(0);

                                //posicoes
                                float4 pos1 = (float4)
                                    (y[2 * (3 * origs[i])], y[2 * (1 + 3 * origs[i])], y[2 * (2 + 3 * origs[i])], 0);

                                float4 pos2 = (float4)
                                    (y[2 * (3 * dests[i])], y[2 * (1 + 3 * dests[i])], y[2 * (2 + 3 * dests[i])], 0);

                                //velocidades
                                float4 vel1 = (float4)
                                    (y[1 + 2 * (3 * origs[i])], y[1 + 2 * (1 + 3 * origs[i])], y[1 + 2 * (2 + 3 * origs[i])], 0);

                                float4 vel2 = (float4)
                                    (y[1 + 2 * (3 * dests[i])], y[1 + 2 * (1 + 3 * dests[i])], y[1 + 2 * (2 + 3 * dests[i])], 0);

                                float4 P12 = pos2 - pos1;
                                float delta = length(P12) - L0[i];

                                float f = k[i] * delta;

                                P12 = normalize(P12);

                                //forcas de amortecimento interno
                                float4 V12 = vel2 - vel1;
                                float deltaV = dot(V12, P12);
                                float fc = c[i] * deltaV;

                                f += fc;

                                //forcas de mola mais amortecimento interno
                                connForces[3 * i] = f * P12.x;
                                connForces[3 * i + 1] = f * P12.y;
                                connForces[3 * i + 2] = f * P12.z;

                            }";

                    /// <summary>Calculates forces to ground. w_dim=1, global_work_size = nMasses</summary>
                    public string floatcalcGroundForces = @"
                            __kernel void
                            floatcalcGroundForces(    __global       float * y,
                                                      __global       float * kGround,
                                                      __global       float * cGround,
                                                      __global       float * posOrig,
                                                      __global       float * forces,
                                                      __global       int * orig,
                                                      __global       int * dest,
                                                      __global       float * connForces,
                                                      __global       int * nConnec,
                                                      __global       int * nodesConnections)
                            {
                                // Vector element index
                                int i = get_global_id(0);
                                int n = get_global_size(0);

                                //posicoes
                                float4 p0 = (float4)
                                    (posOrig[3 * i], posOrig[3 * i + 1], posOrig[3 * i + 2], 0);

                                float4 pd = (float4)
                                    (y[6 * i], y[6 * i + 2], y[6 * i + 4], 0);

                                float4 pD0 = p0 - pd;

                                //forcas
                                forces[3 * i] += pD0.x * kGround[i];
                                forces[3 * i + 1] += pD0.y * kGround[i];
                                forces[3 * i + 2] += pD0.z * kGround[i];

                                //velocidade
                                float4 vel = (float4)
                                    (y[6 * i + 1], y[6 * i + 3], y[6 * i + 5], 0);

                                forces[3 * i] -= cGround[i] * vel.x;
                                forces[3 * i + 1] -= cGround[i] * vel.y;
                                forces[3 * i + 2] -= cGround[i] * vel.z;

                                //acumula forcas internas
                                int j = -1;
                                for (int k = 0; k < 30; k++)
                                {
                                    j = nodesConnections[k + 30 * i];
                                    
                                    if (j >= 0)
                                    {
                                        if (orig[j] == i)
                                        {
                                           forces[3*i]+=connForces[3*j];
                                           forces[3*i+1]+=connForces[3*j+1];
                                           forces[3*i+2]+=connForces[3*j+2];
                                        }
                                        else if (dest[j] == i)
                                        {
                                           forces[3*i]-=connForces[3*j];
                                           forces[3*i+1]-=connForces[3*j+1];
                                           forces[3*i+2]-=connForces[3*j+2];
                                        }
                                    }
                                    else 
                                    { 
                                       k = 30; 
                                    }
                                }

                            }";
                    #endregion

                    #region Nodes connections calculation
                    /// <summary>Calculates forces to ground. w_dim=1, global_work_size = nMasses</summary>
                    public string floatcalcNodesConnections = @"
                            __kernel void
                            floatcalcNodesConnections(__global       int * nodesConnections,
                                                      __global       int * nConnec,
                                                      __global       int * orig,
                                                      __global       int * dest)
                            {
                                // Vector element index
                                int i = get_global_id(0);

                                //Calcula conexoes
                                int k = 0;
                                for (int j = 0; j < nConnec[0]; j++)
                                {
                                    if (orig[j] == i || dest[j] == i)
                                    {
                                       nodesConnections[k + 30 * i] = j;
                                       k++;
                                       if (k == 30) j = nConnec[0];
                                    }
                                }

                            }";

                    #endregion
                }
                #endregion
            }

            /// <summary>Floating point particle system physics</summary>
            public class floatBodyPhysics
            {
                #region RK46 step implementation, for the future
                //                /// <summary>Motion Newton-law 1D solver. Kernel: rk46</summary>
                //                private string ODESolver = @"
                //
                //                            void derivs(float t, float x[], float dydx[], int n,
                //                                               __global __read_only float * forces,
                //                                               __global __read_only float * masses)
                //                            {
                //                                //t - Variavel independente
                //                                //x - Vetor de estados  x[0] - posicao, x[1] - velocidade
                //                                //dydx - Valor das derivadas
                //                                //n - numero da particula em questao
                //
                //                                dydx[0] = x[1];
                //                                dydx[1] = forces[n]/masses[n];
                //                            }
                //
                //
                //                            __kernel void rk46(__global       float * x,
                //                                               __global       float * stepsize,
                //                                               __global       float * forces,
                //                                               __global       float * masses,
                //                                               __global       float * pos,
                //                                               __global       float * vel)
                //                            {
                //                                // Vector element index
                //                                int n = get_global_id(0);
                //                                int nDerivs = 2;
                //
                //                                float k1[2], k2[2], k3[2], k4[2], k5[2], k6[2];
                //                                float ysav[2], y[2], dydx[2];
                //
                //                                y[0] = pos[n]; y[1] = vel[n];
                //
                //                                //Calcula derivadas
                //                                //Salva y original
                //                                for (int i = 0; i < nDerivs; i++) ysav[i] = y[i];
                //
                //                                //primeiro estágio
                //                                derivs(x[0], y, k1, n, forces, masses);
                //
                //                                //segundo estágio
                //                                for (int i = 0; i < nDerivs; i++) y[i] = ysav[i] + 0.5 * stepsize[0] * k1[i];
                //                                derivs(x[0] + 0.5 * stepsize[0], y, k2, n, forces, masses);
                //
                //                                //terceiro estágio
                //                                for (int i = 0; i < nDerivs; i++) y[i] = ysav[i] + 0.25 * stepsize[0] * k1[i] + 0.25 * stepsize[0] * k2[i];
                //                                derivs(x[0] + 0.5 * stepsize[0], y, k3, n, forces, masses);
                //
                //                                //quarto estágio
                //                                for (int i = 0; i < nDerivs; i++) y[i] = ysav[i] - stepsize[0] * k2[i] + 2 * stepsize[0] * k3[i];
                //                                derivs(x[0] + stepsize[0], y, k4, n, forces, masses);
                //
                //                                //quinto estágio
                //                                for (int i = 0; i < nDerivs; i++) y[i] = ysav[i] + stepsize[0] * 0.037037037037037 * (7 * k1[i] + 10 * k2[i] + k4[i]);
                //                                derivs(x[0] + 0.666666666666667 * stepsize[0], y, k5, n, forces, masses);
                //
                //                                //sexto estágio
                //                                for (int i = 0; i < nDerivs; i++) y[i] = ysav[i] + 0.0016 * stepsize[0] * (28 * k1[i] - 125 * k2[i] + 546 * k3[i] + 54 * k4[i] - 378 * k5[i]);
                //                                derivs(x[0] + 0.2 * stepsize[0], y, k6, n, forces, masses);
                //
                //                                //Compoe a resposta
                //                                float erroaux;
                //                                for (int i = 0; i < nDerivs; i++) 
                //                                {
                //                                    erroaux = stepsize[0] * 2.97619047619048E-03 * (-42 * k1[i] - 224 * k3[i] - 21 * k4[i] + 162 * k5[i] + 125 * k6[i]);
                //                                    y[i] = ysav[i] + stepsize[0] * 0.166666666666667 * (k1[i] + 4 * k3[i] + k4[i]) + erroaux;
                //                                }
                //
                //                                //Escreve os resultados
                //                                pos[n] = y[0]; vel[n] = y[1];
                //                                x[0] += stepsize[0];
                //                            }
                //
                //                            ";
                #endregion


                #region Constant acceleration model solver
                /// <summary>Motion Newton-law 1D solver. Kernel: rk46</summary>
                private string ConstAccelMotionEDOSolver = @"

                            __kernel void constAccelStep(__global       float * time,
                                                         __global       float * stepsize,
                                                         __global       float * forces,
                                                         __global       float * masses,
                                                         __global       float * pos,
                                                         __global       float * vel)
                            {
                                // Vector element index
                                int n = get_global_id(0);

                                float a = forces[n]/masses[n/3];

                                pos[n] += vel[n]*stepsize[0] + 0.5*a*stepsize[0]*stepsize[0];
                                vel[n] += a*stepsize[0];
                                time[0] += stepsize[0];
                            }
                            ";
                #endregion

                #region OpenCL Source code
                /// <summary>Force applier to particles. Kernels: ResetForces, ApplyGravity, FloorCollision</summary>
                private string ForceAppliers = @"

                            __kernel void ResetForces(__global       float * forces)
                            {
                                // Vector element index
                                int n = get_global_id(0);
                                forces[n] = 0;
                            }

                            //should be called with max[0]=[nParticles]
                            __kernel void ApplyGravity(__global       float * forces, 
                                                       __global       float * masses,
                                                       __global       float *g)
                            {
                                // Vector element index
                                int n = get_global_id(0);

                                forces[3*n] += g[0]*masses[n];
                                forces[3*n + 1] += g[1]*masses[n];
                                forces[3*n + 2] += g[2]*masses[n];
                            }

                            ";

                /// <summary>Collision applier to particles. Kernels: ResetForces, ApplyGravity, FloorCollision</summary>
                private string CollisionAppliers = @"

                            //should be called with max[0]=[nParticles]
                            __kernel void FloorCollision(__global       float * vel, 
                                                         __global       float * pos,
                                                         __global       float * collisionSizes)
                            {
                                // Vector element index
                                int n = get_global_id(0);

                                if (pos[3*n + 2] < collisionSizes[3*n + 2])
                                {
                                    vel[3*n + 2] = -vel[3*n + 2];
                                }
                            }

                            //should be called with max[0]=[3 * nParticles]
                            __kernel void WallCollision( __global       float * vel, 
                                                         __global       float * pos,
                                                         __global       float * collisionSizes)
                            {
                                // Vector element index
                                int n = get_global_id(0);

                                int nn=n/3;
                                if (pos[n] < -15 + collisionSizes[nn])
                                {
                                    vel[n] = -0.3f*vel[n];
                                    pos[n] = -15 + collisionSizes[nn];
                                }
                                else if (pos[n] > 15 - collisionSizes[nn])
                                {
                                    vel[n] = -0.3f*vel[n];
                                    pos[n] = 15 - collisionSizes[nn];
                                }
                            }
                            //should be called with max[0]=[nParticles], max[1]=[nParticles]
                            __kernel void ResetCloseNeighbors( __global       int * closeNeighbors)
                            {
                                // Vector element index
                                int n = get_global_id(0);
                                closeNeighbors[n]=0;
                            }

                            //should be called with max[0]=[nParticles], max[1]=[nParticles]
                            __kernel void SelfCollision( __global       float * vel, 
                                                         __global       float * pos,
                                                         __global       float * masses,
                                                         __global       float * forces,
                                                         __global       int * closeNeighbors,
                                                         __global       float * collisionSizes)
                            {
                                // Vector element index
                                int n = get_global_id(0);
                                int m = get_global_id(1);

                                if (n < m)
                                {
                                    float4 pos1 = (float4)(pos[3*n], pos[3*n+1], pos[3*n+2], 0.0f);
                                    float4 pos2 = (float4)(pos[3*m], pos[3*m+1], pos[3*m+2], 0.0f);
                                    float4 V12 = pos2 - pos1;
                                    float dist = dot(V12,V12);
                                    float colDist = (collisionSizes[n] + collisionSizes[m])*(collisionSizes[n] + collisionSizes[m]);
                                    if (dist < 1.7f * colDist) 
                                    {
                                        closeNeighbors[n]++; closeNeighbors[m]++;
                                    }

                                    if (dist < colDist)
                                    {
                                        dist = sqrt(dist);
                                        float4 vel1 = (float4)(vel[3*n], vel[3*n+1], vel[3*n+2], 0.0f);
                                        float4 vel2 = (float4)(vel[3*m], vel[3*m+1], vel[3*m+2], 0.0f);
                                        V12 = normalize(V12);

                                        //Componentes normais da velocidade
                                        float u10 = dot(vel1, V12);
                                        float u20 = dot(vel2, V12);

                                        //Novas componentes normais da velocidade
                                        float m1=masses[n]; float m2=masses[m]; float temp=1/(m1+m2);
                                        float u1f = ((m1-m2)*u10+2*m2*u20)*temp;
                                        float u2f = ((m2-m1)*u20+2*m1*u10)*temp;

                                        //remove componentes normais antigas e coloca as novas
                                        vel1 = vel1 - V12*(u10-0.99f*u1f);
                                        vel2 = vel2 - V12*(u20-0.99f*u2f);

                                        //desfaz colisao
                                        dist = collisionSizes[n] + collisionSizes[m] - dist;
                                        float peso1 = collisionSizes[n] /(collisionSizes[n] + collisionSizes[m]), peso2 = 1 - peso1;

                                        pos1 -= dist * peso1 * V12;
                                        pos2 += dist * peso2 * V12;

                                        //copia de volta os argumentos
                                        pos[3*n]=pos1.x;pos[3*n+1]=pos1.y;pos[3*n+2]=pos1.z;
                                        pos[3*m]=pos2.x;pos[3*m+1]=pos2.y;pos[3*m+2]=pos2.z;
                                        vel[3*n]=vel1.x;vel[3*n+1]=vel1.y;vel[3*n+2]=vel1.z;
                                        vel[3*m]=vel2.x;vel[3*m+1]=vel2.y;vel[3*m+2]=vel2.z;
                                    }
                                }
                            }


                            ";
                #endregion

                #region Variables
                //3*(n particles)
                private CLCalc.Program.Variable CL_pos;
                private CLCalc.Program.Variable CL_vel;
                private CLCalc.Program.Variable CL_forces;
                //N particles
                private CLCalc.Program.Variable CL_masses;
                private CLCalc.Program.Variable CL_collisionSizes;
                private CLCalc.Program.Variable CL_closeNeighbors;
                //Escalares
                private CLCalc.Program.Variable CL_t;
                private CLCalc.Program.Variable CL_step;
                private CLCalc.Program.Variable CL_g;
                #endregion



                /// <summary>Initializes physics program. Components indexes: [i] - x, [i+1] - y, [i+2] - z</summary>
                /// <param name="nParticles">Number of particles</param>
                public floatBodyPhysics(int nParticles)
                {
                    string[] s = new string[] { CollisionAppliers, ForceAppliers, ConstAccelMotionEDOSolver };
                    Program.Compile(s);
                    //Kernels
                    MotionStep = new Program.Kernel("constAccelStep");
                    Kernel_ApplyGravity = new Program.Kernel("ApplyGravity");
                    Kernel_FloorCollision = new Program.Kernel("FloorCollision");
                    Kernel_SelfCollision = new Program.Kernel("SelfCollision");
                    Kernel_WallCollision = new Program.Kernel("WallCollision");
                    Kernel_ResetForces = new Program.Kernel("ResetForces");
                    Kernel_ResetCloseNeighbors = new Program.Kernel("ResetCloseNeighbors");

                    float[] t = new float[1] { 0 };
                    float[] gg = new float[3] { 0, 0, 0 };
                    step = new float[1] { 0 };
                    //Tamanho de alocacao de velocidades e posicoes
                    float[] aloc = new float[nParticles * 3];

                    //Tamanho de alocacao de caracteristicas das particulas
                    float[] alocPart = new float[nParticles];
                    //3*Nparticulas
                    CL_pos = new CLCalc.Program.Variable(aloc);
                    CL_vel = new CLCalc.Program.Variable(aloc);
                    CL_forces = new CLCalc.Program.Variable(aloc);
                    //Nparticulas
                    closeNeighbors = new int[nParticles];
                    CL_closeNeighbors = new CLCalc.Program.Variable(closeNeighbors);
                    for (int i = 0; i < nParticles; i++) alocPart[i] = 1f; //inicializa massas como 1 e tamanhos de colisao como 1
                    CL_masses = new CLCalc.Program.Variable(alocPart);
                    CL_collisionSizes = new CLCalc.Program.Variable(alocPart);
                    
                    //escalares
                    CL_t = new CLCalc.Program.Variable(t);
                    CL_step = new CLCalc.Program.Variable(step);

                    //gravidade
                    CL_g = new CLCalc.Program.Variable(gg);

                    //Argumentos de funcoes
                    stepArgs = new CLCalc.Program.Variable[] { CL_t, CL_step, CL_forces, CL_masses, CL_pos, CL_vel };
                    applyGravArgs = new CLCalc.Program.Variable[] { CL_forces, CL_masses, CL_g };
                    floorCollisionArgs = new CLCalc.Program.Variable[] { CL_vel, CL_pos, CL_collisionSizes };
                    wallCollisionArgs = floorCollisionArgs;
                    selfCollisionArgs = new CLCalc.Program.Variable[] { CL_vel, CL_pos, CL_masses, CL_forces, CL_closeNeighbors, CL_collisionSizes };
                    resetForcesArgs = new Program.Variable[] { CL_forces };
                    resetCloseNeighborsArgs = new Program.Variable[] { CL_closeNeighbors };

                    nArgs = new int[1] { nParticles * 3 };
                    nPartics = new int[1] { nParticles };
                    nPartics2 = new int[2] { nParticles, nParticles };
                }

                int[] nArgs, nPartics, nPartics2;


                /// <summary>Sets particles parameters</summary>
                /// <param name="pos">Positions (3*numParticles)</param>
                /// <param name="vel">Speeds (3*numParticles)</param>
                /// <param name="mass">Masses (numParticles)</param>
                /// <param name="collisionSizes">Collision sizes (numParticles)</param>
                public void SetParams(float[] pos, float[] vel, float[] mass, float[] collisionSizes)
                {
                    CL_pos.WriteToDevice(pos);
                    CL_vel.WriteToDevice(vel);
                    CL_masses.WriteToDevice(mass);
                    CL_collisionSizes.WriteToDevice(collisionSizes);
                }

                /// <summary>Gets particles positions</summary>
                public float[] GetPositions()
                {
                    float[] pos = new float[nArgs[0]];
                    CL_pos.ReadFromDeviceTo(pos);
                    return pos;
                }

                private int[] closeNeighbors;
                /// <summary>Gets how many close neighbors a particle has. Use this to avoid drawing unnecessary particles</summary>
                public int[] GetCloseNeighbors()
                {
                    CL_closeNeighbors.ReadFromDeviceTo(closeNeighbors);
                    return closeNeighbors;
                }

                /// <summary>Gets simulation time</summary>
                public float GetTime()
                {
                    float[] t = new float[1];
                    CL_t.ReadFromDeviceTo(t);
                    return t[0];
                }

                #region Comandos do programa

                /// <summary>Stepsize</summary>
                private float[] step;

                /// <summary>Executes an integration step</summary>
                private CLCalc.Program.Kernel MotionStep;
                /// <summary>Motion step arguments</summary>
                private CLCalc.Program.Variable[] stepArgs;
                /// <summary>Takes an integration step</summary>
                /// <param name="stepSize">Step size</param>
                public void Step(float stepSize)
                {
                    step[0] = stepSize;
                    CL_step.WriteToDevice(step);
                    MotionStep.Execute(stepArgs, nArgs);

                    if (EnableFloorCollision) Kernel_FloorCollision.Execute(floorCollisionArgs, nPartics);
                    if (EnableSelfCollision)
                    {
                        Kernel_ResetForces.Execute(resetForcesArgs, nArgs);
                        Kernel_ApplyGravity.Execute(applyGravArgs, nPartics);

                        Kernel_ResetCloseNeighbors.Execute(resetCloseNeighborsArgs, nPartics);
                        Kernel_SelfCollision.Execute(selfCollisionArgs, nPartics2);
                    }
                    if (EnableWallCollision) Kernel_WallCollision.Execute(wallCollisionArgs, nArgs);
                }


                /// <summary>Applies gravity</summary>
                private CLCalc.Program.Kernel Kernel_ApplyGravity;
                /// <summary>Apply gravity arguments</summary>
                private CLCalc.Program.Variable[] applyGravArgs;
                /// <summary>Applies gravity force.</summary>
                /// <param name="value">Gravity force. Remember to use negative for down direction.</param>
                public void ApplyGravity(float[] value)
                {
                    CL_g.WriteToDevice(value);
                    Kernel_ApplyGravity.Execute(applyGravArgs, nPartics);
                }

                /// <summary>Clear forces</summary>
                private CLCalc.Program.Kernel Kernel_ResetForces;
                /// <summary>Apply gravity arguments</summary>
                private CLCalc.Program.Variable[] resetForcesArgs;
                /// <summary>Clears forces</summary>
                public void ResetForces()
                {
                    Kernel_ResetForces.Execute(resetForcesArgs, nArgs);
                }

                /// <summary>Floor collision</summary>
                private CLCalc.Program.Kernel Kernel_FloorCollision;
                /// <summary>Apply floor collision arguments</summary>
                private CLCalc.Program.Variable[] floorCollisionArgs;
                /// <summary>Applies floor collision?</summary>
                public bool EnableFloorCollision = false;

                /// <summary>Wall collision</summary>
                private CLCalc.Program.Kernel Kernel_WallCollision;
                /// <summary>Apply floor collision arguments</summary>
                private CLCalc.Program.Variable[] wallCollisionArgs;
                /// <summary>Applies floor collision?</summary>
                public bool EnableWallCollision = true;

                /// <summary>Self collision</summary>
                private CLCalc.Program.Kernel Kernel_SelfCollision;
                /// <summary>Apply self collision arguments</summary>
                private CLCalc.Program.Variable[] selfCollisionArgs;
                /// <summary>Applies self collision?</summary>
                public bool EnableSelfCollision = true;

                /// <summary>Reset close neighbors</summary>
                private CLCalc.Program.Kernel Kernel_ResetCloseNeighbors;
                /// <summary>Apply self collision arguments</summary>
                private CLCalc.Program.Variable[] resetCloseNeighborsArgs;

                #endregion


            }

        }
    }
}

