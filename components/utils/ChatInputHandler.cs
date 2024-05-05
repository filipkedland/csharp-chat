using System;

namespace CSharpChat
{
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
}
