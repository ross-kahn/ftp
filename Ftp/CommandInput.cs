using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

/**
 * CommandInput.cs
 * Written by Ross Kahn
 * 
 * This class has mostly to do with parsing command-line input and
 * translating it to FTP commands
 */
namespace Ftp
{
    class CommandInput
    {
        // The types of commands the program can handle
        [Flags]
        public enum COMMAND
        {
            ASCII,
            BINARY,
            CD,
            CDUP,
            DEBUG,
            DIR,
            GET,
            HELP,
            PASSIVE,
            PWD,
            QUIT,
            USER
        }

        // The prompt
        public const string PROMPT = "FTP> ";
        public const string USER = "USER";
        public const string TYPE = "TYPE";
        public const string PASSWORD = "PASS";
        public const string PWD = "XPWD";
        public const string QUIT = "QUIT";
        public const string CD = "CWD";
        public const string DIR = "LIST";
        public const string RETURN = "RETR";

        // Instructions for program user
        public static readonly String[] HELP_MESSAGE = {
	        "ascii      --> Set ASCII transfer type",
	        "binary     --> Set binary transfer type",
	        "cd <path>  --> Change the remote working directory",
	        "cdup       --> Change the remote working directory to the\n" + 
                    "               parent directory (i.e., cd ..)",
	        "debug      --> Toggle debug mode",
	        "dir        --> List the contents of the remote directory",
	        "get <path> --> Get a remote file",
	        "help       --> Displays this text",
	        "passive    --> Toggle passive/active mode",
	        "pwd        --> Print the working directory on the server",
            "quit       --> Close the connection to the server and terminate",
	        "user <name>--> Specify the user name (will prompt for password)" };

        // Start reading commands from the command line
        public void begin(Connection connection)
        {
            connection.open();
            signIn(null, connection);

            bool eof = false;
            string input = "";

            do
            {
                // Attempt to read in a line of data
                try
                {
                    Console.Write(PROMPT);
                    input = Console.ReadLine();
                }
                catch (Exception e)
                {
                    eof = true;
                }

                // Keep going if we have not hit end of file
	            if ( eof ){//|| input.Length > 0 ) {
                    break;
                }

                string option = "";
                string[] commandArray = Regex.Split(input, "\\s+");
                
                // Retrieve the input command
		        string argv = commandArray[0];

                // Allows an empty input to just do a prompt feed
                if (argv.Equals(""))
                {
                    continue;
                }

                if (commandArray.Length > 1)
                {
                    option = commandArray[1].Trim();
                }

                
                // Determine if the entered command is in the COMMAND enum
                COMMAND command;
                bool entry = Enum.TryParse<COMMAND>(argv, true, out command);

                // If the command isn't valid, print the help
                if (!argv.Equals("") && !command.ToString().Equals(argv.ToUpper()))
                {
                    Console.WriteLine("ERROR: Unknown Command");
                    command = COMMAND.HELP;
                }

                // Do actions based on the typed command
                switch (command)
                {
                    case COMMAND.ASCII:
                        connection.setEncoding(Connection.Encoding.Ascii);
                        break;

                    case COMMAND.BINARY:
                        connection.setEncoding(Connection.Encoding.Binary);
                        break;

                    case COMMAND.CD:
                        if (option.Equals(""))
                        {
                            Console.Write("Remote Directory: ");
                            option = Console.ReadLine().Trim();
                        }
                        connection.changeDirectory(option);
                        break;

                    case COMMAND.CDUP:
                        connection.changeDirectory("..");
                        break;

                    case COMMAND.DEBUG:
                        bool debug = connection.toggleDebug();
                        string onoff = debug ? "ON" : "OFF";
                        Console.WriteLine("Debugging is " + onoff);
                        break;

                    case COMMAND.DIR:
                        connection.printDirectoryListing();
                        break;

                    case COMMAND.GET:
                        if (option.Equals(""))
                        {
                            Console.Write("Remote file: ");
                            option = Console.ReadLine().Trim();
                        }

                        connection.getFile(option);

                        break;

                    case COMMAND.HELP:
                        for (int i = 0; i < HELP_MESSAGE.Length; i++)
                        {
                            Console.WriteLine(HELP_MESSAGE[i]);
                        }
                        Console.WriteLine();
                        break;

                    case COMMAND.PASSIVE:
                        connection.toggleMode();
                        break;

                    case COMMAND.PWD:
                        connection.printWorkingDirectory();
                        break;

                    case COMMAND.QUIT:
                        connection.close();
                        eof = true;
                        return;

                    case COMMAND.USER:
                        if (option.Equals(""))
                        {
                            signIn(null, connection);
                        }
                        else
                        {
                            signIn(option, connection);
                        }

                        break;
                }

            } while (!eof);

        }

        /**
         * Manages the sign-in process
         */
        private void signIn(string username, Connection connection)
        {
            if (null == username)
            {
                Console.Write("Username: ");
                username = Console.ReadLine().Trim();
            }

            if (connection.sendUser(username))
            {

                Console.Write("Password: ");
                string password = Console.ReadLine().Trim();

                connection.sendPass(password);
            }
        }
 
    }
}
