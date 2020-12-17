using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using SolidFEM.Classes;
namespace SolidFEM.Components
{
    public class DisplayElement : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the DisplayElement class.
        /// </summary>
        public DisplayElement()
          : base("DisplayElement", "dispEl",
              "Mesh representation of finite element",
              "FEM", "Utilities")
        {
        }
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Element", "el", "Input the elements to be displayed", GH_ParamAccess.list); // 0
        }
        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "m", "Mesh representation of the element", GH_ParamAccess.list); // 0
        }
        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Element> els = new List<Element>(); //Declare list of elements
            if (!DA.GetDataList(0, els)) return; //Do not run if no input
            List<Mesh> meshes = new List<Mesh>(); // Create a list of meshes

            foreach (Element el in els) // Iterate through all elements
            {
                el.SortVerticesByGrahamScan(); // Make sure that the element vertices are sorted correctly
                Mesh m = new Mesh(); // Create empty mesh
                var vList = new List<Point3d>();
                // Create a list of Point3d vertices
                foreach (Node n in el.nodes)
                {
                    vList.Add(n.point);
                    vList.Add(n.point);
                }
                m.Vertices.AddVertices(vList); // Add vertices to the mesh

                //Add faces
                m.Faces.AddFace(3, 2, 1, 0); //Bottom face. The direction is opposite of the others to make the normal face outwards
                m.Faces.AddFace(0, 1, 5, 4); //Side
                m.Faces.AddFace(1, 2, 6, 5); // Side
                m.Faces.AddFace(2, 3, 7, 6); // Side
                m.Faces.AddFace(3, 0, 4, 7); // Side
                m.Faces.AddFace(4, 5, 6, 7); // Top face
                //Clean mesh
                m.Compact();
                m.Vertices.CullUnused();
                m.FaceNormals.ComputeFaceNormals();
                meshes.Add(m);
            }
            // Output 
            DA.SetDataList(0, meshes); // 0
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
            get { return new Guid("1c1169b8-bc48-403a-94a3-6367e0121ef8"); }
        }
    }
}