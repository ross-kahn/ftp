using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/**
 * Program.cs
 * Written by Ross Kahn
 * 
 * Sets up main program constructs and begins execution
 */
namespace Ftp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: [mono] Ftp server");
                Console.ReadKey();
                Environment.Exit(1);
            }

            string address = args[0].Trim();

            // Sets up the command connection
            CommandInput input = new CommandInput();
            Connection connection = new Connection(address);

            // Begins the command line input
            input.begin(connection);
        }
    }
}
