using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleUpdateHost
{
    public class Utility
    {
    }

    public class Transfer
    {
        public static string ReceiveString(Socket s)
        {
                byte[] command_buffer = new byte[ReceiveInt(s)];
                s.Receive(command_buffer);
                return Encoding.ASCII.GetString(command_buffer);
        }

        public static void SendString(Socket s, string msg)
        {
            int msg_length = Encoding.ASCII.GetByteCount(msg);
            s.Send(BitConverter.GetBytes(msg_length));

            byte[] msg_buffer = Encoding.ASCII.GetBytes(msg);
            s.Send(msg_buffer);
        }

        public static int ReceiveInt(Socket s)
        {
            byte[] int_buffer = new byte[4];
            s.Receive(int_buffer);
            return BitConverter.ToInt32(int_buffer, 0);
        }

        public static void SendInt(Socket s, int i)
        {
            s.Send(BitConverter.GetBytes(i));
        }

        public static long ReceiveLong(Socket s)
        {
            byte[] int_buffer = new byte[8];
            s.Receive(int_buffer);
            return BitConverter.ToInt64(int_buffer, 0);
        }

        public static void SendLong(Socket s, long i)
        {
            s.Send(BitConverter.GetBytes(i));
        }

        public static void SendLargeFile(Socket s, string file, string file_name)
        {
            SendLong(s, (new FileInfo(file)).Length);

            SendString(s, file_name);

            FileStream stream = File.OpenRead(file);
            int bytesSent = 0;
            byte[] buffer = new byte[1024];
            byte[] buffer2;
            while ((bytesSent = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                buffer2 = new byte[bytesSent];
                Array.Copy(buffer, buffer2, bytesSent);
                s.Send(buffer2);
                if (bytesSent < 1024)
                    break;
                Console.Write("-");
            }
            stream.Close();
        }

        public static void SendLargeFileSecure(Socket s, string file, string file_name, string password)
        {
            ConsoleUtil.Notification("Started Sending File!");
            SendLong(s, (new FileInfo(file)).Length);

            SendString(s, file_name);

            FileStream stream = File.OpenRead(file);
            int bytesSent = 0;
            byte[] buffer = new byte[1024];
            byte[] buffer2;
            while ((bytesSent = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                buffer2 = new byte[bytesSent];
                Array.Copy(buffer, buffer2, bytesSent);
                buffer2 = Security.Encrypt(buffer2, password);
                SendInt(s, buffer2.Length);
                Console.WriteLine("Sent Buffer2 Length! {0}", buffer2.Length);
                s.Send(buffer2);
                Console.WriteLine("Sent Buffer2!");
                if (bytesSent < 1024)
                    break;
                Console.WriteLine("-");
            }
            SendInt(s, 0);
            stream.Close();
        }
    }

    public class ConsoleUtil
    {
        public static void Error(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Notification(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }

    public class Security
    {
        private static readonly byte[] key = new byte[] { 0xfe, 0x07, 0xaf, 0x4d, 0x08, 0x22, 0x3c, 0x26, 0xdc, 0xff, 0x00, 0xad, 0xed, 0x7a, 0xee, 0xc5 };

        public static byte[] Encrypt(byte[] plain, string password)
        {
            MemoryStream memoryStream;
            CryptoStream cryptoStream;
            Rijndael rijndael = Rijndael.Create();
            Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(password, key);
            rijndael.Key = pdb.GetBytes(32);
            rijndael.IV = pdb.GetBytes(16);
            memoryStream = new MemoryStream();
            cryptoStream = new CryptoStream(memoryStream, rijndael.CreateEncryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(plain, 0, plain.Length);
            cryptoStream.Close();
            return memoryStream.ToArray();
        }

        public static byte[] Decrypt(byte[] cipher, string password)
        {
            MemoryStream memoryStream;
            CryptoStream cryptoStream;
            Rijndael rijndael = Rijndael.Create();
            Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(password, key);
            rijndael.Key = pdb.GetBytes(32);
            rijndael.IV = pdb.GetBytes(16);
            memoryStream = new MemoryStream();
            cryptoStream = new CryptoStream(memoryStream, rijndael.CreateDecryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(cipher, 0, cipher.Length);
            cryptoStream.Close();
            return memoryStream.ToArray();
        }
    }

    public class Client
    {
        IPAddress mIPAddress;
        Thread mThread;
        DateTime mTimeJoined;

        public Client(Socket s)
        {
            mIPAddress = (s.RemoteEndPoint as IPEndPoint).Address;
            mThread = Thread.CurrentThread;
            mTimeJoined = DateTime.Now;
        }

        public new string ToString => mIPAddress.ToString() + " | " + mTimeJoined.ToLongDateString();
    }

    public class ClientDataBase
    {
        private static ClientDataBase mInstance;
        private Dictionary<Socket, Client> clientList;
        private Mutex mut;

        private ClientDataBase()
        {
            mut = new Mutex();
            clientList = new Dictionary<Socket, Client>();
        }

        public static ClientDataBase GetInstance()
        {
            if (mInstance == null)
            {
                mInstance = new ClientDataBase();
            }
            return mInstance;
        }

        public void RegisterClient(Socket s)
        {
            mut.WaitOne();
            clientList[s] = new Client(s);
            mut.ReleaseMutex();
        }

        public void RemoveClient(Socket s)
        {
            mut.WaitOne();
            clientList.Remove(s);
            mut.ReleaseMutex();
        }

        public Dictionary<Socket, Client> Clients => clientList;

        public void PrintClients()
        {
            mut.WaitOne();
            int i = 0;
            Console.WriteLine("Clients Connected...");
            foreach (KeyValuePair<Socket, Client> kvp in clientList)
            {
                Console.WriteLine("{0} | {1}", i, kvp.Value.ToString);
                i++;
            }
            if (i == 0)
                Console.WriteLine("No Clients Currently Connected!");
            mut.ReleaseMutex();
        }
    }
}
