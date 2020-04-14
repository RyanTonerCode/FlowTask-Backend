using System;
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
            cmd.Parameters.AddWithValue("@username", username);
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
            int rows_returned;

            SQLiteCommand cmd = new SQLiteCommand(sqlQuery, connection);
            cmd.Parameters.AddWithValue("@id", GraphID);

            SQLiteDataReader sdr = cmd.ExecuteReader();
            rows_returned = sdr.RecordsAffected;

            if (rows_returned > 0)
            {
                List<Node> nodes = new List<Node>(rows_returned);

               while(sdr.Read())
                {

                    //int nodeID, string name, int timeWeight, bool complete, DateTime date, string text, int graphid

                    nodes.Add(new Node(sdr.GetInt32(0), sdr.GetString(1), sdr.GetInt32(2), sdr.GetInt32(3) == 1, DateTime.Parse(sdr.GetString(4)), sdr.GetString(5), sdr.GetInt32(6), sdr.GetInt32(7)));
                }

                Comparison<Node> comp = new Comparison<Node>((x,y) => x.NodeIndex == y.NodeIndex ? 0 : x.NodeIndex < y.NodeIndex ? -1 : 1);

                nodes.Sort(comp);
                
                return nodes;
            }

            return new List<Node>();


        }

        private (bool, string) writeNode(Node node)
        {
            string query = "INSERT INTO Node (Name, TimeWeight, Complete, Dated, Text, GraphID, NodeIndex)";
            query += " VALUES (@Name, @TimeWeight, @Complete, @Dated, @Text, @GraphID, @NodeIndex)";

            SQLiteCommand myCommand = new SQLiteCommand(query, connection);
            myCommand.Parameters.AddWithValue("@Name", node.Name);
            myCommand.Parameters.AddWithValue("@TimeWeight", node.TimeWeight);
            myCommand.Parameters.AddWithValue("@Complete", node.Complete);
            myCommand.Parameters.AddWithValue("@Dated", node.Date);
            myCommand.Parameters.AddWithValue("@Text", node.Text);
            myCommand.Parameters.AddWithValue("@GraphID", node.GraphID);
            myCommand.Parameters.AddWithValue("@NodeIndex", node.NodeIndex);

            int rowsAffected = myCommand.ExecuteNonQuery();

            myCommand.Dispose();

            if (rowsAffected == 0)
                return (false, "Failed");

            return (true, "Success");
        }

        private (bool, string) writeGraph(Graph g)
        {
            string query = "INSERT INTO Graph (GraphID, AdjacencyMatrix)";
            query += " VALUES (@GraphID, @AdjacencyMatrix)";

            SQLiteCommand myCommand = new SQLiteCommand(query, connection);
            myCommand.Parameters.AddWithValue("@GraphID", g.GraphID);
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

        public (bool Result, string FailureString, int TaskID) WriteTask(Task task, AuthorizationCookie ac)
        {
            if(!CheckValidAuthCookie(task.UserID,ac))
                return (false, "Invalid login!", -1);


            string query = "INSERT INTO Task (AssignmentName, GraphID, SubmissionDate, Category, UserID)";
            query += " VALUES (@AssignmentName, @GraphID, @SubmissionDate, @Category, @UserID)";

            SQLiteCommand myCommand = new SQLiteCommand(query, connection);
            myCommand.Parameters.AddWithValue("@AssignmentName", task.AssignmentName);
            myCommand.Parameters.AddWithValue("@GraphID", task.GraphID);
            myCommand.Parameters.AddWithValue("@SubmissionDate", task.SubmissionDate);
            myCommand.Parameters.AddWithValue("@Category", task.Category);
            myCommand.Parameters.AddWithValue("@UserID", task.UserID);

            // ... other parameters
            int rowsAffected = myCommand.ExecuteNonQuery();

            if (rowsAffected == 0)
                return (false, "Failed", -1);

            myCommand.Dispose();

            var id = getID("Task");

            return (true, "Success", id);
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

            if (sdr.RecordsAffected == 1)
                return (true, "Successfully deleted your task!");

            //TODO delete graph, nodes

            return (false, "Failed to delete the task");
        }



    }
}
