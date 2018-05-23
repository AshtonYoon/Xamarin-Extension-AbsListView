using System;
using System.Collections.Generic;
using System.Diagnostics;
using Aurender.Core.Utility;

namespace Aurender.Core.Data.DB
{
    [DebuggerDisplay("Artist : {dbID} {ArtistName}")]
    public abstract class ArtistBase : IArtist, IDataWithList
    {

        /*
         *         //NSString *name      = [rs stringForColumnIndex:0];
        NSInteger aID = [rs intForColumnIndex: 1];
        NSInteger aCount = [rs intForColumnIndex: 2];
        NSInteger sCount = [rs intForColumnIndex: 3];
        NSData* imageData = [rs dataForColumnIndex: 4];
         */


        const int INDEX_ARTIST_ID = 1;
        const int INDEX_ARTIST_NAME = 0;
        const int INDEX_ALBUM_COUNT = 2;
        const int INDEX_SONG_COUNT = 3;
        const int INDEX_ARTIST_KEY = 4;
        const int INDEX_IMAGE = 5;


        internal IList<object> data;
        //internal IPlayableItem trackInfo;

        public override string ToString()
        {
            return $"Artist : {dbID} - {ArtistName} [{CountOfAlbums}]";
        }
        
        public string ArtistName => (string) this.data[INDEX_ARTIST_NAME];

        public int dbID => (int) this.data[INDEX_ARTIST_ID];

        public String Key => (string)this.data[INDEX_ARTIST_KEY];

        IList<object> IDataWithList.data { get => this.data; set => this.data = value; }

        internal protected ArtistBase()
        {
            byte[] d = new byte[] { };

            this.data = new List<object> { -1, "n/a", -1, -1, d };
        }

        protected ArtistBase(IList<object> list)
        {
            this.data = list;
        }

        public object ArtistImage => this.GetImageFromIndex(INDEX_IMAGE) ?? ImageUtility.GetImageSourceFromFile($"album_default_dark.png"); 

        public int CountOfAlbums => (int) this.data[INDEX_ALBUM_COUNT];
        
        public int CountOfSongs => (int) this.data[INDEX_SONG_COUNT];

         public virtual IInformationAvailablity GetAvailability() {

            IInformationAvailablity info = IInformationAvailablity.NONE;

            return info;
        }
    }

    public class Artist : ArtistBase, IArtistFromDB
    {
        public Artist() : base()
        {

        }

        public Artist(IList<object> list) : base(list)
        {

        }

         public override IInformationAvailablity GetAvailability() {

            IInformationAvailablity info = IInformationAvailablity.NONE;

            return info;
        }
    }
}
