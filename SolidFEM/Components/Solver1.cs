using System;
using System.Collections.Generic;
using SolidFEM.Classes;
using Grasshopper.Kernel;
using Rhino.Geometry;

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace SolidFEM.Components
{
    public class Solver1 : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public Solver1()
          : base("CalculateOneELement", "s1",
              "Description",
              "FEM", "Element")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Element","el","single element", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Informations", "", "", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Element el = new Element();
            DA.GetData(0, ref el);
            var info = new List<string>();

            List<Point3d> nodes = new List<Point3d>();
            
            foreach (var n in el.nodes)
            {
                Point3d pt = n.point;
                nodes.Add(pt);
            }

            double v = 0.125;
            Matrix<double> j1 = DenseMatrix.OfArray(new double[,] {
                {-v,v,v,-v,-v,v,v,-v},
                {-v,-v,v,v,-v,-v,v,v},
                {-v,-v,-v,-v,v,v,v,v}});
            info.Add("j1 = " + j1);

            Point3d p1 = el.nodes[0].point;
            Point3d p2 = el.nodes[1].point;
            Point3d p3 = el.nodes[2].point;
            Point3d p4 = el.nodes[3].point;
            Point3d p5 = el.nodes[4].point;
            Point3d p6 = el.nodes[5].point;
            Point3d p7 = el.nodes[6].point;
            Point3d p8 = el.nodes[7].point;

            Matrix<double> j2 = DenseMatrix.OfArray(new double[,] {
                {p1.X, p1.Y, p1.Z},
                {p2.X, p2.Y, p2.Z},
                {p3.X, p3.Y, p3.Z},
                {p4.X, p4.Y, p4.Z},
                {p5.X, p5.Y, p5.Z},
                {p6.X, p6.Y, p6.Z},
                {p7.X, p7.Y, p7.Z},
                {p8.X, p8.Y, p8.Z},
            });
            info.Add("j2 = " + j2);
            var Jacobian = j1.Multiply(j2);
            info.Add("Jacobian = " + Jacobian);
            double detJ = Math.Abs(Jacobian.Determinant());
            info.Add("detJ = " + detJ);
            double volume = detJ * 8;
            info.Add("volume = " + volume);

            List<double> nx = new List<double>() { -v, v, v, -v, -v, v, v, -v };
            List<double> ny = new List<double>() { -v, -v, v, v, -v, -v, v, v };
            List<double> nz = new List<double>() { -v, -v, -v, -v, v, v, v, v };



            Matrix<double> bi =Matrix<double>.Build.Dense(6,24);

            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 24; j++)
                {
                    bi[i, j] = 0;
                }
            }
            //B1
            for (int ik = 0; ik < 8; ik++)
            {
                int k = ik * 3;
                bi[0, 0 + k] = nx[ik];
                bi[1, 1 + k] = ny[ik];
                bi[2, 2 + k] = nz[ik];
                bi[3, 1 + k] = nz[ik];
                bi[3, 2 + k] = ny[ik];
                bi[4, 0 + k] = nz[ik];
                bi[4, 2 + k] = nx[ik];
                bi[5, 0 + k] = ny[ik];
                bi[5, 1 + k] = nx[ik];
            }

            info.Add("B = " + bi);

            Matrix<double> ci = Matrix<double>.Build.Dense(6, 6);
            double ni = 0.3;
            double E = 210; //GPa
            double l1 = ni * E;
            double l2 = (1 + ni) * (1 - 2 * ni);
            double l = l1 / l2;
            double g = 1-2*ni;

            ci[0, 0] = l + 2*g;
            ci[1, 1] = l + 2*g;
            ci[2, 2] = l + 2*g;
            ci[3, 3] = g;
            ci[4, 4] = g;
            ci[5, 5] = g;
            ci[0, 1] = l;
            ci[0, 2] = l;
            ci[1, 0] = l;
            ci[1, 2] = l;
            ci[2, 0] = l;
            ci[2, 1] = l;

            info.Add("C = " + ci);

            //local stiffness matrix 
            Matrix<double> ki= Matrix<double>.Build.Dense(24, 24);
            ki = bi.Transpose() * ci * bi *detJ;
            info.Add("K = " + ki);
            


            info.Add("change brep box into fel"); 
            DA.SetDataList(0, info);
        }
        public double[] linearSF(double d1, double d2, double d3)
        {
            double[] N = new double[8];

            N[0] = 0.125 * (1 - d1) * (1 - d2) * (1 - d3);
            N[1] = 0.125 * (1 + d1) * (1 - d2) * (1 - d3);
            N[2] = 0.125 * (1 + d1) * (1 + d2) * (1 - d3);
            N[3] = 0.125 * (1 - d1) * (1 + d2) * (1 - d3);
            N[4] = 0.125 * (1 - d1) * (1 - d2) * (1 + d3);
            N[5] = 0.125 * (1 + d1) * (1 - d2) * (1 + d3);
            N[6] = 0.125 * (1 + d1) * (1 + d2) * (1 + d3);
            N[7] = 0.125 * (1 - d1) * (1 + d2) * (1 + d3);

            return N;
                    
        }
        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("7342de20-3dd7-4d21-9fa1-1f0151ddc589"); }
        }
    }
}