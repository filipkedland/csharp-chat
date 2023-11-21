using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;

namespace csharpchat
{
    /// <summary>
    /// Base class for Client and Listener classes
    /// </summary>
    class Communicator
    {
        public MessageSender sender = new();
        public MessageReader reader = new();
        public InputHandler input = new();
        public List<Message> messageLog = new();
        public NetworkStream stream;
        public string _username = "USER";

        public void RegisterMessage(Message message)
        {
            // TODO: make separate function for this logic?
            messageLog.Add(message);
            Console.Clear();
            var msgLines = Console.WindowHeight - 5;
            Console.WriteLine($"C# Chat - Chatting with {stream.Socket.RemoteEndPoint}..\nType /help for help\n");
            for(int i = messageLog.Count - msgLines; i < messageLog.Count; i++)
            {
                if (i < 0) i = 0;
                var m = messageLog[i];
                Console.WriteLine($"[{m.DateTimeUtc:HH:mm:ss}] {m.Author}: {m.Text}");
            }
            Console.WriteLine();
            return;
        }

        public async void StartListening(NetworkStream stream)
        {
            while (true)
            {
                RegisterMessage(await reader.AwaitMessage(stream, new byte[1024]));
            }
        }
    }

    /// <summary>
    /// Client class used for handling connection and recieving messages from a Host
    /// </summary>
    class Client : Communicator
    {
        public Client(string username)
        {
            _username = username;
        }

        /// <summary>
        /// Create a TcpClient and connect to a TcpListener of specified ip & port
        /// </summary>
        /// <param name="ip">IP Address of Listener</param>
        /// <param name="port">Port of Listener, 5000 for testing</param>
        public async void TcpConnect(IPAddress ip, int port)
        {
            using TcpClient client = new();
            await client.ConnectAsync(ip, port);
            stream = client.GetStream();
            /* Message message = await reader.AwaitMessage(stream, new byte[1024]);
            Console.WriteLine($"Message received at {message.DateTimeUtc}: {message.Text}"); */
            StartListening(stream);
            Console.ReadLine();
            
        }
    }

    class Host : Communicator
    {
        public Host(string username)
        {
            _username = username;
        }

        public async void Initialize(int port)
        {
            IPEndPoint ipEndPoint = new(IPAddress.Any, port);
            TcpListener listener = new(ipEndPoint);

            try
            {
                listener.Start();
                Console.WriteLine($"Listening on {listener.LocalEndpoint}");
                Console.WriteLine("Waiting for connection...");

                using TcpClient handler = await listener.AcceptTcpClientAsync();  // Waits for a Client to connect
                Console.WriteLine($"Connection aquired. Client: {handler.Client.RemoteEndPoint}");

                stream = handler.GetStream();  // Gets NetworkStream to connected Client
                
                while (true)
                {
                    input.GetInput(this);
                }
            }
            finally 
            {
                /* listener.Stop();
                Console.WriteLine("Stopped listener"); */
            }
        }
    }

    class InputHandler
    {
        public void GetInput(Communicator c)
        {
            Console.Write("Input: ");
            var text = Console.ReadLine();
            if (text.Trim() == "") return;
            if (text.Trim().StartsWith("/") && CommandHandler(text.Trim())) return;
            Message message = new(text, c._username);
            c.sender.SendMessage(c.stream, message);
            c.RegisterMessage(message);
            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns>False if invalid command, continues to send as message</returns>
        private bool CommandHandler(string input)
        {
            string[] args = input[1..].Split(" ");  // Splits substring of input (from the slash) into args
            switch (args[0])
            {
                case "help":
                    Console.WriteLine("HELP PAGE :)");
                    break;

                default:
                    return false;  // If no command was found
            }
            return true;
        }
    }

    class MessageSender
    {
        public async void SendMessage(NetworkStream stream, Message message)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(message);  // Converts Message to byte[] before sending
            try { await stream.WriteAsync(bytes); } catch (Exception e) { Console.WriteLine(e); }
            Console.WriteLine($"Sent message: {message.Text}");
            return;
        }
    }

    class MessageReader
    {
        public async Task<Message> AwaitMessage(NetworkStream stream, byte[] buffer)
        {
            while (true)
            {
                if (!stream.CanRead) continue;
                int received = await stream.ReadAsync(buffer);
                if (received == 0) continue;
                string data = Encoding.UTF8.GetString(buffer, 0, received);
                Message message;
                try
                {
                    message = JsonSerializer.Deserialize<Message>(data);
                }
                catch
                {
                    Console.WriteLine("ERROR: Failed to deserialize message!");
                    continue;
                }
                return message;
            }
        }
    }

    /// <summary>
    /// Holds all information about a message, using JsonConstructor to send over TCP
    /// </summary>
    class Message
    {
        private string text;
        private DateTime dateTimeUtc;
        private string author;
        public string Text {
            get { return text; }
        }
        public DateTime DateTimeUtc {
            get { return dateTimeUtc; }
        }
        public string Author {
            get { return author; }
        }

        public Message(string content, string username) {
            text = content;
            dateTimeUtc = DateTime.UtcNow;
            author = username;
        }

        [JsonConstructor]
        public Message(string Text, DateTime DateTimeUtc, string Author) {
            text = Text;
            dateTimeUtc = DateTimeUtc;
            author = Author;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("C# Chat!");
            Console.Write("Enter your username: ");
            var name = Console.ReadLine();
            Console.WriteLine("Do you want to host or join a chat?");
            var input = Console.ReadLine().ToLower();
            if (input == "join")
            {
                Client client = new Client(name);
                client.TcpConnect(IPAddress.Parse("127.0.0.1"), 5000);
            }
            else if (input == "host")
            {
                Host host = new Host(name);
                /* Thread t = new Thread(() => listener.StartListen(5000));
                t.Start(); */
                host.Initialize(5000);
            }
            while(true){}
        }
    }
}
