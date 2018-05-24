using System;
using System.Collections.Generic;
using OpenCLTemplate;
using System.Text;

namespace OpenCLTemplate.Isosurface
{

    /// <summary>Marching cubes algorithm for isosurface reconstruction</summary>
    public class MarchingCubes
    {
        /// <summary>Compute normals of faces?</summary>
        public bool ComputeNormals = true;

        #region Grid information
        /// <summary>X, y and z increments. step[0] = deltaX, step[1] = deltaY, step[2] = deltaZ</summary>
        private float[] step = new float[] { 1f, 1f, 1f };

        /// <summary>X, y and z initial values. initVals[0] = x0, initVals[1] = y0, initVals[2] = z0</summary>
        private float[] initVals = new float[] { 0f, 0f, 0f };

        /// <summary>Gets or sets x, y and z increments. Increments[0] = deltaX, Increments[1] = deltaY, Increments[2] = deltaZ</summary>
        public float[] Increments
        {
            get
            {
                float[] resp = new float[] { step[0], step[1], step[2] };
                return resp;
            }
            set
            {
                step[0] = value[0]; step[1] = value[1]; step[2] = value[2];
                varStep.WriteToDevice(step);
            }
        }

        /// <summary>Gets or sets x, y and z intial values. InitValues[0] = x0, InitValues[1] = y0, InitValues[2] = z0</summary>
        public float[] InitValues
        {
            get
            {
                float[] resp = new float[] { initVals[0], initVals[1], initVals[2] };
                return initVals;
            }
            set
            {
                initVals[0] = value[0]; initVals[1] = value[1]; initVals[2] = value[2];
                varInitVals.WriteToDevice(initVals);
            }
        }

        /// <summary>Gets or sets current isolevel</summary>
        public float IsoLevel
        {
            get
            {
                return isoLevel[0];
            }
            set
            {
                isoLevel[0] = value;
                varIsoLevel.WriteToDevice(isoLevel);
            }
        }

        #endregion

        #region Geometry data

        /// <summary>Isolevel to look for</summary>
        private float[] isoLevel;

        /// <summary>Values of the function</summary>
        private float[] funcVals;

        /// <summary>Length of each dimension - max[0] = maxX, max[1]=maxY, max[2]=maxZ</summary>
        private int[] max;

        /// <summary>Edge coordinates.</summary>
        private float[] edgeCoords;
        /// <summary>Edge normals.</summary>
        private float[] edgeNormals;
        /// <summary>Edge coordinates.</summary>
        private float[] edgePrelimNormals;

        /// <summary>Element index to build triangles</summary>
        private int[] elementIndex;

        /// <summary>CL variable isolevel</summary>
        private CLCalc.Program.Variable varIsoLevel;

        /// <summary>OpenCL variable that stores function values. F(x,y,z) = CLFuncVals[x+maxX*y+maxX*maxY*z]</summary>
        public CLCalc.Program.Variable CLFuncVals;

        /// <summary>CL Edge coordinates. Geometry data compatible with OpenGL</summary>
        private CLCalc.Program.Variable varEdgeCoords;
        /// <summary>CL Edge normals. Geometry data compatible with OpenGL</summary>
        private CLCalc.Program.Variable varEdgeNormals;
        /// <summary>CL Element index array. Geometry data compatible with OpenGL</summary>
        private CLCalc.Program.Variable varElemIndex;

        /// <summary>Auxiliary/preliminary normals</summary>
        private CLCalc.Program.Variable varEdgePrelimNormals;

        /// <summary>OpenCL x, y and z step sizes</summary>
        private CLCalc.Program.Variable varStep;
        /// <summary>OpenCL x, y and z initial values within grid</summary>
        private CLCalc.Program.Variable varInitVals;

        #endregion

        #region OpenCL Kernels

        /// <summary>Kernel to interpolate points</summary>
        CLCalc.Program.Kernel kernelInterpPts;
        CLCalc.Program.Kernel kernelPolygonize;
        CLCalc.Program.Kernel kernelSmoothNormals;
        CLCalc.Program.Kernel kernelPolygonizeNoNormals;

        #endregion


        /// <summary>Creates a new isosurface calculator. You may pass variables created from a OpenGL context to the CL variables if you are using interop or NULL
        /// if not using OpenCL/GL interop.</summary>
        /// <param name="FuncValues">Values of the evaluated 3D function f(x,y,z). FuncValues=float[maxX,maxY,maxZ]</param>
        public MarchingCubes(float[, ,] FuncValues)
        {
            InitMarchingCubes(FuncValues, null, null, null);
        }

        /// <summary>Creates a new isosurface calculator. You may pass variables created from a OpenGL context to the CL variables if you are using interop or NULL
        /// if not using OpenCL/GL interop.</summary>
        /// <param name="FuncValues">Values of the evaluated 3D function f(x,y,z). FuncValues=float[maxX,maxY,maxZ]</param>
        /// <param name="CLEdgeCoords">OpenCL variable (float) to hold edge coordinates. Dimension has to be 9 * maxX * maxY * maxZ</param>
        /// <param name="CLEdgeNormals">OpenCL variable (float) to hold edge normals. Dimension has to be 9 * maxX * maxY * maxZ</param>
        /// <param name="CLElementArrayIndex">OpenCL variable (int) to hold element array index. Dimension has to be 5 * 3 * (maxX - 1) * (maxY - 1) * (maxZ - 1)</param>
        public MarchingCubes(float[, ,] FuncValues, CLCalc.Program.Variable CLEdgeCoords, CLCalc.Program.Variable CLEdgeNormals, CLCalc.Program.Variable CLElementArrayIndex)
        {
            InitMarchingCubes(FuncValues, CLEdgeCoords, CLEdgeNormals, CLElementArrayIndex);
        }

        /// <summary>Creates a new isosurface calculator. You may pass variables created from a OpenGL context to the CL variables if you are using interop or NULL
        /// if not using OpenCL/GL interop.</summary>
        /// <param name="FuncValues">Values of the evaluated 3D function f(x,y,z). FuncValues=float[maxX,maxY,maxZ]</param>
        /// <param name="CLEdgeCoords">OpenCL variable (float) to hold edge coordinates. Dimension has to be 9 * maxX * maxY * maxZ</param>
        /// <param name="CLEdgeNormals">OpenCL variable (float) to hold edge normals. Dimension has to be 9 * maxX * maxY * maxZ</param>
        /// <param name="CLElementArrayIndex">OpenCL variable (int) to hold element array index. Dimension has to be 5 * 3 * (maxX - 1) * (maxY - 1) * (maxZ - 1)</param>
        private void InitMarchingCubes(float[, ,] FuncValues, CLCalc.Program.Variable CLEdgeCoords, CLCalc.Program.Variable CLEdgeNormals, CLCalc.Program.Variable CLElementArrayIndex)
        {
            if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.Unknown) CLCalc.InitCL();

            if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.UsingCL)
            {
                //Reads maximum lengths
                int maxX = FuncValues.GetLength(0);
                int maxY = FuncValues.GetLength(1);
                int maxZ = FuncValues.GetLength(2);
                max = new int[] { maxX, maxY, maxZ };

                #region Creating variables

                //Isolevel
                isoLevel = new float[1] { 1.32746E-5f };
                varIsoLevel = new CLCalc.Program.Variable(isoLevel);

                //Step size and x0,y0,z0
                varStep = new CLCalc.Program.Variable(step);
                varInitVals = new CLCalc.Program.Variable(initVals);

                //Create and copy function values
                funcVals = new float[maxX * maxY * maxZ];
                CLFuncVals = new CLCalc.Program.Variable(funcVals);
                SetFuncVals(FuncValues);

                //Edge coordinates - 3 coords * 3 possible directions * number of points
                edgeCoords = new float[9 * maxX * maxY * maxZ];
                if (CLEdgeCoords != null)
                {
                    varEdgeCoords = CLEdgeCoords;
                    varEdgeCoords.WriteToDevice(edgeCoords);
                }
                else varEdgeCoords = new CLCalc.Program.Variable(edgeCoords);

                //4 preliminary normals per edge - has to be averaged afterwards
                edgePrelimNormals = new float[36 * maxX * maxY * maxZ];
                varEdgePrelimNormals = new CLCalc.Program.Variable(edgePrelimNormals);

                //Edge normals
                edgeNormals = new float[9 * maxX * maxY * maxZ];
                if (CLEdgeNormals != null)
                {
                    varEdgeNormals = CLEdgeNormals;
                    varEdgeNormals.WriteToDevice(edgeNormals);
                }
                else varEdgeNormals = new CLCalc.Program.Variable(edgeNormals);

                //Number of cubes: (maxX-1)*(maxY-1)*(maxZ-1)
                //Marching cube algorithm: each cube can have 5 triangles drawn, 3 vertexes per triangle
                //q-th vertex of p-th triangle of the ijk-th cube: [(5*(i+(maxX-1)*j+k*(maxX-1)*(maxY-1))+p)*3+q]
                elementIndex = new int[5 * 3 * (maxX - 1) * (maxY - 1) * (maxZ - 1)];
                if (CLElementArrayIndex != null)
                {
                    varElemIndex = CLElementArrayIndex;
                    varElemIndex.WriteToDevice(elementIndex);
                }
                else varElemIndex = new CLCalc.Program.Variable(elementIndex);

                //Edge remapping to build output
                edges = new int[edgeCoords.Length / 3];
                for (int i = 0; i < edges.Length; i++) edges[i] = -1;

                #endregion

                #region Compile code and create kernels

                CLMarchingCubesSrc cmsrc = new CLMarchingCubesSrc();

                CLCalc.Program.Compile(new string[] { cmsrc.definitions, cmsrc.src });
                kernelInterpPts = new CLCalc.Program.Kernel("interpPts");
                kernelPolygonize = new CLCalc.Program.Kernel("Polygonize");
                kernelSmoothNormals = new CLCalc.Program.Kernel("SmoothNormals");
                kernelPolygonizeNoNormals = new CLCalc.Program.Kernel("PolygonizeNoNormals");
                #endregion
            }
            else throw new Exception("OpenCL not available");
        }

        /// <summary>Sets function values</summary>
        /// <param name="FuncVals">Values to set</param>
        public void SetFuncVals(float[, ,] FuncVals)
        {
            //Reads maximum lengths
            int maxX = FuncVals.GetLength(0);
            int maxY = FuncVals.GetLength(1);
            int maxZ = FuncVals.GetLength(2);
            if (max[0] != maxX || max[1] != maxY || max[2] != maxZ) throw new Exception("Invalid FuncVals dimension");

            for (int x = 0; x < maxX; x++)
                for (int y = 0; y < maxY; y++)
                    for (int z = 0; z < maxZ; z++)
                        funcVals[x + maxX * y + maxX * maxY * z] = FuncVals[x, y, z];

            CLFuncVals.WriteToDevice(funcVals);
        }

        /// <summary>Calculates isosurface corresponding to a given isolevel</summary>
        /// <param name="isoLvl"></param>
        public void CalcIsoSurface(float isoLvl)
        {
            //Copies iso level to video memory
            if (isoLvl != isoLevel[0])
            {
                isoLevel[0] = isoLvl;
                varIsoLevel.WriteToDevice(isoLevel);
            }

            //Interpolation
            CLCalc.Program.Variable[] args = new CLCalc.Program.Variable[] { CLFuncVals, varIsoLevel, varEdgeCoords, varInitVals, varStep };
            kernelInterpPts.Execute(args, max);

            if (ComputeNormals)
            {
                //Polygonization
                args = new CLCalc.Program.Variable[] { CLFuncVals, varIsoLevel, varEdgeCoords, varEdgePrelimNormals, varElemIndex };
                int[] GlobalWorkSize = new int[] { max[0] - 1, max[1] - 1, max[2] - 1 };
                kernelPolygonize.Execute(args, GlobalWorkSize);

                //Normal smoothing
                args = new CLCalc.Program.Variable[] { varEdgePrelimNormals, varEdgeNormals };
                kernelSmoothNormals.Execute(args, max);
            }
            else
            {
                //Polygonization
                args = new CLCalc.Program.Variable[] { CLFuncVals, varIsoLevel, varEdgeCoords, varElemIndex };
                int[] GlobalWorkSize = new int[] { max[0] - 1, max[1] - 1, max[2] - 1 };
                kernelPolygonizeNoNormals.Execute(args, GlobalWorkSize);
            }
        }

        /// <summary>Remaps edge coordinates so that ElemArray[ edges[elementIndex[i]] ] points to the same coordinates as elementIndex[i]</summary>
        int[] edges;

        /// <summary>Retrieves edge information. Can be used to draw marching cubes geometry using OpenGL</summary>
        /// <param name="EdgeCoords">Edge vertexes coordinates</param>
        /// <param name="EdgeNormals">Edge vertexes normal vectors</param>
        /// <param name="ElemArray">Element index array (triangles)</param>
        public void GetEdgeInfo(out List<float> EdgeCoords, out List<float> EdgeNormals, out List<int> ElemArray)
        {
            //Read polygons calculated with OpenCL
            varEdgeCoords.ReadFromDeviceTo(edgeCoords);
            varEdgeNormals.ReadFromDeviceTo(edgeNormals);
            varElemIndex.ReadFromDeviceTo(elementIndex);

            //Recalculate data
            ElemArray = new List<int>();
            EdgeCoords = new List<float>();
            EdgeNormals = new List<float>();

            //*********
            //Remaps edge coordinates so that ElemArray[ edges[elementIndex[i]] ] points to the same coordinates as elementIndex[i]

            //for (int i = 0; i < edges.Length; i++) edges[i] = -1;
            List<int> EraseRemapList = new List<int>();
            //*********

            //Rebuild OpenGL buffer objects
            int ind = 0;
            int qtdVerts = 0;
            for (int i = 0; i < elementIndex.Length; i += 3)
            {
                if (elementIndex[i] >= 0)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (edges[elementIndex[i + j]] == -1)
                        {
                            ind = 3 * elementIndex[i + j];
                            //vertexes and normals
                            EdgeCoords.Add(edgeCoords[ind]); EdgeCoords.Add(edgeCoords[ind + 1]); EdgeCoords.Add(edgeCoords[ind + 2]);
                            EdgeNormals.Add(edgeNormals[ind]); EdgeNormals.Add(edgeNormals[ind + 1]); EdgeNormals.Add(edgeNormals[ind + 2]);

                            edges[elementIndex[i + j]] = qtdVerts;
                            //Saves to erase later
                            EraseRemapList.Add(elementIndex[i + j]);
                            qtdVerts++;
                        }

                        //index of vertex
                        ElemArray.Add(edges[elementIndex[i + j]]);
                    }
                }
            }

            //Erase list
            for (int i = 0; i < EraseRemapList.Count; i++)
            {
                edges[EraseRemapList[i]] = -1;
            }
        }

        private class CLMarchingCubesSrc
        {

            #region References

            //Lorensen, W.E. and Cline, H.E.,
            //Marching Cubes: a high resolution 3D surface reconstruction algorithm,
            //Computer Graphics, Vol. 21, No. 4, pp 163-169 (Proc. of SIGGRAPH), 1987.


            //Watt, A., and Watt, M., 
            //Advanced Animation and Rendering Techniques,
            //Addison-Wesley, 1992.

            //Polygonising a scalar field
            //http://local.wasp.uwa.edu.au/~pbourke/geometry/polygonise/ access 19 jun 2010
            //Written by Paul Bourke May 1994 
            #endregion

            #region Lookup tables
            public string definitions = @"

__constant int edgeTable[256]={
0x0  , 0x109, 0x203, 0x30a, 0x406, 0x50f, 0x605, 0x70c,
0x80c, 0x905, 0xa0f, 0xb06, 0xc0a, 0xd03, 0xe09, 0xf00,
0x190, 0x99 , 0x393, 0x29a, 0x596, 0x49f, 0x795, 0x69c,
0x99c, 0x895, 0xb9f, 0xa96, 0xd9a, 0xc93, 0xf99, 0xe90,
0x230, 0x339, 0x33 , 0x13a, 0x636, 0x73f, 0x435, 0x53c,
0xa3c, 0xb35, 0x83f, 0x936, 0xe3a, 0xf33, 0xc39, 0xd30,
0x3a0, 0x2a9, 0x1a3, 0xaa , 0x7a6, 0x6af, 0x5a5, 0x4ac,
0xbac, 0xaa5, 0x9af, 0x8a6, 0xfaa, 0xea3, 0xda9, 0xca0,
0x460, 0x569, 0x663, 0x76a, 0x66 , 0x16f, 0x265, 0x36c,
0xc6c, 0xd65, 0xe6f, 0xf66, 0x86a, 0x963, 0xa69, 0xb60,
0x5f0, 0x4f9, 0x7f3, 0x6fa, 0x1f6, 0xff , 0x3f5, 0x2fc,
0xdfc, 0xcf5, 0xfff, 0xef6, 0x9fa, 0x8f3, 0xbf9, 0xaf0,
0x650, 0x759, 0x453, 0x55a, 0x256, 0x35f, 0x55 , 0x15c,
0xe5c, 0xf55, 0xc5f, 0xd56, 0xa5a, 0xb53, 0x859, 0x950,
0x7c0, 0x6c9, 0x5c3, 0x4ca, 0x3c6, 0x2cf, 0x1c5, 0xcc ,
0xfcc, 0xec5, 0xdcf, 0xcc6, 0xbca, 0xac3, 0x9c9, 0x8c0,
0x8c0, 0x9c9, 0xac3, 0xbca, 0xcc6, 0xdcf, 0xec5, 0xfcc,
0xcc , 0x1c5, 0x2cf, 0x3c6, 0x4ca, 0x5c3, 0x6c9, 0x7c0,
0x950, 0x859, 0xb53, 0xa5a, 0xd56, 0xc5f, 0xf55, 0xe5c,
0x15c, 0x55 , 0x35f, 0x256, 0x55a, 0x453, 0x759, 0x650,
0xaf0, 0xbf9, 0x8f3, 0x9fa, 0xef6, 0xfff, 0xcf5, 0xdfc,
0x2fc, 0x3f5, 0xff , 0x1f6, 0x6fa, 0x7f3, 0x4f9, 0x5f0,
0xb60, 0xa69, 0x963, 0x86a, 0xf66, 0xe6f, 0xd65, 0xc6c,
0x36c, 0x265, 0x16f, 0x66 , 0x76a, 0x663, 0x569, 0x460,
0xca0, 0xda9, 0xea3, 0xfaa, 0x8a6, 0x9af, 0xaa5, 0xbac,
0x4ac, 0x5a5, 0x6af, 0x7a6, 0xaa , 0x1a3, 0x2a9, 0x3a0,
0xd30, 0xc39, 0xf33, 0xe3a, 0x936, 0x83f, 0xb35, 0xa3c,
0x53c, 0x435, 0x73f, 0x636, 0x13a, 0x33 , 0x339, 0x230,
0xe90, 0xf99, 0xc93, 0xd9a, 0xa96, 0xb9f, 0x895, 0x99c,
0x69c, 0x795, 0x49f, 0x596, 0x29a, 0x393, 0x99 , 0x190,
0xf00, 0xe09, 0xd03, 0xc0a, 0xb06, 0xa0f, 0x905, 0x80c,
0x70c, 0x605, 0x50f, 0x406, 0x30a, 0x203, 0x109, 0x0   };


__constant int triTable[256][16] =
{{-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 1, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 8, 3, 9, 8, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 3, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{9, 2, 10, 0, 2, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{2, 8, 3, 2, 10, 8, 10, 9, 8, -1, -1, -1, -1, -1, -1, -1},
{3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 11, 2, 8, 11, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 9, 0, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 11, 2, 1, 9, 11, 9, 8, 11, -1, -1, -1, -1, -1, -1, -1},
{3, 10, 1, 11, 10, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 10, 1, 0, 8, 10, 8, 11, 10, -1, -1, -1, -1, -1, -1, -1},
{3, 9, 0, 3, 11, 9, 11, 10, 9, -1, -1, -1, -1, -1, -1, -1},
{9, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 3, 0, 7, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 1, 9, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 1, 9, 4, 7, 1, 7, 3, 1, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 10, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{3, 4, 7, 3, 0, 4, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1},
{9, 2, 10, 9, 0, 2, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1},
{2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4, -1, -1, -1, -1},
{8, 4, 7, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{11, 4, 7, 11, 2, 4, 2, 0, 4, -1, -1, -1, -1, -1, -1, -1},
{9, 0, 1, 8, 4, 7, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1},
{4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1, -1, -1, -1, -1},
{3, 10, 1, 3, 11, 10, 7, 8, 4, -1, -1, -1, -1, -1, -1, -1},
{1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4, -1, -1, -1, -1},
{4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3, -1, -1, -1, -1},
{4, 7, 11, 4, 11, 9, 9, 11, 10, -1, -1, -1, -1, -1, -1, -1},
{9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{9, 5, 4, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 5, 4, 1, 5, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{8, 5, 4, 8, 3, 5, 3, 1, 5, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 10, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{3, 0, 8, 1, 2, 10, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1},
{5, 2, 10, 5, 4, 2, 4, 0, 2, -1, -1, -1, -1, -1, -1, -1},
{2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8, -1, -1, -1, -1},
{9, 5, 4, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 11, 2, 0, 8, 11, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1},
{0, 5, 4, 0, 1, 5, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1},
{2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5, -1, -1, -1, -1},
{10, 3, 11, 10, 1, 3, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1},
{4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10, -1, -1, -1, -1},
{5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3, -1, -1, -1, -1},
{5, 4, 8, 5, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1},
{9, 7, 8, 5, 7, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{9, 3, 0, 9, 5, 3, 5, 7, 3, -1, -1, -1, -1, -1, -1, -1},
{0, 7, 8, 0, 1, 7, 1, 5, 7, -1, -1, -1, -1, -1, -1, -1},
{1, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{9, 7, 8, 9, 5, 7, 10, 1, 2, -1, -1, -1, -1, -1, -1, -1},
{10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3, -1, -1, -1, -1},
{8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2, -1, -1, -1, -1},
{2, 10, 5, 2, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1},
{7, 9, 5, 7, 8, 9, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1},
{9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11, -1, -1, -1, -1},
{2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7, -1, -1, -1, -1},
{11, 2, 1, 11, 1, 7, 7, 1, 5, -1, -1, -1, -1, -1, -1, -1},
{9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11, -1, -1, -1, -1},
{5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0, -1},
{11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0, -1},
{11, 10, 5, 7, 11, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 3, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{9, 0, 1, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 8, 3, 1, 9, 8, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1},
{1, 6, 5, 2, 6, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 6, 5, 1, 2, 6, 3, 0, 8, -1, -1, -1, -1, -1, -1, -1},
{9, 6, 5, 9, 0, 6, 0, 2, 6, -1, -1, -1, -1, -1, -1, -1},
{5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8, -1, -1, -1, -1},
{2, 3, 11, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{11, 0, 8, 11, 2, 0, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1},
{0, 1, 9, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1},
{5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11, -1, -1, -1, -1},
{6, 3, 11, 6, 5, 3, 5, 1, 3, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6, -1, -1, -1, -1},
{3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9, -1, -1, -1, -1},
{6, 5, 9, 6, 9, 11, 11, 9, 8, -1, -1, -1, -1, -1, -1, -1},
{5, 10, 6, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 3, 0, 4, 7, 3, 6, 5, 10, -1, -1, -1, -1, -1, -1, -1},
{1, 9, 0, 5, 10, 6, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1},
{10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4, -1, -1, -1, -1},
{6, 1, 2, 6, 5, 1, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7, -1, -1, -1, -1},
{8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6, -1, -1, -1, -1},
{7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9, -1},
{3, 11, 2, 7, 8, 4, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1},
{5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11, -1, -1, -1, -1},
{0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1},
{9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6, -1},
{8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6, -1, -1, -1, -1},
{5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11, -1},
{0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7, -1},
{6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9, -1, -1, -1, -1},
{10, 4, 9, 6, 4, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 10, 6, 4, 9, 10, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1},
{10, 0, 1, 10, 6, 0, 6, 4, 0, -1, -1, -1, -1, -1, -1, -1},
{8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10, -1, -1, -1, -1},
{1, 4, 9, 1, 2, 4, 2, 6, 4, -1, -1, -1, -1, -1, -1, -1},
{3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4, -1, -1, -1, -1},
{0, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{8, 3, 2, 8, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1},
{10, 4, 9, 10, 6, 4, 11, 2, 3, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6, -1, -1, -1, -1},
{3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10, -1, -1, -1, -1},
{6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1, -1},
{9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3, -1, -1, -1, -1},
{8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1, -1},
{3, 11, 6, 3, 6, 0, 0, 6, 4, -1, -1, -1, -1, -1, -1, -1},
{6, 4, 8, 11, 6, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{7, 10, 6, 7, 8, 10, 8, 9, 10, -1, -1, -1, -1, -1, -1, -1},
{0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10, -1, -1, -1, -1},
{10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0, -1, -1, -1, -1},
{10, 6, 7, 10, 7, 1, 1, 7, 3, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7, -1, -1, -1, -1},
{2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9, -1},
{7, 8, 0, 7, 0, 6, 6, 0, 2, -1, -1, -1, -1, -1, -1, -1},
{7, 3, 2, 6, 7, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7, -1, -1, -1, -1},
{2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7, -1},
{1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11, -1},
{11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1, -1, -1, -1, -1},
{8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6, -1},
{0, 9, 1, 11, 6, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0, -1, -1, -1, -1},
{7, 11, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{3, 0, 8, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 1, 9, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{8, 1, 9, 8, 3, 1, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1},
{10, 1, 2, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 10, 3, 0, 8, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1},
{2, 9, 0, 2, 10, 9, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1},
{6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8, -1, -1, -1, -1},
{7, 2, 3, 6, 2, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{7, 0, 8, 7, 6, 0, 6, 2, 0, -1, -1, -1, -1, -1, -1, -1},
{2, 7, 6, 2, 3, 7, 0, 1, 9, -1, -1, -1, -1, -1, -1, -1},
{1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6, -1, -1, -1, -1},
{10, 7, 6, 10, 1, 7, 1, 3, 7, -1, -1, -1, -1, -1, -1, -1},
{10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8, -1, -1, -1, -1},
{0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7, -1, -1, -1, -1},
{7, 6, 10, 7, 10, 8, 8, 10, 9, -1, -1, -1, -1, -1, -1, -1},
{6, 8, 4, 11, 8, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{3, 6, 11, 3, 0, 6, 0, 4, 6, -1, -1, -1, -1, -1, -1, -1},
{8, 6, 11, 8, 4, 6, 9, 0, 1, -1, -1, -1, -1, -1, -1, -1},
{9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6, -1, -1, -1, -1},
{6, 8, 4, 6, 11, 8, 2, 10, 1, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6, -1, -1, -1, -1},
{4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9, -1, -1, -1, -1},
{10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3, -1},
{8, 2, 3, 8, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1},
{0, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8, -1, -1, -1, -1},
{1, 9, 4, 1, 4, 2, 2, 4, 6, -1, -1, -1, -1, -1, -1, -1},
{8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1, -1, -1, -1, -1},
{10, 1, 0, 10, 0, 6, 6, 0, 4, -1, -1, -1, -1, -1, -1, -1},
{4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3, -1},
{10, 9, 4, 6, 10, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 9, 5, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 3, 4, 9, 5, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1},
{5, 0, 1, 5, 4, 0, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1},
{11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5, -1, -1, -1, -1},
{9, 5, 4, 10, 1, 2, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1},
{6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5, -1, -1, -1, -1},
{7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2, -1, -1, -1, -1},
{3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6, -1},
{7, 2, 3, 7, 6, 2, 5, 4, 9, -1, -1, -1, -1, -1, -1, -1},
{9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7, -1, -1, -1, -1},
{3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0, -1, -1, -1, -1},
{6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8, -1},
{9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7, -1, -1, -1, -1},
{1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4, -1},
{4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10, -1},
{7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10, -1, -1, -1, -1},
{6, 9, 5, 6, 11, 9, 11, 8, 9, -1, -1, -1, -1, -1, -1, -1},
{3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5, -1, -1, -1, -1},
{0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11, -1, -1, -1, -1},
{6, 11, 3, 6, 3, 5, 5, 3, 1, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6, -1, -1, -1, -1},
{0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10, -1},
{11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5, -1},
{6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3, -1, -1, -1, -1},
{5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2, -1, -1, -1, -1},
{9, 5, 6, 9, 6, 0, 0, 6, 2, -1, -1, -1, -1, -1, -1, -1},
{1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8, -1},
{1, 5, 6, 2, 1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6, -1},
{10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0, -1, -1, -1, -1},
{0, 3, 8, 5, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{10, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{11, 5, 10, 7, 5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{11, 5, 10, 11, 7, 5, 8, 3, 0, -1, -1, -1, -1, -1, -1, -1},
{5, 11, 7, 5, 10, 11, 1, 9, 0, -1, -1, -1, -1, -1, -1, -1},
{10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1, -1, -1, -1, -1},
{11, 1, 2, 11, 7, 1, 7, 5, 1, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11, -1, -1, -1, -1},
{9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7, -1, -1, -1, -1},
{7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2, -1},
{2, 5, 10, 2, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1},
{8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5, -1, -1, -1, -1},
{9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2, -1, -1, -1, -1},
{9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2, -1},
{1, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 7, 0, 7, 1, 1, 7, 5, -1, -1, -1, -1, -1, -1, -1},
{9, 0, 3, 9, 3, 5, 5, 3, 7, -1, -1, -1, -1, -1, -1, -1},
{9, 8, 7, 5, 9, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{5, 8, 4, 5, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1},
{5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0, -1, -1, -1, -1},
{0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5, -1, -1, -1, -1},
{10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4, -1},
{2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8, -1, -1, -1, -1},
{0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11, -1},
{0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5, -1},
{9, 4, 5, 2, 11, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4, -1, -1, -1, -1},
{5, 10, 2, 5, 2, 4, 4, 2, 0, -1, -1, -1, -1, -1, -1, -1},
{3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9, -1},
{5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2, -1, -1, -1, -1},
{8, 4, 5, 8, 5, 3, 3, 5, 1, -1, -1, -1, -1, -1, -1, -1},
{0, 4, 5, 1, 0, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5, -1, -1, -1, -1},
{9, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 11, 7, 4, 9, 11, 9, 10, 11, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11, -1, -1, -1, -1},
{1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11, -1, -1, -1, -1},
{3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4, -1},
{4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2, -1, -1, -1, -1},
{9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3, -1},
{11, 7, 4, 11, 4, 2, 2, 4, 0, -1, -1, -1, -1, -1, -1, -1},
{11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4, -1, -1, -1, -1},
{2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9, -1, -1, -1, -1},
{9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7, -1},
{3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10, -1},
{1, 10, 2, 8, 7, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 9, 1, 4, 1, 7, 7, 1, 3, -1, -1, -1, -1, -1, -1, -1},
{4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1, -1, -1, -1, -1},
{4, 0, 3, 7, 4, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 8, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{9, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{3, 0, 9, 3, 9, 11, 11, 9, 10, -1, -1, -1, -1, -1, -1, -1},
{0, 1, 10, 0, 10, 8, 8, 10, 11, -1, -1, -1, -1, -1, -1, -1},
{3, 1, 10, 11, 3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 11, 1, 11, 9, 9, 11, 8, -1, -1, -1, -1, -1, -1, -1},
{3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9, -1, -1, -1, -1},
{0, 2, 11, 8, 0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{3, 2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{2, 3, 8, 2, 8, 10, 10, 8, 9, -1, -1, -1, -1, -1, -1, -1},
{9, 10, 2, 0, 9, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8, -1, -1, -1, -1},
{1, 10, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 3, 8, 9, 1, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 9, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 3, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}};
";
            #endregion

            #region OpenCL source
            public string src = @"




//global_work_size = [maxX, maxY, maxZ]

__kernel void interpPts(__global float * fVals,
                        __global float * isoLevel,
                        __global float * edgeCoords,
                        __global float * initVals,
                        __global float * steps)

{
  int maxX = get_global_size(0);
  int maxY = get_global_size(1);
  int maxXY = maxX*maxY;
  //int maxZ = get_global_size(2);
  int x = get_global_id(0);
  int y = get_global_id(1);
  int z = get_global_id(2);
  
  float isolvl = isoLevel[0];
  
  //Value of this point
  int ind = x+maxX*y+maxXY*z;
  float curVal = fVals[ind];
  
  //Coordinate of this point
  float stepX=steps[0], stepY=steps[1], stepZ=steps[2];
  float curX = mad((float)x,stepX,initVals[0]);
  float curY = mad((float)y,stepY,initVals[1]);
  float curZ = mad((float)z,stepZ,initVals[2]);
  
  float mu;
  int ind2;
  //x direction
  mu = (isolvl - curVal) / (fVals[ind + 1] - curVal); 
  ind2 = 9*ind;
  edgeCoords[ind2]   = mad(mu, stepX, curX);
  edgeCoords[ind2+1] = curY;
  edgeCoords[ind2+2] = curZ;
  
  //y direction
  mu = (isolvl - curVal) / (fVals[ind + maxX] - curVal); 
  ind2 = 3*(3*ind+1);
  edgeCoords[ind2]   = curX;
  edgeCoords[ind2+1] = mad(mu, stepY, curY);
  edgeCoords[ind2+2] = curZ;

  //z direction
  mu = (isolvl - curVal) / (fVals[ind + maxXY] - curVal); 
  ind2 = 3*(3*ind+2);
  edgeCoords[ind2]   = curX;
  edgeCoords[ind2+1] = curY;
  edgeCoords[ind2+2] = mad(mu, stepZ, curZ);
  
}

//Transforms a conventional index into a globally localized index
int GetEdgeIndex(int convind, int x, int y, int z, int maxX, int maxXY, int ind)
{
  //ind = x+maxX*y+maxXY*z
  if (convind <= 5)
  {
     if (convind<=2)
     {
        if (convind == 0) return 3*ind;
        else if (convind == 1) return 3*(ind+1)+2;
        else return 3*(ind+maxXY);
     }
     else
     {
        if (convind == 3) return 3*ind+2;
        else if (convind == 4) return 3*(ind+maxX);
        else return 3*(ind+1+maxX)+2;
     }
  }
  else
  {
     if (convind<=8)
     {
        if (convind == 6) return 3*(ind+maxX+maxXY);
        else if (convind == 7) return 3*(ind+maxX)+2;
        else return 3*ind+1;
     }
     else
     {
        if (convind == 9) return 3*(ind+1)+1;
        else if (convind == 10) return 3*(ind+1+maxXY)+1;
        else return 3*(ind+maxXY)+1;
     }
  }
}

int GetPrelimNormalSumIndex(int edgeInd)
{
  if (edgeInd == 0 || edgeInd == 8 || edgeInd == 3)
      return 0; 
  else if (edgeInd == 4 || edgeInd == 9 || edgeInd == 1)
      return 1; 
  else if (edgeInd == 2 || edgeInd == 11 || edgeInd == 7)
      return 2; 
  else
      return 3; 
  
}

//global_work_size = [maxX-1, maxY-1, maxZ-1]
__kernel void Polygonize(__global float * fVals,
                         __global float * isoLevel,
                         __global float * edgeCoords,
                         __global float * edgePrelimNormals,
                         __global int   * indexArray)
{
  int maxX = get_global_size(0)+1;
  int maxY = get_global_size(1)+1;
  int maxXY = maxX*maxY;
  //int maxZ = get_global_size(2);
  int x = get_global_id(0);
  int y = get_global_id(1);
  int z = get_global_id(2);
  
  //Read isolevel
  float isolevel = isoLevel[0];
  
  //Read cube values to conventional val
  int ind = x + maxX*y + maxXY*z;
  float val[8];
  
  val[0] = fVals[ind]; 
  val[1] = fVals[ind+1];
  val[2] = fVals[ind+1+maxXY];
  val[3] = fVals[ind+maxXY];
  val[4] = fVals[maxX + ind]; 
  val[5] = fVals[maxX + ind+1];
  val[6] = fVals[maxX + ind+1+maxXY];
  val[7] = fVals[maxX + ind+maxXY];
  
  int cubeindex = 0; 
  if (val[0] < isolevel) cubeindex |= 1; 
  if (val[1] < isolevel) cubeindex |= 2; 
  if (val[2] < isolevel) cubeindex |= 4;
  if (val[3] < isolevel) cubeindex |= 8; 
  if (val[4] < isolevel) cubeindex |= 16; 
  if (val[5] < isolevel) cubeindex |= 32; 
  if (val[6] < isolevel) cubeindex |= 64; 
  if (val[7] < isolevel) cubeindex |= 128; 
  int ntriang = 0;
  int indTemp = 5*(x+(maxX-1)*y+(maxX-1)*(maxY-1)*z);
  int indTri, i;
  
  int indvert1, indvert2, indvert3, indConv1, indConv2, indConv3;
  
  int tempNormalPos, indNormal;
  
  for (i=0; i<15; i+=3) 
  {
      indTri = 3*(indTemp+ntriang);
      if (triTable[cubeindex][i  ]>=0)
      {
         indConv1 = triTable[cubeindex][i  ];         
         indConv2 = triTable[cubeindex][i+1];         
         indConv3 = triTable[cubeindex][i+2];         
         indvert1 = GetEdgeIndex(indConv1,x,y,z,maxX, maxXY, ind);
         indvert2 = GetEdgeIndex(indConv2,x,y,z,maxX, maxXY, ind);
         indvert3 = GetEdgeIndex(indConv3,x,y,z,maxX, maxXY, ind);
         indexArray[indTri]   = indvert1;
         indexArray[indTri+1] = indvert2;
         indexArray[indTri+2] = indvert3;
         
         //Normals
         float4 v1 = (float4)(edgeCoords[3*indvert1],edgeCoords[3*indvert1+1],edgeCoords[3*indvert1+2],0);
         float4 v2 = (float4)(edgeCoords[3*indvert2],edgeCoords[3*indvert2+1],edgeCoords[3*indvert2+2],0);
         float4 v3 = (float4)(edgeCoords[3*indvert3],edgeCoords[3*indvert3+1],edgeCoords[3*indvert3+2],0);
         
         v2 -= v1; v3 -= v1;
         
         v1 = normalize(cross(v3,v2));
         
         //Where to write the normal?
         tempNormalPos = GetPrelimNormalSumIndex(indConv1);
         indNormal = 3*(4*(indvert1)+tempNormalPos);
         edgePrelimNormals[indNormal]  = v1.x;
         edgePrelimNormals[indNormal+1]= v1.y;
         edgePrelimNormals[indNormal+2]= v1.z;
         
         tempNormalPos = GetPrelimNormalSumIndex(indConv2);
         indNormal = 3*(4*(indvert2)+tempNormalPos);
         edgePrelimNormals[indNormal]  = v1.x;
         edgePrelimNormals[indNormal+1]= v1.y;
         edgePrelimNormals[indNormal+2]= v1.z;
         
         tempNormalPos = GetPrelimNormalSumIndex(indConv3);
         indNormal = 3*(4*(indvert3)+tempNormalPos);
         edgePrelimNormals[indNormal]  = v1.x;
         edgePrelimNormals[indNormal+1]= v1.y;
         edgePrelimNormals[indNormal+2]= v1.z;
      }
      else
      {
         indexArray[indTri]   = -2;
         indexArray[indTri+1] = -2;
         indexArray[indTri+2] = -2;
      }
      ntriang++;
  }
}

//global_work_size = [maxX, maxY, maxZ]

__kernel void SmoothNormals(__global float * edgePrelimNormals,
                            __global float * edgeNormals)

{
  int maxX = get_global_size(0);
  int maxY = get_global_size(1);
  int maxXY = maxX*maxY;
  //int maxZ = get_global_size(2);
  int x = get_global_id(0);
  int y = get_global_id(1);
  int z = get_global_id(2);
  
  int ind = x+maxX*y+maxXY*z;

  //Variables
  int indNormal, indPrelim;
  float xMed, yMed, zMed;
  float4 vec;  

  //Edge going in x direction
  indNormal = 9*ind;
  indPrelim = 3*4*(3*ind);
  xMed = (edgePrelimNormals[indPrelim]   + edgePrelimNormals[indPrelim+3] +
          edgePrelimNormals[indPrelim+6] + edgePrelimNormals[indPrelim+9]);
  yMed = (edgePrelimNormals[indPrelim+1] + edgePrelimNormals[indPrelim+4] +
          edgePrelimNormals[indPrelim+7] + edgePrelimNormals[indPrelim+10]);
  zMed = (edgePrelimNormals[indPrelim+2] + edgePrelimNormals[indPrelim+5] +
          edgePrelimNormals[indPrelim+8] + edgePrelimNormals[indPrelim+11]);

  vec = (float4)(xMed, yMed, zMed, 0);
  vec = normalize(vec);

  edgeNormals[indNormal  ] = vec.x;
  edgeNormals[indNormal+1] = vec.y;
  edgeNormals[indNormal+2] = vec.z;
 
  //Edge going in y direction
  indNormal = 3*(3*ind+1);
  indPrelim = 3*4*(3*ind+1);
  xMed = (edgePrelimNormals[indPrelim]   + edgePrelimNormals[indPrelim+3] +
          edgePrelimNormals[indPrelim+6] + edgePrelimNormals[indPrelim+9]);
  yMed = (edgePrelimNormals[indPrelim+1] + edgePrelimNormals[indPrelim+4] +
          edgePrelimNormals[indPrelim+7] + edgePrelimNormals[indPrelim+10]);
  zMed = (edgePrelimNormals[indPrelim+2] + edgePrelimNormals[indPrelim+5] +
          edgePrelimNormals[indPrelim+8] + edgePrelimNormals[indPrelim+11]);

  vec = (float4)(xMed, yMed, zMed, 0);
  vec = normalize(vec);

  edgeNormals[indNormal  ] = vec.x;
  edgeNormals[indNormal+1] = vec.y;
  edgeNormals[indNormal+2] = vec.z;
  
    //Edge going in z direction
  indNormal = 3*(3*ind+2);
  indPrelim = 3*4*(3*ind+2);
  xMed = (edgePrelimNormals[indPrelim]   + edgePrelimNormals[indPrelim+3] +
          edgePrelimNormals[indPrelim+6] + edgePrelimNormals[indPrelim+9]);
  yMed = (edgePrelimNormals[indPrelim+1] + edgePrelimNormals[indPrelim+4] +
          edgePrelimNormals[indPrelim+7] + edgePrelimNormals[indPrelim+10]);
  zMed = (edgePrelimNormals[indPrelim+2] + edgePrelimNormals[indPrelim+5] +
          edgePrelimNormals[indPrelim+8] + edgePrelimNormals[indPrelim+11]);

  vec = (float4)(xMed, yMed, zMed, 0);
  vec = normalize(vec);

  edgeNormals[indNormal  ] = vec.x;
  edgeNormals[indNormal+1] = vec.y;
  edgeNormals[indNormal+2] = vec.z;
}


//global_work_size = [maxX-1, maxY-1, maxZ-1]
__kernel void PolygonizeNoNormals(__global float * fVals,
                                  __global float * isoLevel,
                                  __global float * edgeCoords,
                                  __global int   * indexArray)
{
  int maxX = get_global_size(0)+1;
  int maxY = get_global_size(1)+1;
  int maxXY = maxX*maxY;
  //int maxZ = get_global_size(2);
  int x = get_global_id(0);
  int y = get_global_id(1);
  int z = get_global_id(2);
  
  //Read isolevel
  float isolevel = isoLevel[0];
  
  //Read cube values to conventional val
  int ind = x + maxX*y + maxXY*z;
  float val[8];
  
  val[0] = fVals[ind]; 
  val[1] = fVals[ind+1];
  val[2] = fVals[ind+1+maxXY];
  val[3] = fVals[ind+maxXY];
  val[4] = fVals[maxX + ind]; 
  val[5] = fVals[maxX + ind+1];
  val[6] = fVals[maxX + ind+1+maxXY];
  val[7] = fVals[maxX + ind+maxXY];
  
  int cubeindex = 0; 
  if (val[0] < isolevel) cubeindex |= 1; 
  if (val[1] < isolevel) cubeindex |= 2; 
  if (val[2] < isolevel) cubeindex |= 4;
  if (val[3] < isolevel) cubeindex |= 8; 
  if (val[4] < isolevel) cubeindex |= 16; 
  if (val[5] < isolevel) cubeindex |= 32; 
  if (val[6] < isolevel) cubeindex |= 64; 
  if (val[7] < isolevel) cubeindex |= 128; 
  int ntriang = 0;
  int indTemp = 5*(x+(maxX-1)*y+(maxX-1)*(maxY-1)*z);
  int indTri, i;
  for (i=0; i<15; i+=3) 
  {
      indTri = 3*(indTemp+ntriang);
      if (triTable[cubeindex][i  ]>=0)
      {
         indexArray[indTri]  =  GetEdgeIndex(triTable[cubeindex][i  ],x,y,z,maxX, maxXY, ind);
         indexArray[indTri+1] = GetEdgeIndex(triTable[cubeindex][i+1],x,y,z,maxX, maxXY, ind);
         indexArray[indTri+2] = GetEdgeIndex(triTable[cubeindex][i+2],x,y,z,maxX, maxXY, ind);
      }
      else
      {
         indexArray[indTri]   = -2;
         indexArray[indTri+1] = -2;
         indexArray[indTri+2] = -2;
      }
      ntriang++;
  }
}

";
            #endregion
        }
    }
}

