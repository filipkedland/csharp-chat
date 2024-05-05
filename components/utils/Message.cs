using System;
using System.Text.Json.Serialization;

namespace CSharpChat
{
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
}
