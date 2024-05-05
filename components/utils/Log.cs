using System.Collections.Generic;

namespace CSharpChat
{
    /// <summary>
    /// Generic class for logging messages/commands/etc.
    /// </summary>
    /// <typeparam name="T">Type of item to log</typeparam>
    class Log<T> {
        private readonly List<T> _list = new();

        /// <summary>
        /// An indexer to provide access to _list
        /// </summary>
        /// <value>The index of the item being accessed/modified</value>
        public T this[int i]
        {
            get 
            { 
                try { return _list[i]; }
                catch { return default(T); }
            }
            set 
            { 
                try { _list[i] = value; }
                catch { _list.Add(value); }
            }
        }

        /// <summary>
        /// Reads and returns the length of _list
        /// </summary>
        /// <returns>Number of items in _list</returns>
        public int GetLength() {
            return _list.Count;
        }

        /// <summary>
        /// Adds provided item to Log by appending it to _list
        /// </summary>
        /// <param name="item">Item to add to Log</param>
        public void Add(T item) {
            _list.Add(item);
            return;
        }

        /// <summary>
        /// Empties _list
        /// </summary>
        public void Clear() {
            _list.Clear();
            return;
        }
    }
}
