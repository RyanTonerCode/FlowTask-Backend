using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTask_Backend
{
    class Task
    {
        public int TaskID { get; set; }
        public string AssignmentName { get; set; }
        public int DecompID { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string Category { get; set; }
        public int[] UserIDs { get; set; }
        internal Decomposition Decomp { get; set; }

        public Task(int taskID, string assignmentName, int decompID, DateTime submissionDate, string category, int[] userIDs)
        {

        }

        static Task CreateTask(int[] userIDs, String name)
        {
            return null;
        }

        static void ShareTask(int taskID, int[] userIDs)
        {
            return;
        }

        string CategorizeText(Task task, string text)
        {
            return "";
        }

    }
}
