using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace OpenCLTemplate.OMR
{
    /// <summary>Polygon finder class</summary>
    public class PolygonFinder
    {
        #region Convex Hull
        // 2D cross product of OA and OB vectors, i.e. z-component of their 3D cross product.
        // Returns a positive value, if OAB makes a counter-clockwise turn,
        // negative for clockwise turn, and zero if the points are collinear.
        static float cross(PointF O, PointF A, PointF B)
        {
            return (A.X - O.X) * (B.Y - O.Y) - (A.Y - O.Y) * (B.X - O.X);
        }

        static int compareTo(PointF A, PointF B)
        {
            int ans = A.X.CompareTo(B.X);
            if (ans == 0) return A.Y.CompareTo(B.Y);
            else return ans;
        }

        // Implementation of Andrew's monotone chain 2D convex hull algorithm.
        // Asymptotic complexity: O(n log n).
        // Practical performance: 0.5-1.0 seconds for n=1000000 on a 1GHz machine.
        //From: wikipedia

        // Returns a list of points on the convex hull in counter-clockwise order.
        // Note: the last point in the returned list is the same as the first one.

        /// <summary>Returns a set which is the convex hull of a List of points</summary>
        /// <param name="P">List of points to compute convex hull</param>
        public static List<PointF> convex_hull(List<PointF> P)
        {
            int n = P.Count(), k = 0;
            PointF[] H = new PointF[2 * n];

            // Sort points lexicographically
            P.Sort(compareTo);

            // Build lower hull
            for (int i = 0; i < n; i++)
            {
                while (k >= 2 && cross(H[k - 2], H[k - 1], P[i]) <= 0) k--;
                H[k++] = P[i];
            }

            // Build upper hull
            for (int i = n - 2, t = k + 1; i >= 0; i--)
            {
                while (k >= t && cross(H[k - 2], H[k - 1], P[i]) <= 0) k--;
                H[k++] = P[i];
            }

            List<PointF> ans = new List<PointF>();
            for (int i = 0; i < k; i++) ans.Add(H[i]);

            return ans;
        }
        #endregion

        #region Polygon approximation
        /// <summary>Approximates a list of points using a limited amount of vertexes. This algorithm uses convex_hull subroutine</summary>
        /// <param name="PP">Set of points to approximate using nVertexes.</param>
        /// <param name="nVertexes">Number of vertexes to use</param>
        /// <returns></returns>
        public static List<PointF> ApproximatePolygon(List<PointF> PP, int nVertexes)
        {
            List<PointF> P = convex_hull(PP);

            if (nVertexes < 3 || P.Count < 4 || P.Count < nVertexes + 1)
                return P;
                //throw new Exception("Need at least 3 vertexes");
            List<PointAngle> pa = new List<PointAngle>();

            for (int i = 0; i < P.Count - 1; i++)
            {
                PointF O = P[i];
                PointF A, B = P[i + 1];
                if (i == 0) A = P[P.Count - 2]; else A = P[i - 1];

                PointF OA = new PointF(A.X - O.X, A.Y - O.Y);
                PointF OB = new PointF(B.X - O.X, B.Y - O.Y);

                float ang = (float)Math.Acos((OA.X * OB.X + OA.Y * OB.Y) / (norm(OA) * norm(OB)));

                if (!float.IsNaN(ang)) pa.Add(new PointAngle(ang, P[i], i));
            }

            pa.Sort(PointAngle.CompareAngleTo);

            if (pa.Count-nVertexes > 0) pa.RemoveRange(nVertexes, pa.Count - nVertexes);

            //make sure to preserve orientation
            pa.Sort(PointAngle.CompareorigIdx);

            List<PointF> ans = new List<PointF>();
            int minNumElems = Math.Min(nVertexes, pa.Count);
            for (int i = 0; i < minNumElems; i++) ans.Add(pa[i].P);

            return ans;
        }

        private static float norm(PointF P)
        {
            return (float)Math.Sqrt(P.X * P.X + P.Y * P.Y);
        }

        private class PointAngle
        {
            public float angle;
            public PointF P;
            public int origIdx;
            public PointAngle(float a, PointF pt, int OrigIdx)
            {
                this.angle = a;
                this.P = pt;
                origIdx = OrigIdx;
            }
            public static int CompareAngleTo(PointAngle A, PointAngle B)
            {
                return A.angle.CompareTo(B.angle);
            }
            public static int CompareorigIdx(PointAngle A, PointAngle B)
            {
                return A.origIdx.CompareTo(B.origIdx);
            }
            public override string ToString()
            {
                return (angle * 180 / Math.PI).ToString();
            }
        }
        #endregion


    }
}
    

