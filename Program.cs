using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace csharpchat
{
    /// <summary>
    /// Base class for Client and Listener classes
    /// </summary>
    class Communicator
    {
        private MessageSender sender = new();
        private MessageReader reader = new();
        private string _username = "USER";


    }

    /// <summary>
    /// Client class used for handling connection and recieving messages from a Listener
    /// </summary>
    class Client
    {
        private MessageSender sender = new();
        private MessageReader reader= new();
        private string _username;

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
            await using NetworkStream stream = client.GetStream();
            Message message = await reader.AwaitMessage(stream);
            Console.WriteLine($"Message received at {message.DateTimeUtc}: {message.Text}");
        }
    }

    class Listener
    {
        private MessageSender sender = new();
        private MessageReader reader= new();
        public async void StartListen(int port)
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

                await using NetworkStream stream = handler.GetStream();  // Gets NetworkStream to connected Client

                sender.SendMessage(stream, new Message(content: "TESTING"));  // Sends message to Client
            }
            finally 
            {
                /* listener.Stop();
                Console.WriteLine("Stopped listener"); */
            }
        }
    }

    static class InputHandler
    {

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
        readonly byte[] buffer = new byte[1024]; // set size for testing
        public async Task<Message> AwaitMessage(NetworkStream stream)
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
                    Console.WriteLine("failed to deserialize message");
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
        public string Text {
            get { return text; }
        }
        public DateTime DateTimeUtc {
            get { return dateTimeUtc; }
        }
        
        public Message(string content) {
            text = content;
            dateTimeUtc = DateTime.UtcNow;
        }

        [JsonConstructor]
        public Message(string Text, DateTime DateTimeUtc) {
            text = Text;
            dateTimeUtc = DateTimeUtc;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("C# Chat!");

            Console.WriteLine("Do you want to host or join a chat?");
            var input = Console.ReadLine().ToLower();
            if (input == "join")
            {
                Client client = new Client();
                client.TcpConnect(IPAddress.Parse("127.0.0.1"), 5000);
            }
            else if (input == "host")
            {
                Listener listener = new Listener();
                /* Thread t = new Thread(() => listener.StartListen(5000));
                t.Start(); */
                listener.StartListen(5000);
            }
            while(true){}
        }
    }
}
