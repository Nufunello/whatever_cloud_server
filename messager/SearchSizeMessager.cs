using System.Collections.Generic;

namespace messager
{
    public class SearchSizeRequest
    {
        public string Context { get; set; }
    }
    public class SearchSizeResponse
    {
        public string Context { get; set; }
        public int Size { get; set; }
    }

    public delegate void SearchSizeMessageHandler(object sender, SearchSizeRequest e);

    public interface ISearchSizeMessager
    {
        event SearchSizeMessageHandler OnSearchRequest;
        void SendSearchResult(SearchSizeResponse response);
    }

    public class SearchSizeMessager : ISearchSizeMessager
    {
        private readonly IMessager messager;

        public SearchSizeMessager(IMessager messager)
        {
            this.messager = messager;
            this.messager.OnMessage += (sender, e) =>
            {
                OnSearchRequest?.Invoke(this, System.Text.Json.JsonSerializer.Deserialize<SearchSizeRequest>(e.Span));
            };
        }

        public event SearchSizeMessageHandler OnSearchRequest;

        public void SendSearchResult(SearchSizeResponse response)
        {
            messager.SendMessage(System.Text.Json.JsonSerializer.Serialize(response));
        }
    }
}
