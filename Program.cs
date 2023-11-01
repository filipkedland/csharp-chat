using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace csharpchat
{
    class Client
    {
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
            
            var buffer = new byte[1_024]; // set size for testing
            int received = await stream.ReadAsync(buffer);
            var message = Encoding.UTF8.GetString(buffer, 0, received);
            Console.WriteLine($"Message received: {message}");
        }
    }

    class Listener
    {
        public async void StartListen(int port)
        {
            IPEndPoint ipEndPoint = new(IPAddress.Any, port);
            TcpListener listener = new(ipEndPoint);

            try 
            {
                listener.Start();
                using TcpClient handler = await listener.AcceptTcpClientAsync();
                await using NetworkStream stream = handler.GetStream();
                var message = "TEST";
                var bytes = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(bytes);
                Console.WriteLine("Sent message");
            }
            finally 
            {
                listener.Stop();
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var input = Console.ReadLine().ToLower();
            if (input == "client")
            {
                Client client = new Client();
                client.TcpConnect(IPAddress.Parse("127.0.0.1"), 5000);
            }
            else if (input == "listener")
            {
                Listener listener = new Listener();
                listener.StartListen(5000);
            }
            Console.WriteLine("Program end");
            Console.ReadLine();
        }
    }
}
