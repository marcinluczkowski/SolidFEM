using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolidFEM.Classes
{
    class Solid_Mesh
    {
        public int ID;
        public string name;
        public List<Element> elements;
        public List<Node> nodes;

        public Solid_Mesh()
        { 
            //empty constructor
        }
    }
}
