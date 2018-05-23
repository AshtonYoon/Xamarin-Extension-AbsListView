using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.Melon
{



    class MelonTrack : StreamingTrack
    {
        int feature;
        private MelonTrack() : base(ContentType.Melon) { }
        string smallImageUrl;
        String imgUrl;
        String imgUrlLarge;

        public MelonTrack(JToken token) : this()
        {
            if (token["StreamingID"] != null)
            {
                this.StreamingID = token["StreamingID"].ToString();
                this.Genre      = token["Genre"].ToString();
                this.StreamingArtistID = token["StreamingArtistID"].ToString();
                this.StreamingAlbumID  = token["StreamingAlbumID"].ToString();
                this.Title             = token["Title"].ToString();
                this.ArtistName        = token["AritstName"].ToString();
                this.AlbumTitle       = token["AlbumTitle"].ToString();
                this.AllowStreaming = token["AllowStreaming"].ToObject<bool>();
                //this.IsPremiumOnly = token["IsPreminumOnly"].ToObject<bool>();

            this.Duration = token["Duration"].ToObject<short>();
            this.DiscIndex = token["DiscIndex"].ToObject<short>();
            this.TrackIndex = token["TrackIndex"].ToObject<short>();

            }
            else
            {
                if (token["SONGID"] != null)
                    this.StreamingID = token["SONGID"].ToObject<String>();
                else
                    this.StreamingID = token["CID"].ToObject<String>();

                this.StreamingAlbumID = token["ALBUMID"].ToObject<String>();

                Object albumName = token["ALBUMNAME"];
                if (albumName != null)
                {
                    this.AlbumTitle = token["ALBUMNAME"].ToObject<String>();
                }
                else
                    this.AlbumTitle = String.Empty;

                if (token["ALBUMIMG"] != null)
                {
                    smallImageUrl = imgUrl = token["ALBUMIMG"].ToObject<String>();
                    if (token["ALBUMIMGLARGE"] != null)
                        this.imgUrlLarge = token["ALBUMIMGLARGE"].ToObject<String>();
                    else
                        imgUrlLarge = imgUrl;
                }
                else if (token["CONTENTIMGPATH"] != null)
                {
                    imgUrlLarge = imgUrl = token["CONTENTIMGPATH"].ToObject<String>();
                    if (token["CONTENTTHUMBIMGPATH"] != null)
                        smallImageUrl = token["CONTENTTHUMBIMGPATH"].ToObject<String>();
                    else
                        smallImageUrl = imgUrl;
                }
                else
                    smallImageUrl = imgUrlLarge = imgUrl = String.Empty;


                var artistID = token["ARTISTID"];
                if (artistID != null && artistID.Type != JTokenType.Null)
                {
                    this.StreamingArtistID = artistID.ToString();
                }
                else
                {
                    var artists = token["ARTISTS"];

                    if (artists == null)
                        artists = token["ARTISTLIST"];
                    if (artists.Type == JTokenType.Array)
                    {
                        JArray array = artists as JArray;
                        if (array.Count > 0)
                        {
                            JToken artistInfo = array[0] as JToken;
                            this.StreamingArtistID = artistInfo["ARTISTID"].ToString();
                            this.ArtistName = artistInfo["ARTISTNAME"].ToString();
                        }
                        else
                            this.StreamingID = "N/A";
                    }
                    else
                    {
                        Debug.Assert(false, "Melon artists information is different from expected");
                    }
                }

                this.AllowStreaming = MelonUtility.IsStreamingReady(token);

                if (token["SONGNAME"] != null)
                    this.Title = token["SONGNAME"].ToObject<String>();
                else if (token["SONGTITLE"] != null)
                    this.Title = token["SONGTITLE"].ToObject<String>();
                else if (token["CNAME"] != null)
                    this.Title = token["CNAME"].ToObject<String>();
                else
                    this.Title = "N/A";

                if (token["TRACKNO"] != null)
                    this.TrackIndex = token["TRACKNO"].ToObject<int>();
                if (token["CDNO"] != null)
                    this.DiscIndex = token["CDNO"].ToObject<int>();
                if (token["PLAYTIME"] != null)
                    this.Duration = token["PLAYTIME"].ToObject<int>();


                feature = 0;

                if (token["ISADULT"].ToString() == "Y")
                    feature = (feature | (int)StreamingTrackFeatures.Adult);
                if (IsPremiumOnly)
                    feature = (feature | (int)StreamingTrackFeatures.Prime);
            }
        }


        public override string ImageUrl => smallImageUrl;

        public override string ImageUrlForMediumSize => imgUrl;

        public override string ImageUrlForLargeSize => imgUrlLarge;



        internal void SetDisallowStreaming() => this.AllowStreaming = false;


        public override object GetExtraImage()
        {
            StreamingTrackFeatures ft = (StreamingTrackFeatures)feature;
            if (ft.IsForAdult() && ft.IsPrime())
            {

            }
            else if (ft.IsForAdult())
            {

            }
            else if (ft.IsPrime())
            {

            }

            return null;
        }
    }


    static class MelonUtility {

        internal static string ToDurationInString(int duration)
        {

            return "";
        }

        internal static bool IsStreamingReady(JToken token)
        {
            bool isReady = false;

            if (token["ISSERVICE"] == null || token["ISSERVICE"].Type == JTokenType.Null)
            {
                isReady = false;
            }
            else
            {
                bool isService = token["ISSERVICE"].ToObject<bool>();

                bool isHoldback = false;
                
                if (token["ISHOLDBACK"] != null)
                    isHoldback = token["ISHOLDBACK"].ToObject<bool>();

                isReady = isService && !isHoldback;
            }

            return isReady;
        }

        internal static StreamingObjectDescription ProcessArtistDescription(JToken token)
        {
            StreamingObjectDescription desc = new StreamingObjectDescription();

            StringBuilder sb = new StringBuilder();

            List<(String fileld, String name)> lsts = new List<(string fileld, string name)>()
            {
                ("NATIONALITY", "국적"),
                ("DEBUTDATE", "데뷔"),
                ("COMPNAME", "소속사"),
                ("DEBUTSONGNAME", "데뷔곡"),
                ("GENDER", "성별"),
                ("ACTTYPE", "활동방식"),
                ("ACTGENRE", "장르")
            };

            JToken t = JToken.Parse(token.ToString());

            foreach (var item in lsts)
            {
                var obj = t[item.fileld];
                if (obj != null)
                {
                    sb.AppendLine($"{item.name} : {obj.ToString()}");
                }
            }

            desc.Add(DescriptionField.Summary, sb.ToString());

            return desc;
        }

        internal static StreamingObjectDescription ProcessAlbumDescription(String textToParse)
        {
            return null;
        }

        internal static bool IsRequestSucess(JToken jtoken)
        {
            var result = jtoken["RESULT"];

            if (result != null)
            {
                if (result.ToObject<String>() == "0")
                    return true;
            }

            return false; 
        }
    }
}
