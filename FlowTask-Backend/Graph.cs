using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTask_Backend
{
    public class Graph
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
            var tuples = DBAdjacency.Split(';');
            foreach (var s in tuples)
            {
                var coords = s.Split(',');

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
            return sb.ToString().Substring(0,sb.Length - 1);
        }

        public void CreateEdge(Node a, Node b)
        {
            Adjacencies.Add((a.NodeIndex, b.NodeIndex));
        }

        public List<Node> GetNeighbors(int NodeIndex)
        {
            //cool functional code to do this
            var neighbors = Adjacencies.Where(x => x.Item1 == NodeIndex).Select(x => Nodes[x.Item2]).ToList();
            Comparison<Node> comp = new Comparison<Node>((x, y) => x.NodeIndex == y.NodeIndex ? 0 : x.NodeIndex < y.NodeIndex ? -1 : 1);
            neighbors.Sort(comp);
            return neighbors;
        }

        public void AddNode(Node n)
        {
            n.NodeIndex = Nodes.Count;
            Nodes.Add(n);
        }

        public void AddNodes(params Node[] nodes)
        {
            Nodes.AddRange(nodes);
        }

        public DateTime GetSoonestDate()
        {
            return Nodes.Min(x => x.Date);
        }
    }
}
