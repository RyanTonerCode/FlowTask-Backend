using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace FlowTask_Backend
{
    class DatabaseController
    {
        private static DatabaseController singleton;

        /// <summary>
        /// singleton database controller
        /// </summary>
        public static DatabaseController dbController { 
            get {
                return singleton ?? (singleton=new DatabaseController());
            }
        }

        SqlConnection connection;

        public DatabaseController()
        {
            Connect();
        }

        private void Connect()
        {
            var connectionStr = @"Server=LENOVO-PC\FLOWTASKSERVER;Database=Flow;Trusted_Connection=true;";
            connection = new SqlConnection(connectionStr);
            connection.Open();

        }

        public (bool, string) AccountExists(string username, string email)
        {
            const string sqlQuery = @"SELECT Username, Email FROM UserTable WHERE Username = @username OR Email = @email";

            DataTable dt = new DataTable();
            int rows_returned;

            SqlCommand cmd = new SqlCommand(sqlQuery, connection);
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@email", email);

            using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
            {
                cmd.CommandType = CommandType.Text;
                rows_returned = sda.Fill(dt);
            }
            
            bool usernameTaken = false;
            bool emailTaken = false;

            foreach(DataRow x in dt.Rows)
            {
                if (((string)x[0]).Equals(username))
                    usernameTaken = true;
                if (((string)x[1]).Equals(email))
                    emailTaken = true;
            }

            if (usernameTaken && emailTaken)
                return (false, "Try another email and username");
            if (usernameTaken)
                return (false, "Try another username");
            if (emailTaken)
                return (false, "Try another email");

            return (true, "done");
        }

        public (bool, string) WriteUser(User user)
        {
            var availability = AccountExists(user.Username, user.Email);
            if (availability.Item1 == false)
                return availability;

            string query = "INSERT INTO UserTable (HashedPassword, Username, FirstName, LastName, Email)";
            query += " VALUES (@HashedPassword, @Username, @FirstName, @LastName, @Email)";

            SqlCommand myCommand = new SqlCommand(query, connection);
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


        public User GetUser(string username, string hashedpassword)
        {
            const string sqlQuery = @"SELECT * FROM UserTable WHERE Username = @user AND HashedPassword = @hash";

            DataTable dt = new DataTable();
            int rows_returned;

            SqlCommand cmd = new SqlCommand(sqlQuery, connection);
            cmd.Parameters.AddWithValue("@user", username);
            cmd.Parameters.AddWithValue("@hash", hashedpassword);

            using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
            {
                cmd.CommandType = CommandType.Text;
                rows_returned = sda.Fill(dt);
            }

            if (rows_returned == 1)
            {
                DataRow dr = dt.Rows[0];
                User user = new User((int)dr[0], (string)dr[1], (string)dr[2], (string)dr[3], (string)dr[4], (string)dr[5]);

                return user;
            }

            return null;
        }

        private User GetUser(int ID)
        {
            const string sqlQuery = @"SELECT * FROM UserTable WHERE UserID = @id";

            DataTable dt = new DataTable();
            int rows_returned;

            SqlCommand cmd = new SqlCommand(sqlQuery, connection);
            cmd.Parameters.AddWithValue("@id", ID);

            using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
            {
                cmd.CommandType = CommandType.Text;
                rows_returned = sda.Fill(dt);
            }

            if(rows_returned == 1)
            {
                DataRow dr = dt.Rows[0];
                User user = new User((int)dr[0], (string)dr[1], (string)dr[2], (string)dr[3] , (string)dr[4], (string)dr[5]);

                return user;
            }

            return null;
        }


        public List<Node> GetNodes(int GraphID)
        {
            const string sqlQuery = @"SELECT * FROM Node WHERE GraphID = @id";

            DataTable dt = new DataTable();
            int rows_returned;

            SqlCommand cmd = new SqlCommand(sqlQuery, connection);
            cmd.Parameters.AddWithValue("@id", GraphID);

            using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
            {
                cmd.CommandType = CommandType.Text;
                rows_returned = sda.Fill(dt);
            }

            if (rows_returned > 0)
            {
                List<Node> nodes = new List<Node>(rows_returned);

                for(int i = 0; i < dt.Rows.Count; i++)
                {
                    DataRow dr = dt.Rows[i];

                    //int nodeID, string name, int timeWeight, bool complete, DateTime date, string text, int graphid

                    nodes.Add(new Node((int)dr[0], (string)dr[1], (int)dr[2], (bool)dr[3], (DateTime)dr[4], (string)dr[5], (int)dr[6], (int)dr[7]));
                }

                Comparison<Node> comp = new Comparison<Node>((x,y) => x.NodeIndex == y.NodeIndex ? 0 : x.NodeIndex < y.NodeIndex ? -1 : 1);

                nodes.Sort(comp);
                
                return nodes;
            }

            return new List<Node>();


        }

        public (bool, string) WriteNode(Node node)
        {
            string query = "INSERT INTO Node (Name, TimeWeight, Complete, Dated, Text, GraphID, NodeIndex)";
            query += " VALUES (@Name, @TimeWeight, @Complete, @Dated, @Text, @GraphID, @NodeIndex)";

            SqlCommand myCommand = new SqlCommand(query, connection);
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

        public (bool, string) WriteGraph(Graph g)
        {
            string query = "INSERT INTO Graph (GraphID, AdjacencyMatrix)";
            query += " VALUES (@GraphID, @AdjacencyMatrix)";

            SqlCommand myCommand = new SqlCommand(query, connection);
            myCommand.Parameters.AddWithValue("@GraphID", g.GraphID);
            myCommand.Parameters.AddWithValue("@AdjacencyMatrix", g.GetDBFormatAdjacency());

            int rowsAffected = myCommand.ExecuteNonQuery();

            myCommand.Dispose();

            if (rowsAffected == 0)
                return (false, "Failed");

            return (true, "Success");
        }


        public Graph GetGraph(int GraphID)
        {
            var nodes = GetNodes(GraphID);

            const string sqlQuery = @"SELECT * FROM Graph WHERE GraphID = @id";

            DataTable dt = new DataTable();
            int rows_returned;

            SqlCommand cmd = new SqlCommand(sqlQuery, connection);
            cmd.Parameters.AddWithValue("@id", GraphID);

            using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
            {
                cmd.CommandType = CommandType.Text;
                rows_returned = sda.Fill(dt);
            }

            if (rows_returned != 0)
                return null;

            DataRow dr = dt.Rows[0];
            var graph = new Graph((int)dr[0], nodes, (string)dr[2]);

            return graph;
        }



    }
}
