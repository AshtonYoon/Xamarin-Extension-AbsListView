using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Aurender.Core.Data.DB
{
    [DebuggerDisplay("Artist : {dbID} {Name}")]
    public class Genre : IGenreFromDB, IDataWithList
    {

        /*
         select genre_id, genre, albumCount, songCount, cover_m 
                from audio_genres
         */

        const int INDEX_GENRE_ID = 0;
        const int INDEX_GENRE_NAME = 1;
        const int INDEX_ALBUM_COUNT = 2;
        const int INDEX_SONG_COUNT = 3;
        const int INDEX_IMAGE = 4;


        internal IList<object> data;


        public int dbID => (int)this.data[INDEX_GENRE_ID];

        public string Name => (string)this.data[INDEX_GENRE_NAME];

        IList<object> IDataWithList.data { get => this.data; set => this.data = value; }

        public Genre(IList<object> list)
        {
            this.data = list;

        }
        public String Key => "#";

        public Genre()
        {
            byte[] d = new byte[] { };

            this.data = new List<object> { -1, "n/a", -1, -1, d };
        }

        public override string ToString()
        {
            return $"Genre : {dbID} {Name} {CountOfAlbums} {CountOfSongs}";
        }

        public object GenreImage => this.GetImageFromIndex(INDEX_IMAGE);

        public int CountOfAlbums => (int)this.data[INDEX_ALBUM_COUNT];
        public int CountOfSongs => (int)this.data[INDEX_SONG_COUNT];


        public IInformationAvailablity GetAvailability() {

            IInformationAvailablity info = IInformationAvailablity.NONE;

            return info;
        }
    }
}
