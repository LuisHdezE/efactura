namespace ApplicationCore.Exceptions
{
    public class GatewayException : Exception
    {
        public GatewayException()
        { }

        public GatewayException(string message) : base(message)
        {
        }

        public GatewayException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}