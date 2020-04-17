using System;

namespace FlowTask_Backend
{
    internal class Program
    {
        /// <summary>
        /// If you change the output type to Console Application in the .csproj, you can run some quick unit tests of the DB.
        /// Otherwise, this is not used.
        /// </summary>
        /// <param name="args"></param>
        internal static void Main(string[] args)
        {
            var db = DatabaseController.GetDBController();

            User ryan = new User(0, "ryanat", "Ryan", "Toner", "ryan.toner@student.fairfield.edu", "test");

            var (user, ac) = db.GetUser("ryanat", "test");

            var res = db.WriteUser(ryan);
            Console.WriteLine(res);

            Console.ReadLine();
        }
    }
}
