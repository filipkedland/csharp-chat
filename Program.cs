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
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace CSharpChat
{
    /// <summary>
    /// Creates and handles Communicators
    /// </summary>
    static class CommunicatorHandler
    {
        /// <summary>
        /// Runs the CommunicatorHandler
        /// </summary>
        public static void Run()
        {
            while (true)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Initializes the chat by getting user input and starting the requested ChatBase
        /// </summary>
        private static void Initialize()
        {
            Console.Clear();
            Console.WriteLine("C# Chat!\n");
            Console.Write("Enter your username: ");
            var name = Console.ReadLine();
            Type typeOfChatBase = WhichType();
            var chatBase = Activator.CreateInstance(typeOfChatBase, name);
            RunChatBase((ChatBase)chatBase);
        }

        /// <summary>
        /// Asks the user if they want to host or join a chat
        /// </summary>
        /// <returns>The type of ChatBase to create</returns>
        private static Type WhichType()
        {
            while (true)
            {
                Console.Write("\nDo you want to host or join a chat? ");
                var input = Console.ReadLine().Trim().ToLower();

                if (input == "join") return typeof(Client);
                else if (input == "host") return typeof(Host);

                Console.WriteLine("\nType either JOIN or HOST!");
                continue;
            }
        }

        /// <summary>
        /// Runs the ChatBase
        /// </summary>
        /// <param name="chatBase">The ChatBase to run</param>
        private static void RunChatBase(ChatBase chatBase)
        {
            chatBase.Start();
        }
    }

    /// <summary>
    /// Abstract class representing a communicator
    /// </summary>
    abstract class Communicator
    {
        protected NetworkStream Stream;

        /// <summary>
        /// Listens for incoming messages
        /// </summary>
        protected async Task ListenForMessage()
        {
            while (true)
            {
                Message message = await MessageReader.AwaitMessage(Stream, new byte[1024]);
                if (message == null)
                {
                    return;
                }
                this.RegisterMessage(message);
            }
        }

        /// <summary>
        /// Sends a message using MessageSender
        /// </summary>
        /// <param name="message">The Message to send</param>
        public void SendMessage(Message message)
        {
            MessageSender.SendMessage(this.Stream, message);
            this.RegisterMessage(message);
        }

        /// <summary>
        /// Method to be implemented by children when the connection is closed
        /// </summary>
        protected abstract void ConnectionClosed();

        /// <summary>
        /// Method to be implemented by chilren for registering a Message
        /// </summary>
        /// <param name="message">The Message to register</param>
        public abstract void RegisterMessage(Message message);
    }

    /// <summary>
    /// Class representing a chat base
    /// </summary>
    class ChatBase : Communicator
    {
        protected ChatInputHandler InputHandler;
        protected Log<Message> MessageLog = new();
        public string Username;

        /// <summary>
        /// Displays the chat messages
        /// </summary>
        /// <param name="acceptInput">Flag indicating if input is accepted</param>
        public void DisplayChat(bool acceptInput)
        {
            Console.Clear();
            var msgLines = Console.WindowHeight - 5;
            if (acceptInput) Console.WriteLine($"C# Chat - Chatting with {this.Stream.Socket.RemoteEndPoint}..\nType /help for help\n");
            for (int i = this.MessageLog.GetLength() - msgLines; i < this.MessageLog.GetLength(); i++)
            {
                if (this.MessageLog.GetLength() < 1) break;
                if (i < 0) i = 0;
                var message = this.MessageLog[i];
                Console.WriteLine($"[{message.DateTimeUtc:HH:mm:ss}] {message.Author}: {message.Text}");
            }
            Console.WriteLine();
            if (acceptInput) Console.Write("Input: ");
        }

        /// <summary>
        /// Registers a Message and displays the chat
        /// </summary>
        /// <param name="message">The Message to register</param>
        public override void RegisterMessage(Message message)
        {
            this.MessageLog.Add(message);
            this.DisplayChat(true);
        }

        /// <summary>
        /// Starts the ChatBase
        /// </summary>
        public virtual void Start()
        {
            InputHandler = new ChatInputHandler(this);
        }

        /// <summary>
        /// Method to be called when the connection is closed
        /// </summary>
        protected override void ConnectionClosed()
        {
            this.DisplayChat(false);
            this.MessageLog.Clear();
            Console.WriteLine("Connection closed!\nRestarting in 5 seconds..");
            Thread.Sleep(5000);
        }
    }

    /// <summary>
    /// Client class used for handling connection and recieving messages from a Host
    /// </summary>
    class Client : ChatBase
    {
        /// <summary>
        /// Initializes a new instance of the Client class
        /// </summary>
        /// <param name="username">The username of the Client. Defaults to "USER"</param>
        public Client(string username = "USER")
        {
            Username = username;
        }

        /// <summary>
        /// Overrides the Start method from the base class
        /// Connects the client to the server using the TcpConnect method
        /// </summary>
        public override void Start()
        {
            base.Start();
            this.TcpConnect(GetEndPoint()).Wait();
        }

        /// <summary>
        /// Gets the IPEndPoint for the server from the user input
        /// </summary>
        /// <returns>The IPEndPoint representing the Host's IP address and port</returns>
        private static IPEndPoint GetEndPoint()
        {
            Console.Clear();
            while (true)
            {
                Console.WriteLine("Enter host ip and port (IP:PORT): ");

                // Splits input into IP:PORT
                var input = Console.ReadLine().Trim().Split(":");  
                try
                {
                    IPEndPoint endPoint = new(IPAddress.Parse(input[0]), int.Parse(input[1]));
                    return endPoint;
                }
                catch
                {
                    Console.WriteLine("Failed to parse IP!\n");
                }
            }
        }

        /// <summary>
        /// Creates a TcpClient and connects to a TcpListener of specified ip & port
        /// </summary>
        /// <param name="endPoint">IPEndPoint of Host</param>
        private async Task TcpConnect(IPEndPoint endPoint)
        {
            try
            {
                // Initializes a TcpClient and connects to endPoint
                using TcpClient client = new();
                await client.ConnectAsync(endPoint);

                // Gets NetworkStream to connected Host
                this.Stream = client.GetStream();

                // Displays chat, then starts listening for Messages
                this.DisplayChat(true);
                Task streamReader = this.ListenForMessage();

                // Starts a Task that keeps getting chat input until Client disconnects
                Task inputHandler = new(() =>
                {
                    while (true)
                    {
                        if (streamReader.IsCompleted) return;
                        this.InputHandler.GetInput();
                    }
                });
                inputHandler.Start();

                // Check if connected every 2 seconds
                // Shuts down if not
                while (true)
                {
                    if (!streamReader.IsCompleted) await Task.Delay(2000);
                    else
                    {
                        this.ConnectionClosed();
                        return;
                    }
                }
            }
            catch
            {
                // Exception likely thrown because of connection problem, run ConnectionClosed
                this.ConnectionClosed();
                return;
            }
        }
    }

    // TODO: Implement dynamic port
    /// <summary>
    /// Represents a Host that can initiate a chat session with a Client.
    /// </summary>
    class Host : ChatBase
    {
        private readonly IPEndPoint _ipEndPoint;
        private readonly TcpListener _listener;

        /// <summary>
        /// Constructor for Host class.
        /// </summary>
        /// <param name="username">The username of the Host. Default is "USER"</param>
        public Host(string username = "USER")
        {
            Username = username;
            _ipEndPoint = new(IPAddress.Any, 5000); 
            _listener = new(_ipEndPoint);
        }

        /// <summary>
        /// Starts the Host and initializes the chat session
        /// </summary>
        public override void Start()
        {
            base.Start();
            this.Initialize().Wait();
        }

        /// <summary>
        /// Initializes the chat session by starting the TcpListener 
        /// and handling incoming connections
        /// </summary>
        public async Task Initialize()
        {
            try
            {
                this._listener.Start();
                Console.Clear();
                Console.WriteLine($"Waiting for connection on port {this._ipEndPoint.Port}...");

                // Creates a thread that waits 15s then sends a help message unless connected
                Thread helper = new(ConnectionHelp); 
                helper.Start();

                // Waits for a Client to connect
                using TcpClient handler = await this._listener.AcceptTcpClientAsync();  

                // Gets NetworkStream to connected Client
                this.Stream = handler.GetStream();  

                // Displays chat and starts listening for Messages
                this.DisplayChat(true);
                Task streamReader = this.ListenForMessage();

                // Starts a Task that keeps getting chat input until Client disconnects
                Task inputHandler = new(() => 
                {
                    while (true)
                    {
                        if (streamReader.IsCompleted) return;
                        this.InputHandler.GetInput();
                    }
                });
                inputHandler.Start();
                
                // Check if connected every 2 seconds
                // Shuts down if not
                while (true)
                {
                    if (!streamReader.IsCompleted) await Task.Delay(2000);
                    else
                    {
                        this.ConnectionClosed();
                        return;
                    }
                }
            } 
            catch 
            {
                // Exception likely thrown because of connection problem, run ConnectionClosed
                this.ConnectionClosed();
                return;
            }
        }

        /// <summary>
        /// Handles the clean-up when the connection is closed
        /// </summary>
        protected override void ConnectionClosed()
        {
            try { this._listener.Stop(); }
            finally { base.ConnectionClosed(); }
        }

        /// <summary>
        /// Waits 15 seconds before sending help message, if connection 
        /// hasn't been made before then.
        /// </summary>
        private void ConnectionHelp()
        {
            Thread.Sleep(15000);
            if(this.HasConnected()) return;
            Console.WriteLine("\nTrouble connecting? Go to https://whatismyip.com/ to find your IP address.");
            Console.WriteLine("If you're still having problems, you might have to forward port 5000 in your router settings.");
        }

        /// <summary>
        /// Determines whether a connection has been made to this Host
        /// </summary>
        /// <returns>True if stream has been assigned to after initialization</returns>
        private bool HasConnected()
        {
            return this.Stream != null;
        }
    }

    /// <summary>
    /// Class for handling chat input
    /// Sends messages and handles commands
    /// </summary>
    class ChatInputHandler
    {
        private readonly ChatBase _chatBase;

        /// <summary>
        /// Constructor for ChatInputHandler
        /// </summary>
        /// <param name="chatBase">ChatBase to handle chat input for</param>
        public ChatInputHandler(ChatBase chatBase)
        {
            _chatBase = chatBase;
        }

        /// <summary>
        /// Determines whether chat input is a message or command
        /// and handles it appropriately
        /// </summary>
        public void GetInput()
        {
            var text = Console.ReadLine();
            if (text.Trim() == "") return;
            if (text.Trim().StartsWith("/"))
            {
                Message cmd = CommandHandler(text.Trim());
                if (cmd != null) 
                {
                    _chatBase.RegisterMessage(cmd);
                    return;
                }
            } 
            Message message = new(text, _chatBase.Username);
            _chatBase.SendMessage(message);
        }

        /// <summary>
        /// Responds to a given command
        /// </summary>
        /// <param name="input">User chat input</param>
        /// <returns>null if invalid command, continues to send as Message
        /// Otherwise returns response as Message from System</returns>
        private static Message CommandHandler(string input)
        {
            // Splits substring of input (from the slash) into args
            string[] args = input[1..].Split(" ");  
            string output;
            switch (args[0])
            {
                case "help":
                    output = "HELP PAGE :)";
                    break;

                default:
                    // If no command was found
                    return null;  
            }
            return new Message(output, "System");
        }
    }

    /// <summary>
    /// Static class used for sending Messages
    /// </summary>
    static class MessageSender
    {
        /// <summary>
        /// Sends a Message through a given NetworkStream
        /// </summary>
        /// <param name="stream">NetworkStream to send Message over</param>
        /// <param name="message">Message to send</param>
        public static async void SendMessage(NetworkStream stream, Message message)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(message);  // Converts Message to byte[] before sending
            try { await stream.WriteAsync(bytes); } catch (Exception e) { Console.WriteLine(e); }
            Console.WriteLine($"Sent message: {message.Text}");
        }
    }

    /// <summary>
    /// Static class with utilities for reading and deserializing Messages from a NetworkStream
    /// </summary>
    static class MessageReader
    {
        /// <summary>
        /// Waits until a Message is recieved, then deserializes it
        /// </summary>
        /// <param name="stream">NetworkStream to read from</param>
        /// <param name="buffer">Size of buffer to read</param>
        /// <returns>Message that was read from NetworkStream</returns>
        public static async Task<Message> AwaitMessage(NetworkStream stream, byte[] buffer)
        {
            while (true)
            {
                if (!stream.CanRead) continue;
                int received;
                
                try {
                    received = await stream.ReadAsync(buffer);
                } catch {
                    // Connection lost
                    return null;  
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
    /// Generic class for logging messages/commands/etc.
    /// </summary>
    /// <typeparam name="T">Type of item to log</typeparam>
    class Log<T> {
        private readonly List<T> _list = new();

        /// <summary>
        /// An indexer to provide access to _list
        /// </summary>
        /// <value>The index of the item being accessed/modified</value>
        public T this[int i]
        {
            get 
            { 
                try { return _list[i]; }
                catch { return default(T); }
            }
            set 
            { 
                try { _list[i] = value; }
                catch { _list.Add(value); }
            }
        }

        /// <summary>
        /// Reads and returns the length of _list
        /// </summary>
        /// <returns>Number of items in _list</returns>
        public int GetLength() {
            return _list.Count;
        }

        /// <summary>
        /// Adds provided item to Log by appending it to _list
        /// </summary>
        /// <param name="item">Item to add to Log</param>
        public void Add(T item) {
            _list.Add(item);
            return;
        }

        /// <summary>
        /// Empties _list
        /// </summary>
        public void Clear() {
            _list.Clear();
            return;
        }
    }

    /// <summary>
    /// Holds all information about a message, using JsonConstructor to send over TCP
    /// </summary>
    class Message
    {
        private readonly string _text;
        private readonly DateTime _dateTimeUtc;
        private readonly string _author;
        public string Text {
            get { return _text; }
        }
        public DateTime DateTimeUtc {
            get { return _dateTimeUtc; }
        }
        public string Author {
            get { return _author; }
        }

        public Message(string content, string username) {
            _text = content;
            _dateTimeUtc = DateTime.UtcNow;
            _author = username;
        }

        [JsonConstructor]
        public Message(string Text, DateTime DateTimeUtc, string Author) {
            _text = Text;
            _dateTimeUtc = DateTimeUtc;
            _author = Author;
        }
    }

    /// <summary>
    /// Command class to implement specific commands
    /// Note: Not currently implemented
    /// </summary>
    class Command 
    {
        private readonly string name;
        public string Name {
            get { return name; }
        }
        public virtual void Execute() {}
    }

    /// <summary>
    /// Program entry point, runs the CommunicatorHandler
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            CommunicatorHandler.Run();
        }
    }
}
