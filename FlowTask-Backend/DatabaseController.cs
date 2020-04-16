﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;

namespace FlowTask_Backend
{
    public class DatabaseController
    {
        private static DatabaseController singleton;

        private static readonly Dictionary<int, AuthorizationCookie> logins = new Dictionary<int, AuthorizationCookie>();

        private static readonly RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();

        private class Encrypter{

            private static readonly byte[] Key = { 203, 181, 150, 14, 78, 58, 151, 106, 149, 77, 124, 227, 219, 155, 123, 40, 30, 99, 213, 33, 67, 96, 50, 206, 177, 137, 171, 119, 166, 94, 75, 230 };
            private static readonly byte[] IV = { 159, 66, 13, 210, 131, 209, 219, 111, 108, 87, 128, 240, 84, 68, 62, 219 };

            public static byte[] EncryptStringToBytes_Aes(string plainText)
            {
                // Check arguments.
                if (plainText == null || plainText.Length <= 0)
                    throw new ArgumentNullException("plainText");
                if (Key == null || Key.Length <= 0)
                    throw new ArgumentNullException("Key");
                if (IV == null || IV.Length <= 0)
                    throw new ArgumentNullException("IV");
                byte[] encrypted;

                // Create an Aes object
                // with the specified key and IV.
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Key;
                    aesAlg.IV = IV;

                    // Create an encryptor to perform the stream transform.
                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    // Create the streams used for encryption.
                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                //Write all data to the stream.
                                swEncrypt.Write(plainText);
                            }
                            encrypted = msEncrypt.ToArray();
                        }
                    }
                }

                // Return the encrypted bytes from the memory stream.
                return encrypted;
            }

            public static string DecryptStringFromBytes_Aes(byte[] cipherText)
            {
                // Check arguments.
                if (cipherText == null || cipherText.Length <= 0)
                    throw new ArgumentNullException("cipherText");
                if (Key == null || Key.Length <= 0)
                    throw new ArgumentNullException("Key");
                if (IV == null || IV.Length <= 0)
                    throw new ArgumentNullException("IV");

                // Declare the string used to hold
                // the decrypted text.
                string plaintext = null;

                // Create an Aes object
                // with the specified key and IV.
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Key;
                    aesAlg.IV = IV;

                    // Create a decryptor to perform the stream transform.
                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    // Create the streams used for decryption.
                    using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {

                                // Read the decrypted bytes from the decrypting stream
                                // and place them in a string.
                                plaintext = srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }

                return plaintext;
            }
        }

        /// <summary>
        /// singleton database controller
        /// </summary>
        public static DatabaseController dbController { 
            get {
                return singleton ?? (singleton=new DatabaseController());
            }
        }

        SQLiteConnection connection;

        public DatabaseController()
        {
            Connect();
        }

        private void Connect()
        {
            string dbpath = @"C:\Users\Ryan\Source\Repos\FlowTask-Backend\FlowTask-Backend\flowtaskdb.db";
            connection = new SQLiteConnection("Data Source=" + dbpath);
            connection.Open();
        }

        private (bool AccountExists, string ErrorCode) accountExists(string username, string email)
        {
            const string sqlQuery = @"SELECT Username, Email FROM UserTable WHERE Username = @username OR Email = @email";

            SQLiteCommand cmd = new SQLiteCommand(sqlQuery, connection);
            cmd.Parameters.AddWithValue("@username", username.ToLowerInvariant().Trim());
            cmd.Parameters.AddWithValue("@email", email);

            SQLiteDataReader sdr = cmd.ExecuteReader();
            
            bool usernameTaken = false;
            bool emailTaken = false;

            while(sdr.Read())
            {
                if (sdr.GetString(0).Equals(username))
                    usernameTaken = true;
                if (sdr.GetString(1).Equals(email))
                    emailTaken = true;
            }

            if (usernameTaken && emailTaken)
                return (false, "Sorry, please use another email and username.");
            if (usernameTaken)
                return (false, "Please try another username.");
            if (emailTaken)
                return (false, "Please try another email.");

            return (true, "Successfully created your account.");
        }

        public (bool, string) WriteUser(User user)
        {
            var availability = accountExists(user.Username, user.Email);
            if (availability.AccountExists == false)
                return availability;

            //hash the user's password here

            string query = "INSERT INTO UserTable (HashedPassword, Username, FirstName, LastName, Email)";
            query += " VALUES (@HashedPassword, @Username, @FirstName, @LastName, @Email)";

            SQLiteCommand myCommand = new SQLiteCommand(query, connection);
            myCommand.Parameters.AddWithValue("@HashedPassword", Encrypter.EncryptStringToBytes_Aes(user.HashedPassword));
            myCommand.Parameters.AddWithValue("@Username", user.Username);
            myCommand.Parameters.AddWithValue("@FirstName", user.FirstName);
            myCommand.Parameters.AddWithValue("@LastName", user.LastName);
            myCommand.Parameters.AddWithValue("@Email", user.Email);

            // ... other parameters
            int rowsAffected = myCommand.ExecuteNonQuery();

            myCommand.Dispose();

            if (rowsAffected == 0)
                return (false, "Failed");

            return (true, "Success");
        }

        private AuthorizationCookie getAuthCookie()
        {
            byte[] randomAuth = new byte[256];

            rngCsp.GetBytes(randomAuth, 0, randomAuth.Length);

            return new AuthorizationCookie(randomAuth);
        }


    public (User user, AuthorizationCookie ac) GetUser(string username, string hashedpassword)
        {
            username = username.ToLowerInvariant().Trim();

            const string sqlQuery = @"SELECT * FROM UserTable WHERE Username = @user AND HashedPassword = @hash";

            SQLiteCommand cmd = new SQLiteCommand(sqlQuery, connection);
            cmd.Parameters.AddWithValue("@user", username);
            cmd.Parameters.AddWithValue("@hash", Encrypter.EncryptStringToBytes_Aes(hashedpassword));

            SQLiteDataReader sdr = cmd.ExecuteReader();

            if (!sdr.HasRows)
                return (null, new AuthorizationCookie());

            sdr.Read();

            User user = new User(sdr.GetInt32(0), "", sdr.GetString(2), sdr.GetString(3), sdr.GetString(4), sdr.GetString(5));

            AuthorizationCookie ac = getAuthCookie();

            logins.Add(user.UserId, ac);

            getTasks(user);

            return (user, ac);

        }

        private List<Node> getNodes(int GraphID)
        {
            const string sqlQuery = @"SELECT * FROM Node WHERE GraphID = @id";

            SQLiteCommand cmd = new SQLiteCommand(sqlQuery, connection);
            cmd.Parameters.AddWithValue("@id", GraphID);

            SQLiteDataReader sdr = cmd.ExecuteReader();

            if (sdr.HasRows)
            {
                List<Node> nodes = new List<Node>(20);

               while(sdr.Read())
                    nodes.Add(new Node(sdr.GetInt32(0), sdr.GetString(1), sdr.GetInt32(2), sdr.GetInt32(3) == 1, DateTime.Parse(sdr.GetString(4)), sdr.GetString(5), sdr.GetInt32(6), sdr.GetInt32(7)));


                Comparison<Node> comp = new Comparison<Node>((x,y) => x.NodeIndex == y.NodeIndex ? 0 : x.NodeIndex < y.NodeIndex ? -1 : 1);

                nodes.Sort(comp);
                
                return nodes;
            }

            return new List<Node>();


        }

        private (bool succeeded, string result, int NodeID) writeNode(Node node)
        {
            string query = "INSERT INTO Node (Name, TimeWeight, Complete, Date, Text, GraphID, NodeIndex)";
            query += " VALUES (@Name, @TimeWeight, @Complete, @Date, @Text, @GraphID, @NodeIndex)";

            SQLiteCommand myCommand = new SQLiteCommand(query, connection);
            myCommand.Parameters.AddWithValue("@Name", node.Name);
            myCommand.Parameters.AddWithValue("@TimeWeight", node.TimeWeight);
            myCommand.Parameters.AddWithValue("@Complete", node.Complete);
            myCommand.Parameters.AddWithValue("@Date", node.Date);
            myCommand.Parameters.AddWithValue("@Text", node.Text);
            myCommand.Parameters.AddWithValue("@GraphID", node.GraphID);
            myCommand.Parameters.AddWithValue("@NodeIndex", node.NodeIndex);

            int rowsAffected = myCommand.ExecuteNonQuery();

            myCommand.Dispose();

            int nodeID = getID("Node");

            if (rowsAffected == 0)
                return (false, "Failed", nodeID);

            return (true, "Success", nodeID);
        }

        private (bool, string) writeGraph(Graph g)
        {
            string query = "INSERT INTO Graph (AdjacencyMatrix)";
            query += " VALUES (@AdjacencyMatrix)";

            SQLiteCommand myCommand = new SQLiteCommand(query, connection);
            myCommand.Parameters.AddWithValue("@AdjacencyMatrix", g.GetDBFormatAdjacency());

            int rowsAffected = myCommand.ExecuteNonQuery();

            myCommand.Dispose();

            if (rowsAffected == 0)
                return (false, "Failed");

            return (true, "Success");
        }


        private Graph getGraph(int GraphID)
        {
            var nodes = getNodes(GraphID);

            const string sqlQuery = @"SELECT * FROM Graph WHERE GraphID = @id";

            SQLiteCommand cmd = new SQLiteCommand(sqlQuery, connection);
            cmd.Parameters.AddWithValue("@id", GraphID);

            SQLiteDataReader sdr = cmd.ExecuteReader();

            if (!sdr.HasRows)
                return null;

            sdr.Read();

            var graph = new Graph(sdr.GetInt32(0), nodes, sdr.GetString(1));

            return graph;
        }

        private bool CheckValidAuthCookie(int userID, AuthorizationCookie ac) => logins.ContainsKey(userID) && logins[userID].BitString.Equals(ac.BitString);

        public (bool Result, string FailureString, Task fullTask) WriteTask(Task task, AuthorizationCookie ac)
        {
            if(!CheckValidAuthCookie(task.UserID,ac))
                return (false, "Invalid login!", null);


            string query = "INSERT INTO Task (AssignmentName, GraphID, SubmissionDate, Category, UserID)";
            query += " VALUES (@AssignmentName, @GraphID, @SubmissionDate, @Category, @UserID)";

            //get graph id before anything
            int graphID = getID("Graph") + 1;

            //create a graph first...
            Graph g = new Graph();

            if (task.Category.Equals("Research Paper"))
            {
                Node header = new Node(task.Category, 0, false, task.SubmissionDate, "", graphID, 0);
                Node n1 = new Node("Collect Sources", 0, false, task.SubmissionDate.AddDays(-11), "Research", graphID, 1);
                Node n2 = new Node("Write Thesis", 0, false, task.SubmissionDate.AddDays(-10), "", graphID, 2);
                Node n3 = new Node("Introduction", 0, false, task.SubmissionDate.AddDays(-9), "", graphID, 3);
                Node n4 = new Node("Body Paragraph 1", 0, false, task.SubmissionDate.AddDays(-7), "", graphID, 4);
                Node n5 = new Node("Body Paragraph 2", 0, false, task.SubmissionDate.AddDays(-5), "", graphID, 5);
                Node n6 = new Node("Body Paragraph 3", 0, false, task.SubmissionDate.AddDays(-3), "", graphID, 6);
                Node n7 = new Node("Conclusion", 0, false, task.SubmissionDate.AddDays(-2), "", graphID, 7);
                Node n8 = new Node("Citations", 0, false, task.SubmissionDate.AddDays(-1), "", graphID, 8);

                g.AddNodes(header, n1, n2, n3, n4, n5, n6, n7, n8);
                g.CreateEdge(header, n1);
                g.CreateEdge(header, n2);
                g.CreateEdge(header, n3);
                g.CreateEdge(header, n4);
                g.CreateEdge(header, n5);
                g.CreateEdge(header, n6);
                g.CreateEdge(header, n7);
                g.CreateEdge(header, n8);

                var result_graph = writeGraph(g);
                foreach(Node n in g.Nodes)
                    writeNode(n);

            }
            else if(task.Category.Equals("Agile Software Project"))
            {
                Node header = new Node(task.Category, 0, false, task.SubmissionDate, "", graphID, 0);
                Node n1 = new Node("Sprint 1", 0, false, task.SubmissionDate.AddDays(-13), "Deliverable", graphID, 1);
                Node n1_1 = new Node("Create Wireframe", 0, false, task.SubmissionDate.AddDays(-13), "Lucidchart", graphID, 2);
                Node n2 = new Node("Sprint 2", 0, false, task.SubmissionDate.AddDays(-7), "Deliverable", graphID, 3);
                Node n2_1 = new Node("Create SQLite Database", 0, false, task.SubmissionDate.AddDays(-10), "", graphID, 4);
                Node n2_2 = new Node("Write Web API", 0, false, task.SubmissionDate.AddDays(-9), "REST API", graphID, 5);
                Node n2_3 = new Node("Code Review", 0, false, task.SubmissionDate.AddDays(-6), "Pair Programming", graphID, 6);
                Node n3 = new Node("Sprint 3", 0, false, task.SubmissionDate, "Deliverable", graphID, 7);
                Node n3_1 = new Node("Unit Test", 0, false, task.SubmissionDate.AddDays(-2), "Ryan and Kyle", graphID, 8);
                Node n3_2 = new Node("Release Version 1", 0, false, task.SubmissionDate.AddDays(-1), "GitHub Release", graphID, 9);

                g.AddNodes(header, n1, n1_1, n2, n2_1, n2_2, n2_3, n3, n3_1, n3_2);
                g.CreateEdge(header, n1);
                g.CreateEdge(header, n2);
                g.CreateEdge(header, n3);
                g.CreateEdge(n1, n1_1);
                g.CreateEdge(n2, n2_1);
                g.CreateEdge(n2, n2_2);
                g.CreateEdge(n2, n2_3);
                g.CreateEdge(n3, n3_1);
                g.CreateEdge(n3, n3_2);

                var result_graph = writeGraph(g);
                foreach (Node n in g.Nodes)
                    writeNode(n);
            }

            SQLiteCommand myCommand = new SQLiteCommand(query, connection);
            myCommand.Parameters.AddWithValue("@AssignmentName", task.AssignmentName);
            myCommand.Parameters.AddWithValue("@GraphID", graphID);
            myCommand.Parameters.AddWithValue("@SubmissionDate", task.SubmissionDate);
            myCommand.Parameters.AddWithValue("@Category", task.Category);
            myCommand.Parameters.AddWithValue("@UserID", task.UserID);

            // ... other parameters
            int rowsAffected = myCommand.ExecuteNonQuery();

            if (rowsAffected == 0)
                return (false, "Failed", null);

            myCommand.Dispose();

            var id = getID("Task");

            Task return_task = new Task(id, task.AssignmentName, graphID, task.SubmissionDate, task.Category, task.UserID);

            return_task.AddGraph(g);

            return (true, "Success", return_task);
        }

        private int getID(string tablename)
        {
            var query = "Select seq from sqlite_sequence where name=@Name";
            var myCommand = new SQLiteCommand(query, connection);
            myCommand.Parameters.AddWithValue("@Name", tablename);

            SQLiteDataReader sdr = myCommand.ExecuteReader();
            if (sdr.HasRows)
            {
                sdr.Read();

                return sdr.GetInt32(0);
            }
            else
                return -1;
        }


        private List<Task> getTasks(User user)
        {

            const string sqlQuery = @"SELECT * FROM Task WHERE UserID = @id";

            SQLiteCommand cmd = new SQLiteCommand(sqlQuery, connection);
            cmd.Parameters.AddWithValue("@id", user.UserId);

            SQLiteDataReader sdr = cmd.ExecuteReader();

            List<Task> tasks = user.Tasks;

            while (sdr.HasRows && sdr.Read())
            {
                Task t = new Task(sdr.GetInt32(0), sdr.GetString(1), sdr.GetInt32(2), sdr.GetDateTime(3), sdr.GetString(4), sdr.GetInt32(5));

                Graph g = getGraph(t.GraphID);

                t.AddGraph(g);

                tasks.Add(t);
            }

            return tasks;


        }

        public (bool Success, string ErrorMessage) DeleteTask(Task t, AuthorizationCookie ac)
        {
            if (!CheckValidAuthCookie(t.UserID, ac))
                return (false, "Invalid login!");


            const string sqlQuery = @"DELETE FROM Task WHERE TaskID = @id";

            SQLiteCommand cmd = new SQLiteCommand(sqlQuery, connection);
            cmd.Parameters.AddWithValue("@id", t.TaskID);

            SQLiteDataReader sdr = cmd.ExecuteReader();

            deleteGraph(t.GraphID);

            if (sdr.RecordsAffected == 1)
                return (true, "Successfully deleted your task!");

            return (false, "Failed to delete the task");
        }

        private (bool Success, string ErrorMessage) deleteGraph(int graphID)
        {
            deleteNodes(graphID);

            const string sqlQuery = @"DELETE FROM Graph WHERE GraphID = @id";

            SQLiteCommand cmd = new SQLiteCommand(sqlQuery, connection);
            cmd.Parameters.AddWithValue("@id", graphID);

            SQLiteDataReader sdr = cmd.ExecuteReader();

            if (sdr.RecordsAffected == 1)
                return (true, "Successfully deleted the graph.");

            return (false, "Failed to delete the graph");
        }

        private (bool Success, string ErrorMessage) deleteNodes(int graphID)
        {
            const string sqlQuery = @"DELETE FROM Node WHERE GraphID = @id";

            SQLiteCommand cmd = new SQLiteCommand(sqlQuery, connection);
            cmd.Parameters.AddWithValue("@id", graphID);

            SQLiteDataReader sdr = cmd.ExecuteReader();

            if (sdr.RecordsAffected == 1)
                return (true, "Successfully deleted nodes.");

            return (false, "Failed to delete nodes");
        }



    }
}
