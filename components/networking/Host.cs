using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CSharpChat
{
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
}
