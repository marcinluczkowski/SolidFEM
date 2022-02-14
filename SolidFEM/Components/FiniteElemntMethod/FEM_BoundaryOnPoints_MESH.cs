﻿using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using SolidFEM.Classes;

namespace SolidFEM.FiniteElementMethod
{
    public class FEM_BoundaryOnPoints_MESH : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the FEM_BoundaryOnPoints class.
        /// </summary>
        public FEM_BoundaryOnPoints_MESH()
          : base("FEM_BoundaryOnPoints", "Nickname",
              "Description",
              "SmartMesh", "FEM-Mesh")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Mesh", "mesh", "The mesh to apply boundary conditions on", GH_ParamAccess.list); // 0
            pManager.AddPointParameter("SupportPoints", "pt", "Position to place boundary points", GH_ParamAccess.list); // 1
            pManager.AddBooleanParameter("Tx", "", "", GH_ParamAccess.item, true); // 2
            pManager.AddBooleanParameter("Ty", "", "", GH_ParamAccess.item, true); // 3
            pManager.AddBooleanParameter("Ty", "", "", GH_ParamAccess.item, true); // 4
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Supports", "sup", "Support to apply to model", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // -- variables -- 
            bool tx = true; // 2
            bool ty = true; // 3
            bool tz = true; // 4
            List<Mesh> meshList = new List<Mesh>(); // 0
            //SmartMesh sMesh = new SmartMesh(); // 0
            List<Point3d> positions = new List<Point3d>(); // 1

            // - input --

            if (!DA.GetDataList(0, meshList)) return;
            if (!DA.GetDataList(1, positions)) return;
            DA.GetData(2, ref tx);
            DA.GetData(3, ref ty);
            DA.GetData(4, ref tz);


            // -- method --
            // clean the mesh and sort nodes
            var newMeshList = new List<Mesh>();
            int c = 0; // delete after testing
            foreach (Mesh mesh in meshList)
            {
                if (mesh.Vertices.Count == 8)
                {
                    Mesh nM = GrahamScan.DoGrahamScan(mesh);

                    if (nM.IsValid)
                    {
                        newMeshList.Add(nM);
                    }
                    else newMeshList.Add(mesh);
                    c++;
                }
                else
                {
                    newMeshList.Add(mesh);
                }
            }


            /* Delete if OK
            var newMeshList = new List<Mesh>();
            foreach (Mesh mesh in meshList)
            {
                Mesh nM = GrahamScan.DoGrahamScan(mesh);

                if (nM.IsValid)
                {
                    newMeshList.Add(nM);
                }
                else newMeshList.Add(mesh);
            }*/


            List<Point3d> nodePts = FEM_Utility.GetMeshNodes(newMeshList);
            List<Support> supportList = new List<Support>();
            foreach (var pt in positions)
            {
                int nodeIndex = GetClosestNodeIndex(nodePts, pt);
                if (nodeIndex != -1)
                {
                    Support sup = new Support(nodePts[nodeIndex], tx, ty, tz);
                    supportList.Add(sup);
                }
                
            }
            


            // -- output --
            DA.SetDataList(0, supportList);

        }
        private int GetClosestNodeIndex(List<Point3d> nodePoints, Point3d loadPosition)
        {
            int nodeIndex = -1;

            for (int i = 0; i < nodePoints.Count; i++)
            {
                double distance = nodePoints[i].DistanceToSquared(loadPosition);
                if (distance < 0.00001) // set tolerance
                {
                    nodeIndex = i;
                    break;
                }
            }
            return nodeIndex;
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return Properties.Resources.FESupport;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6f64f9c3-3741-4655-ba50-9d0b2f221844"); }
        }
    }
}