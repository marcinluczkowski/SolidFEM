using System;
using System.Collections.Generic;
using SolidFEM.Classes;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace SolidFEM.Deconstructors
{
    public class DeconsructElement : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public DeconsructElement()
          : base("DeconstructEl", "DeEl",
              "Deconstruct basic element class",
              "FEM", "Deconstructors")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Element", "el", "solid fem element", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Informations", "", "", GH_ParamAccess.list);
            pManager.AddGenericParameter("Nodes", "ns", "solid fem nodes", GH_ParamAccess.list);
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
            int i = 0;
            foreach (var n in el.nodes)
            {
                Point3d pt = n.point;
                nodes.Add(pt);
            }
            el.SortVerticesByGrahamScan();

            

            List<int> ids = new List<int>();
            foreach(Node n in el.nodes)
            {
                ids.Add(n.ID);
            }

            info.Add("change brep box into fel"); ;
            DA.SetDataList(0, info);
            DA.SetDataList(1, el.TopologyVertices);
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
            get { return new Guid("51fe47af-a45c-400b-8511-c947dcffc77b"); }
        }
    }
}