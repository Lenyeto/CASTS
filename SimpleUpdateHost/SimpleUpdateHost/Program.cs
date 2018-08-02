using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

/// <summary>
/// TODO :  MAKE THIS MULTITHREADED FOR MULTIPLE CLIENTS AT ONCE! --basically done, should get optimized.
///         FIX ENCRYPTION / DECRYPTION
///         OPTIMIZE
///             Consolidate the SendLargeFile + SendLargeFileSecure into one function, that will change based on the length of password being more than 0 or not "".
///                 That will depend on whether or not the logic for the longer byte arrays will be weird.
///         FIX ISSUE WITH CLIENT TRYING TO DOWNLOAD AN INVALID FILE CAUSING THEM TO CRASH
///             Send an check message, if it is valid, then it sends something like "check", if it is not valid, then send something like "error".
///         FIX DOWNLOAD *.* ISSUES
///         
/// </summary>

namespace SimpleUpdateHost
{
    class Program
    {
        static void Main(string[] args)
        {
            string password = "";

            bool retry = true;

            string folder = "";
            while (retry)
            {
                Console.Write("Folder Name to Host:");
                string fold = Console.ReadLine();
                if (Directory.Exists(fold))
                {
                    folder = fold;
                    retry = false;
                }
                else
                {
                    Console.WriteLine("Folder Does Not Exist");
                }
            }

            int port = 0;
            retry = true;
            while (retry)
            {
                Console.Write("Port:");
                if (int.TryParse(Console.ReadLine(), out port))
                {
                    retry = false;
                }
                else
                {
                    Console.WriteLine("This is not an integer!");
                }
            }

#pragma warning disable CS0618
            TcpListener tcp = new TcpListener(port);
#pragma warning restore CS0618
            tcp.Start();

            //FIRST ATTEMPT AT MUTLITHREADING


            while (true)
            {
                Socket s = tcp.AcceptSocket();

                ConsoleUtil.Notification("Conntected to Client!");

                ClientDataBase.GetInstance().RegisterClient(s);

                ClientDataBase.GetInstance().PrintClients();

                Thread socketThread = new Thread(() =>
                {
                    //try
                    //{
                        while (true)
                        {

                            string command = Transfer.ReceiveString(s);

                            if (command.StartsWith("download"))
                            {
                                string fileName = Transfer.ReceiveString(s);
                                if (File.Exists(folder + "/" + fileName))
                                {
                                    Transfer.SendInt(s, 1);
                                }
                                else
                                {
                                    Transfer.SendInt(s, 0);
                                    continue;
                                }
                                if (fileName == "*.*")
                                {
                                    //Download all.
                                    int i = Directory.GetFiles(folder).Length;
                                    Transfer.SendInt(s, i);

                                    foreach (string str in Directory.GetFiles(folder))
                                    {
                                        string proper_file_name = str.Substring(folder.Length + 1);
                                        if (password != "")
                                        {
                                            Transfer.SendLargeFileSecure(s, str, proper_file_name, password);
                                        }
                                        else
                                        {
                                            Transfer.SendLargeFile(s, str, proper_file_name);
                                        }
                                    }
                                }
                                else
                                {
                                    if (password != "")
                                    {
                                        Transfer.SendLargeFileSecure(s, folder + "/" + fileName, fileName, password);
                                        Console.WriteLine("Sent Secure File To: {0}", s.AddressFamily.ToString()); //SHOULD MOVE THIS TO THE SENDFILE FUNCTION.
                                    }
                                    else
                                    {
                                        Transfer.SendLargeFile(s, folder + "/" + fileName, fileName);
                                        Console.WriteLine("Sent File To: {0}", s.AddressFamily.ToString()); //SHOULD MOVE THIS TO THE SENDFILE FUNCTION.
                                    }
                                }
                            }
                            else if (command.StartsWith("list"))
                            {
                                int i = Directory.GetFiles(folder).Length;
                                Transfer.SendInt(s, i);
                                foreach (string str in Directory.GetFiles(folder))
                                {
                                    Transfer.SendString(s, str);
                                }
                            }
                            else if (command.StartsWith("secure"))
                            {
                                password = Transfer.ReceiveString(s);
                            }
                        }
                    //}
                    //catch (Exception e)
                    //{
                    //    Console.WriteLine("Got Exception : {0}", e.Message);
                    //    ConsoleUtil.Error("Closed Connection with Client...\n{Connection was Closed Forcibly!}");
                    //    ClientDataBase.GetInstance().RemoveClient(s);
                    //    ClientDataBase.GetInstance().PrintClients();
                    //}
                });
                socketThread.Start();
            }
        }

    }
}
