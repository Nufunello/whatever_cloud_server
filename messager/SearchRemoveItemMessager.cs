using System.Collections.Generic;

namespace messager
{
    public class SearchUpdateItemRequest
    {
        public class Item
        {
            public int Index { get; set; }
            public string Action { get; set; }
        }
        public string Context { get; set; }
        public IEnumerable<Item> Items { get; set; }
    }

    public delegate void SearchUpdateItemMessageHandler(object sender, SearchUpdateItemRequest e);

    public interface ISearchUpdateItemMessager
    {
        event SearchUpdateItemMessageHandler OnSearchRequest;
    }

    public class SearchUpdateItemMessager : ISearchUpdateItemMessager
    {
        private readonly IMessager messager;

        public SearchUpdateItemMessager(IMessager messager)
        {
            this.messager = messager;
            this.messager.OnMessage += (sender, e) =>
            {
                OnSearchRequest?.Invoke(this, System.Text.Json.JsonSerializer.Deserialize<SearchUpdateItemRequest>(e.Span));
            };
        }

        public event SearchUpdateItemMessageHandler OnSearchRequest;
    }
}
