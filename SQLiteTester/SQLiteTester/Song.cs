using System;

namespace SQLiteTester
{
    // Photo: contains image resource ID and caption:
    public class Song
    {
        public string Title { get; set; }
        public string Detail { get; set; }
        public string Duration { get; set; }
        public byte[] Cover { get; set; }
    }
}