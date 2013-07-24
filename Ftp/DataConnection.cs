using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text.RegularExpressions;

/**
 * DataConnection.cs
 * Written by Ross Kahn
 * 
 * This class has mostly to do with Data connections
 */
namespace Ftp
{
    class DataConnection
    {
        bool debugmode;             // Whether the program is in debug mode 
        StreamWriter commandWriter; // To send commands over the Command connection
        StreamReader commandReader; // To read responses from the Command connection

        StreamReader dataReader;    // To read incoming responses from the Data connection

        Connection.Mode currMode;   // Allows an Active TcpClient to be generated before reading data
        TcpListener listener;       // Only relevant for Active connections

        public DataConnection(Connection.Mode mode, StreamReader commReader, StreamWriter commWriter, bool debug=false)
        {
            debugmode = debug;
            commandWriter = commWriter;
            commandReader = commReader;

            switch (mode)
            {
                // Set up an active connection. The reader cannot be built until the proper command
                // has been sent on the Command connection (which, in this program, is right before
                // data is about to be read)
                case Connection.Mode.Active:
                    currMode = Connection.Mode.Active;
                    startActiveConnection();
                    break;

                // The reader can be generated from a Passive connection immediately
                case Connection.Mode.Passive:
                    currMode = Connection.Mode.Passive;
                    dataReader = startPassiveConnection();
                    break;
            }
        }

        #region Passive Connections

        /**
         * Opens a passive connection
         */
        private StreamReader startPassiveConnection()
        {
            try
            {
                if (debugmode)
                {
                    Console.WriteLine("DATA:::: Sending 'PASV'");
                }

                // Send a PASV command over the command connection
                commandWriter.WriteLine("PASV");
                commandWriter.Flush();

                // The response will be data to open a data connection
                string response = commandReader.ReadLine();

                Console.WriteLine(response);
                
                // If the command response is 227 (success), parse the response data
                int code = int.Parse(response.Substring(0, 3));
                if (code == 227)
                {
                    // Find the comma-separated number array
                    string[] responses = response.Split();  
                    string nums = responses[responses.Length - 1].Trim();

                    // Take out the erroneous characters
                    nums = nums.Trim('(', '.', ')');
                    
                    // Split the numbers on commas, put into an array
                    string[] rawnums = nums.Split(',');

                    // Start a client with the given IP and port information
                    return buildPassiveClient(rawnums);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        /**
         * Performs the calculations on the PASV response to open a TCPClient
         */
        private StreamReader buildPassiveClient(string[] parameters)
        {
            // Build the IP address as a string
            string ip = "";
            for (int ipindex = 0; ipindex < 4; ipindex++)
            {
                // The first 4 numbers in the sequence separated by a "."
                ip += parameters[ipindex] + ".";
            }
            ip = ip.Substring(0, ip.Length - 1);    // Take off the last "."

            if (debugmode)
            {
                Console.WriteLine("DEBUG::: IP = '"+ip+"'");
            }

            // The last 2 numbers, p1 and p2
            // final port number = (p1 * 256) + p2
            int portmult = int.Parse(parameters[4]);
            int portadd = int.Parse(parameters[5]);
            int port = (portmult * 256) + portadd;

            if (debugmode)
            {
                Console.WriteLine("DEBUG::: Port = '" + port + "'");
            }

            TcpClient dataConnection = new TcpClient(ip, port);
            StreamReader dataReader = new StreamReader(dataConnection.GetStream());

            return dataReader;
        }

        #endregion

        #region Active Connections

        /**
         * Starts an active connection
         */
        private bool startActiveConnection()
        {
            #region Inspired by http://stackoverflow.com/questions/1069103/how-to-get-my-own-ip-address-in-c
            
                IPHostEntry host;
                IPAddress localIP = null;
                host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIP = ip;
                    }
                }

            #endregion

            // The local IP could not be found
                if (null == localIP)
                {
                    Console.WriteLine("Error: Local IP could not be found");
                    return false;
                }
        
            return buildActiveClient(localIP);
        }

        /**
         * Sends a PORT command with proper parameters 
         */
        private bool buildActiveClient(IPAddress localIP)
        {
            // Opens a TcpListener with the localIP and a "random" port connection, then starts it
            listener = new TcpListener(localIP, 0);
            listener.Start();

            // Finds IP and port information from the listener
            string[] endpointInfo = listener.LocalEndpoint.ToString().Split(':');

            if (debugmode)
            {
                Console.WriteLine("DEBUG:::: IP ---- Port Information:");
                Console.WriteLine("\t" + endpointInfo[0] + " ---- " + endpointInfo[1]);
            }
            
            // Converts the raw IP and port information into a format suitable for sending with
            // a PORT command
            string port_params = convertEndpointToPortString(endpointInfo[0], endpointInfo[1]);

            if (debugmode)
            {
                Console.WriteLine("DEBUG:::: Sending 'PORT " + port_params + "'");
            }

            commandWriter.WriteLine("PORT " + port_params);
            commandWriter.Flush();

            // The response will be data to open a data connection
            string response = commandReader.ReadLine();

            Console.WriteLine(response);

            // Find the response code for the PORT command
            int code = int.Parse(response.Substring(0, 3));

            // Return if the PORT command was successful
            return code == 200;
        }

        /**
         * Parses and calculates IP and port information, and puts it into 
         * Comma-separated values
         */
        private string convertEndpointToPortString(string ip_str, string port_str)
        {
            // Calculate IP string
            string[] ipOctects = ip_str.Split('.');

            string port_csv = "";
            foreach( string str in ipOctects)
            {
                port_csv += str + ",";
            }

            // Calculate port string
            int port = int.Parse(port_str);
            int p2 = port % 256;
            int p1 = (port - p2) / 256;

            port_csv += p1.ToString();
            port_csv += "," + p2.ToString();

            if (debugmode)
            {
                Console.WriteLine("DEBUG:::: PORT paramters = (" + port_csv + ")");
            }
            
            return port_csv;
        }
        #endregion

        #region Read Data

        // Read data until there's nothing left, then close the stream
        public void readData(string toFile)
        {

            if (currMode.Equals(Connection.Mode.Active))
            {
                TcpClient client = listener.AcceptTcpClient();
                dataReader = new StreamReader(client.GetStream());
            }

            if (toFile != null)
            {
                writeToFile(toFile);
            }
            else
            {
                string data = "";
                do
                {
                    data = dataReader.ReadLine();
                    Console.WriteLine(data);
                } while (data != null);
            }

            

            dataReader.Close();
        }

        private void writeToFile(string filename)
        {
            if (debugmode)
            {
                string directory = Directory.GetCurrentDirectory();
                Console.WriteLine("DEBUG:::: Current Working Directory = '" + directory + "'");
            }

            // Create a file in the current working directory with the file's filename
            StreamWriter file = new StreamWriter(filename);
            string data = "";

            do
            {
                data = dataReader.ReadLine();
                file.WriteLine(data);
            } while (data != null);

            file.Close();
        }

        #endregion
    }
}
