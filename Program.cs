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
using System.Runtime.CompilerServices;

namespace csharpchat
{
    /// <summary>
    /// Creates and handles Communicators
    /// </summary>
    class CommunicatorHandler
    {
        public void Initialize()
        {
            Console.WriteLine("C# Chat!\n");
            Console.Write("Enter your username: ");
            var name = Console.ReadLine();
            var communicator = Activator.CreateInstance(WhichType(), name);
            RunCommunicator((Communicator)communicator);
            while (true) {}
        }

        private static Type WhichType()
        {
            while (true) {
                Console.Write("\nDo you want to host or join a chat? ");
                var input = Console.ReadLine().ToLower();

                if (input == "join") return typeof(Client);
                else if (input == "host") return typeof(Host);

                Console.WriteLine("\nType either JOIN or HOST!");
                continue;
            }
        }

        private static void RunCommunicator(Communicator communicator)
        {
            communicator.Start();
        }
    }

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
        public string Username;
        public bool IsConnected = false;

        public void DisplayChat()
        {
            Console.Clear();
            var msgLines = Console.WindowHeight - 5;
            Console.WriteLine($"C# Chat - Chatting with {stream.Socket.RemoteEndPoint}..\nType /help for help\n");
            for(int i = messageLog.Count - msgLines; i < messageLog.Count; i++)
            {
                if (messageLog.Count < 1) break;
                if (i < 0) i = 0;
                var m = messageLog[i];
                Console.WriteLine($"[{m.DateTimeUtc:HH:mm:ss}] {m.Author}: {m.Text}");
            }
            Console.WriteLine();
            Console.Write("Input: ");
        }

        public void RegisterMessage(Message message)
        {
            messageLog.Add(message);
            DisplayChat();
            return;
        }

        public async void StartListening()
        {
            while (true)
            {
                Message message = await reader.AwaitMessage(stream, new byte[1024]);
                if (message == null) 
                {
                    ConnectionClosed();
                    return;
                }
                RegisterMessage(message);
            }
        }

        public virtual void Start() {}  
        public virtual void ConnectionClosed() 
        {
            Console.WriteLine("\nConnection closed!\nRestarting in 5 seconds..");
            Thread.Sleep(5000);
            Start();
        }
    }

    /// <summary>
    /// Client class used for handling connection and recieving messages from a Host
    /// </summary>
    class Client : Communicator
    {
        public Client(string username = "USER")
        {
            Username = username;
        }

        public override void Start()
        {
            Console.Clear();
            TcpConnect(GetEndPoint());
        }

        private static IPEndPoint GetEndPoint()
        {
            while (true)
            {
                Console.WriteLine("Enter host ip and port (IP:PORT): ");
                var input = Console.ReadLine().Trim().Split(":");  // Splits input into IP:PORT
                try {
                    IPEndPoint ep = new(IPAddress.Parse(input[0]), int.Parse(input[1]));
                    return ep;
                } catch {
                    Console.WriteLine("Failed to parse IP!\n");
                }
            }
        }

        /// <summary>
        /// Create a TcpClient and connect to a TcpListener of specified ip & port
        /// </summary>
        /// <param name="ep">IPEndPoint of Listener</param>
        private async void TcpConnect(IPEndPoint ep)
        {
            // TODO: FIX ERROR WHEN CONNECTING SECOND TIME
            using TcpClient client = new();
            await client.ConnectAsync(ep);
            IsConnected = true;
            stream = client.GetStream();
            DisplayChat();
            StartListening();
            
            while (true)
            {
                input.GetInput(this);
            }
        }
    }

    class Host : Communicator
    {
        private readonly IPEndPoint ipEndPoint;
        private readonly TcpListener listener;

        public Host(string username = "USER", int port = 5000)
        {
            Username = username;
            ipEndPoint = new(IPAddress.Any, port);
            listener = new(ipEndPoint);
        }

        public override void Start()
        {
            Initialize();
        }

        public async void Initialize()
        {
            // TODO: FIX ERROR WHEN RESTARTING (CLIENT DISCONNECT) (Socket can only be used once)
            try
            {
                listener.Start();
                Console.WriteLine($"Waiting for connection on port {ipEndPoint.Port}...");

                Thread helper = new(ConnectionHelp);  // Creates a thread that waits 15s then sends a help message
                helper.Start();

                using TcpClient handler = await listener.AcceptTcpClientAsync();  // Waits for a Client to connect
                IsConnected = true;

                stream = handler.GetStream();  // Gets NetworkStream to connected Client

                DisplayChat();
                StartListening();
                
                while (true)
                {
                    input.GetInput(this);
                }
            } catch {
                Console.WriteLine("Unexpected error occurred!");
                ConnectionClosed();
            }
        }

        public override void ConnectionClosed()
        {
            try { listener.Stop(); }
            finally 
            { 
                Console.WriteLine("TcpListener stopped..");
                base.ConnectionClosed();
            }
        }

        private void ConnectionHelp()
        {
            Thread.Sleep(15000);  // Waits 15 seconds
            if (IsConnected) return;  // Cancels if connected
            Console.WriteLine("\nTrouble connecting? Go to https://whatismyip.com/ to find your IP address.");
            Console.WriteLine("If you're still having problems, you might have to forward port 5000 in your router settings.");
        }
    }

    class InputHandler
    {
        public void GetInput(Communicator c)
        {
            var text = Console.ReadLine();
            if (text.Trim() == "") return;
            if (text.Trim().StartsWith("/"))
            {
                Message cmd = CommandHandler(text.Trim());
                if (cmd != null) 
                {
                    c.RegisterMessage(cmd);
                    return;
                }
            } 
            Message message = new(text, c.Username);
            c.sender.SendMessage(c.stream, message);
            c.RegisterMessage(message);
            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns>False if invalid command, continues to send as message</returns>
        private Message CommandHandler(string input)
        {
            string[] args = input[1..].Split(" ");  // Splits substring of input (from the slash) into args
            string output;
            switch (args[0])
            {
                case "help":
                    output = "HELP PAGE :)";
                    break;

                default:
                    return null;  // If no command was found
            }
            return new Message(output, "System");
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
                int received;
                
                try {
                    received = await stream.ReadAsync(buffer);
                } catch {
                    return null;  // Connection lost
                }
                
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
        private readonly string text;
        private readonly DateTime dateTimeUtc;
        private readonly string author;
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
            CommunicatorHandler handler = new();
            handler.Initialize();
        }
    }
}
