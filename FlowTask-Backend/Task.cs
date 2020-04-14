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
        public string SubmissionDate { get; set; }
        public string Category { get; set; }
        public int UserID { get; private set; }
        public int GraphID { get; set; }

        public Graph Decomposition { get; private set; }

        public Task(int taskID, string assignmentName, int graphID, string submissionDate, string category, int userID)
        {
            TaskID = taskID;
            AssignmentName = assignmentName;
            SubmissionDate = submissionDate;
            Category = category;
            UserID = userID;
        }

        public void AddGraph(Graph g)
        {
            Decomposition = g;
        }

    }
}
