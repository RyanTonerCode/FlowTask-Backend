using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTask_Backend
{
    class Graph
    {
        public int GraphID { get; set; }
        internal Node[] Nodes { get; set; }
        public int[][] Adjacencies { get; set; }

        public Graph(int graphID, Node[] nodes, int[][] adjacencies )
        {

        }

        DateTime processDate(Node n)
        {
            return (DateTime)(new object());
        }

        int createEdge(Node a, Node b)
        {
            return 0;
        }

        Node createNode(String process, int timeweight, bool complete)
        {
            return null;
        }
    }
}
