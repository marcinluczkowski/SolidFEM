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
        public string Name;
        public Point3d Point;

        public Node()
        {
            //empty constructor
        }
        public Node(Point3d point, int id, string name)
        {
            ID = id;
            Point = point;
            Name = name;
        }

    }
}
