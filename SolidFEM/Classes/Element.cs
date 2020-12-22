using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace SolidFEM.Classes
{

    class Element
    {
        public int ID;
        public string name;
        public Mesh element_mesh;
        public List<Node> nodes;
        public List<Point3d> TopologyVertices
        {
            get
            {
                List<Point3d> vertices = new List<Point3d>();
                foreach (Node n in nodes)
                {
                    vertices.Add(n.point);
                }
                return vertices;
            }
        }
        



        public Matrix<double> localK; //local stiffness matrix

        public Element()
        {
            //empty constructor
        }

        public Element(List<Node> nodeList, int id)
        {
            ID = id;
            nodes = nodeList;
            name = "Element: " + ID.ToString();
        }

        //Sort the vertices of the 
        //Sort the vertices of the 
        public void SortVerticesByGrahamScan()
        {
            List<Point3d> vertices = this.TopologyVertices; // Get all the topology vertices. 
            //Subtract top and bottom vertices
            #region Split into top an bottom vertices
            List<Point3d> sortedNodes = new List<Point3d>();
            // Calculate the center point
            double sumX = 0;
            double sumY = 0;
            double sumZ = 0;
            foreach (Point3d pt in vertices)
            {
                sumX += pt.X;
                sumY += pt.Y;
                sumZ += pt.Z;
            }
            Point3d centerPt = new Point3d(sumX / vertices.Count, sumY / vertices.Count, sumZ / vertices.Count);
            // If points are below centerPt,
            var bottomNodes = new List<Point3d>();
            var topNodes = new List<Point3d>();
            // Assign the nodes in top and bottom list
            foreach (Point3d pt in vertices)
            {
                if (pt.Z > centerPt.Z)
                {
                    topNodes.Add(pt);
                }
                else
                    bottomNodes.Add(pt);
            }
            #endregion
            #region Sort top and bottom nodes
            topNodes = topNodes.OrderBy(pt => pt.Y).ThenBy(pt => pt.X).ToList(); // Is it working if several points has the same y-value?
            topNodes = topNodes.OrderBy(pt => Math.Atan2(pt.Y - topNodes[0].Y, pt.X - topNodes[0].X)).ToList();
            List<Point3d> sortedTop = new List<Point3d>();
            while (topNodes.Count > 0)
            {
                GrahamScan(ref topNodes, ref sortedTop);
            }
            bottomNodes = bottomNodes.OrderBy(pt => pt.Y).ThenBy(pt => pt.X).ToList();
            bottomNodes = bottomNodes.OrderBy(pt => Math.Atan2(pt.Y - bottomNodes[0].Y, pt.X - bottomNodes[0].X)).ToList();
            List<Point3d> sortedBottom = new List<Point3d>();
            while (bottomNodes.Count > 0)
            {
                GrahamScan(ref bottomNodes, ref sortedBottom);
            }
            #endregion
            #region Modify element vertices with new list.
            List<Point3d> sortedVertices = new List<Point3d>();
            sortedVertices.AddRange(sortedBottom);
            sortedVertices.AddRange(sortedTop);

            for (int i = 0; i < this.nodes.Count; i++)
            {
                this.nodes[i].point = sortedVertices[i];
                //this.nodes[i].point = sortedVertices[i]; Delete this if everything works. 
            }
            #endregion

        }




        private void GrahamScan(ref List<Point3d> pts, ref List<Point3d> selPts)
        {
            if (pts.Count > 0)
            {
                var pt = pts[0];
                if (selPts.Count <= 1)
                {
                    selPts.Add(pt);
                    pts.RemoveAt(0);
                }
                else
                {
                    var pt1 = selPts[selPts.Count - 1];
                    var pt2 = selPts[selPts.Count - 2];
                    Vector3d dir1 = pt1 - pt2;
                    Vector3d dir2 = pt - pt1;
                    var crossProd = Vector3d.CrossProduct(dir1, dir2);
                    if (crossProd.Z < 0) // Check if the turn is clockwise or counter-clockwise
                    {
                        selPts.RemoveAt(selPts.Count - 1); //
                    }
                    else
                    {
                        selPts.Add(pt);
                        pts.RemoveAt(0);
                    }
                }
            }
        }
    }
}
