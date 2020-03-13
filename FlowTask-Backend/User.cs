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
        public string Username { 
            get {
                return Username.ToLowerInvariant();
            } 
            private set { 
                Username = value.ToLowerInvariant(); 
            } 
        }
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string Email { get; private  set; }
        internal Task[] Task { get; private set; }

        public User(string username, string firstName, string lastName, string email, string hashedPassword)
        {
            UserId = -1;
            HashedPassword = hashedPassword;
            Username = username;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
        }

        public User(int userID, string username, string firstName, string lastName, string email, string hashedPassword)
        {
            UserId = userID;
            HashedPassword = hashedPassword;
            Username = username;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
        }
    }
}
