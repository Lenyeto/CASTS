using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SimpleUpdater
{
    class Program
    {
        static void Main(string[] args)
        {
            string password = "";

            Console.WriteLine("Simple Download Utility | Use the 'help' command to see valid commands!");
            Console.WriteLine("IP Recomended is 74.133.10.153 with Port 25566");
            Console.Write("IP Address:");
            IPAddress IP;
            if (!IPAddress.TryParse(Console.ReadLine(), out IP))
            {
                Console.WriteLine("IP could not be parsed, the IP is invalid");
                return;
            }

            Console.Write("Port:");
            int Port;
            if (!int.TryParse(Console.ReadLine(), out Port))
            {
                Console.WriteLine("Port could not be parsed, the Port is invalid.");
                return;
            }


            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect(IP, Port);
            Socket s = tcpClient.Client;


            bool done = false;
            while (!done)
            {
                string Input = Console.ReadLine();
                if (Input == "help")
                {
                    Console.WriteLine("Commands:");
                    Console.WriteLine("list");
                    Console.WriteLine(">Gets the list of hosted files.");
                    Console.WriteLine("download (file name - not including folder)");
                    Console.WriteLine(">Downloads the file specified.");
                }
                else if (Input == "list")
                {
                    Transfer.SendString(s, "list");

                    int num = Transfer.ReceiveInt(s);
                    for (int i = 0; i < num; i++)
                    {
                        Console.WriteLine(Transfer.ReceiveString(s));
                    }
                }
                else if (Input.StartsWith("download"))
                {
                    Transfer.SendString(s, "download");
                    string fileName = Input.Split(' ')[1];

                    Transfer.SendString(s, fileName);
                    int i = Transfer.ReceiveInt(s);

                    if (i == 0)
                    {
                        ConsoleUtil.Error("That File Does Not Exist!");
                        continue;
                    }

                    if (fileName == "*.*")
                    {
                        //SendString(s, fileName);
                        //int i = ReceiveInt(s); //Irrelevent
                        for (int j = 0; j < i; j++)
                        {
                            if (password != "")
                            {
                                Transfer.ReceiveLargeFileSecure(s, password);
                            }
                            else
                            {
                                Transfer.ReceiveLargeFile(s);
                            }
                        }
                    }
                    else
                    {
                        //DateTime dt = DateTime.Now;
                        if (password != "")
                        {
                            Transfer.ReceiveLargeFileSecure(s, password);
                            Console.WriteLine("SECURE");
                        }
                        else
                        {
                            Transfer.ReceiveLargeFile(s);
                            Console.WriteLine("NON-SECURE");
                        }
                    }
                    //TimeSpan dt2 = DateTime.Now - dt;
                    //Console.WriteLine(dt2);
                }
                else if (Input.StartsWith("secure"))
                {
                    Transfer.SendString(s, "secure");
                    Console.Write("Enter a Password:");
                    password = Console.ReadLine();
                    Transfer.SendString(s, password);
                }

            }

        }
    }
}
