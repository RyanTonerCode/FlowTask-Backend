using FlowTask_Backend;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace FlowTask_Test
{
    [TestClass]
    public class DatabaseDriver
    {

        private static readonly DatabaseController db = DatabaseController.GetDBController();

        [TestMethod]
        public void TestDatabaseConnection()
        {
            Assert.IsNotNull(db);
        }

        [TestMethod]
        public void TestDatabasePasswordEncryption()
        {
            var characterSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[50];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
                stringChars[i] = characterSet[random.Next(characterSet.Length)];

            var finalString = new string(stringChars);

            var aes_bytes = DatabaseController.Encrypter.EncryptStringToBytes_Aes(finalString);
            var decrypted_string = DatabaseController.Encrypter.DecryptStringFromBytes_Aes(aes_bytes);

            Assert.AreEqual(finalString, decrypted_string);
        }

        [TestMethod]
        public void TestCreateUser()
        {
            string username = DateTime.Now.ToString().ToLowerInvariant();
            string email = username;
            string firstName = "test_first";
            string lastName = "test_last";
            string password = "test_password";

            User newUser = new User(password, username, firstName, lastName, email);

            (bool succeeded, _) = db.WriteUser(newUser);

            Assert.IsTrue(succeeded);

            var (user, ac) = db.GetUser(username, password);
            Assert.IsTrue(ac.HasValue);
            Assert.IsNotNull(user);

            Assert.AreEqual(username, user.Username);
            Assert.AreEqual(firstName, user.FirstName);
            Assert.AreEqual(lastName, user.LastName);
            Assert.AreEqual(email, user.Email);
        }

        [TestMethod]
        public void TestLogin()
        {
            var (user, ac) = db.GetUser("a", "a");
            Assert.IsTrue(ac.HasValue);
            Assert.IsNotNull(user);
            Assert.AreEqual(user.Username, "a");
        }

        [TestMethod]
        public void TestWriteTask()
        {
            DateTime date = DateTime.Now.AddDays(14);
            Task newtask = new Task("Project", date, "Research Paper", 16);

            (User user, AuthorizationCookie? ac) = db.GetUser("a", "a");
            (bool succeeded, _, Task fullTask) = db.WriteTask(newtask, ac.Value);
            Assert.IsTrue(succeeded);
            Assert.AreEqual(fullTask.UserID, user.UserID);
            Assert.AreEqual(newtask.AssignmentName, fullTask.AssignmentName);
            Assert.AreEqual(newtask.Category, fullTask.Category);

            TestUpdateComplete(fullTask, user, ac.Value); //update nodes in the task to complete
        }
    
        public void TestUpdateComplete(Task newtask, User user, AuthorizationCookie ac)
        {
            Node node1 = newtask.Decomposition.GetSoonestNode();
            node1.SetCompleteStatus(true);

            (bool succeed, _) = db.UpdateComplete(ac, user.UserID, node1.NodeID,true);

            Assert.IsTrue(succeed);

            var node2 = newtask.Decomposition.GetSoonestNode();
            node2.SetCompleteStatus(true);

            Assert.IsTrue(node2.Date > node1.Date);

            (bool succeed2, _) = db.UpdateComplete(ac, user.UserID, node2.NodeID, true);

            Assert.IsTrue(succeed2);

            //refresh the user
            (user, _) = db.GetUser("a", "a");

            Task updatedTask = user.Tasks.First(x => x.TaskID == newtask.TaskID);
            Assert.IsTrue(updatedTask.RemainingFlowSteps == newtask.RemainingFlowSteps);

            Assert.IsTrue(updatedTask.Decomposition.Nodes.First(x => x.NodeIndex == node1.NodeIndex).Complete);
            Assert.IsTrue(updatedTask.Decomposition.Nodes.First(x => x.NodeIndex == node2.NodeIndex).Complete);

            TestDeleteTask(updatedTask, user, ac); //delete the task
        }
  
        public void TestDeleteTask(Task to_delete, User user, AuthorizationCookie ac)
        {
            //refresh the user
            (user, _) = db.GetUser("a", "a");
            int totalTasks = user.Tasks.Count;

            (bool succeed, _) = db.DeleteTask(to_delete, ac);
            Assert.IsTrue(succeed);

            (user, _) = db.GetUser("a", "a");

            Assert.IsTrue(user.Tasks.Count == totalTasks - 1);
            Assert.IsNull(user.Tasks.Find(x => x.TaskID == to_delete.TaskID));
        }

    }
}