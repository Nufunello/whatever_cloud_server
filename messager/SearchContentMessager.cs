using System.Collections.Generic;

namespace messager
{
    public class SearchContentRequest
    {
        public string Context { get; set; }
        public int Index { get; set; }
        public int Count { get; set; }
    }
    public class SearchContentResponse
    {
        public class SearchResponseItem
        {
            public int Index { get; set; }
            public string Path { get; set; }
        }
        public string Context { get; set; }
        public IEnumerable<SearchResponseItem> Items { get; set; }
    }

    public delegate void SearchContentMessageHandler(object sender, SearchContentRequest e);

    public interface ISearchContentMessager
    {
        event SearchContentMessageHandler OnSearchRequest;
        void SendSearchResult(SearchContentResponse response);
    }

    public class SearchContentMessager : ISearchContentMessager
    {
        private readonly IMessager messager;

        public SearchContentMessager(IMessager messager)
        {
            this.messager = messager;
            this.messager.OnMessage += (sender, e) =>
            {
                OnSearchRequest?.Invoke(this, System.Text.Json.JsonSerializer.Deserialize<SearchContentRequest>(e.Span));
            };
        }

        public event SearchContentMessageHandler OnSearchRequest;

        public void SendSearchResult(SearchContentResponse response)
        {
            messager.SendMessage(System.Text.Json.JsonSerializer.Serialize(response));
        }
    }
}
