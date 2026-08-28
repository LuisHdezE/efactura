namespace ApplicationCore.Exceptions
{
    public class DBOperationException : Exception
    {
        public DBOperationException()
        { }

        public DBOperationException(string message) : base(message)
        {
        }

        public DBOperationException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}