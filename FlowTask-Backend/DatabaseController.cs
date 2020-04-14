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

            string query = "INSERT INTO UserTable (HashedPassword, Username, FirstName, LastName, Email)";
            query += " VALUES (@HashedPassword, @Username, @FirstName, @LastName, @Email)";

            SQLiteCommand myCommand = new SQLiteCommand(query, connection);
            myCommand.Parameters.AddWithValue("@HashedPassword", user.HashedPassword);
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
            cmd.Parameters.AddWithValue("@hash", hashedpassword);

            SQLiteDataReader sdr = cmd.ExecuteReader();

            if (!sdr.HasRows)
                return (null, new AuthorizationCookie());

            sdr.Read();

            User user = new User(sdr.GetInt32(0), sdr.GetString(1), sdr.GetString(2), sdr.GetString(3), sdr.GetString(4), "");

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

        public (bool Result, string FailureString) WriteTask(Task task, AuthorizationCookie ac)
        {
            if (!logins.ContainsKey(task.UserID) || logins[task.UserID].BitString != ac.BitString)
                return (false, "Invalid login!");


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

            myCommand.Dispose();

            if (rowsAffected == 0)
                return (false, "Failed");

            return (true, "Success");
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
                Task t = new Task(sdr.GetInt32(0), sdr.GetString(1), sdr.GetInt32(2), sdr.GetString(3), sdr.GetString(4), sdr.GetInt32(5));

                Graph g = getGraph(t.GraphID);

                t.AddGraph(g);

                tasks.Add(t);
            }

            return tasks;


        }



    }
}
