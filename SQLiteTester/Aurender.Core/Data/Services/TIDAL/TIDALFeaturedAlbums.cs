using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.TIDAL
{
    static class TIDALUtility
    {
        internal static String CoverLink(JToken album, String albumID)
        {
            string position = "{0}";
            var cover = album["cover"];

            if (cover == null)
            {
                return $"http://images.osl.wimpmusic.com/im/im?w={position}&h={position}&albumid={albumID}";
            }
            else
            {
                string cLink = cover.ToObject<String>()?.Replace("-", "/");

                return cLink != null 
                    ? $"https://resources.tidal.com/images/{cLink}/{position}x{position}.jpg" : null;
            }
        }

        internal static StreamingObjectDescription ProcessDescription(Tuple<bool, string> t)
        {
            JToken sInfo = JToken.Parse(t.Item2);

            StreamingObjectDescription description = new StreamingObjectDescription();
            JToken token = sInfo["source"];

            if (token != null)
            {
                String source = token.ToObject<String>();
                description.Add(DescriptionField.Source, source);
            }
            token = sInfo["text"];

            if (token != null)
            {
                String text = token.ToObject<String>();
                description.Add(DescriptionField.Review, text);
            }

            token = sInfo["summary"];
            if (token != null)
            {
                String summary = token.ToObject<String>();
                description.Add(DescriptionField.Summary, summary);
            }

            token = sInfo["lastUpdated"];
            if (token != null)
            {
                String updated = token.ToObject<String>().Substring(0, 10);
                description.Add(DescriptionField.UpdatedDate, updated);
            }

            return description;
        }

        internal static StreamingObjectDescription ProcessCredits(Tuple<bool, string> t)
        {
            StreamingObjectDescription description = new StreamingObjectDescription();
            JArray sInfo = JArray.Parse(t.Item2);
            
            if (sInfo != null)
            {
                StringBuilder creditBuilder = new StringBuilder();
                StringBuilder contributorsBuilder = new StringBuilder();

                foreach(var credit in sInfo)
                {
                    string type = credit.Value<string>("type");
                    JArray contibutors = (JArray)credit["contributors"];

                    foreach(var contributor in contibutors)
                    {
                        var name = contributor.Value<string>("name");
                        var id = contributor.Value<int?>("id");
                        if (id == null)
                            contributorsBuilder.Append($"{name}, ");
                        else
                            contributorsBuilder.Append($"<a href='artist://{id}'>{name}</a>, ");
                    }

                    var contributorsResult = contributorsBuilder.ToString().TrimEnd(',');

                    creditBuilder.AppendLine($"{type} : {contributorsResult}");
                    creditBuilder.AppendLine("<br>");
                }
                description.Add(DescriptionField.Content, creditBuilder.ToString());
            }
           
            return description;
        }
    }
    class TIDALFeaturedAlbums : TIDALCollectionBase<IStreamingAlbum>
    {
        public TIDALFeaturedAlbums(string title, string url) : base(50, title, token => new TIDALAlbum(token))
        {
            this.urlForData = ServiceManager.Service(ContentType.TIDAL).URLFor(url);
        }
    }

    class TIDALAlbumsForArtist : TIDALCollectionBase<IStreamingAlbum> , IStreamingAlbumCollectionsForArtist<IStreamingAlbum>
    {
        public TIDALAlbumsForArtist(TIDALArtist artist) : base(200, artist.ArtistName, token => new TIDALAlbum(token))
        {
            this.urlForData = ServiceManager.Service(ContentType.TIDAL).URLFor($"artists/{artist.StreamingID}/albums?filter=ALL");
        }

        public IList<IStreamingAlbum> GetAlbumsByType(string type)
        {
            if (AlbumsByType.ContainsKey(type))
                return AlbumsByType[type];
            return new List<IStreamingAlbum>();
        }

        public IList<string> GetTypesOtherThanAlbum()
        {
            return AlbumsByType.Keys.ToArray();
        }

        protected override bool ProcessItems(Dictionary<string, object> sInfo, IList<IStreamingAlbum> newItems)
        {
            int totalCount;
            bool sucess = int.TryParse(sInfo["totalNumberOfItems"].ToString(), out totalCount);

            if (!sucess)
            {
                this.EP("TIDAL Collection", $"Failed to get totalNumberOfItems \n{sInfo}");
                this.items.Clear();
                Count = 0;
                return sucess;
            }

            if (Count == -1)
                Count = totalCount;

          //  Debug.Assert(totalCount == Count);

            var items = sInfo["items"] as JArray;
            
            var results = items.GroupBy(x => x["type"]);
            AlbumsByType = new Dictionary<string, List<IStreamingAlbum>>();
            foreach (var albumGroup in results)
            {
                List<IStreamingAlbum> groupedItems = new List<IStreamingAlbum>();
                foreach (var album in albumGroup)
                {
                    var item = constructor(album);
                    newItems.Add(item);
                    groupedItems.Add(item);
                }
                AlbumsByType.Add(albumGroup.Key.ToString(), groupedItems);
            }

            return sucess;
        }

        protected void GetAllAlbums(Dictionary<string, object> sInfo)
        {
            bool sucess = int.TryParse(sInfo["totalNumberOfItems"].ToString(), out int totalCount);

            if (!sucess)
            {
                this.EP("TIDAL Collection", $"Failed to get totalNumberOfItems \n{sInfo}");
                this.items.Clear();
                Count = 0;
            }

            if (Count == -1)
                Count = totalCount;

            //  Debug.Assert(totalCount == Count);

            var items = sInfo["items"] as JArray;

            var results = items.GroupBy(x => x["type"]).Select(x => x);
            foreach (var albumGroup in results)
            {
                var albums = new List<IStreamingAlbum>();
                foreach (var album in albumGroup)
                {
                    var item = constructor(album);
                    albums.Add(item);
                }
                AlbumsByType.Add(albumGroup.Key.ToString(), albums);
            }
        }
    }

    class TIDALAlbumsForGenre : TIDALFeaturedAlbums
    {
        public TIDALAlbumsForGenre(string path) : base("", $"genres/{path}/albums")
        {
        }
    }
    class TIDALSearchResultForAlbums : TIDALSearchCollectionBase<IStreamingAlbum>
    {
        public TIDALSearchResultForAlbums(String title) : base(50, title, "ALBUMS", token => new TIDALAlbum(token))
        {

        }
    }


}
