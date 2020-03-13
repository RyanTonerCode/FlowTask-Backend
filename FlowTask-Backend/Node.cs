using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTask_Backend
{
    class Node
    {
        public int NodeID { get; set; }
        public string Name { get; set; }
        public int TimeWeight { get; set; }
        public bool Complete { get; set; }

        public Node(int nodeID, string name, int timeWeight, bool complete)
        {
            
        }


    }
}
