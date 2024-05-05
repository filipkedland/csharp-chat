using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CSharpChat
{
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
}
