using System;

namespace FlowTask_Backend
{
    class Program
    {
        static void Main(string[] args)
        {
            var db = DatabaseController.dbController;


            User ryan = new User("ryanat", "Ryan", "Toner", "ryan.toner@student.fairfield.edu", "test");


            var User = db.GetUser("ryanat", "test");

            //var res = db.WriteUser(ryan);

            Console.ReadLine();
        }
    }
}
