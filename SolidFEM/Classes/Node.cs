using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace SolidFEM.Classes
{
    class Node
    {
        public int ID;
        public string name;
        public Point3d point;

        public Node()
        {
            //empty constructor
        }

        public Node(Point3d _point, int _id)
        {
            ID = _id;
            point = _point;
            name = "Node: " + ID.ToString();
        }

        public Node(Point3d _point, int _id, string _name)
        {
            ID = _id;
            name = _name;
            point = _point;
        }

    }
}
