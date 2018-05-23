using System;
using System.Collections.Generic;
using System.Diagnostics;
using Aurender.Core.Contents;
using Aurender.Core.Player.mpd;

namespace Aurender.Core.Data.DB
{
    [DebuggerDisplay("Song : {dbID} {Title}")]
    public class Song : ISongFromDB, IDataWithList
    {
        /*
property(nonatomic, assign) NSInteger songID;
@property(nonatomic, assign) NSInteger artistID;

@property(nonatomic, retain)   NSString* title;
@property(nonatomic, readonly) NSString* album;
@property(nonatomic, retain)   NSString* artist;
@property(nonatomic, retain)   NSString* composer;
@property(nonatomic, retain)   NSString* conductor;

@property(nonatomic, assign) NSInteger duration;
@property(nonatomic, readonly) NSString* durationInString;

@property(nonatomic, assign) NSInteger indexOfAlbum;
@property(nonatomic, readonly) NSInteger trackCount;
@property(nonatomic, assign) NSInteger trackID;
@property(nonatomic, readonly) NSArray* tracks;

@property(nonatomic, assign) NSInteger discIndex;
@property(nonatomic, readonly) NSString* sortOrder;
@property(nonatomic, readonly) NSInteger rate;
@property NSInteger f2;

            */
        //song_id, artistsNames, album, indexOfSong, song_title, duration, totalTracks, track_id, a.songRate, a.f2 
       
        const int INDEX_SONG_ID       = 0;
        const int INDEX_ARTIST_NAMES  = 1;
        const int INDEX_ALBUMT_TITLE  = 2;
        const int INDEX_SONG_INDEX    = 3;
        const int INDEX_SONG_TITLE    = 4;
        const int INDEX_DURATION      = 5;
        const int INDEX_TOTAL_TRACKS  = 6;
        const int INDEX_TRACK_ID      = 7;
        const int INDEX_SONG_RATE     = 8;
        const int INDEX_F2            = 9;
        const int INDEX_SONG_KEY      = 10;
        const int INDEX_ALBUM_INDEX   = 11;
        const int INDEX_GENRE_INDEX   = 12;

        internal IList<Object> data;
        protected String filePath;

        public Song()
        {
            this.data = new List<object> { -1, "n/a", "n/a", -1, "n/a", -1, -1, -1, 0, 1 };
        }

        public Song(IList<Object> list)
        {
            this.data = list;
        }

        public override String ToString()
        {
            return $"Song : {dbID} {DiscIndex}{TrackIndex} {Title}";
        }

        IList<object> IDataWithList.data
        {
            get => data;
            set
            {
                this.data = value;
            }
        }

        public string ComposerName => "N/A";

        public string Conductor => "N/A";

        public int DiscIndex
        {
            get
            {
                if (this.data.Count > INDEX_ALBUM_INDEX)
                    return Convert.ToInt32(this.data[INDEX_ALBUM_INDEX]);
                return 1;
            }
        }
        public String Genre
        {
            get
            {
                if (this.data.Count > INDEX_GENRE_INDEX)
                {
                    var data = this.data[INDEX_GENRE_INDEX];
                    if (data != null)
                        return data.ToString();
                    else
                        return string.Empty;
                }
                return String.Empty;
            }
        }

        public int GetGenreID()
        {
            int genreID = AurenderBrowser.GetCurrentAurender().GenreManager.GetGenreIDBySongID(dbID);
            return genreID;
        }

        public int TrackIndex => (int)this.data[INDEX_SONG_INDEX];

        public string Title => (string)this.data[INDEX_SONG_TITLE];

        public string ArtistName => (string)this.data[INDEX_ARTIST_NAMES];

        string IPlayableItem.ItemPath
        {
            get
            {
                if (filePath == null)
                {
                    filePath = AurenderBrowser.GetCurrentAurender().SongManager.FilePathBySongID(this.dbID);
                }

                return filePath;
            }
        }

        string IPlayableItem.AlbumTitle => (string)this.data[INDEX_ALBUMT_TITLE];

        public String Key
        {
            get
            {
                var result = "";

                if (data.Count <= INDEX_SONG_KEY)
                {
                    result = "error";
                    Debug.Assert(false, "Failed to get song_key from db", $"data count : {data.Count}");
                }
                else
                    result = (string)this.data[INDEX_SONG_KEY];

                return result;
            }
        } 

        object IPlayableItem.FrontCover
        {
            get
            {
                IARLogStatic.Error($"Song", $"Doesn't support to get front cover");
                return null;
            }
        }

        object IPlayableItem.BackCover
        {
            get
            {
                IARLogStatic.Error($"Song", $"Doesn't support to get back cover");
                return null;
            }
        }


        ulong IPlayableItem.FileSize => 0;

        public int Duration => (int)this.data[INDEX_DURATION];
        int IPlayableItem.Duration => this.Duration;

        string IPlayableItem.ContainerFormat => "N/A container";

        byte IPlayableItem.BitWidth => 0;

        int IPlayableItem.SamplingRate => 0;

        int IPlayableItem.Bitrate => 0;

        public byte Rating
        {
            get => Convert.ToByte(this.data[INDEX_SONG_RATE]);
            set => this.data[INDEX_SONG_RATE] = value;
        }
         byte IRatableDBItem.Rating
        {
            get => this.Rating;
            set
            {
                this.Rating = value;
                UpdateRating(this.Rating);
            }
        }

        byte IPlayableItem.Rating
        {
            get => this.Rating;
        }

        ContentType IPlayableItem.ServiceType => ContentType.Local;

        int IDatabaseItem.dbID => this.dbID;

        public int dbID => (int)this.data[INDEX_SONG_ID];
        
        IAlbum ISong.GetAlbum()
        {
            //Aurender.Core.Player.mpd.AurenderBrowser.CurrentAurender().AlbumManager.get
            IARLogStatic.Error("Song", $"Doesn't support to get album");
            return null;
        }

        Credits IPlayableItem.GetAlbumCredits()
        {
            IARLogStatic.Info("Song", $"Doesn't support to get album credits");
            return null;
        }

        IArtist ISong.GetArtist()
        {
            return AurenderBrowser.GetCurrentAurender().ArtistManager.GetArtistBySongID(this.dbID);
        }

        IArtist ISong.GetComposer()
        {
            return AurenderBrowser.GetCurrentAurender().ComposerManager.GetComposerBySongID(this.dbID);
        }

        IArtist ISong.GetConductor()
        {
            return AurenderBrowser.GetCurrentAurender().ConductorManager.GetConductorBySongID(this.dbID);
        }

        Credits IPlayableItem.GetSongCredits()
        {
            IARLogStatic.Info("Song", $"Doesn't support to get song credits");
            return null;
        }

        public IList<String> GetLyrics()
        {
            return AurenderBrowser.GetCurrentAurender().SongManager.GetLyrics(this.dbID).Split('\n');
        }

        public IList<IPlayableItem> GetTracks(ISongManager manager)
        {
            return new List<IPlayableItem> { this };
        }

         public IInformationAvailablity GetAvailability() {

            IInformationAvailablity info = AurenderBrowser.GetCurrentAurender().SongManager.GetAvailability(this);
            return info;
        }


        public void UpdateRating(int rate)
        {
            var mgr = manager();

            if (mgr != null)
            {
                mgr.UpdateRating(this, rate);
            }
            else
            {
                IARLogStatic.Log("SongRate", "No manager to update rate");
            }
        }

        private Managers.SongManager manager()
        {
            var aurender = AurenderBrowser.GetCurrentAurender();
            if (aurender != null)
                return aurender.SongManager as Managers.SongManager;

            return null;
        }
    }
}
