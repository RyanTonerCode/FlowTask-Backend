using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTask_Backend
{
    public class Task
    {
        public int TaskID { get; set; }
        public string AssignmentName { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string Category { get; set; }
        public int UserID { get; private set; }
        public int GraphID { get; set; }

        public Graph Decomposition { get; private set; }

        public Task(int taskID, string assignmentName, int graphID, DateTime submissionDate, string category, int userID)
        {
            TaskID = taskID;
            AssignmentName = assignmentName.Trim();
            SubmissionDate = submissionDate;
            Category = category.Trim();
            UserID = userID;
            GraphID = graphID;
        }

        public Task(string assignmentName, DateTime submissionDate, string category, int userID)
        {
            AssignmentName = assignmentName.Trim();
            SubmissionDate = submissionDate;
            Category = category.Trim();
            UserID = userID;
        }

        public void AddGraph(Graph g)
        {
            Decomposition = g;
        }

        public int RemainingFlowSteps {
            get
            {
                if (Decomposition == null || Decomposition.Nodes == null || Decomposition.Nodes.Count == 0)
                    return 0;
                return Decomposition.Nodes.Where(x => !x.Complete).Count() - 1;
            }
        }

    }
}
