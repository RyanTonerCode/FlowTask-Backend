using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlowTask_Backend
{
    /// <summary>
    /// The graph class encapsulates the decomposition for a Task.
    /// </summary>
    public class Graph
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public int GraphID { get; private set; }
        /// <summary>
        /// List of all nodes this graph stores
        /// </summary>
        public List<Node> Nodes { get; private set; }
        /// <summary>
        /// List of all adjacent pairs in tuple form (X,Y) means Y is a neighbor of X.
        /// </summary>
        public List<(int X, int Y)> Adjacencies { get; private set; }

        /// <summary>
        /// Construct an empty graph
        /// </summary>
        public Graph()
        {
            Nodes = new List<Node>();
            Adjacencies = new List<(int, int)>();
        }

        /// <summary>
        /// Construct a graph from the database
        /// </summary>
        /// <param name="graphID"></param>
        /// <param name="nodes"></param>
        /// <param name="DB_Adjacency"></param>
        public Graph(int graphID, List<Node> nodes, string DB_Adjacency)
        {
            GraphID = graphID;
            Adjacencies = new List<(int, int)>();

            Nodes = nodes;
            //process adjacencies by semi-colon delimited values
            var tuples = DB_Adjacency.Split(';');
            foreach (var s in tuples)
            {
                var coords = s.Split(',');

                int id1 = int.Parse(coords[0]);
                int id2 = int.Parse(coords[1]);

                //process pairs by comma delimited values
                Adjacencies.Add((id1, id2));
            }
        }

        /// <summary>
        /// Formats the adjacency list to be stored efficiently in the DB
        /// </summary>
        /// <returns></returns>
        public string GetDBFormatAdjacency()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var (X, Y) in Adjacencies)
                sb.Append(X).Append(",").Append(Y).Append(";");
            return sb.ToString().Substring(0, sb.Length - 1);
        }

        /// <summary>
        /// Place an edge from node A to node B
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public void CreateEdge(Node a, Node b)
        {
            Adjacencies.Add((a.NodeIndex, b.NodeIndex));
        }

        /// <summary>
        /// Function to compare nodes
        /// </summary>
        public static Comparison<Node> CompareNodes = new Comparison<Node>((x, y) => x.NodeIndex == y.NodeIndex ? 0 : x.NodeIndex < y.NodeIndex ? -1 : 1);

        /// <summary>
        /// Get all the neighbors of a node
        /// </summary>
        /// <param name="NodeIndex">Specify the node index</param>
        /// <returns></returns>
        public List<Node> GetNeighbors(int NodeIndex)
        {
            //cool functional code to do this
            var neighbors = Adjacencies.Where(x => x.X == NodeIndex).Select(x => Nodes[x.Y]).ToList();

            //Sort the nodes first
            neighbors.Sort(CompareNodes);

            return neighbors;
        }

        /// <summary>
        /// Add a node to the graph
        /// Automatically sets the node index (for procedural adding)
        /// </summary>
        /// <param name="n"></param>
        public void AddNode(Node n)
        {
            n.NodeIndex = Nodes.Count;
            Nodes.Add(n);
        }

        /// <summary>
        /// Add many nodes to this graph
        /// </summary>
        /// <param name="nodes"></param>
        public void AddNodes(params Node[] nodes)
        {
            Nodes.AddRange(nodes);
        }

        /// <summary>
        /// Return the soonest date
        /// </summary>
        /// <returns></returns>
        public DateTime GetSoonestDate()
        {
            //return the min date from the incomplete nodes.
            var incomplete = Nodes.Where(x => !x.Complete);

            //return the last node if there are none ncomplete
            if (incomplete.Count() == 0)
            {
                if(Nodes.Count > 0)
                    return Nodes[0].Date;
                return DateTime.Now;
            }

            //otherwise actually return the min
            return incomplete.Min(x => x.Date);
        }

        /// <summary>
        /// Returns the soonest node
        /// </summary>
        /// <returns></returns>
        public Node GetSoonestNode()
        {
            DateTime soon = GetSoonestDate();
            return Nodes.Where(x => !x.Complete).Where(x => x.Date == soon).FirstOrDefault();
        }
    }
}
