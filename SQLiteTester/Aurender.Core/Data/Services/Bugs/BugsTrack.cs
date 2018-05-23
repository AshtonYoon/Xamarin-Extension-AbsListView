using System;
using System.Collections.Generic;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.Bugs
{

    internal class BugsTrack : StreamingTrack
    {

        public StreamingTrackFeatures feature;
        public Dictionary<String, String> coverLinks;
        public BugsTrack() : base(ContentType.Bugs)
        {

        }
        
        public BugsTrack(JToken token) : base(ContentType.Bugs)
        {
            this.StreamingID = token["track_id"].ToObject<String>();

            if (token["album_link_yn"].ToString() == "Y")
                this.StreamingAlbumID = token["album_id"].ToObject<String>();
            else
                this.StreamingAlbumID = string.Empty;

            this.AlbumTitle = token["album_title"].ToObject<String>();
            this.coverLinks = BugsUtilty.GetImageUrls(token["img_urls"]);


            this.ArtistName = token["artist_nm"].ToObject<String>();
            this.StreamingArtistID = token["artist_id"].ToObject<String>();

            this.AllowStreaming = token["track_str_rights"].ToObject<bool>();

            this.Title = token["track_title"].ToObject<String>();

            this.TrackIndex = token["track_no"].ToObject<int>();
            this.DiscIndex = token["disc_id"].ToObject<int>();
            this.Duration = BugsUtilty.GetDurationFromString(token["len"].ToString());


            bool IsPremiumOnly = token["is_flac_str_premium"].ToObject<bool>();


            feature = 0;

            if (token["bside_yn"].ToString() == "Y")
                feature = StreamingTrackFeatures.BSide;
            if (token["adult_yn"].ToString() == "Y")
                feature =  (feature | StreamingTrackFeatures.Adult);
            if (IsPremiumOnly)
                feature =  (feature | StreamingTrackFeatures.Prime);

        }

        public override string ImageUrl => coverLinks[BugsUtilty.Small];

        public override string ImageUrlForMediumSize => coverLinks[BugsUtilty.Medium];

        public override string ImageUrlForLargeSize => coverLinks[BugsUtilty.Large];

        public override object GetExtraImage()
        {
            string source = null;

            if(feature.HasFlag(StreamingTrackFeatures.Adult) &&
                feature.HasFlag(StreamingTrackFeatures.Prime) &&
                feature.HasFlag(StreamingTrackFeatures.BSide))
            {
                source = "bugs_19_premium_bside.png";
            }
            else if(feature.HasFlag(StreamingTrackFeatures.BSide) &&
                feature.HasFlag(StreamingTrackFeatures.Adult))
            {
                source = "bugs_beside_19.png";
            }
            else if (feature.HasFlag(StreamingTrackFeatures.Prime) &&
                feature.HasFlag(StreamingTrackFeatures.Adult))
            {
                source = "bugs_premium_19.png";
            }
            else if (feature.HasFlag(StreamingTrackFeatures.BSide) &&
                feature.HasFlag(StreamingTrackFeatures.Prime))
            {
            }
            else if (feature.HasFlag(StreamingTrackFeatures.BSide))
            {
                source = "bugs_bside.png";
            }
            else if (feature.HasFlag(StreamingTrackFeatures.Prime))
            {
                source = "bugs_premium.png";
            }
            else if (feature.HasFlag(StreamingTrackFeatures.Adult))
            {
                source = "adult.png";
            }

            return ImageUtility.GetImageSourceFromFile(source);
        }
        public bool IsRestrictedByAge => (feature & StreamingTrackFeatures.Adult) == StreamingTrackFeatures.Adult;
    }

    internal static class BugsUtilty
    {
        // album (track)
        // 200 original 140 1000 350 75 500
        // artist
        // 200 70 140 350 75 500
        // playlist
        // 200 120 140 1000 350 75 500

        public const string Small = "140";
        public const string Medium = "500";
        public const string Large = "1000";

        internal static Tuple<bool, String> IsRequestSucess(JToken message)
        {
            bool sucess = "0".Equals(message["ret_code"].ToString());
            String errorMessag = "";
            if (!sucess)
            {
                errorMessag = message["ret_detail_msg"]?.ToString() ?? "No message";
            }
            return new Tuple<bool, string>(sucess, errorMessag);
            
        }

        internal static string GetAlbumType(String albumCode)
        {
            switch(albumCode)
            {
                case "SP":
                    return "스페셜";
                case "EP":
                    return "미니앨범";
                case "DS":
                    return "디지털 싱클";
                case "RL":
                    return "정규";
                case "CP":
                    return "콤필레이션";
                case "LV":
                    return "라이브";
                case "RM":
                    return "리마스터";
                case "SL":
                    return "싱글";
                case "BS":
                    return "베스트";
                default:
                    return String.Empty;
            }
        }

        internal static Dictionary<String, String> GetImageUrls(JToken token)
        {
            Dictionary<String, String> links = new Dictionary<String, string>();

            if (token != null)
            {
                    foreach (JProperty prop in token.Children())
                    {
                        links.Add(prop.Name, prop.Value.ToString());
                    }
            }

            var sizes = new String[] { BugsUtilty.Small, BugsUtilty.Medium, BugsUtilty.Large };

            foreach(String size in sizes)
            {
                if (!links.ContainsKey(size))
                {
                    links.Add(size, null);
                }
            }
 
            return links;
        }

        internal static int GetDurationFromString(String durationInStr)
        {
            var items = durationInStr.Split(':');
            int duration = 0;
            int tmp;

            if (items.Length == 2)
            {
                if (int.TryParse(items[0], out tmp))
                    duration = tmp * 60;
                else
                    IARLogStatic.Error("Bugs", $"Failed to parse duration {duration}");

                if (int.TryParse(items[1], out tmp))
                    duration += tmp;
                else
                    IARLogStatic.Error("Bugs", $"Failed to parse duration {duration}");

                return duration;
            }
            else if (items.Length == 3)
            {
                if (int.TryParse(items[0], out tmp))
                    duration = tmp * 3600;
                else
                    IARLogStatic.Error("Bugs", $"Failed to parse duration {duration}");

               if (int.TryParse(items[1], out tmp))
                    duration = tmp * 60;
                else
                    IARLogStatic.Error("Bugs", $"Failed to parse duration {duration}");

                if (int.TryParse(items[2], out tmp))
                    duration += tmp;
                else
                    IARLogStatic.Error("Bugs", $"Failed to parse duration {duration}");

                return duration;

            }
            else
            {
                IARLogStatic.Error("Bugs", $"Failed to parse duration {duration}");
                return 0;
            }

        }


        internal static StreamingObjectDescription ProcessArtistDescription(String t)
        {
            JToken sInfo = JToken.Parse(t);

            StreamingObjectDescription description = new StreamingObjectDescription();
            JToken token = sInfo["ret_msg"];

            if (token != null)
            {
                String message = token.ToString();
                if ("SUCCESS" == message)
                {
                    JToken result = sInfo["result"];
                    if (result != null)
                    {
                        String bio = result.ToObject<String>();
                        description.Add(DescriptionField.Content, bio);
                    }
                    else
                        IARLogStatic.Log("Bugs", "No description for Artist.");
                }
                else
                {
                    IARLogStatic.Log("Bugs", $"Failed to get artist description. :{t}");
                }
            }
            else
            {
                IARLogStatic.Error("Bugs", $"Failed to get Aritst BIO from : [{t}]");
            }
            return description;
        }


        internal static StreamingObjectDescription ProcessAlbumDescription(string str)
        {
            JToken sInfo = JToken.Parse(str);

            StreamingObjectDescription description = new StreamingObjectDescription();
            JToken token = sInfo["ret_msg"];

            if (token != null)
            {
                String message = token.ToString();
                if ("SUCCESS" == message)
                {
                    JToken result = sInfo["result"];
                    if (result != null)
                    {
                        String bio = result.ToObject<String>();
                        description.Add(DescriptionField.Content, bio);
                    }
                    else
                        IARLogStatic.Log("Bugs", "No description for Artist.");
                }
                else
                {
                    IARLogStatic.Log("Bugs", $"Failed to get artist description. :{str}");
                }
            }
            else
            {
                IARLogStatic.Error("Bugs", $"Failed to get Aritst BIO from : [{str}]");
            }
            return description;
        }


    }

}
