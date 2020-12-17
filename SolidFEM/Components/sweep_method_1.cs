using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using SolidFEM.Classes;

namespace SolidFEM.Components
{
    public class mesh_sweep_1 : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the mesh_sweep_1 class.
        /// </summary>
        public mesh_sweep_1()
          : base("mesh_sweep_1", "Nickname",
              "Description",
              "FEM", "Meshing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Planar mesh", "m", "Base mesh for the sweep", GH_ParamAccess.item); // 0
            pManager.AddCurveParameter("Guide Curve", "c", "Curve to sweep the mesh along", GH_ParamAccess.item); // 1
            pManager.AddIntegerParameter("Number of divisions", "numDiv", "Number of elements along the rail curve", GH_ParamAccess.item); // 2
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Elements", "el", "Elements from sweep", GH_ParamAccess.tree); //0 
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region Register input and assign to parameters
            Mesh baseMesh = new Mesh(); // 0
            Curve guideCurve = null; // 1
            int div = 0; // 2

            if (!DA.GetData(0, ref baseMesh)) return; // 0
            if (!DA.GetData(1, ref guideCurve)) return; // 1
            DA.GetData(2, ref div); // 2



            #endregion

            #region Control input
            /// Intersection between mesh and curve
            /// Initially it is assumed that the guide curve starts or ends at a corner of the mesh. 
            /// If not: an error message should appear
            ///

            int[] face_id;
            Point3d[] intersections;
            PolylineCurve polyline = guideCurve.ToPolyline(0.001, 0.01, 0.001, 10000);

            intersections = Rhino.Geometry.Intersect.Intersection.MeshPolyline(baseMesh, polyline, out face_id);

            if (intersections.Length < 1) //Check if there are intersections
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No intersections between curve and mesh");
                return;

            }

            #endregion

            #region Sweep operation

            // Start by splitting the guide curve into the wanted amount of divisions. 
            Point3d[] pts; guideCurve.DivideByCount(div, true, out pts);

            //Calculate the vector between the first vector and the points on curve. 
            List<Vector3d> vec = new List<Vector3d>();


            foreach (Point3d pt in pts)
            {
                vec.Add(pt - intersections[0]);
            }

            //Create a list of the meshes along the guide curve
            List<Mesh> mList = new List<Mesh>();
            foreach (Vector3d v in vec)
            {
                Mesh tempMesh = baseMesh.DuplicateMesh(); // Create a copy of original mesh
                tempMesh.Translate(v); //Translate the new copy
                mList.Add(tempMesh); // Add it to the list
            }

            ///DataTree of elements
            /// Create a dataTree structure of elements. The dataTree structure is choosen to keep the different layers
            /// created by the sweep method separated. 
            ///            
            Grasshopper.DataTree<Element> tree = new Grasshopper.DataTree<Element>(); // initiate an empty tree
            List<Element> testOut = new List<Element>();


            #region New method

            //Create the nodes
            int mCount = mList.Count; // Numbes of meshes            
            int meshVertices = baseMesh.Vertices.Count;
            List<Node> nodes = new List<Node>(); // Create empty list for nodes

            int count = 0;
            for (int i = 0; i < mCount; i++) // Run through all the meshes to create nodes. 
            {
                //Create nodes from vertices. 
                Mesh m1 = mList[i];


                Point3d[] vertices = m1.Vertices.ToPoint3dArray();

                int vLength = vertices.GetLength(0); ;
                // Add all the nodes.
                for (int j = 0; j < vLength; j++)
                {
                    nodes.Add(new Node(vertices[j], count + 1));
                    count++;
                }
                // 


            }

            // Create all elements
            int elID = 0;
            for (int i = 0; i < mCount - 1; i++) //Run through the list of meshes
            {
                List<Element> eList = new List<Element>();
                //Run through the faces of each mesh
                for (int j = 0; j < mList[i].Faces.Count; j++)
                {
                    //Create an element based on j-th face an the one directly above it.
                    List<Node> elementNodes = new List<Node>();
                    //Add BottomNodes
                    elementNodes.Add(nodes[mList[i].Faces[j].A + (i * meshVertices)]);
                    elementNodes.Add(nodes[mList[i].Faces[j].B + (i * meshVertices)]);
                    elementNodes.Add(nodes[mList[i].Faces[j].C + (i * meshVertices)]);
                    elementNodes.Add(nodes[mList[i].Faces[j].D + (i * meshVertices)]);
                    //Add top nodes
                    elementNodes.Add(nodes[mList[i + 1].Faces[j].A + ((i + 1) * meshVertices)]);
                    elementNodes.Add(nodes[mList[i + 1].Faces[j].B + ((i + 1) * meshVertices)]);
                    elementNodes.Add(nodes[mList[i + 1].Faces[j].C + ((i + 1) * meshVertices)]);
                    elementNodes.Add(nodes[mList[i + 1].Faces[j].D + ((i + 1) * meshVertices)]);

                    Element e = new Element(elementNodes, 1 + elID + (i * mList[i].Faces.Count));
                    elID++;
                    e.SortVerticesByGrahamScan();
                    eList.Add(e);


                }
                // Something is not working here...
                // Something about creating the elements whith vertices not being sorted... The error could be in the sorting in Element.cs
                //  
                tree.AddRange(eList, new Grasshopper.Kernel.Data.GH_Path(i));
            }

            #endregion


            #region Old Method
            /*
             * 
             * 
             * int elID = 0;
            for (int i = 0; i < mList.Count - 1; i++) // Loop through the layers of meshes. 
            {
                List<Element> eList = new List<Element>(); // Create empty list for the element of that tree

                for (int j = 0; j < mList[i].Faces.Count; j++) // Iterate throuh each face
                {
                    //Create an element from each face of the mesh and the corresponding one above. 
                    Element e = new Element(); // Initiate element
                    e.ID = elID + 1; // Start count from 1
                    e.name = "Hex8-el: " + e.ID.ToString(); // Name of the element

                    List<Node> nodes = new List<Node>(); // Initiate list of element nodes

                    // Generalise to work or both recangles and quads in future. The triangle part need more work
                    if (mList[i].Faces[j].IsQuad)
                    {
                        Point3d a1 = mList[i].TopologyVertices[mList[i].Faces[j].A];
                        Point3d b1 = mList[i].TopologyVertices[mList[i].Faces[j].B];
                        Point3d c1 = mList[i].TopologyVertices[mList[i].Faces[j].C];
                        Point3d d1 = mList[i].TopologyVertices[mList[i].Faces[j].D];

                        Point3d a2 = mList[i + 1].TopologyVertices[mList[i + 1].Faces[j].A];
                        Point3d b2 = mList[i + 1].TopologyVertices[mList[i + 1].Faces[j].B];
                        Point3d c2 = mList[i + 1].TopologyVertices[mList[i + 1].Faces[j].C];
                        Point3d d2 = mList[i + 1].TopologyVertices[mList[i + 1].Faces[j].D];

                        Node nA1 = new Node(a1, 1, "Node: " + (1).ToString());
                        Node nB1 = new Node(b1, 2, "Node: " + (2).ToString());
                        Node nC1 = new Node(c1, 3, "Node: " + (3).ToString());
                        Node nD1 = new Node(d1, 4, "Node: " + (4).ToString());

                        Node nA2 = new Node(a2, 5, "Node: " + (5).ToString());
                        Node nB2 = new Node(b2, 6, "Node: " + (6).ToString());
                        Node nC2 = new Node(c2, 7, "Node: " + (7).ToString());
                        Node nD2 = new Node(d2, 8, "Node: " + (8).ToString());

                        List<Node> elementNodes = new List<Node>() { nA1, nB1, nC1, nD1, nA2, nB2, nC2, nD2 };

                        e.nodes = elementNodes;


                    }
                    else if (mList[i].Faces[i].IsTriangle)
                    {
                        Point3d a1 = mList[i].TopologyVertices[mList[i].Faces[j].A];
                        Point3d b1 = mList[i].TopologyVertices[mList[i].Faces[j].B];
                        Point3d c1 = mList[i].TopologyVertices[mList[i].Faces[j].C];

                        Point3d a2 = mList[i].TopologyVertices[mList[i + 1].Faces[j].A];
                        Point3d b2 = mList[i].TopologyVertices[mList[i + 1].Faces[j].B];
                        Point3d c2 = mList[i].TopologyVertices[mList[i + 1].Faces[j].C];

                        Node nA1 = new Node(a1, 1, "Node: " + (1).ToString());
                        Node nB1 = new Node(b1, 2, "Node: " + (2).ToString());
                        Node nC1 = new Node(c1, 3, "Node: " + (3).ToString());

                        Node nA2 = new Node(a2, 5, "Node: " + (5).ToString());
                        Node nB2 = new Node(b2, 6, "Node: " + (6).ToString());
                        Node nC2 = new Node(c2, 7, "Node: " + (7).ToString());

                        List<Node> elementNodes = new List<Node>() { nA1, nB1, nC1, nA2, nB2, nC2 };

                        e.nodes = elementNodes;
                    }
                    e.SortVerticesByGrahamScan();
                    eList.Add(e);
                    testOut = eList;

                    elID++;
                }

                // The list of elements from one layer is then added to the tree
                tree.AddRange(eList, new Grasshopper.Kernel.Data.GH_Path(i));


            }
             * 
             * 
             * */
            #endregion



            #endregion
            DA.SetDataTree(0, tree); // 0 Should be tree in final version. 

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
            get { return new Guid("d742594c-30f8-4a4c-b981-62961f3b5386"); }
        }
    }
}