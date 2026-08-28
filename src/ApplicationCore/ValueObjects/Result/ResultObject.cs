namespace ApplicationCore.ValueObjects.Result
{
    public class ResultObject : ResultObject<object>
    { }

    public class ResultObject<T>
    {
        public bool Status { get; set; } = false;
        public string Message { get; set; }
        public string Detail { get; set; }
        public string ErrorCode { get; set; }
        public T Data { get; set; }

        public ResultObject()
        { }
    }
}