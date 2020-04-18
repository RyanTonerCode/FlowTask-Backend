using System;
using System.Linq;

namespace FlowTask_Backend
{
    /// <summary>
    /// Well. We are called FlowTask, so here's our actual Task class!
    /// </summary>
    public class Task
    {
        /// <summary>
        /// Primary Key
        /// </summary>
        public int TaskID { get; set; }

        /// <summary>
        /// Name of this assignment
        /// </summary>
        public string AssignmentName { get; set; }

        /// <summary>
        /// Final submission deadline
        /// </summary>
        public DateTime SubmissionDate { get; set; }

        /// <summary>
        /// The assignment type
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Primary key of the user this belongs to
        /// </summary>
        public int UserID { get; private set; }

        /// <summary>
        /// The graph ID associated with this Task
        /// </summary>
        public int GraphID { get; set; }

        /// <summary>
        /// The decomposition of this task
        /// </summary>
        public Graph Decomposition { get; private set; }

        /// <summary>
        /// Constructor for the DB
        /// </summary>
        /// <param name="taskID"></param>
        /// <param name="assignmentName"></param>
        /// <param name="graphID"></param>
        /// <param name="submissionDate"></param>
        /// <param name="category"></param>
        /// <param name="userID"></param>
        public Task(int taskID, string assignmentName, int graphID, DateTime submissionDate, string category, int userID)
        {
            TaskID = taskID;
            AssignmentName = assignmentName.Trim();
            SubmissionDate = submissionDate;
            Category = category.Trim();
            UserID = userID;
            GraphID = graphID;
        }

        /// <summary>
        /// Constructor for the front end
        /// </summary>
        /// <param name="assignmentName"></param>
        /// <param name="submissionDate"></param>
        /// <param name="category"></param>
        /// <param name="userID"></param>
        public Task(string assignmentName, DateTime submissionDate, string category, int userID)
        {
            AssignmentName = assignmentName.Trim();
            SubmissionDate = submissionDate;
            Category = category.Trim();
            UserID = userID;
        }

        /// <summary>
        /// Sets the graph of this task
        /// </summary>
        /// <param name="g"></param>
        public void AddGraph(Graph g)
        {
            Decomposition = g;
        }

        /// <summary>
        /// The number of incomplete nodes in the decomposition
        /// </summary>
        public int RemainingFlowSteps
        {
            get
            {
                if (Decomposition == null || Decomposition.Nodes == null || Decomposition.Nodes.Count == 0)
                    return 0;
                //filter out the first node.
                return Decomposition.Nodes.Where(x => !x.Complete).Count();
            }
        }

    }
}
