using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;

namespace FlowTask_Backend
{
    /// <summary>
    /// The database interface for the outside world
    /// </summary>
    public class DatabaseController
    {
        /// <summary>
        /// Uses singleton design pattern
        /// </summary>
        private static DatabaseController dbSingleton;

        /// <summary>
        /// Stores a mapping of active logins of UUID -> AuthCookie
        /// </summary>
        private static readonly Dictionary<int, AuthorizationCookie> activeLogins = new Dictionary<int, AuthorizationCookie>();

        /// <summary>
        /// Checks whether or not the provided auth cookie is valid for the given user id.
        /// </summary>
        /// <param name="UserID"></param>
        /// <param name="ac"></param>
        /// <returns></returns>
        private bool CheckValidAuthCookie(int UserID, AuthorizationCookie ac) => activeLogins.ContainsKey(UserID) && activeLogins[UserID].GetBitString().Equals(ac.GetBitString());

        /// <summary>
        /// Generate random bytes securely
        /// </summary>
        private static readonly RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();

        /// <summary>
        /// Hidden internal class. Clearly the Key and IV value should not be hardcoded, but since this isn't deployed, it doesn't matter.
        /// </summary>
        private class Encrypter
        {

            private static readonly byte[] Key = { 203, 181, 150, 14, 78, 58, 151, 106, 149, 77, 124, 227, 219, 155, 123, 40, 30, 99, 213, 33, 67, 96, 50, 206, 177, 137, 171, 119, 166, 94, 75, 230 };
            private static readonly byte[] IV = { 159, 66, 13, 210, 131, 209, 219, 111, 108, 87, 128, 240, 84, 68, 62, 219 };

            /// <summary>
            /// Generate a hash for the given plain text.
            /// </summary>
            /// <param name="plainText"></param>
            /// <returns></returns>
            public static byte[] EncryptStringToBytes_Aes(string plainText)
            {
                // Check arguments.
                if (plainText == null || plainText.Length <= 0)
                    throw new ArgumentNullException(nameof(plainText));
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

            /// <summary>
            /// Decryot the hash (for verification purposes)
            /// </summary>
            /// <param name="cipherText"></param>
            /// <returns></returns>
            public static string DecryptStringFromBytes_Aes(byte[] cipherText)
            {
                // Check arguments.
                if (cipherText == null || cipherText.Length <= 0)
                    throw new ArgumentNullException(nameof(cipherText));
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
        /// Singleton database controller
        /// </summary>
        public static DatabaseController GetDBController(bool IsLocal = false)
        {
            return dbSingleton ?? (dbSingleton = new DatabaseController(IsLocal));
        }

        /// <summary>
        /// Connection to SQLite database
        /// </summary>
        private SQLiteConnection connection;

        /// <summary>
        /// Connect upon construction.
        /// </summary>
        public DatabaseController(bool IsLocal) => Connect(IsLocal);

        private void Connect(bool IsLocal)
        {
            //SET THIS DB PATH TO WHERE IT NEEDS TO BE
            string dbpath;
            if(IsLocal)
                dbpath = @"C:\Users\Ryan\Source\Repos\FlowTask-Backend\FlowTask-Backend\flowtaskdb.db";
            else
                dbpath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\flowtaskdb.db";
            connection = new SQLiteConnection("Data Source=" + dbpath);
            connection.Open();
        }


        /// <summary>
        /// Returns whether or not an account exists with the given username OR email
        /// </summary>
        /// <param name="username"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        private (bool AccountExists, string ErrorCode) accountExists(string username, string email)
        {
            const string query = @"SELECT Username, Email FROM UserTable WHERE Username = @username OR Email = @email";

            SQLiteCommand cmd = new SQLiteCommand(query, connection);
            cmd.Parameters.AddWithValue("@username", username.ToLowerInvariant().Trim());
            cmd.Parameters.AddWithValue("@email", email);

            SQLiteDataReader sdr = cmd.ExecuteReader();

            bool usernameTaken = false;
            bool emailTaken = false;

            while (sdr.Read())
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

        /// <summary>
        /// Writes a user to the database with the given information.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public (bool Succeeded, string ErrorMessage) WriteUser(User user)
        {
            var availability = accountExists(user.Username, user.Email);
            if (availability.AccountExists == false)
                return availability;

            //Hash the user's password here

            const string query = @"INSERT INTO UserTable (HashedPassword, Username, FirstName, LastName, Email) 
            VALUES (@HashedPassword, @Username, @FirstName, @LastName, @Email)";

            SQLiteCommand myCommand = new SQLiteCommand(query, connection);
            myCommand.Parameters.AddWithValue("@HashedPassword", Encrypter.EncryptStringToBytes_Aes(user.Password));
            myCommand.Parameters.AddWithValue("@Username", user.Username);
            myCommand.Parameters.AddWithValue("@FirstName", user.FirstName);
            myCommand.Parameters.AddWithValue("@LastName", user.LastName);
            myCommand.Parameters.AddWithValue("@Email", user.Email);

            int rowsUpdated = myCommand.ExecuteNonQuery();

            myCommand.Dispose();

            if (rowsUpdated == 0)
                return (false, "Failed");

            return (true, "Success");
        }

        /// <summary>
        /// Returns an authorization cookie with a secure random Bitstring
        /// </summary>
        /// <returns></returns>
        private AuthorizationCookie getAuthCookie()
        {
            byte[] randomAuth = new byte[256];

            rngCsp.GetBytes(randomAuth, 0, randomAuth.Length);

            return new AuthorizationCookie(randomAuth);
        }

        /// <summary>
        /// Login the user with the username and password
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns>The User and an accompanying Authorization Cookie</returns>
        public (User user, AuthorizationCookie? ac) GetUser(string username, string password)
        {
            //scrub the username
            username = username.ToLowerInvariant().Trim();

            const string query = @"SELECT * FROM UserTable WHERE Username = @user AND HashedPassword = @hash";

            SQLiteCommand cmd = new SQLiteCommand(query, connection);
            cmd.Parameters.AddWithValue("@user", username);

            //use the encryption here to verify the password
            cmd.Parameters.AddWithValue("@hash", Encrypter.EncryptStringToBytes_Aes(password));

            SQLiteDataReader sdr = cmd.ExecuteReader();

            //Failed to log the user in
            if (!sdr.HasRows)
                return (null, null);

            sdr.Read();

            User user = new User(sdr.GetInt32(0), "", sdr.GetString(2), sdr.GetString(3), sdr.GetString(4), sdr.GetString(5));
            AuthorizationCookie ac = getAuthCookie();

            activeLogins.Add(user.UserID, ac);

            //retrieve the tasks for the user
            getTasks(user);

            return (user, ac);
        }

        /// <summary>
        /// Get the nodes for a given Graph ID.
        /// </summary>
        /// <param name="GraphID"></param>
        /// <returns></returns>
        private List<Node> getNodes(int GraphID)
        {
            const string query = @"SELECT * FROM Node WHERE GraphID = @id";

            SQLiteCommand cmd = new SQLiteCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", GraphID);

            SQLiteDataReader sdr = cmd.ExecuteReader();

            if (sdr.HasRows)
            {
                List<Node> nodes = new List<Node>(20);

                //add each node to the nodes list
                while (sdr.Read())
                    nodes.Add(new Node(sdr.GetInt32(0), sdr.GetString(1), sdr.GetInt32(2), sdr.GetInt32(3) == 1, DateTime.Parse(sdr.GetString(4)), sdr.GetString(5), sdr.GetInt32(6), sdr.GetInt32(7)));

                //sort the nodes with the provided comparator
                nodes.Sort(Graph.CompareNodes);

                return nodes;
            }

            return new List<Node>();
        }

        /// <summary>
        /// Write a node to the database
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private (bool Succeeded, string Result, Node UpdatedNode) writeNode(Node node)
        {
            const string query = @"INSERT INTO Node (Name, TimeWeight, Complete, Date, Text, GraphID, NodeIndex) 
            VALUES (@Name, @TimeWeight, @Complete, @Date, @Text, @GraphID, @NodeIndex)";

            SQLiteCommand myCommand = new SQLiteCommand(query, connection);
            myCommand.Parameters.AddWithValue("@Name", node.Name);
            myCommand.Parameters.AddWithValue("@TimeWeight", node.TimeWeight);
            myCommand.Parameters.AddWithValue("@Complete", node.Complete);
            myCommand.Parameters.AddWithValue("@Date", node.Date);
            myCommand.Parameters.AddWithValue("@Text", node.Text);
            myCommand.Parameters.AddWithValue("@GraphID", node.GraphID);
            myCommand.Parameters.AddWithValue("@NodeIndex", node.NodeIndex);

            int rowsUpdated = myCommand.ExecuteNonQuery();

            myCommand.Dispose();

            int nodeID = getID("Node");
            if (rowsUpdated == 0)
                return (false, "Failed to write the node!", null);

            //update the node id
            Node updated = new Node(nodeID, node.Name, node.TimeWeight, node.Complete, node.Date, node.Text, node.GraphID, node.NodeIndex);

            return (true, "Success", updated);
        }

        /// <summary>
        /// Write a graph to the database
        /// </summary>
        /// <param name="g"></param>
        /// <returns></returns>
        private (bool, string) writeGraph(Graph g)
        {
            const string query = "INSERT INTO Graph (AdjacencyMatrix) VALUES (@AdjacencyMatrix)";

            SQLiteCommand myCommand = new SQLiteCommand(query, connection);
            myCommand.Parameters.AddWithValue("@AdjacencyMatrix", g.GetDBFormatAdjacency());

            int rowsUpdated = myCommand.ExecuteNonQuery();

            myCommand.Dispose();

            if (rowsUpdated == 0)
                return (false, "Failed");

            return (true, "Success");
        }

        /// <summary>
        /// Retrieve a graph by the graph ID.
        /// </summary>
        /// <param name="GraphID"></param>
        /// <returns></returns>
        private Graph getGraph(int GraphID)
        {
            //get the nodes first
            var nodes = getNodes(GraphID);

            const string query = @"SELECT * FROM Graph WHERE GraphID = @id";

            SQLiteCommand cmd = new SQLiteCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", GraphID);

            SQLiteDataReader sdr = cmd.ExecuteReader();

            if (!sdr.HasRows)
                return null;

            sdr.Read();

            //provide the nodes here
            var graph = new Graph(sdr.GetInt32(0), nodes, sdr.GetString(1));

            return graph;
        }

        /// <summary>
        /// Write a task into the database. Returns a new task with the decomposition.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="ac"></param>
        /// <returns></returns>
        public (bool Succeeded, string FailureString, Task fullTask) WriteTask(Task task, AuthorizationCookie ac)
        {
            if (!CheckValidAuthCookie(task.UserID, ac))
                return (false, "Invalid login!", null);


            string query = @"INSERT INTO Task (AssignmentName, GraphID, SubmissionDate, Category, UserID) 
            VALUES (@AssignmentName, @GraphID, @SubmissionDate, @Category, @UserID)";

            //Retrieve the last graph ID and increment by 1.
            int graphID = getID("Graph") + 1;

            //create a graph first...
            Graph new_graph = new Graph();
            
            //some pre-defined scenarios...
            if (task.Category.Equals("Research Paper"))
            {
                //vertex set
                Node header = new Node(task.Category, 0, false, task.SubmissionDate, "", graphID, 0);
                Node n1 = new Node("Collect Sources", 0, false, task.SubmissionDate.AddDays(-11), "Research", graphID, 1);
                Node n2 = new Node("Write Thesis", 0, false, task.SubmissionDate.AddDays(-10), "", graphID, 2);
                Node n3 = new Node("Introduction", 0, false, task.SubmissionDate.AddDays(-9), "", graphID, 3);
                Node n4 = new Node("Body Paragraph 1", 0, false, task.SubmissionDate.AddDays(-7), "", graphID, 4);
                Node n5 = new Node("Body Paragraph 2", 0, false, task.SubmissionDate.AddDays(-5), "", graphID, 5);
                Node n6 = new Node("Body Paragraph 3", 0, false, task.SubmissionDate.AddDays(-3), "", graphID, 6);
                Node n7 = new Node("Conclusion", 0, false, task.SubmissionDate.AddDays(-2), "", graphID, 7);
                Node n8 = new Node("Citations", 0, false, task.SubmissionDate.AddDays(-1), "", graphID, 8);
                new_graph.AddNodes(header, n1, n2, n3, n4, n5, n6, n7, n8);
                //edge set
                new_graph.CreateEdge(header, n1);
                new_graph.CreateEdge(header, n2);
                new_graph.CreateEdge(header, n3);
                new_graph.CreateEdge(header, n4);
                new_graph.CreateEdge(header, n5);
                new_graph.CreateEdge(header, n6);
                new_graph.CreateEdge(header, n7);
                new_graph.CreateEdge(header, n8);

                //write the graph.
                writeGraph(new_graph);
            }
            else if (task.Category.Equals("Agile Software Project"))
            {
                //vertex set
                Node header = new Node(task.Category, 0, false, task.SubmissionDate, "", graphID, 0);
                Node n1 = new Node("Sprint 1", 0, false, task.SubmissionDate.AddDays(-13), "Deliverable", graphID, 1);
                Node n1_1 = new Node("Create Wireframe", 0, false, task.SubmissionDate.AddDays(-14), "Lucidchart", graphID, 2);
                Node n2 = new Node("Sprint 2", 0, false, task.SubmissionDate.AddDays(-6), "Deliverable", graphID, 3);
                Node n2_1 = new Node("Create SQLite Database", 0, false, task.SubmissionDate.AddDays(-11), "", graphID, 4);
                Node n2_2 = new Node("Write Web API", 0, false, task.SubmissionDate.AddDays(-9), "REST API", graphID, 5);
                Node n2_3 = new Node("Code Review", 0, false, task.SubmissionDate.AddDays(-7), "Pair Programming", graphID, 6);
                Node n3 = new Node("Sprint 3", 0, false, task.SubmissionDate, "Deliverable", graphID, 7);
                Node n3_1 = new Node("Unit Test", 0, false, task.SubmissionDate.AddDays(-2), "Ryan and Kyle", graphID, 8);
                Node n3_2 = new Node("Release Version 1", 0, false, task.SubmissionDate.AddDays(-1), "GitHub Release", graphID, 9);
                new_graph.AddNodes(header, n1, n1_1, n2, n2_1, n2_2, n2_3, n3, n3_1, n3_2);
                //edge set
                new_graph.CreateEdge(header, n1);
                new_graph.CreateEdge(header, n2);
                new_graph.CreateEdge(header, n3);
                new_graph.CreateEdge(n1, n1_1);
                new_graph.CreateEdge(n2, n2_1);
                new_graph.CreateEdge(n2, n2_2);
                new_graph.CreateEdge(n2, n2_3);
                new_graph.CreateEdge(n3, n3_1);
                new_graph.CreateEdge(n3, n3_2);

                //write the graph.
                writeGraph(new_graph);
            }

            //Write all the new nodes
            for (int i = 0; i < new_graph.Nodes.Count; i++)
            {
                Node n = new_graph.Nodes[i];
                (bool Succeeded, _, Node UpdatedNode) = writeNode(n);
                //make sure to pass the updated node with the correct ID information.
                if(Succeeded && UpdatedNode != null)
                    new_graph.Nodes[i] = UpdatedNode;
            }

            SQLiteCommand myCommand = new SQLiteCommand(query, connection);
            myCommand.Parameters.AddWithValue("@AssignmentName", task.AssignmentName);
            myCommand.Parameters.AddWithValue("@GraphID", graphID);
            myCommand.Parameters.AddWithValue("@SubmissionDate", task.SubmissionDate);
            myCommand.Parameters.AddWithValue("@Category", task.Category);
            myCommand.Parameters.AddWithValue("@UserID", task.UserID);

            int rowsUpdated = myCommand.ExecuteNonQuery();
            myCommand.Dispose();

            if (rowsUpdated == 0)
                return (false, "Failed", null);

            var id = getID("Task");

            //generate an updated task with this decomposition info to return to the user
            Task return_task = new Task(id, task.AssignmentName, graphID, task.SubmissionDate, task.Category, task.UserID);

            return_task.AddGraph(new_graph);

            return (true, "Success", return_task);
        }

        /// <summary>
        /// Get the last ID of a given table name.
        /// </summary>
        /// <param name="tablename"></param>
        /// <returns></returns>
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
                return 0;
        }

        /// <summary>
        /// Gets all the tasks in the database for a given user.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private List<Task> getTasks(User user)
        {
            const string query = @"SELECT * FROM Task WHERE UserID = @id";

            SQLiteCommand cmd = new SQLiteCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", user.UserID);

            SQLiteDataReader sdr = cmd.ExecuteReader();

            List<Task> tasks = user.Tasks;

            while (sdr.HasRows && sdr.Read())
            {
                Task t = new Task(sdr.GetInt32(0), sdr.GetString(1), sdr.GetInt32(2), sdr.GetDateTime(3), sdr.GetString(4), sdr.GetInt32(5));

                Graph g = getGraph(t.GraphID);

                t.AddGraph(g);

                //udate the tasks into the user's list
                tasks.Add(t);
            }

            return tasks;
        }

        /// <summary>
        /// Delete a task (and its decomposition) from the database
        /// </summary>
        /// <param name="t"></param>
        /// <param name="ac"></param>
        /// <returns></returns>
        public (bool Succeeded, string ErrorMessage) DeleteTask(Task t, AuthorizationCookie ac)
        {
            if (!CheckValidAuthCookie(t.UserID, ac))
                return (false, "Invalid login!");


            const string query = @"DELETE FROM Task WHERE TaskID = @id";

            SQLiteCommand cmd = new SQLiteCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", t.TaskID);

            SQLiteDataReader sdr = cmd.ExecuteReader();

            deleteGraph(t.GraphID);

            if (sdr.RecordsAffected == 1)
                return (true, "Successfully deleted your task!");

            return (false, "Failed to delete the task");
        }

        /// <summary>
        /// Delete the graph from the database (and its nodes)
        /// </summary>
        /// <param name="graphID"></param>
        /// <returns></returns>
        private (bool Succeeded, string ErrorMessage) deleteGraph(int graphID)
        {
            deleteNodes(graphID);

            const string query = @"DELETE FROM Graph WHERE GraphID = @id";

            SQLiteCommand cmd = new SQLiteCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", graphID);

            SQLiteDataReader sdr = cmd.ExecuteReader();

            if (sdr.RecordsAffected == 1)
                return (true, "Successfully deleted the graph.");

            return (false, "Failed to delete the graph");
        }

        /// <summary>
        /// Delete all nodes with the provided Graph ID
        /// </summary>
        /// <param name="graphID"></param>
        /// <returns></returns>
        private (bool Succeeded, string ErrorMessage) deleteNodes(int graphID)
        {
            const string query = @"DELETE FROM Node WHERE GraphID = @id";

            SQLiteCommand cmd = new SQLiteCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", graphID);

            SQLiteDataReader sdr = cmd.ExecuteReader();

            if (sdr.RecordsAffected >= 1)
                return (true, "Successfully deleted nodes.");

            return (false, "Failed to delete nodes");
        }

        /// <summary>
        /// Updates the completion status of a given node
        /// </summary>
        /// <param name="ac"></param>
        /// <param name="userID"></param>
        /// <param name="nodeID"></param>
        /// <param name="complete"></param>
        /// <returns></returns>
        public (bool Succeeded, string ErrorMessage) UpdateComplete(AuthorizationCookie ac, int userID, int nodeID, bool complete)
        {
            if (!CheckValidAuthCookie(userID, ac))
                return (false, "Invalid login!");

            const string query = @"UPDATE NODE SET Complete = @complete WHERE NodeID = @id";

            SQLiteCommand cmd = new SQLiteCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", nodeID);
            cmd.Parameters.AddWithValue("@complete", complete ? 1 : 0);

            SQLiteDataReader sdr = cmd.ExecuteReader();

            if (sdr.RecordsAffected == 1)
                return (true, "Successfully updated value.");

            return (false, "Failed to update nodes");
        }


    }
}
