using System;

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
}
