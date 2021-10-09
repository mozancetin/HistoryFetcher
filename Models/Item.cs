using System;

namespace HistoryFetcher
{
    public class Item
    {
        public int ID { get; set; }
        public string title { get; set; }
        public string url { get; set; }
        public DateTime date { get; set; }
        public string TotalMilliseconds { get; set; }
    }
}
