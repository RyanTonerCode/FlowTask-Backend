using System;

namespace FlowTask_Backend
{
    /// <summary>
    /// Node values to be stored in a graph decomposition
    /// </summary>
    public class Node
    {
        /// <summary>
        /// Primary Key
        /// </summary>
        public int NodeID { get; private set; }

        /// <summary>
        /// The name of this node
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The time weight associated with traveling to this node (not currently used)
        /// </summary>
        public int TimeWeight { get; private set; }

        /// <summary>
        /// Whether or not this node has been completed
        /// </summary>
        public bool Complete { get; private set; }

        /// <summary>
        /// The text associated with this node e.g. description/ caption/ qualification
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// The graph's primary key this node belongs to
        /// </summary>
        public int GraphID { get; private set; }

        /// <summary>
        /// The date this node is due
        /// </summary>
        public DateTime Date { get; private set; }

        /// <summary>
        /// The node index relative to the graph's adjacency matrix
        /// </summary>
        public int NodeIndex { get; internal set; }

        /// <summary>
        /// Constructor to be used by the database
        /// </summary>
        /// <param name="nodeID"></param>
        /// <param name="name"></param>
        /// <param name="timeWeight"></param>
        /// <param name="complete"></param>
        /// <param name="date"></param>
        /// <param name="text"></param>
        /// <param name="graphid"></param>
        /// <param name="nodeIndex"></param>
        public Node(int nodeID, string name, int timeWeight, bool complete, DateTime date, string text, int graphid, int nodeIndex)
        {
            NodeID = nodeID;
            Name = name.Trim();
            TimeWeight = timeWeight;
            Complete = complete;
            Text = text.Trim();
            GraphID = graphid;
            Date = date;
            NodeIndex = nodeIndex;
        }

        /// <summary>
        /// Constructor for external use
        /// </summary>
        /// <param name="name"></param>
        /// <param name="timeWeight"></param>
        /// <param name="complete"></param>
        /// <param name="date"></param>
        /// <param name="text"></param>
        /// <param name="graphid"></param>
        /// <param name="nodeIndex"></param>
        public Node(string name, int timeWeight, bool complete, DateTime date, string text, int graphid, int nodeIndex)
        {
            Name = name.Trim();
            TimeWeight = timeWeight;
            Complete = complete;
            Text = text.Trim();
            GraphID = graphid;
            Date = date;
            NodeIndex = nodeIndex;
        }

        /// <summary>
        /// Sets the complete value to the provided new value
        /// </summary>
        /// <param name="complete"></param>
        public void SetCompleteStatus(bool complete)
        {
            Complete = complete;
        }

    }
}
