using System.Net.Sockets;
using System.Threading.Tasks;

namespace CSharpChat
{
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
}
