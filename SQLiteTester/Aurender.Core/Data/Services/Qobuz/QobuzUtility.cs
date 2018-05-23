using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Aurender.Core.Contents.Streaming;

namespace Aurender.Core.Data.Services.Qobuz
{
    internal class QobuzUtility
    {
        internal static List<string> CoverLink(JToken images)
        {
            List<String> links = new List<string>();

            var sizes = new List<String>() { "thumbnail", "small", "large" };

            foreach (String size in sizes)
            {
                var link = images[size];
                if (link != null)
                {
                    links.Add(link.ToString());
                }
                else
                {
                    links.Add(null);
                }
            }

            return links;
        }
        internal static bool IsLaterThanNow(long tickFrom1970)
        {
            tickFrom1970 /= 1000;
            TimeSpan t = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1));

            long timestamp = (long)t.TotalSeconds;

            long diff = (tickFrom1970 - timestamp);

            return diff >= 0;
        }

        internal static List<string> CoverLinkForArtist(JToken token)
        {
            List<String> links = new List<string>();
            var images = token["image"];
            if (images == null)
            {
                links.Add(null);
                links.Add(null);
                links.Add(null);
                return links;
            }

            var sizes = new List<String>() { "small", "medium", "large" };

            foreach (String size in sizes)
            {
                var link = images[size];
                if (link != null)
                {
                    links.Add(link.ToString());
                }
                else
                {
                    links.Add(null);
                }
            }

            return links;
        }

        internal static String GetYYYYMMDDFromTickFrom1970(long tickFrom1970)
        {
            tickFrom1970 /= 1000;
            TimeSpan t = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1));

            long timestamp = (long)t.TotalSeconds;

            long diff = (tickFrom1970 - timestamp);
            String date = DateTime.UtcNow.AddSeconds(diff).ToString("yyyyMMdd");

            return date;
        }

        internal static StreamingObjectDescription ProcessDescription(string biography)
        {
            StreamingObjectDescription description = new StreamingObjectDescription();
            Object obj = Newtonsoft.Json.JsonConvert.DeserializeObject(biography);
            JToken token = obj as JToken;
            if (token != null)
            {
                JToken value = token["content"];
                if (value != null)
                {
                    description.Add(DescriptionField.Content, value.ToString());
                }
                value = token["summary"];
                if (value != null)
                    description.Add(DescriptionField.Summary, value.ToString());
            }

            return description;
        }

        internal static string CoverLinkForPlaylist(JToken images)
        {
            if (images != null && images.Type == JTokenType.Array)
            {
                JArray array = images as JArray;
                if (array.Count == 1)
                    return array[0].ToString();
                if (array.Count > 1)
                    return array[array.Count - 1].ToString();
            }
            return String.Empty;
        }
    }
}