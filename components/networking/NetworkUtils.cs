using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CSharpChat
{
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
}