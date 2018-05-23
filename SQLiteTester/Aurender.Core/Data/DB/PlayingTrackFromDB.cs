using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Aurender.Core.Contents;
using Aurender.Core.Data.DB.Managers;
using Aurender.Core.Player.mpd;

namespace Aurender.Core.Data.DB
{
    public class PlayingTrackFromDB : IPlayableItem, IDatabaseItem, IPlayingTrack, IRatableDBItem
    {
        private static readonly Regex RegexForStreamingPrefix = new Regex("^([a-zA-Z]*)://");

        static internal readonly Type[] Types = new Type[]
        {
            typeof(int), //albumID
            typeof(int), //song_id
            typeof(int), //track_id
            typeof(String),//album
            typeof(byte), //mediaIndex
            typeof(Int16), //songIndex
            typeof(String), // indexOfSong
            typeof(String), //song_title
            typeof(String), // track_title
            typeof(String), // artistNames
            typeof(String), // fileName
            typeof(Int32), // duration 
            typeof(String), // albumArtist
            typeof(Int16), // discIndex
            typeof(byte), // songRate
            typeof(Int32), // f2
            typeof(Int32), // bitrate
            typeof(String), // container
            typeof(byte), // bitwidth
            typeof(Int64), // size
        };

        const int INDEX_ALBUM_ID      = 0;
        const int INDEX_SONG_ID       = 1;
        const int INDEX_TRACK_ID      = 2;
        const int INDEX_ALBUMT_TITLE  = 3;
        const int INDEX_MEDIA_INDEX   = 4;
        const int INDEX_SONG_INDEX    = 5;
        const int INDEX_INDEX_OF_SONG = 6;
        const int INDEX_SONG_TITLE    = 7;
        const int INDEX_TRACK_TITLE   = 8;
        const int INDEX_ARTIST_NAMES  = 9;
        const int INDEX_FILE_NAME     = 10;
        const int INDEX_DURATION      = 11;
        const int INDEX_ALBUM_ARTIST  = 12;
        const int INDEX_DISC_INDEX    = 13;
        const int INDEX_SONG_RATE     = 14;
        const int INDEX_F2            = 15;
        const int INDEX_BITRATE       = 16;
        const int INDEX_CONTAINER     = 17;
        const int INDEX_BITWIDTH      = 18;
        const int INDEX_SIZE          = 19;

        private IList<Object> data;

        public PlayingTrackFromDB(IList<Object> data)
        {
            this.data = data;
        }
        public string Key => " ";

        public int dbID => (Int32)this.data[INDEX_SONG_ID];

        public string Title => (String)this.data[INDEX_SONG_TITLE];

        public string ArtistName => (String)this.data[INDEX_ARTIST_NAMES];

        public string ItemPath => (String)this.data[INDEX_FILE_NAME];

        public string AlbumTitle => (String)this.data[INDEX_ALBUMT_TITLE];

        public object FrontCover => null;

        public object BackCover => null;

        public ulong FileSize
        {
            get
            {
                Int64 va = (Int64)this.data[INDEX_SIZE];
                return Convert.ToUInt64(va / 1024 / 1024);
            }
        }

        public int Duration => (int)this.data[INDEX_DURATION];

        public string ContainerFormat => ((String)this.data[INDEX_CONTAINER]).ToUpper();

        public byte BitWidth => (byte)this.data[INDEX_BITWIDTH];

        public int SamplingRate => ((int)this.data[INDEX_F2]);

        public int Bitrate => (int)this.data[INDEX_BITRATE];

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
                this.data[INDEX_SONG_RATE] = value;
                this.Rating = value;
                UpdateRating(this.Rating);
            }
        }
        public ContentType ServiceType => ContentType.Local;

        public event PropertyChangedEventHandler PropertyChanged;
        bool isPlaying;
        public bool IsPlaying
        {
            get => isPlaying; set
            {
                isPlaying = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPlaying)));
            }
        }

        public Credits GetAlbumCredits()
        {
            return null;
        }

        public Credits GetSongCredits()
        {
            return null;
        }

        public IInformationAvailablity GetAvailability() 
        {
            IInformationAvailablity info = AurenderBrowser.GetCurrentAurender().SongManager.GetAvailabilityByPath(this.ItemPath);

            return info;
        }

        public void UpdateRating(int rate)
        {
            var songManager = AurenderBrowser.GetCurrentAurender().SongManager as SongManager;

            songManager.UpdateRating(this, rate);
        }
    }
}
