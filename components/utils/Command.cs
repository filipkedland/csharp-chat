namespace CSharpChat
{
    /// <summary>
    /// Command class to implement specific commands
    /// Note: Not currently implemented
    /// </summary>
    class Command 
    {
        private readonly string name;
        public string Name {
            get { return name; }
        }
        public virtual void Execute() {}
    }
}
