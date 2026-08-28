namespace ApplicationCore.ValueObjects.Result
{
    public class PagedResult<T> : PagedResultBase where T : class
    {
        public IEnumerable<T> Results { get; set; } = new List<T>();

        public PagedResult()
        { }
    }
}