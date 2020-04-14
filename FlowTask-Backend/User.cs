using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTask_Backend
{
    public class User
    {

        public int UserId { get; private set; }
        public string HashedPassword { get; private set; }
        private string username;
        public string Username { 
            get {
                return username.ToLowerInvariant();
            } 
            private set {
                username = value.ToLowerInvariant(); 
            } 
        }
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string Email { get; private set; }
        public List<Task> Tasks { get; private set; }

        public User(string username, string firstName, string lastName, string email, string hashedPassword)
        {
            UserId = -1;
            HashedPassword = hashedPassword;
            Username = username;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            Tasks = new List<Task>();
        }

        public User(int userID, string hashedPassword, string username, string firstName, string lastName, string email)
        {
            UserId = userID;
            HashedPassword = hashedPassword;
            Username = username;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            Tasks = new List<Task>();
        }
    }
}
