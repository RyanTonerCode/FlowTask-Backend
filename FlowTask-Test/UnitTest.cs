using FlowTask_Backend;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace FlowTask_Test
{
    [TestClass]
    public class UnitTest
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
        public void CreateUser()
        {
            string username = DateTime.Now.ToString();
            string firstName = "test_first";
            string lastName = "test_second";
            string email = "test_email";
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
            var (user, ac) = db.GetUser("a", "a");
            (bool succeeded, _, Task fullTask) = db.WriteTask(newtask, ac.Value);
            Assert.IsTrue(succeeded);
            Assert.AreEqual(newtask.AssignmentName, fullTask.AssignmentName);
            Assert.AreEqual(newtask.Category, fullTask.Category);
            
        }

        [TestMethod]
        public void TestUpdateComplete()
        {
            var (user, ac) = db.GetUser("a", "a");

            Task myTask = user.Tasks[0];

            var node1 = myTask.Decomposition.GetSoonestNode();
            node1.SetCompleteStatus(true);

            (bool succeed, String _)  = db.UpdateComplete(ac.Value, user.UserID, node1.NodeID,true);

            Assert.IsTrue(succeed);

            var node2 = myTask.Decomposition.GetSoonestNode();
            node2.SetCompleteStatus(true);

            Assert.IsTrue(node2.Date > node1.Date);

            (bool succeed2, String _) = db.UpdateComplete(ac.Value, user.UserID, myTask.Decomposition.GetSoonestNode().NodeID, true);

            Assert.IsTrue(succeed2);
        }

        [TestMethod]
        public void TestDeleteTask()
        {
            var (user, ac) = db.GetUser("a", "a");

            Task to_delete = user.Tasks[0];

            (bool succeed,string _) = db.DeleteTask(to_delete, ac.Value);
            Assert.IsTrue(succeed);
        }

    }
}