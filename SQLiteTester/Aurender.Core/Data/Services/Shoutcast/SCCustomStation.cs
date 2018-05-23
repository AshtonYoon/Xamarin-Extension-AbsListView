using System;
using System.Text.RegularExpressions;

namespace Aurender.Core.Data.Services.Shoutcast
{

    class SCCustomStation : SCStation
    {
        static internal readonly String NA_ID = "N/A";
        static Regex StationParser = new Regex("(a[0-9]*)::(.*)::(.*)");
        
        internal SCCustomStation(String line)
        {
            var matches = StationParser.Match(line);

            if (matches.Success)
            {
                this.StreamingID = matches.Groups[1].Value;
                this.Title = matches.Groups[2].Value;
                this.itemPath = matches.Groups[3].Value;
            }
            else
            {
                this.StreamingID = NA_ID;
                this.Title = "Failed to parse";
                this.itemPath = "";
            }
        }
        public override string ItemPath => $"shout://{ItemLine}";
        internal string ItemLine => $"{StreamingID}::{Title}::{itemPath}";

        public override string ToString()
        {
            return base.ToString();
        }
    }

}