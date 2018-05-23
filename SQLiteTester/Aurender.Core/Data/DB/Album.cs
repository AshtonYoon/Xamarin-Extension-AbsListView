using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Aurender.Core.Data.DB.Managers;
using Aurender.Core.Player.mpd;
using Aurender.Core.Utility;

namespace Aurender.Core.Data.DB
{
    [DebuggerDisplay("Album : {dbID} {AlbumTitle} [by {AlbumArtistName}]")]
    public class Album : IAlbumFromDB, IDataWithList
    {
        //album_id, album, artistNames, songCount, cover_m

        const int INDEX_ALBUM_ID = 0;
        const int INDEX_ALBUM_NAME = 1;
        const int INDEX_ARTIST_NAMES = 2;
        const int INDEX_SONG_COUNT = 3;
        const int INDEX_ALBUM_KEY = 4;
        const int INDEX_IMAGE = 5;


        internal IList<Object> data;


        public int dbID => (int)this.data[INDEX_ALBUM_ID];

        public string AlbumTitle  => (String)this.data[INDEX_ALBUM_NAME];

        public String Key => (String)this.data[INDEX_ALBUM_KEY];

        IList<object> IDataWithList.data { get => this.data; set => this.data = value; }


        public Album()
        {
            byte[] d = new byte[] { };

            this.data = new List<object> { -1, "n/a", "n/a", -1, d };
        }

        public Album(IList<Object> list)
        {
            this.data = list;
        }

        public override String ToString()
        {
            return $"Album : {dbID} {AlbumArtistName} - {AlbumTitle} [{ReleaseYear}]";
        }

        public object FrontCover
        {
            get
            {
                var image = this.GetImageFromIndex(INDEX_IMAGE);
                if (image == null)
                {
                    image = ImageUtility.GetDefaultAlbumCover();
                }
                return image;
            }
        }

        public object BackCover
        {
            get
            {
                var image = AurenderBrowser.GetCurrentAurender().AlbumManager.GetLargeSizeBackCoverURLByFilePath(string.Empty);
                return image;
            }
        }

        public void LoadSongs()
        {
            var songs = AlbumUtil.LoadSongsForAlbum(this.dbID);

            this.Songs = songs;
            var info = AlbumUtil.LoadAlbumYearAndPublisher(this.dbID);

            this.ReleaseYear = 0;
            this.Publisher = String.Empty;

            if (info != null)
            {
                if (info.Count > 0) {
                    if (info[0] is List<object> data)
                    {
                        if (data.Count > 0)
                            this.ReleaseYear = Convert.ToInt16((int)data[0]);
                        else
                            this.ReleaseYear = 0;

                        if (data.Count > 1)
                            this.Publisher = data[1] != null ? data[1].ToString() : string.Empty;
                        else
                            this.Publisher = string.Empty;
                    }
                }
            }
        }

        public Credits AlbumCredit()
        {
            System.Diagnostics.Debug.WriteLine("Don't support loading Album credit");
            return null;
        }

        public int SongCount { get => (int)this.data[INDEX_SONG_COUNT]; }

        public string AlbumArtistName => (String)this.data[INDEX_ARTIST_NAMES];

        public IArtist AlbumArtist
        {
            get
            {
                var artist = AurenderBrowser.GetCurrentAurender().ArtistManager.GetAlbumArtistByAlbumID(this.dbID);
                return artist;
            }
        }

        public short ReleaseYear
        {
            private set; get;
        }

        public string Publisher
        {
            private set; get;
        }

        public short NumberOfDisc
        {
            get
            {
                if (this.Songs != null)
                {
                    return this.Songs.Max(song => (short)song.DiscIndex);
                }
                return 1;
            }
        }

        public short TotalSongs => (short)(int)this.data[INDEX_SONG_COUNT];

        public IList<ISong> Songs
        {
            get;
            protected set;
        }

        public IInformationAvailablity GetAvailability() {

            IInformationAvailablity info = IInformationAvailablity.NONE;

            return info;
        }
    }
}
