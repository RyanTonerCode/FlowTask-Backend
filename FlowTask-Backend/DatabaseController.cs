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

        public (bool, string) WriteUser(User user)
        {
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

    }
}
