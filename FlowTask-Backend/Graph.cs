using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTask_Backend
{
    class Graph
    {
        public int GraphID { get; private set; }
        public List<Node> Nodes { get; private set; }
        public List<(int,int)> Adjacencies { get; private set; }

        public Graph()
        {
            Nodes = new List<Node>();
            Adjacencies = new List<(int, int)>();
        }

        public Graph(int graphID, List<Node> nodes, string DBAdjacency)
        {
            GraphID = graphID;
            Adjacencies = new List<(int, int)>();

            Nodes = nodes;
            var tuples = DBAdjacency.Split(";");
            foreach (var s in tuples)
            {
                var coords = s.Split(",");

                int id1 = int.Parse(coords[0]);
                int id2 = int.Parse(coords[1]);

                Adjacencies.Add((id1, id2));
            }
        }

        public string GetDBFormatAdjacency()
        {
            StringBuilder sb = new StringBuilder();
            foreach(var x in Adjacencies)
                sb.Append(x.Item1).Append(",").Append(x.Item2).Append(";");
            return sb.ToString().Substring(sb.Length - 1);
        }

        public void CreateEdge(Node a, Node b)
        {
            Adjacencies.Add((a.NodeIndex, b.NodeIndex));
        }

        public void AddNode(Node n)
        {
            n.NodeIndex = Nodes.Count;
            Nodes.Add(n);
        }
    }
}
