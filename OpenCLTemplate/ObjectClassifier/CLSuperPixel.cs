using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenCLTemplate;
using System.Drawing;
using Cloo;
using System.Threading.Tasks;

namespace OpenCLTemplate.OMR
{
    /// <summary>Superpixel analysis class</summary>
    public class CLSuperPixel
    {
        /// <summary>Maximum number of iterations during color propagation</summary>
        private int MAXITERCOLORPROPAGATION = 15; //12;
        /// <summary>Minimum number of pixels per color region</summary>
        private int MINNUMBEROFPIXELSPERREGION = 15; //28;
        /// <summary>Tolerance in standard deviation to consider pixel groups as neighbors</summary>
        private float STDDEVTOLERANCETOCONSIDERNEIGHBOR = 9.75f; //9.25f;

        /// <summary>Use Sobel border instead of variance border?</summary>
        public bool USESOBELBORDER = true;
        /// <summary>Use Median filter? May not be necessary with a good camera</summary>
        public bool USEMEDIANFILTER = false;
        /// <summary>Use Blur filter?</summary>
        public bool USEBLURFILTER = true;

        /// <summary>Number of pixels in border (typical)</summary>
        private float NBORDERPIXELS = 14;
        /// <summary>Tolerance in area max - min difference: min * AREAPERIMETERTOLERANCE more than max </summary>
        private float AREAPERIMETERTOLERANCE = 1.7f;//2.25f;

        /// <summary>Minimum number of internal squares to consider a region as checkerboard</summary>
        private float MINPERCENTREGIONSTOCONSIDERCHECKERBOARD = 0.4f;
        
        /// <summary>Tolerance in center distance to consider as concentric region</summary>
        private float CONCENTRICREGIONDISTTOL = 1.3f;
        /// <summary>Tolerance in radius difference to consider as concentric region</summary>
        private float CONCENTRICREGIONRADIUSTOL = 1.5f;

        #region Static initializations

        static CLCalc.Program.Kernel kernelmedianFilter;
        static CLCalc.Program.Kernel kernelBlur;

        static CLCalc.Program.Kernel kernelPropagateSuperPixelsCol;
        static CLCalc.Program.Kernel kernelPropagateSuperPixelsRow;
        static CLCalc.Program.Kernel kernelCreateClusterImg;
        static CLCalc.Program.Kernel kernelImageVariance;
        static CLCalc.Program.Kernel kernelSobel;
        static CLCalc.Program.Kernel kernelinitSuperPixelList;

        static CLCalc.Program.Kernel kernelExtractColorRegFeats;

        /// <summary>Static constructor.</summary>
        static CLSuperPixel()
        {
            initSquaresInCheckerboard();

            #region OpenCL source

            #region Old algorithms, unused

//            string unusedSrc = @"
//#define COLORTHRESH 25
//#define STDDEVTHRESH 12
//
//__kernel void PropagateSuperPixelsCol(__read_only    image2d_t bmp,
//                                      __global const float *   bmpStdDev,
//                                      __constant int *         bmpDim,
//                                      __global int *           superPixelList,
//                                      __global int *           changed)
//
//{
//    const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
//                          CLK_ADDRESS_CLAMP | //Clamp to zeros
//                          CLK_FILTER_NEAREST; //Don't interpolate
//
//    int x = get_global_id(0);
//    int y = 0;
//    int2 coord = (int2)(x,y);
//    uint4 pix = read_imageui(bmp, smp, coord);
//    uint4 pixPrev;
//    int idSuperPixel = x;
//
//    int prevColorIdx;
//
//    for (y = 1; y < bmpDim[1]; y++)
//    {
//       coord = (int2)(x,y);
//       pixPrev = pix;
//       pix = read_imageui(bmp, smp, coord);
//
//       prevColorIdx = superPixelList[idSuperPixel];
//       idSuperPixel += bmpDim[0];
//
//       //superPixelList[idSuperPixel] = -1;
//       if (//colorDif(pix,pixPrev) < COLORTHRESH && 
//           prevColorIdx < superPixelList[idSuperPixel] &&
//           bmpStdDev[idSuperPixel] < STDDEVTHRESH)
//       {
//          superPixelList[idSuperPixel] = prevColorIdx;
//          changed[0] = 1;
//       }
//    }
//
//    y = bmpDim[1]-1;
//    coord = (int2)(x,y);
//    pix = read_imageui(bmp, smp, coord);
//    idSuperPixel = x + bmpDim[0]*y;
//    for (y = bmpDim[1]-2; y >= 0; y--)
//    {
//       coord = (int2)(x,y);
//       pixPrev = pix;
//       pix = read_imageui(bmp, smp, coord);
//       prevColorIdx = superPixelList[idSuperPixel];
//       idSuperPixel -= bmpDim[0];
//       if (//colorDif(pix,pixPrev) < COLORTHRESH && 
//           prevColorIdx<superPixelList[idSuperPixel] &&
//           bmpStdDev[idSuperPixel] < STDDEVTHRESH)
//       {
//          superPixelList[idSuperPixel] = prevColorIdx;
//          changed[0] = 1;
//       }
//    }
//
//}
//
//
//__kernel void PropagateSuperPixelsRow(__read_only    image2d_t bmp,
//                                      __global const float *   bmpStdDev,
//                                      __constant int *         bmpDim,
//                                      __global int *           superPixelList,
//                                      __global int *           changed)
//
//{
//    const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
//                          CLK_ADDRESS_CLAMP | //Clamp to zeros
//                          CLK_FILTER_NEAREST; //Don't interpolate
//
//    int y = get_global_id(0);
//    int x = 0;
//    int2 coord = (int2)(x,y);
//    uint4 pix = read_imageui(bmp, smp, coord);
//    uint4 pixPrev;
//
//    int prevColorIdx;
//
//
//    int idSuperPixel =  x + bmpDim[0]*y;
//    for (x = 1; x < bmpDim[0]; x++)
//    {
//       coord = (int2)(x,y);
//       pixPrev = pix;
//       pix = read_imageui(bmp, smp, coord);
//       prevColorIdx = superPixelList[idSuperPixel];
//       idSuperPixel++;
//
//       //superPixelList[idSuperPixel] = -1;
//       if (//colorDif(pix,pixPrev) < COLORTHRESH && 
//           prevColorIdx<superPixelList[idSuperPixel] &&
//           bmpStdDev[idSuperPixel] < STDDEVTHRESH)
//       {
//          superPixelList[idSuperPixel] = prevColorIdx;
//          changed[0] = 1;
//       }
//    }
//
//    x = bmpDim[0]-1;
//    coord = (int2)(x,y);
//    pix = read_imageui(bmp, smp, coord);
//    idSuperPixel =  x + bmpDim[0]*y;
//    for (x = bmpDim[0]-2; x >= 0; x--)
//    {
//       coord = (int2)(x,y);
//       pixPrev = pix;
//       pix = read_imageui(bmp, smp, coord);
//       prevColorIdx = superPixelList[idSuperPixel];
//       idSuperPixel--;
//       if (//colorDif(pix,pixPrev) < COLORTHRESH && 
//           prevColorIdx<superPixelList[idSuperPixel] &&
//           bmpStdDev[idSuperPixel] < STDDEVTHRESH)
//       {
//          superPixelList[idSuperPixel] = prevColorIdx;
//          changed[0] = 1;
//       }
//    }
//
//}
//
//            __kernel void
//         LoG(__read_only  image2d_t imgSrc,
//                           __write_only image2d_t imgFiltered,
//                           __global float*        imgBorderIntens) //,
//                           //__constant int*        SobelThreshold)
//{
//   const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
//         CLK_ADDRESS_CLAMP | //Clamp to zeros
//         CLK_FILTER_NEAREST; //Don't interpolate

//   int x = get_global_id(0);
//   int y = get_global_id(1);
   
//   float4 P[5][5];
//   uint4 pix;
   
//   int2 coords;
//   for (int i=-2;i<=2;i++)
//   {
//       coords.x = x+i;
//       coords.y = y-2;   pix = read_imageui(imgSrc, smp, coords); P[i+2][0] = (float4)((float)pix.x, (float)pix.y, (float)pix.z, 0.0f);
//       coords.y = y-1;   pix = read_imageui(imgSrc, smp, coords); P[i+2][1] = (float4)((float)pix.x, (float)pix.y, (float)pix.z, 0.0f);
//       coords.y = y; pix = read_imageui(imgSrc, smp, coords); P[i+2][2] = (float4)((float)pix.x, (float)pix.y, (float)pix.z, 0.0f);
//       coords.y = y+1; pix = read_imageui(imgSrc, smp, coords); P[i+2][3] = (float4)((float)pix.x, (float)pix.y, (float)pix.z, 0.0f);
//       coords.y = y+2; pix = read_imageui(imgSrc, smp, coords); P[i+2][4] = (float4)((float)pix.x, (float)pix.y, (float)pix.z, 0.0f);

//   }
              
//   float4 LoG = fabs(16.0f * P[2][2] - 2.0f * (P[2][1]+P[1][2]+P[3][2]+P[2][3]) 
//                - P[1][1]-P[1][3]-P[3][1]-P[3][3] - P[2][0]-P[0][2]-P[4][2]-P[2][4]);
//   LoG.x = fmax(LoG.x, fmax(LoG.y, LoG.z));
//   float dG = (0.0625f * LoG.x);  //*1/16

//   float4 dx = P[1][1] + P[1][2] + P[1][3]
//              -P[3][1] - P[3][2] - P[3][3];

//   float4 dy = P[1][1] + P[2][1] + P[3][1]
//              -P[1][3] - P[2][3] - P[3][3];
              
//   dx = fabs(dx); dy = fabs(dy);

//   float gxx = fmax(fmax(dx.x,dx.y),dx.z);
//   float gyy = fmax(fmax(dy.x,dy.y),dy.z);
//   //max value is sqrt(2)*3*255 = 1081 => multiply by 255/1081
//   float G = 0.2357f * native_sqrt(mad(gxx,gxx,gyy*gyy));


//   dx = P[1][1] + P[1][2] + P[1][3]
//              -P[3][1] - P[3][2] - P[3][3];

//   dy = P[1][1] + P[2][1] + P[3][1]
//              -P[1][3] - P[2][3] - P[3][3];
//   //G = 0.176776f * native_sqrt(mad(gxx,gxx,gyy*gyy));

//   //ponders using LoG
//   //G = native_sqrt(native_sqrt(G*G*G*dG));
//   //G = dG > 10 ? 255.0f : 0.0f; //fmin(255.0f, 2*G) : G;

//   uint4 outP = (uint4)((uint)G, (uint)G, (uint)G, (uint)255);
   
//   coords.x = x;
//   coords.y = y;
   
//   write_imageui(imgFiltered, coords, outP);
//   imgBorderIntens[x+get_global_size(0)*y] = G;
//}
//";

            #endregion

        
            #region Filters, median and mean
            string srcMedianFilter = @"

#define BLOCK_SIZE 16

void QuickSort9(float* vals)
{
    //Start-stop indexes
    int subListStarts[7];
    int subListEnds[7];
    int nLists = 1;

    subListStarts[0] = 0;
    subListEnds[0] = 8;

    int ind = nLists-1, ind0, indf, pivot;
    int leftIdx, rightIdx, inttemp, k;
    float temp;
    while (nLists > 0)
    {
        ind0 = subListStarts[ind];
        indf = subListEnds[ind];

        pivot = (ind0 + indf) >> 1;
        leftIdx = ind0;
        rightIdx = indf;

        while (leftIdx <= pivot && rightIdx >= pivot)
        {
            while (vals[leftIdx] < vals[pivot] && leftIdx <= pivot)
                leftIdx++;
            while (vals[rightIdx] > vals[pivot] && rightIdx >= pivot)
                rightIdx--;

            temp = vals[leftIdx];
            vals[leftIdx] = vals[rightIdx];
            vals[rightIdx] = temp;

            leftIdx++;
            rightIdx--;
            if (leftIdx - 1 == pivot)
            {
                rightIdx++;
                pivot = rightIdx;
            }
            else if (rightIdx + 1 == pivot)
            {
                leftIdx--;
                pivot = leftIdx;
            }
        }

        nLists--;
        inttemp = subListStarts[nLists];
        subListStarts[nLists] = subListStarts[ind];
        subListStarts[ind] = inttemp;

        inttemp = subListEnds[nLists];
        subListEnds[nLists] = subListEnds[ind];
        subListEnds[ind] = inttemp;

        if (pivot - 1 - ind0 > 0)
        {
            subListStarts[nLists] = ind0;
            subListEnds[nLists] = pivot - 1;
            nLists++;
        }
        if (indf - pivot - 1 > 0)
        {
            subListStarts[nLists] = pivot + 1;
            subListEnds[nLists] = indf;
            nLists++;
        }

        for (k = 0; k < nLists; k++)
        {
            if (subListStarts[k]<=4 && 4<=subListEnds[k])
            {
                ind = k; k = nLists+1;
            }
        }
        if (k == nLists) nLists=0;
    }
}

//Applies a 3x3 median filter
__kernel __attribute__((reqd_work_group_size(BLOCK_SIZE, BLOCK_SIZE, 1))) void

         medianFilter(__read_only  image2d_t imgSrc,
                      __write_only image2d_t imgFiltered)
{
   const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
         CLK_ADDRESS_CLAMP | //Clamp to zeros
         CLK_FILTER_NEAREST; //Don't interpolate
         
         
    __local uint4 P[BLOCK_SIZE+2][BLOCK_SIZE+2];
    
    //Identification of this workgroup
   int i = get_group_id(0);
   int j = get_group_id(1);

   //Identification of work-item
   int idX = get_local_id(0);
   int idY = get_local_id(1); 

   int ii = i*BLOCK_SIZE + idX;
   int jj = j*BLOCK_SIZE + idY;
   
   int2 coords = (int2)(ii, jj);

   //Reads pixels
   P[idX][idY] = read_imageui(imgSrc, smp, coords);

   //Needs to read extra elements for the 5x5 filter in the borders
   if (idX == BLOCK_SIZE-1 && idY == BLOCK_SIZE-1)
   {
      for (int p=0; p<3; p++)
      {
         coords.x = ii + p;
         for (int q=0; q<3; q++)
         {
            coords.y = jj + q;
            P[idX+p][idY+q] = read_imageui(imgSrc, smp, coords);
         }
      }
   }
   else if (idX == BLOCK_SIZE-1)
   {
      for (int p=1; p<3; p++)
      {
         coords.x = ii + p;
         P[idX+p][idY] = read_imageui(imgSrc, smp, coords);
      }
   }
   else if (idY == BLOCK_SIZE-1)
   {
      for (int q=1; q<3; q++)
      {
         coords.y = jj + q;
         P[idX][idY+q] = read_imageui(imgSrc, smp, coords);
      }
   }
   barrier(CLK_LOCAL_MEM_FENCE);
   
   //Aplies median filter to element P[idX][idY]
   float R, G, B;
   
   //Blue
   float vals[9];
   int ind;
   for (int i=0; i < 3;i++)
   {
       ind = 3*i;
       vals[ind]   =   (float)P[idX+i][idY].x;
       vals[ind+1] = (float)P[idX+i][idY+1].x;
       vals[ind+2] = (float)P[idX+i][idY+2].x;
   }
   QuickSort9(vals);
   B = vals[4];
   
   //Green
   for (int i=0; i < 3;i++)
   {
       ind = 3*i;
       vals[ind]   =   (float)P[idX+i][idY].y;
       vals[ind+1] = (float)P[idX+i][idY+1].y;
       vals[ind+2] = (float)P[idX+i][idY+2].y;
   }
   QuickSort9(vals);
   G = vals[4];
   
   //Red
   for (int i=0; i < 3;i++)
   {
       ind = 3*i;
       vals[ind]   =   (float)P[idX+i][idY].z;
       vals[ind+1] = (float)P[idX+i][idY+1].z;
       vals[ind+2] = (float)P[idX+i][idY+2].z;
   }
   QuickSort9(vals);
   R = vals[4];

   P[idX+1][idY+1] = (uint4)((uint)B, (uint)G, (uint)R, (uint)255);
   barrier(CLK_LOCAL_MEM_FENCE);


   coords = (int2)(ii+1, jj+1);
   write_imageui(imgFiltered, coords, P[idX+1][idY+1]);
}




";
            #endregion

            #region Sobel filter and variance per pixel calculator
            string srcFilters = @"

__kernel void Blur(__read_only  image2d_t src,
                     __write_only image2d_t dst)
{

     const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
         CLK_ADDRESS_CLAMP | //Clamp to zeros
         CLK_FILTER_NEAREST; //Don't interpolate

     int x = get_global_id(0);
     int y = get_global_id(1);

     float4 pixMean;
     uint4 pix;
     
     int2 coords = (int2)(x - 1,y - 1);
     pix = read_imageui(src, smp, coords);
     pixMean = (float4)((float)pix.x,(float)pix.y,(float)pix.z,0.0f);
     
     coords.x++;
     pix = read_imageui(src, smp, coords);
     pixMean += (float4)((float)pix.x,(float)pix.y,(float)pix.z,0.0f);

     coords.x++;
     pix = read_imageui(src, smp, coords);
     pixMean += (float4)((float)pix.x,(float)pix.y,(float)pix.z,0.0f);

     coords.x = x-1; coords.y++;
     pix = read_imageui(src, smp, coords);
     pixMean += (float4)((float)pix.x,(float)pix.y,(float)pix.z,0.0f);

     coords.x++;
     pix = read_imageui(src, smp, coords);
     pixMean += (float4)((float)pix.x,(float)pix.y,(float)pix.z,0.0f);

     coords.x++;
     pix = read_imageui(src, smp, coords);
     pixMean += (float4)((float)pix.x,(float)pix.y,(float)pix.z,0.0f);

     coords.x = x-1; coords.y++;
     pix = read_imageui(src, smp, coords);
     pixMean += (float4)((float)pix.x,(float)pix.y,(float)pix.z,0.0f);

     coords.x++;
     pix = read_imageui(src, smp, coords);
     pixMean += (float4)((float)pix.x,(float)pix.y,(float)pix.z,0.0f);

     coords.x++;
     pix = read_imageui(src, smp, coords);
     pixMean += (float4)((float)pix.x,(float)pix.y,(float)pix.z,0.0f);

     pixMean *= 0.1111111111f;

     pix = (uint4)((uint)pixMean.x,(uint)pixMean.y,(uint)pixMean.z,255);
     coords.x=x;coords.y=y;
     write_imageui(dst, coords, pix);
}

__kernel void
         sobelWithoutLocal(__read_only  image2d_t imgSrc,
                           __write_only image2d_t imgFiltered,
                           __global float*        imgBorderIntens) //,
                           //__constant int*        SobelThreshold)
{
   const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
         CLK_ADDRESS_CLAMP | //Clamp to zeros
         CLK_FILTER_NEAREST; //Don't interpolate

   int x = get_global_id(0);
   int y = get_global_id(1);
   
   float4 P[3][3];
   uint4 pix;
   
   int2 coords;
   for (int i=-1;i<=1;i++)
   {
       coords.x = x+i;
       coords.y = y-1;   pix = read_imageui(imgSrc, smp, coords); P[i+1][0] = (float4)((float)pix.x, (float)pix.y, (float)pix.z, 0.0f);
       coords.y = y; pix = read_imageui(imgSrc, smp, coords); P[i+1][1] = (float4)((float)pix.x, (float)pix.y, (float)pix.z, 0.0f);
       coords.y = y+1; pix = read_imageui(imgSrc, smp, coords); P[i+1][2] = (float4)((float)pix.x, (float)pix.y, (float)pix.z, 0.0f);

   }

   float4 dx = P[0][0] + 2.0f*P[0][1] + P[0][2]
              -P[2][0] - 2.0f*P[2][1] - P[2][2];

   float4 dy = P[0][0] + 2.0f*P[1][0] + P[2][0]
              -P[0][2] - 2.0f*P[1][2] - P[2][2];
              
   dx = fabs(dx); dy = fabs(dy);

   float gxx = fmax(fmax(dx.x,dx.y),dx.z);
   float gyy = fmax(fmax(dy.x,dy.y),dy.z);
   //max value is sqrt(2)*4*255 = 1442.5 => multiply by 255/1442.5
   float G = 0.176776f * native_sqrt(mad(gxx,gxx,gyy*gyy));

   //G = fmax(0.0f, G);
   //G = G < SobelThreshold[0] ? 0.0f : 255.0f;

   uint4 outP = (uint4)((uint)G, (uint)G, (uint)G, (uint)255);
   
   coords.x = x;
   coords.y = y;
   
   write_imageui(imgFiltered, coords, outP);
   imgBorderIntens[x+get_global_size(0)*y] = G;
}



__kernel void ImageVariance(__read_only image2d_t bmp,
                            __write_only image2d_t imgFiltered,
                            __global float*       imgStdDev)
{

    const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
                          CLK_ADDRESS_CLAMP | //Clamp to zeros
                          CLK_FILTER_NEAREST; //Don't interpolate

   int x = get_global_id(0);
   int y = get_global_id(1);
   
   float4 sum = 0.0f;
   float4 sumSquare = 0.0f;
   
   for (int i=-2; i<=2; i++)
   {
       for (int j=-2; j<=2; j++)
       {
           if (abs(i) >=2 || abs(j) >=2)
           {
               int2 coord = (int2)(x+i,y+j);
               uint4 pix = read_imageui(bmp, smp, coord);
               float4 pixf = (float4)((float)pix.x,(float)pix.y,(float)pix.z,(float)pix.w);
               sum += pixf;
               sumSquare += pixf*pixf;
           }
       }
   }
   
   float4 maxStdDev = (sumSquare - sum*sum*0.0625f)*0.066666667f; //16 pixels
   //float4 maxStdDev = (sumSquare - sum*sum*0.04f)*0.041666666f; //25 pixels
   //float4 maxStdDev = (sumSquare - sum*sum*0.1111111111f)*0.125f; //9 pixels
   maxStdDev.x = fmax(maxStdDev.x,fmax(maxStdDev.y,maxStdDev.z));
   float G = native_sqrt(maxStdDev.x);
   imgStdDev[x+get_global_size(0)*y] = G;

   uint4 outP = (uint4)((uint)G, (uint)G, (uint)G, (uint)255);
   int2 coords;
   coords.x = x;
   coords.y = y;
   
   write_imageui(imgFiltered, coords, outP);
}


";
            #endregion

            #region Postprocessing
            string srcAnalyses = @"
#pragma OPENCL EXTENSION cl_khr_byte_addressable_store : enable
__kernel void ExtractColorRegFeats(__read_only image2d_t bmp,
                              __constant int*       bmpDim,
                              __global const int2*  coords,
                              __global int *        superPixelList,
                              __global uchar*       colors,
                              __global uchar*       neighbors)
{
    const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
                          CLK_ADDRESS_CLAMP | //Clamp to zeros
                          CLK_FILTER_NEAREST; //Don't interpolate

   int i = get_global_id(0);
   
   //gets pixel coordinate
   int2 coord = coords[i];
   
   uint4 pix = read_imageui(bmp, smp, coord);

   int i3 = i*3;
   colors[i3  ] = (uchar)pix.x;
   colors[i3+1] = (uchar)pix.y;
   colors[i3+2] = (uchar)pix.z;
   
   //Checks neighborhood
   uchar nn = 0;
   int idx = coord.x+coord.y*bmpDim[0];
   int idColor = superPixelList[idx];
   if (superPixelList[idx-1]        !=idColor) nn+=1;
   if (superPixelList[idx-bmpDim[0]]!=idColor) nn+=2;
   if (superPixelList[idx+1]        !=idColor) nn+=4;
   if (superPixelList[idx+bmpDim[0]]!=idColor) nn+=8;
   
   neighbors[i] = nn;
}

";
            #endregion

            #region Clustering
            string srcClustering = @"
//superPixelList[x+w*y] = x+w*y (assumed initialization)
//#define COLORTHRESH 25
#define STDDEVTHRESH 12

__kernel void initSuperPixelList(__global int * superPixelList)
{
   int x = get_global_id(0);
   int y = get_global_id(1);
   int w = get_global_size(0);
   int idx = x+w*y;
   superPixelList[idx] = idx;
}

int colorDif(uint4 c1, uint4 c2)
{
   return max(max(abs((int)c1.x-(int)c2.x),abs((int)c1.y-(int)c2.y)),abs((int)c1.z-(int)c2.z));
}


__kernel void PropagateSuperPixelsCol(__read_only    image2d_t bmp,
                                      __global const float *   bmpStdDev,
                                      __constant int *         bmpDim,
                                      __global int *           superPixelList,
                                      __global int *           changed)

{
    int x = get_global_id(0);
    int y = 0;
    int2 coord = (int2)(x,y);
    int idSuperPixel = x;

    int prevColorIdx;

    for (y = 1; y < bmpDim[1]; y++)
    {
       coord = (int2)(x,y);
       prevColorIdx = superPixelList[idSuperPixel];
       idSuperPixel += bmpDim[0];

       //superPixelList[idSuperPixel] = -1;
       if (prevColorIdx < superPixelList[idSuperPixel] &&
           bmpStdDev[idSuperPixel] < STDDEVTHRESH)
       {
          superPixelList[idSuperPixel] = prevColorIdx;
          changed[0] = 1;
       }
    }

    y = bmpDim[1]-1;
    coord = (int2)(x,y);
    idSuperPixel = x + bmpDim[0]*y;
    for (y = bmpDim[1]-2; y >= 0; y--)
    {
       coord = (int2)(x,y);
       prevColorIdx = superPixelList[idSuperPixel];
       idSuperPixel -= bmpDim[0];
       if (prevColorIdx<superPixelList[idSuperPixel] &&
           bmpStdDev[idSuperPixel] < STDDEVTHRESH)
       {
          superPixelList[idSuperPixel] = prevColorIdx;
          changed[0] = 1;
       }
    }

}


__kernel void PropagateSuperPixelsRow(__read_only    image2d_t bmp,
                                      __global const float *   bmpStdDev,
                                      __constant int *         bmpDim,
                                      __global int *           superPixelList,
                                      __global int *           changed)

{

    int y = get_global_id(0);
    int x = 0;
    int2 coord = (int2)(x,y);
    int prevColorIdx;


    int idSuperPixel =  x + bmpDim[0]*y;
    for (x = 1; x < bmpDim[0]; x++)
    {
       coord = (int2)(x,y);
       prevColorIdx = superPixelList[idSuperPixel];
       idSuperPixel++;

       //superPixelList[idSuperPixel] = -1;
       if (prevColorIdx<superPixelList[idSuperPixel] &&
           bmpStdDev[idSuperPixel] < STDDEVTHRESH)
       {
          superPixelList[idSuperPixel] = prevColorIdx;
          changed[0] = 1;
       }
    }

    x = bmpDim[0]-1;
    coord = (int2)(x,y);
    idSuperPixel =  x + bmpDim[0]*y;
    for (x = bmpDim[0]-2; x >= 0; x--)
    {
       coord = (int2)(x,y);
       prevColorIdx = superPixelList[idSuperPixel];
       idSuperPixel--;
       if (prevColorIdx<superPixelList[idSuperPixel] &&
           bmpStdDev[idSuperPixel] < STDDEVTHRESH)
       {
          superPixelList[idSuperPixel] = prevColorIdx;
          changed[0] = 1;
       }
    }

}




//Image representation of superpixels
__kernel void CreateClusterImg(__write_only image2d_t dst,
                                __global const int *  superPixelList,
                                __global const int4 *     colors)
{
   int x = get_global_id(0);
   int y = get_global_id(1);
   int w = get_global_size(0);
   int idx = superPixelList[x+y*w];
   uint4 c = (uint4)((uint)colors[idx].x,(uint)colors[idx].y,(uint)colors[idx].z, (uint)colors[idx].w);
   
   write_imageui(dst, (int2)(x,y), c);  
}

";
            #endregion

            #endregion

            if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.Unknown) CLCalc.InitCL();

            CLCalc.Program.Compile(srcMedianFilter + srcFilters + srcAnalyses + srcClustering);

            kernelmedianFilter = new CLCalc.Program.Kernel("medianFilter");
            kernelBlur = new CLCalc.Program.Kernel("Blur");

            kernelPropagateSuperPixelsCol = new CLCalc.Program.Kernel("PropagateSuperPixelsCol");
            kernelPropagateSuperPixelsRow = new CLCalc.Program.Kernel("PropagateSuperPixelsRow");
            kernelCreateClusterImg = new CLCalc.Program.Kernel("CreateClusterImg");
            kernelImageVariance = new CLCalc.Program.Kernel("ImageVariance");
            kernelExtractColorRegFeats = new CLCalc.Program.Kernel("ExtractColorRegFeats");
            kernelSobel = new CLCalc.Program.Kernel("sobelWithoutLocal");
            kernelinitSuperPixelList = new CLCalc.Program.Kernel("initSuperPixelList");

        }
        #endregion

        #region Constructor, variables and initializations

        /// <summary>Bitmap in CL memory</summary>
        CLCalc.Program.Image2D CLbmp, CLbmpAux;
        int[] superPixelList;
        /// <summary>List of pixels categorized as belonging to the same group</summary>
        CLCalc.Program.Variable CLsuperPixelList;

        /// <summary>Standard deviation of pixel values - image border intensity</summary>
        CLCalc.Program.Variable CLPixStdDev;

        /// <summary>Bitmap dimensions</summary>
        CLCalc.Program.Variable CLbmpDim;
        /// <summary>Has assignment been changed?</summary>
        CLCalc.Program.Variable CLChanged;
        /// <summary>Simple list of colors to apply in visual representation of cluster</summary>
        CLCalc.Program.Variable CLColors;


        /// <summary>Pixel coordinates of a given color region</summary>
        CLCalc.Program.Variable CLPixelCoords;
        /// <summary>Pixel colors</summary>
        CLCalc.Program.Variable CLPixelColors;
        /// <summary>Pixel neighborhood to check for boundary pixels</summary>
        CLCalc.Program.Variable CLPixelNeighbors;

        /// <summary>Colors to use to draw regions</summary>
        int[] regionDrawColors;

        /// <summary>Creates a new superpixel analyzer using Bitmap dimensions</summary>
        /// <param name="bmp">Bitmap to use</param>
        public CLSuperPixel(Bitmap bmp)
        {
            //Creates colors
            Random rnd = new Random(11);
            regionDrawColors = new int[6000];
            for (int i = 0; i < 6000; i += 4)
            {
                regionDrawColors[i] = rnd.Next(200);
                regionDrawColors[i+1] = rnd.Next(200);
                regionDrawColors[i+2] = rnd.Next(200);
                regionDrawColors[i+3] = 255;
            }
            //Set borders to yellow. Remember C# weird BGRA order
            regionDrawColors[0] = 0; regionDrawColors[1] = 255; regionDrawColors[2] = 255;

            CLColors = new CLCalc.Program.Variable(regionDrawColors);

            CLbmpDim = new CLCalc.Program.Variable(new int[2]);
            CLChanged = new CLCalc.Program.Variable(new int[1]);

            SetBmp(bmp);
        }
        #endregion

        #region Retrieval of important features
        /// <summary>Stores relevant region information</summary>
        public class RegionData
        {
            /// <summary>Color ID of region</summary>
            public int colorID;

            /// <summary>Pixels -> [x0 y0 x1 y1 ...</summary>
            public List<int> Pixels;
            /// <summary>Colors -> [r1 g1 b1 r2 g2 b2 ...</summary>
            public byte[] Colors;
            /// <summary>Region center [0] - X, [1] - Y</summary>
            public float[] Center;
            /// <summary>Region mean color</summary>
            public float[] Color;
            /// <summary>Average distance of pixels to center</summary>
            public float meanDistToCenter;
            /// <summary>Standard deviation of pixel distance to center</summary>
            public float stdDevDistToCenter;
            /// <summary>Area and perimeter of region</summary>
            public float Area, Perimeter;

            /// <summary>Which regions are connected to this one?</summary>
            public List<RegionData> Connections = new List<RegionData>();

            /// <summary>String representation</summary>
            /// <returns></returns>
            public override string ToString()
            {
                return "C: " + Center[0].ToString() + " " + Center[1].ToString();
            }
        }

        /// <summary>Contiguous regions found in image</summary>
        private List<RegionData> _regionData = new List<RegionData>();

        /// <summary>Initializes variables based on Bitmap</summary>
        /// <param name="bmp">Reference bitmap</param>
        private void initBmp(Bitmap bmp)
        {
            if (CLbmp == null || CLbmp.Height != bmp.Height || CLbmp.Width != bmp.Width)
            {
                CLbmp = new CLCalc.Program.Image2D(bmp);
                CLbmpAux = new CLCalc.Program.Image2D(bmp);

                CLbmpDim.WriteToDevice(new int[] { bmp.Width, bmp.Height });
                superPixelList = new int[bmp.Width * bmp.Height];
                CLPixStdDev = new CLCalc.Program.Variable(typeof(float), bmp.Width * bmp.Height);
                CLsuperPixelList = new CLCalc.Program.Variable(superPixelList);

                CLPixelCoords = new CLCalc.Program.Variable(typeof(int), bmp.Width * bmp.Height * 2);
                CLPixelColors = new CLCalc.Program.Variable(typeof(byte), bmp.Width * bmp.Height * 3);
                CLPixelNeighbors = new CLCalc.Program.Variable(typeof(byte), bmp.Width * bmp.Height);
            }
            else CLbmp.WriteBitmap(bmp);
        }

        /// <summary>Sets a new bitmap to be analyzed. Returns image representation of clusterization</summary>
        /// <param name="bmp">New bitmap</param>
        public void SetBmp(Bitmap bmp)
        {

            initBmp(bmp);

            //TODO: Remove stopwatches
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch swFilter = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch swCL = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch swBorder = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch swRegionData = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch swPostProcessing = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch swWaste = new System.Diagnostics.Stopwatch();

            sw.Start();

            //Proceeds to analysis
            swFilter.Start();
            if (USEMEDIANFILTER)
            {
                //filter
                
                int BLOCK_SIZE = 16;
                int groupSizeX = (bmp.Width - 4) / BLOCK_SIZE;
                int groupSizeY = (bmp.Height - 4) / BLOCK_SIZE;

                kernelmedianFilter.Execute(new CLCalc.Program.MemoryObject[] { CLbmp, CLbmpAux }, new int[] { groupSizeX * BLOCK_SIZE, groupSizeY * BLOCK_SIZE }, new int[] { BLOCK_SIZE, BLOCK_SIZE });
                CLCalc.Program.Image2D tempBmp = CLbmp;
                CLbmp = CLbmpAux;
                CLbmpAux = tempBmp;
                CLCalc.Program.CommQueues[CLCalc.Program.DefaultCQ].Finish();
                
            }
            else if (USEBLURFILTER)
            {

                kernelBlur.Execute(new CLCalc.Program.MemoryObject[] { CLbmp, CLbmpAux }, new int[] {CLbmp.Width,CLbmp.Height});
                CLCalc.Program.Image2D tempBmp = CLbmp;
                CLbmp = CLbmpAux;
                CLbmpAux = tempBmp;
                CLCalc.Program.CommQueues[CLCalc.Program.DefaultCQ].Finish();
            }
            swFilter.Stop();


            //Edge computation
            swBorder.Start();
            //Sobel?
            if (USESOBELBORDER) kernelSobel.Execute(new CLCalc.Program.MemoryObject[] { CLbmp, CLbmpAux, CLPixStdDev }, new int[] { bmp.Width, bmp.Height });
            //Variance? - seems better to avoid leak, Sobel seems to need Closing operation
            else kernelImageVariance.Execute(new CLCalc.Program.MemoryObject[] { CLbmp, CLbmpAux, CLPixStdDev }, new int[] { CLbmp.Width, CLbmp.Height });
            CLCalc.Program.CommQueues[CLCalc.Program.DefaultCQ].Finish();
            swBorder.Stop();

            swCL.Start();
            //float[] temp = new float[bmp.Width * bmp.Height];
            //CLPixStdDev.ReadFromDeviceTo(temp);

            //resets superpixel list
            kernelinitSuperPixelList.Execute(new CLCalc.Program.MemoryObject[] { CLsuperPixelList }, new int[] { bmp.Width, bmp.Height });

            int[] changed = new int[] { 1 };

            int nIter = 0;
            while (changed[0] > 0 && nIter < MAXITERCOLORPROPAGATION)
            {
                nIter++;

                changed[0] = 0;
                CLChanged.WriteToDevice(changed);
                kernelPropagateSuperPixelsCol.Execute(new CLCalc.Program.MemoryObject[] { CLbmp, CLPixStdDev, CLbmpDim, CLsuperPixelList, CLChanged }, CLbmp.Width);
                kernelPropagateSuperPixelsRow.Execute(new CLCalc.Program.MemoryObject[] { CLbmp, CLPixStdDev, CLbmpDim, CLsuperPixelList, CLChanged }, CLbmp.Height);
                CLChanged.ReadFromDeviceTo(changed);
                
            }

            CLsuperPixelList.ReadFromDeviceTo(superPixelList);
            swCL.Stop();

            swRegionData.Start();
            #region Identify unique colors and segregate region pixels

            //identifies unique colors. int[2] - new color number, number of pixels with that color
            int[] arrayUniqueColors = new int[bmp.Width * bmp.Height];
            int[] reMapUniqueColors = new int[bmp.Width * bmp.Height];

            //SortedList<int, int[]> uniqueColors = new SortedList<int, int[]>();
            foreach (int n in superPixelList) arrayUniqueColors[n]++;
            int numUniqueColors = 0;
            for (int i = 0; i < arrayUniqueColors.Length; i++)
            {
                if (arrayUniqueColors[i] > MINNUMBEROFPIXELSPERREGION)
                {
                    //uniqueColors.Add(i, new int[] { numUniqueColors, arrayUniqueColors[i] });
                    reMapUniqueColors[i] = numUniqueColors + 1;
                    numUniqueColors++;
                }
            }

            //Splits and stores region pixels
            //Note: index ZERO represents boundaries and unclassified pixels
            
            List<List<int>> lstRegionPixels = new List<List<int>>();
            lstRegionPixels.Add(new List<int>());

            for (int i = 0; i < numUniqueColors; i++)
            {
                //uniqueColors.Values[i][0] = i + 1;
                lstRegionPixels.Add(new List<int>());
            }

            int bmpW = bmp.Width;
            int bmpH = bmp.Height;
            for (int x = 0; x < bmpW; x++)
            {
                int i = x;
                for (int y = 0; y < bmpH; y++)
                {
                    //if (uniqueColors.ContainsKey(superPixelList[i])) superPixelList[i] = uniqueColors[superPixelList[i]][0];
                    if (arrayUniqueColors[superPixelList[i]] > MINNUMBEROFPIXELSPERREGION) superPixelList[i] = reMapUniqueColors[superPixelList[i]];
                    else superPixelList[i] = 0;

                    lstRegionPixels[superPixelList[i]].Add(x);
                    lstRegionPixels[superPixelList[i]].Add(y);

                    i += bmpW;
                }
            }
            #endregion

            swRegionData.Stop();

            swPostProcessing.Start();
            CLsuperPixelList.WriteToDevice(superPixelList);
            
            //Compute region centers, area, perimeter and color
            List<byte[]> pixelColors = new List<byte[]>();
            List<byte[]> pixelNeighbors = new List<byte[]>();
            pixelColors.Add(new byte[3]);
            pixelNeighbors.Add(new byte[1]);

            #region Extract relevant information from regions
            //for each region extract pixel colors and neighboring pixels of other colors
            //
            //  0  2  0
            //  1  X  4
            //  0  8  0
            //if no pixel borders X with a different color, neighbors = 0. If left and right, n=5 etc.
            for (int i = 1; i < lstRegionPixels.Count; i++)
            {
                //Pixel coordinates
                int[] coords = lstRegionPixels[i].ToArray();

                //Use Cloo to copy only the desired number of values
                unsafe
                {
                    ComputeCommandQueue CQ = CLCalc.Program.CommQueues[CLCalc.Program.DefaultCQ];
                    fixed (void* ponteiro = coords)
                    {
                        CQ.Write<int>((ComputeBuffer<int>)CLPixelCoords.VarPointer, true, 0, coords.Length, (IntPtr)ponteiro, null);
                    }
                }

                kernelExtractColorRegFeats.Execute(new CLCalc.Program.MemoryObject[] { 
                    CLbmp, CLbmpDim, CLPixelCoords, CLsuperPixelList, CLPixelColors, CLPixelNeighbors 
                    }, lstRegionPixels[i].Count / 2);

                byte[] pixColors = new byte[3 * lstRegionPixels[i].Count / 2];
                byte[] pixNeighbors = new byte[lstRegionPixels[i].Count / 2];

                #region Reads back relevant information
                //Use Cloo to copy only the desired number of values
                unsafe
                {
                    ComputeCommandQueue CQ = CLCalc.Program.CommQueues[CLCalc.Program.DefaultCQ];
                    fixed (void* ponteiro = pixColors)
                    {
                        CQ.Read<byte>((ComputeBuffer<byte>)CLPixelColors.VarPointer, true, 0, pixColors.Length, (IntPtr)ponteiro, null);
                    }
                }
                unsafe
                {
                    ComputeCommandQueue CQ = CLCalc.Program.CommQueues[CLCalc.Program.DefaultCQ];
                    fixed (void* ponteiro = pixNeighbors)
                    {
                        CQ.Read<byte>((ComputeBuffer<byte>)CLPixelNeighbors.VarPointer, true, 0, pixNeighbors.Length, (IntPtr)ponteiro, null);
                    }
                }
                #endregion

                pixelColors.Add(pixColors);
                pixelNeighbors.Add(pixNeighbors);
            }
            #endregion

            #region Region properties
            List<float[]> regionCenters = new List<float[]>();
            List<float[]> regionColors = new List<float[]>();
            List<float> regionAreas = new List<float>();
            List<float> regionPerimeters = new List<float>();

            List<float> regionMeanDistToCenter = new List<float>();
            List<float> regionStdDevDistToCenter = new List<float>();


            for (int i = 0; i < lstRegionPixels.Count; i++)
            {
                regionCenters.Add(new float[2]);
                regionColors.Add(new float[3]);
                regionAreas.Add(0);
                regionPerimeters.Add(0);
                regionMeanDistToCenter.Add(0);
                regionStdDevDistToCenter.Add(0);
            }
            
            Parallel.For(1, lstRegionPixels.Count, i =>
            //for (int i=1;i<lstRegionPixels.Count;i++)
            {
                //Compute centers
                float xC = 0, yC = 0;
                for (int k = 0; k < lstRegionPixels[i].Count; k += 2)
                {
                    xC += lstRegionPixels[i][k];
                    yC += lstRegionPixels[i][k + 1];
                }
                float temp = 2.0f / lstRegionPixels[i].Count;
                xC *= temp; yC *= temp;
                regionCenters[i][0] = xC;
                regionCenters[i][1] = yC;

                //Compute center distances and variance
                float sum = 0;
                float sumSquared = 0;
                for (int k = 0; k < lstRegionPixels[i].Count; k += 2)
                {
                    float d = (xC - lstRegionPixels[i][k]) * (xC - lstRegionPixels[i][k]) +
                        (yC - lstRegionPixels[i][k + 1]) * (yC - lstRegionPixels[i][k + 1]);
                    sumSquared += d;
                    d = (float)Math.Sqrt(d);
                    sum += d;
                }
                float nPix = (float)(lstRegionPixels[i].Count >> 1);
                float temp2 = sum / nPix;
                regionMeanDistToCenter[i] = temp2;
                regionStdDevDistToCenter[i] = (float)Math.Sqrt((sumSquared - sum * temp2) / (nPix - 1));

                float rr = 0, gg = 0, bb = 0;
                for (int k = 0; k < pixelColors[i].Length; k += 3)
                {
                    bb += pixelColors[i][k];
                    gg += pixelColors[i][1 + k];
                    rr += pixelColors[i][2 + k];
                }
                rr *= temp; gg *= temp; bb *= temp;
                regionColors[i][0] = rr;
                regionColors[i][1] = gg;
                regionColors[i][2] = bb;

                regionAreas[i] = lstRegionPixels[i].Count;
                regionPerimeters[i] = pixelNeighbors[i].Where(pix => pix > 0).Count();

                //int[] cnt = new int[15];
                //for (int kk=0;kk<cnt.Length;kk++) cnt[kk] = pixelNeighbors[i].Where(pix => pix == kk).Count();

            });

            _regionData.Clear();
            for (int i = 1; i < lstRegionPixels.Count; i++)
            {
                RegionData rd = new RegionData();
                rd.colorID = i;
                rd.Pixels = lstRegionPixels[i];
                rd.Colors = pixelColors[i];

                rd.Center = regionCenters[i];
                rd.Color = regionColors[i];

                //assign average region color to the region
                if (i < (regionDrawColors.Length >> 2))
                {
                    regionDrawColors[((i) << 2)] = (int)regionColors[i][2];
                    regionDrawColors[((i) << 2) + 1] = (int)regionColors[i][1];
                    regionDrawColors[((i) << 2) + 2] = (int)regionColors[i][0];
                }

                rd.meanDistToCenter = regionMeanDistToCenter[i];
                rd.stdDevDistToCenter = regionStdDevDistToCenter[i];

                rd.Area = regionAreas[i];
                rd.Perimeter = regionPerimeters[i];

                _regionData.Add(rd);
            }
            #endregion

            //float[] tempp = new float[regionAreas.Count];
            //for (int k = 0; k < regionAreas.Count; k++)
            //{
            //    tempp[k] = regionStdDevDistToCenter[k] * regionPerimeters[k] / regionAreas[k];
            //}

            //TODO: Compute principal directions?
            //Reminder: use normalized features for comparison
            swPostProcessing.Stop();


            sw.Stop();

        }

        #endregion

        #region Use region data for high level information extraction

        #region Concentric Region
        /// <summary>Stores concentric region information</summary>
        public class ConcentricRegionInfo
        {
            /// <summary>Creates new concentric region information</summary>
            /// <param name="cr">Region data of concentric region constituents</param>
            public ConcentricRegionInfo(List<RegionData> cr)
            {
                CenterX = 0;
                CenterY = 0;
                foreach (RegionData r in cr)
                {
                    CenterX += r.Center[0];
                    CenterY += r.Center[1];
                }
                float temp = 1.0f / (float)cr.Count;
                CenterX *= temp;
                CenterY *= temp;

                this.ID = GetID(cr);
            }

            /// <summary>String representation</summary>
            public override string ToString()
            {
                return ID;
            }

            /// <summary>X coord of concentric region</summary>
            public float CenterX;
            /// <summary>Y coord of concentric region</summary>
            public float CenterY;

            /// <summary>String ID of this region</summary>
            public string ID;

            /// <summary>Gets string ID from concentric region data</summary>
            /// <param name="cr">Concentric region</param>
            public static string GetID(List<RegionData> cr)
            {
                string s = "";
                foreach (RegionData r in cr)
                {
                    float minColor = Math.Min(Math.Min(r.Color[0], r.Color[1]), r.Color[2]);
                    float maxColor = Math.Max(Math.Max(r.Color[0], r.Color[1]), r.Color[2]);

                    int nTol = 15;

                    if (maxColor - minColor < 45 && maxColor < 55) s += "P";//black
                    else if (maxColor - minColor < 80 && minColor > 100) s += "W"; //white


                    //now the only choices are red, green and blue or unknown
                    else if (r.Color[0] > r.Color[1] + nTol && r.Color[0] > r.Color[2] + nTol) s += "R";
                    else if (r.Color[1] > r.Color[0] + nTol && r.Color[1] > r.Color[2] + nTol) s += "G";
                    else if (r.Color[2] > r.Color[1] + nTol && r.Color[2] > r.Color[0] + nTol) s += "B";
                    else s += "?";
                }
                return s;
            }
        }

        /// <summary>Finds targets inside image. Targets are concentric regions of the same color in the image.
        /// Reference image: bullseye target</summary>
        /// <param name="minConcentricRegions">Minimum number of concentric regions</param>
        public List<ConcentricRegionInfo> FindTargets(int minConcentricRegions)
        {
            //Criteria:

            // - Centers must be close
            // - mean distance of previous + 2*stddev = mean distance of next - 2*stddev

            //Clears connections
            foreach (RegionData r in _regionData) r.Connections.Clear();


            //Removes regions which can't be circular
            List<RegionData> candidateRegions = _regionData.Where(
                i => i.meanDistToCenter > i.stdDevDistToCenter * 0.5f &&
                    //regionStdDevDistToCenter[k] * regionPerimeters[k] / regionAreas[k];
                    i.stdDevDistToCenter * i.Perimeter / i.Area < 3.0f
                ).ToList<RegionData>();

            List<List<RegionData>> concentricRegions = new List<List<RegionData>>();

            for (int i = 0; i < candidateRegions.Count; i++)
            {
                RegionData r1 = candidateRegions[i];


                for (int j = i + 1; j < candidateRegions.Count; j++)
                {
                    RegionData r2 = candidateRegions[j];
                    if (r1.Connections.Count == 0 && r2.Connections.Count == 0)
                    {

                        float minSigma = Math.Min(r1.stdDevDistToCenter, r2.stdDevDistToCenter);


                        //Centers are close enough?
                        //checks if radii are close enough. use Sigmas as a proxy
                        if (distCenter(r1, r2) < minSigma * CONCENTRICREGIONDISTTOL &&
                            Math.Abs(r1.stdDevDistToCenter - r2.stdDevDistToCenter) < CONCENTRICREGIONRADIUSTOL * minSigma)
                        {
                            if (r1.meanDistToCenter < r2.meanDistToCenter) r2.Connections.Add(r1);
                            else r1.Connections.Add(r2);
                        }
                    }
                }
            }

            //consolidates connections
            for (int i = 0; i < candidateRegions.Count; i++)
            {
                RegionData r = candidateRegions[i];
                if (r.Connections.Count > 0)
                {
                    bool found = false;
                    foreach (List<RegionData> cr in concentricRegions)
                    {
                        if (cr.Contains(r.Connections[0]))
                        {
                            cr.Add(r);
                            found = true;
                            break;
                        }
                        else if (cr.Contains(r))
                        {
                            cr.Add(r.Connections[0]);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        List<RegionData> cr = new List<RegionData>();
                        cr.Add(r);
                        cr.Add(r.Connections[0]);
                        concentricRegions.Add(cr);
                    }
                }
            }

            List<ConcentricRegionInfo> ans = new List<ConcentricRegionInfo>();
            //sorts and stores
            foreach (List<RegionData> cr in concentricRegions)
            {
                if (cr.Count >= minConcentricRegions)
                {
                    cr.Sort(CompareMeanDist);
                    ans.Add(new ConcentricRegionInfo(cr));
                }
            }

            return ans;
        }
        #endregion

        #region Neighborhood and checkerboard
        /// <summary>Determines neighborhoods of regions</summary>
        public List<List<RegionData>> FindNeighborhood()
        {
            //Clears connections
            foreach (RegionData r in _regionData) r.Connections.Clear();

            //Consider which regions?
            List<RegionData> regions = _regionData;


            for (int i = 0; i < regions.Count; i++)
            {
                RegionData r1 = regions[i];
                for (int j = i + 1; j < regions.Count; j++)
                {
                    RegionData r2 = regions[j];

                    float dCenters = distCenter(r1, r2);

                    //adjacency criterion
                    if (Math.Max(r1.meanDistToCenter, r2.meanDistToCenter) < dCenters //no overlap
                        && dCenters < NBORDERPIXELS + 2 * r1.meanDistToCenter + STDDEVTOLERANCETOCONSIDERNEIGHBOR * r1.stdDevDistToCenter //not much further than r1 size. 
                        && dCenters < NBORDERPIXELS + 2 * r2.meanDistToCenter + STDDEVTOLERANCETOCONSIDERNEIGHBOR * r2.stdDevDistToCenter //not much further than r2 size
                        && Math.Min(r1.Area, r2.Area) * AREAPERIMETERTOLERANCE > Math.Max(r1.Area, r2.Area)//area not too different
                        && Math.Min(r1.Perimeter, r2.Perimeter) * AREAPERIMETERTOLERANCE > Math.Max(r1.Perimeter, r2.Perimeter)) //perimeter not too different
                    {
                        r1.Connections.Add(r2);
                        r2.Connections.Add(r1);
                    }
                }
            }

            //Clusters regions together
            List<List<RegionData>> connectedRegions = new List<List<RegionData>>();
            List<RegionData> regionsNotVisited = new List<RegionData>();
            regionsNotVisited.AddRange(regions);

            while (regionsNotVisited.Count > 0)
            {
                List<RegionData> connectedRegion = new List<RegionData>();

                connectedRegion.Add(regionsNotVisited[0]);
                regionsNotVisited.Remove(regionsNotVisited[0]);

                for (int i = 0; i < connectedRegion.Count; i++)
                {
                    foreach (RegionData r in connectedRegion[i].Connections)
                    {
                        if (regionsNotVisited.Contains(r)) // && !connectedRegion.Contains(r))
                        {
                            regionsNotVisited.Remove(r);
                            connectedRegion.Add(r);
                        }
                    }
                }

                connectedRegions.Add(connectedRegion);
            }


            return connectedRegions;
        }

        private int CompareMeanDist(RegionData r1, RegionData r2)
        {
            return r1.meanDistToCenter.CompareTo(r2.meanDistToCenter);
        }
        private float distCenter(RegionData rd1, RegionData rd2)
        {
            float temp1 = rd1.Center[0] - rd2.Center[0];
            float temp2 = rd1.Center[1] - rd2.Center[1];

            return (float)Math.Sqrt(temp1 * temp1 + temp2 * temp2);
        }

        /// <summary>Finds checkerboards in the image, from 7x5 to 19x19. Returns index information of regions</summary>
        /// <param name="neighborhood">Neighborhood information</param>
        public List<RegionData[,]> FindCheckerboards(List<List<CLSuperPixel.RegionData>> neighborhoodData)
        {
            //Checkerboard features:

            //- Number of vertical/horizontal squares
            //- Center
            //- Can find all internal points from 4 outer ones

            List<RegionData[,]> Checkerboards = new List<RegionData[,]>();

            foreach (List<CLSuperPixel.RegionData> region in neighborhoodData)
            {
                List<PointF> centers = new List<PointF>();
                foreach (CLSuperPixel.RegionData r in region) centers.Add(new PointF(r.Center[0], r.Center[1]));
                List<PointF> quad = PolygonFinder.ApproximatePolygon(centers, 4);

                if (region.Count > 5 && quad.Count == 4)
                {
                    RegionData[,] checkerPos = null;
                    for (int ii = 7; ii < 21; ii += 2)
                    {
                        for (int jj = 5; jj <= ii; jj += 2)
                        {
                            checkerPos = GetSquareCoordinates(ii, jj, region, quad, false);
                            if (ii != jj && checkerPos == null) checkerPos = GetSquareCoordinates(jj, ii, region, quad, false);

                            if (checkerPos != null) break;

                        }
                        if (checkerPos != null) break;
                    }

                    //why not just check all? will improve robustness

                    ////checks if this number of regions could represent a checkerboard
                    //int[] checkerDimensions;
                    //if (SquaresInCheckerboard.TryGetValue(region.Count, out checkerDimensions))
                    //{
                    //    RegionData[,] checkerPos = GetSquareCoordinates(checkerDimensions[0], checkerDimensions[1], region);
                    //    if (checkerDimensions[0]!=checkerDimensions[1] && checkerPos == null)
                    //        checkerPos = GetSquareCoordinates(checkerDimensions[1], checkerDimensions[0], region);

                    //    if (checkerPos != null) Checkerboards.Add(checkerPos);
                    //}

                    if (checkerPos != null) Checkerboards.Add(checkerPos);
                }
            }

            
            return Checkerboards;
        }

        /// <summary>Attempts to assign positions in the checkerboard to each region. Returns assigned positions
        /// or NULL if invalid checkerboard</summary>
        /// <param name="n">Number of squares in width</param>
        /// <param name="m">Number of squares in height</param>
        /// <param name="region">Region to analyze</param>
        /// <param name="quad">4 point approximation of Polygon</param>
        /// <param name="BlackSquaresOnly">Will regions contain only black squares? (false for a regular checkerboard)</param>
        private RegionData[,] GetSquareCoordinates(int n, int m, List<RegionData> region, List<PointF> quad, 
            bool BlackSquaresOnly)
        {
            //Rejects if number of regions is not enough to conclude about a n x m checkerboard
            float neededSquares, theoreticalSquares;
            if (BlackSquaresOnly)
            {
                neededSquares = (((n - 2) * (m - 2) + 1) >> 1) * MINPERCENTREGIONSTOCONSIDERCHECKERBOARD + n + m - 2;
                theoreticalSquares = (((n - 2) * (m - 2) + 1) >> 1) + n + m - 2;
            }
            else
            {
                neededSquares = (n - 2) * (m - 2) * MINPERCENTREGIONSTOCONSIDERCHECKERBOARD + n + m - 2;
                theoreticalSquares = (n - 2) * (m - 2) + n + m - 2;
            }
            if (neededSquares > region.Count || region.Count > theoreticalSquares) return null;

            //Computes expected positions
            PointF[,] expectedPos = new PointF[n, m];
            for (int x = 0; x < n; x++)
            {
                float xx = (float)x / (float)(n - 1);
                for (int y = 0; y < m; y++)
                {
                    float yy = (float)y / (float)(m - 1);
                    expectedPos[x, y] = MidPoint(MidPoint(quad[0], quad[3], xx), MidPoint(quad[1], quad[2], xx), yy);
                }
            }

            //Assigns expected positions
            RegionData[,] regionPos = new RegionData[n, m];
            foreach (RegionData r in region)
            {
                float minDist = float.MaxValue;
                int curX = 0, curY = 0;

                //finds expected position closest to this region center
                for (int x = 0; x < n; x++)
                    for (int y = 0; y < m; y++)
                    {
                        float dx = expectedPos[x, y].X - r.Center[0];
                        float dy = expectedPos[x, y].Y - r.Center[1];
                        float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            curX = x;
                            curY = y;
                        }
                    }

                if (regionPos[curX, curY] != null)
                {
                    //location is already occupied; invalid checkerboard
                    regionPos = null;
                    break;
                }
                else
                    regionPos[curX, curY] = r;
            }

            if (regionPos != null && (regionPos[n - 1, 0] == null || regionPos[n - 1, m - 1] == null || regionPos[0, m - 1] == null))
                regionPos = null;


            return regionPos;
        }

        /// <summary>Computes A + alpha*AB</summary>
        private PointF MidPoint(PointF A, PointF B, float alpha)
        {
            return new PointF(A.X + alpha * (B.X - A.X), A.Y + alpha * (B.Y - A.Y));
        }


        /// <summary>Possible number of squares in checkerboard</summary>
        static SortedList<int, int[]> SquaresInCheckerboard = new SortedList<int, int[]>();

        /// <summary>Initializes squares in checkerboard list</summary>
        private static void initSquaresInCheckerboard()
        {
            //Note that for an MxN checkerboard the number of contiguous regions
            //is going to be: (M-2)*(N-2)+(M+1)+(N+1) -4 
            // (white squares will join the sheet background) subtract 4 because corners are counted twice

            //note that 9x3 and 5x5 would contain the same; 9x3 is going to be ignored

            for (int i = 5; i < 21; i += 2)
                for (int j = 5; j <= i; j += 2)
                {
                    int val = (i - 2) * (j - 2) + i + j - 2;
                    if (!SquaresInCheckerboard.ContainsKey(val))
                        SquaresInCheckerboard.Add((i - 2) * (j - 2) + i + j - 2, new int[] { j, i });
                }


        }

        #endregion

        #endregion

        #region Visual representations

        /// <summary>Retrieves color region identification. Use Setbmp first to process bitmap</summary>
        /// <param name="DrawUniqueColors">Redraw image using unique colors. Set to false to draw on top of original image</param>
        /// <param name="DrawPixelMeanDists">Redraw image drawing circles to represent pixel distances</param>
        /// <returns></returns>
        public Bitmap GetColorRegionIdentification(bool DrawUniqueColors, bool DrawPixelMeanDists)
        {

            if (DrawUniqueColors)
            {
                CLColors.WriteToDevice(regionDrawColors);

                kernelCreateClusterImg.Execute(new CLCalc.Program.MemoryObject[] { CLbmp, CLsuperPixelList, CLColors },
                    new int[] { CLbmp.Width, CLbmp.Height });
            }

            //computes image representation
            Bitmap bmpRep = CLbmp.ReadBitmap();


            Graphics g = Graphics.FromImage(bmpRep);

            if (DrawPixelMeanDists)
            {
                foreach (RegionData rd in _regionData)
                {
                    Color color = Color.FromArgb(255 - (int)rd.Color[0], 255 - (int)rd.Color[1], 255 - (int)rd.Color[2]);
                    g.FillRectangle(new SolidBrush(color), rd.Center[0] - 2, rd.Center[1] - 2, 4, 4);

                    g.DrawEllipse(new Pen(color), rd.Center[0] - rd.meanDistToCenter, rd.Center[1] - rd.meanDistToCenter,
                        2 * rd.meanDistToCenter, 2 * rd.meanDistToCenter);

                    float muPlusSigma = rd.meanDistToCenter + 2 * rd.stdDevDistToCenter;
                    g.DrawEllipse(new Pen(color), rd.Center[0] - muPlusSigma, rd.Center[1] - muPlusSigma,
                        2 * muPlusSigma, 2 * muPlusSigma);
                }
            }

            return bmpRep;
        }

        #endregion

        #region Filters - for testing
        /// <summary>Finds image borders using Sobel filter</summary>
        /// <param name="bmp">Bitmap to process</param>
        /// <returns></returns>
        public Bitmap Sobel(Bitmap bmp)
        {
            initBmp(bmp);

            kernelSobel.Execute(new CLCalc.Program.MemoryObject[] { CLbmp, CLbmpAux, CLPixStdDev }, new int[] { bmp.Width, bmp.Height });

            Bitmap ans = CLbmpAux.ReadBitmap();

            return ans;
        }

        /// <summary>Finds image borders using Sobel filter</summary>
        /// <param name="bmp">Bitmap to process</param>
        /// <returns></returns>
        public Bitmap PixelVariance(Bitmap bmp)
        {
            initBmp(bmp);

            kernelImageVariance.Execute(new CLCalc.Program.MemoryObject[] { CLbmp, CLbmpAux, CLPixStdDev }, new int[] { bmp.Width, bmp.Height });

            Bitmap ans = CLbmpAux.ReadBitmap();

            return ans;
        }
        #endregion

    }
}
