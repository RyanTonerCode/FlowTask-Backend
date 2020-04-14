using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTask_Backend
{
    public class Node
    {
        public int NodeID { get; private set; }
        public string Name { get; private set; }
        public int TimeWeight { get; private set; }
        public bool Complete { get; private set; }
        public string Text { get; private set; }
        public int GraphID { get; private set; }
        public DateTime Date { get; private set; }
        public int NodeIndex { get; internal set; }

        public Node(int nodeID, string name, int timeWeight, bool complete, DateTime date, string text, int graphid, int NodeIndex)
        {
            
        }


    }
}
