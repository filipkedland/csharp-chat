using System;
using System.Threading;

namespace CSharpChat
{
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
}
