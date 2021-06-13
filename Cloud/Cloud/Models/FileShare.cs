using System;

namespace Cloud.Models
{
    public class FileShare
    {
        public int Id { get; set; }
        public string File { get; set; }
        public string ShareLink { get; set; }
        public string Key { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}