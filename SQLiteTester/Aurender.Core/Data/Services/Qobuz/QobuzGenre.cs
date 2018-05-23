using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aurender.Core.Contents.Streaming;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Data.Services.Qobuz
{

    class QobuzGenre : StreamingGenre 
    {
        internal readonly String genreType;

        public QobuzGenre(JToken token): base(ContentType.Qobuz)
        {
            this.Name = token["name"].ToObject<String>();
            this.StreamingID = token["id"].ToObject<String>();
            this.genreType = "";
            this.supportsAlbums = true;
        }

        internal QobuzGenre(String name, string streamingID, String type): base(ContentType.Qobuz)
        {
            this.Name = name;
            this.StreamingID = streamingID;
            this.genreType = type;
            this.supportsAlbums = true;
        }


        public override string ImageUrl => String.Empty;

        public override string ImageUrlForMediumSize => String.Empty;

        public override string ImageUrlForLargeSize => String.Empty;

        public override int CountOfAlbums => this.Albums.Count;

        public override async Task<IStreamingObjectCollection<IStreamingAlbum>> LoadAlbumsAsync()
        {
                if (this.Albums == null)
                {
                    this.Albums = new QobuzAlbumsForGenres(this);
                }
            await this.Albums.LoadNextAsync().ConfigureAwait(false);


                return this.Albums;
        }

        public override Task<IStreamingObjectCollection<IStreamingPlaylist>> LoadPlaylistsAsync()
        {
            throw new NotImplementedException();
        }
    }

    class QobuzGenreCollection : QobuzCollectionBase<IStreamingGenre>
    {
        public QobuzGenreCollection() : base(100, typeof(QobuzGenre), "Genres")
        {
            this.urlForData = "genre/list";
        }

        protected override bool ProcessItems(Dictionary<string, object> info, IList<IStreamingGenre> newItems)
        {
            if (info.ContainsKey("genres"))
            {
                JToken sInfo = info["genres"] as JToken;

                return this.ProcessItems(sInfo, newItems);
            }

            this.EP("Qobuze", "Failed to parse pucrchased albums");
            return false;
        }
    }

    class QobuzGenreCollectionForSection : List<IStreamingGenre>, IStreamingObjectCollection<IStreamingGenre>
    {
        
        internal QobuzGenreCollectionForSection(QobuzGenreCollection genres, String title, String type) : base (genres.Count)
        {
            this.Title = title;
            var enr = from genre in genres select new QobuzGenre(genre.Name, genre.StreamingID, type);
            var sortedEnr = enr.OrderBy(genre => genre.Name);

            foreach (var g in sortedEnr) {
                this.Add(g);
            }
        }

        public string Title { protected set; get; }

        public int CountForLoadedItems => this.Count;

        public Dictionary<string, List<IStreamingAlbum>> AlbumsByType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Task LoadNextAsync()
        {
            return Task.Delay(5);
        }

        public void Reset()
        {
            
        }

        IEnumerable<IStreamingGenre> IStreamingObjectCollection<IStreamingGenre>.GetRange(int index, int count)
        {
            return this.GetRange(index, count); 
        }
    }
}