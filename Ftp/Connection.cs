using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text.RegularExpressions;

/**
 * Connection.cs
 * Written by Ross Kahn
 * 
 * This class has to do mostly with the Command connection to the FTP server
 */
namespace Ftp
{
    class Connection
    {

        #region Variables, Constants, and Enums

        private StreamReader reader;
        private StreamWriter writer;
        private string address;
        private static readonly int PORT = 21;
        private static readonly Regex regex = new Regex(@"^[0-9]{3} ");

        private bool debug;
        private Mode currMode;

        public enum Encoding
        {
            Ascii,
            Binary
        }

        public enum Mode
        {
            Passive,
            Active
        }
        #endregion


        /**
         * Constructor. Sets up the starting options
         */
        public Connection(string address)
        {
            this.address = address;
            debug = false;
            currMode = Mode.Active;
        }


        #region Setting up the TCP connection

        /**
         * Opens the Command TCP connection, then prints out the resulting response message
         */
        public bool open()
        {
            try
            {
                TcpClient conn = new TcpClient(address, PORT);
                reader = new StreamReader(conn.GetStream());
                writer = new StreamWriter(conn.GetStream());

                // Prints out the server's initial response message
                bool connected = sendOverLine(null);
                
                // Print whether the TcpClient connected successfully or not
                if (debug)
                {
                    string msg = connected ? "CONNECTED" : "NOT CONNECTED";
                    Console.WriteLine("DEBUG:::: Application is " + connected);
                }

                return true;
            }
            catch (Exception e)
            {
                if (debug)
                {
                    Console.WriteLine(e.Message);
                }
                return false;
            }
        }

        /**
         * Closes the command reader and writer
         */
        public void close()
        {
            try
            {
                quit();
                reader.Close();
                writer.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
        }

        /**
         * Toggles the current mode from Active to Passive and vice versa
         */
        public void toggleMode()
        {
            // Toggles the current mode from active to passive or vice versa
            currMode = currMode.Equals(Mode.Passive) ? Mode.Active : Mode.Passive;

            Console.WriteLine("Connection mode set to " + currMode.ToString());
        }

#endregion

        #region Commands Sent Over Connection

        /**
         * Sends a USER command over the connection with the supplied username
         */
        public bool sendUser(string user)
        {
            if (debug)
            {
                Console.WriteLine("DEBUG:::: Sending 'USER " + user +"'");
            }
            return sendOverLine(CommandInput.USER + " " + user);
        }

        /**
         * Sends a PASS command over the connection with the supplied password
         */
        public bool sendPass(string pass)
        {
            if (debug)
            {
                Console.WriteLine("DEBUG:::: Sending 'PASS " + pass + "'");
            }
            return sendOverLine(CommandInput.PASSWORD + " " + pass);
        }

        /**
         * Sends a XPWD command over the connection to print the current working directory on the sever
         */
        public void printWorkingDirectory()
        {
            if (debug)
            {
                Console.WriteLine("DEBUG:::: Sending 'XPWD'");
            }
            sendOverLine(CommandInput.PWD);
        }

        /**
         * Toggles debug mode on or off
         */
        public bool toggleDebug()
        {
            // Switch debug mode on/off
            debug = debug ? false : true;

            // Return current state of debug
            return debug;
        }

        /**
         * Toggles the encoding between Ascii and Binary
         * Sends TYPE A over the command line for Ascii
         * Sends TYPE I over command line for binary
         */
        public void setEncoding(Encoding enc)
        {
            string toSend = enc.Equals(Encoding.Ascii) ? "A" : "I";

            if (debug)
            {
                Console.WriteLine("DEBUG:::: Sending 'TYPE " + toSend + "'");
            }
            sendOverLine(CommandInput.TYPE + " " + toSend);
        }

        /**
         * Closes the connection to the server and ends the program
         */
        public void quit()
        {
            if (debug)
            {
                Console.WriteLine("Sending 'QUIT'");
            }
            sendOverLine(CommandInput.QUIT);
        }

        /**
         * Changes the directory on the server to the specified path
         */
        public void changeDirectory(string directory)
        {
            if (debug)
            {
                Console.WriteLine("DEBUG:::: Sending 'CWD " + directory + "'");
            }
            sendOverLine(CommandInput.CD + " " + directory);
        }

        private void getData(string command)
        {
            getDataToFile(command, null);
        }

        /**
         * Sets up either an Active or Passive data connection, then sends and reads data
         */
        private void getDataToFile(string command, string fileToRead)
        {
            // Set up a data connection based on the current mode
            DataConnection dataConnection = new DataConnection(currMode, reader, writer, debug);

            // Send a command for the current directory listing. The data will return over the data connection.
            // sendOverLine will return false if the user is not able to do a LIST command for any reason
            if (sendOverLine(command))
            {

                // Read all the data that came over the data connection
                // If fileToRead is null, it'll just print out the data
                dataConnection.readData(fileToRead);

                // Print any success/failure messages from the command connection
                sendOverLine(null);
            }
        }

        public void printDirectoryListing()
        {
            getData(CommandInput.DIR);
        }

        public void getFile(string path)
        {
            getDataToFile(CommandInput.RETURN + " " + path, path);
        }

        #endregion

        /**
         * Sends a message over the Command connection, then prints out any response from the server
         */
        private bool sendOverLine(string message)
        {     
            
            try
            {
                if (message != null)
                {
                    writer.WriteLine(message);
                    writer.Flush();
                }

                string response = "";
                do
                {
                    response = reader.ReadLine();
                    Console.WriteLine(response);

                }while(!regex.IsMatch(response, 0));

                int code = int.Parse(response.Substring(0, 3));

                return code < 500;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
    }
}
