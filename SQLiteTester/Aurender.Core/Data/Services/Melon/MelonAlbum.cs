using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Player;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.Melon
{

    class MelonAlbum : StreamingAlbum
    {
        protected String imageURL;
        protected String imageLarge;
        StreamingObjectDescription description;

        public MelonAlbum(JToken token) : base(ContentType.Melon)
        {
            var songList = extractSongs(token);
            if (token["ALBUMID"] == null)
            {
                this.AlbumType = String.Empty;

                if (songList.Count > 0)
                {
                    JToken song = songList[0];
                    this.StreamingID = song["ALBUMID"].ToObject<String>();

                    this.StreamingID = song["ALBUMID"].ToString();
                    this.imageURL = song["ALBUMIMG"].ToString();
                    this.imageLarge = song["ALBUMIMGLARGE"].ToString();

                    this.AlbumTitle = song["ALBUMNAME"].ToString();
                    this.ReleaseYear = releaseYear(song);
                    this.AllowStreaming = MelonUtility.IsStreamingReady(song);

                    string artistID = "N/A";
                    string artistName = "N/A";
                    var artists = song["ARTISTS"] as JArray;
                    var artist = artists[0];

                    artistID = artist["ARTISTID"].ToString();
                    artistName = artist["ARTISTNAME"].ToString();


                    foreach(var s in songList)
                    {
                        artists = song["ARTISTS"] as JArray;
                        bool found = false;

                        foreach(var a in artists)
                        {
                            if (a["ARTISTID"].ToString().Equals(artistID))
                            {
                                break;
                            }
                        }
                        if (!found)
                        {
                            Debug.Assert(false, $"Please tell Eric for this with AlbumID{this.StreamingID}");
                        }
                    }
                    this.AlbumArtistName = artistName;
                    this.StreamingArtistID = artistID;
                }
                else
                {
                    this.StreamingID = "N/A";
                    this.imageLarge = String.Empty;
                    this.imageURL = String.Empty;
                    this.StreamingArtistID = "N/A";
                    this.AlbumArtistName = "N/A";
                }
            }
            else
            {
                this.StreamingID = token["ALBUMID"].ToObject<String>();
                // String stringImageURL = String.Empty;
                this.imageURL = token["ALBUMIMG"].ToString();
                this.imageLarge = token["ALBUMIMGLARGE"].ToString();

                if (token["ALBUMNAME"] != null)
                    this.AlbumTitle = token["ALBUMNAME"].ToObject<String>();
                else
                {
                    if (songList.Count > 0)
                    {
                        foreach (var s in songList)
                        {
                            var at = s["ALBUMNAME"];

                            if (at != null)
                            {
                                this.AlbumTitle = at.ToObject<String>();
                                break;
                            }

                        }
                    }
                    else
                    {
                        this.AlbumTitle = "N/A";
                    }
                }

                var artistToken = token["ARTISTID"];

                if (artistToken != null && artistToken.Type != JTokenType.Null)
                {
                    this.StreamingArtistID = artistToken.ToObject<String>();
                    this.AlbumArtistName = token["ARTISTNAME"].ToObject<String>();
                }
                else
                {
                    var artists = token["ARTISTS"];
                    if (artists == null)
                        artists = token["ARTISTLIST"];

                    if (artists != null && artists.Type == JTokenType.Array)
                    {
                        var artistArray = artists as JArray;
                        var firstArtist = artistArray[0];

                        this.StreamingArtistID = firstArtist["ARTISTID"].ToObject<String>();
                        this.AlbumArtistName = firstArtist["ARTISTNAME"].ToObject<String>();
                    }
                    else
                    {
                        this.AlbumArtistName = String.Empty;
                    }

                }
                this.AllowStreaming = MelonUtility.IsStreamingReady(token);

                this.ReleaseYear = releaseYear(token);

                var genreToken = token["GENRENAME"];
                if (genreToken != null && genreToken.Type != JTokenType.Null)
                    this.Genre = genreToken.ToString();
                else if (token["GENRELIST"] != null)
                {
                    JArray genres = (JArray) token["GENRELIST"];

                    StringBuilder gsb = new StringBuilder();

                    foreach (var g in genres)
                    {
                        gsb.AppendFormat("{0},", g["GENRENAME"]);
                    }
                    if (genres.Count > 0)
                        gsb.Remove(gsb.Length - 1, 1);
                    this.Genre = gsb.ToString();
                }
                else
                    this.Genre = String.Empty;
            if (token["ALBUMTYPE"] != null)
                this.AlbumType = token["ALBUMTYPE"].ToObject<String>();
            else
                this.AlbumType = String.Empty;
            }





            int drt = 0;
            if (songList.Count > 0)
            {
                StringBuilder sb = new StringBuilder();

                foreach (var songToken in songList)
                {
                    drt += songToken["PLAYTIME"].ToObject<int>();

                    var genre = songToken["GENRE"];
                    if (genre != null)
                    {
                        sb.AppendFormat("{0},", genre.ToString());
                    }
                    else if (songToken["GENRES"] != null)
                    {
                        var genres = songToken["GENRES"] as JArray;
                        if (genres.Count > 1)
                        {
                            sb.Clear();
                            sb.Append(genres[0]["GENRENAME"].ToObject<String>());
                            break;
                        }
                        else
                        {
                            var g = genres[0]["GENRENAME"].ToString();
                            if (!sb.ToString().Contains(g))
                            {
                                sb.AppendFormat("{0},", g);
                            }
                        }
                    }
                }
                sb.Remove(sb.Length - 2, 1);
                if (this.Genre == null || this.Genre.Length == 0)
                    this.Genre = sb.ToString();
            }

            this.duration = drt;
            this.Copyright = String.Empty;

        }

        internal MelonAlbum(String albumID, JToken token) : base(ContentType.Melon)
        {

        }

        private static short releaseYear(JToken token)
        {
            var year = token["ISSUEDATE"];

            if (year != null && year.ToString().Length > 3 && year.Type != JTokenType.Null)
            {
                var partial = year.ToString().Substring(0, 4);
                bool sucess = short.TryParse(partial, out short shortYear);
                return sucess ? shortYear : (short)0;
            }
            else
                return 0;
        }

        private static List<JToken> extractSongs(JToken token)
        {
            List<JToken> songsInCD = new List<JToken>();

            var cdList = token["CDLIST"];
            if (cdList != null && cdList.Type == JTokenType.Array)
            {
                JArray array = cdList as JArray;

                foreach( var cd in cdList)
                {
                    var songList = cd["SONGLIST"];
                    if (songList != null && songList.Type == JTokenType.Array)
                    {
                        songsInCD.AddRange(songList as JArray);
                    }
                }
            }

            return songsInCD;
        }

        public override string ImageUrl => imageURL;

        public override string ImageUrlForMediumSize => imageURL;

        public override string ImageUrlForLargeSize => imageLarge;



        public override Credits AlbumCredit()
        {
            IARLogStatic.Error("BugsAlbum", "Doesn't support icon yet");
            return null;
        }

        public override object IconForAdditionalInfo()
        {
            IARLogStatic.Error("BugsAlbum", "Doesn't support icon yet");
            return null;
        }

        public override async Task<StreamingObjectDescription> LoadDescriptionAsync()
        {
            var tracks = await LoadSongsAsync();

            if (this.description == null)
            {
                lock(this)
                {
                    this.description = new StreamingObjectDescription();
                    StringBuilder sb = new StringBuilder();

                    if (this.AlbumType.Length > 0)
                        sb.AppendLine($"유형   :  {this.AlbumType}");
                    sb.AppendLine($"장르    :  {this.Genre}");
                    sb.AppendLine($"발매일  :  {this.ReleaseYear}");
                    sb.AppendLine($"곡수    :  {this.Songs.Count} {PlayerHelper.ToTimeFormatString(this.duration)}");

                    this.description.Add(DescriptionField.Summary, sb.ToString());
                }
            }
            return this.description;
        }

        public override async Task<IStreamingObjectCollection<IStreamingTrack>> LoadSongsAsync()
        {
            if (this.Tracks == null)
            {
                this.Tracks = new MelonTracksForAlbum(this);
            }
            if (this.Tracks.CountForLoadedItems != this.Tracks.Count)
                await this.Tracks.LoadNextAsync().ConfigureAwait(false);

            return this.Tracks;

        }

        public override object GetExtraImage()
        {
            return null; 
        }
    }

}